using SkiaSharp;
using System;
using System.Diagnostics;
using System.IO;

namespace BrainCard.Legacy;

internal static class SkiaDebugDump
{
    private const string DumpDirEnv = "BRAIN_CARD_SKIA_DUMP_DIR";

    public static string? TryGetDumpDir()
    {
        try
        {
            var dir = Environment.GetEnvironmentVariable(DumpDirEnv);
            if (string.IsNullOrWhiteSpace(dir)) return null;
            Directory.CreateDirectory(dir);
            return dir;
        }
        catch
        {
            return null;
        }
    }

    public static void TryDumpBitmapPng(SKBitmap bmp, string fileName)
    {
        try
        {
            var dir = TryGetDumpDir();
            if (dir == null) return;

            if (string.IsNullOrWhiteSpace(fileName)) return;
            fileName = SanitizeFileName(fileName);

            var path = Path.Combine(dir, fileName);
            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            if (data == null) return;
            File.WriteAllBytes(path, data.ToArray());
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[SkiaDump] TryDumpBitmapPng failed: {ex}");
        }
    }

    public static SKBitmap UpscaleAlpha8ToBgra(SKBitmap alpha8, int scale)
    {
        if (scale < 1) scale = 1;

        var w = Math.Max(1, alpha8.Width);
        var h = Math.Max(1, alpha8.Height);

        var dst = new SKBitmap(new SKImageInfo(w * scale, h * scale, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(dst);
        canvas.Clear(SKColors.Transparent);

        using var paint = new SKPaint
        {
            IsAntialias = false,
            FilterQuality = SKFilterQuality.None
        };

        using var img = SKImage.FromBitmap(alpha8);
        canvas.DrawImage(img, new SKRect(0, 0, dst.Width, dst.Height), paint);

        return dst;
    }

    public static SKBitmap CompositeOnSolidBackground(SKBitmap src, SKColor background)
    {
        var w = Math.Max(1, src.Width);
        var h = Math.Max(1, src.Height);

        var dst = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));
        using var canvas = new SKCanvas(dst);
        canvas.Clear(background);

        using var paint = new SKPaint
        {
            IsAntialias = false,
            FilterQuality = SKFilterQuality.None,
            BlendMode = SKBlendMode.SrcOver
        };

        using var img = SKImage.FromBitmap(src);
        canvas.DrawImage(img, 0, 0, paint);

        return dst;
    }

    public static SKBitmap BoostAlpha(SKBitmap src, float multiplier)
    {
        if (multiplier <= 0) multiplier = 1;

        var w = Math.Max(1, src.Width);
        var h = Math.Max(1, src.Height);

        var dst = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul));

        // Work on pixels for determinism and to avoid shader/colorfilter version differences.
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                var c = src.GetPixel(x, y);
                // Convert premul -> straight RGB for scaling alpha.
                var a = c.Alpha;
                if (a == 0)
                {
                    dst.SetPixel(x, y, SKColors.Transparent);
                    continue;
                }

                var r = (byte)Math.Clamp((int)Math.Round(c.Red * 255.0 / a), 0, 255);
                var g = (byte)Math.Clamp((int)Math.Round(c.Green * 255.0 / a), 0, 255);
                var b = (byte)Math.Clamp((int)Math.Round(c.Blue * 255.0 / a), 0, 255);

                var a2 = (byte)Math.Clamp((int)Math.Round(a * multiplier), 0, 255);

                // Premultiply back.
                var r2 = (byte)((r * a2 + 127) / 255);
                var g2 = (byte)((g * a2 + 127) / 255);
                var b2 = (byte)((b * a2 + 127) / 255);

                dst.SetPixel(x, y, new SKColor(r2, g2, b2, a2));
            }
        }

        return dst;
    }

    public readonly record struct AlphaStats(int NonZeroCount, SKRectI Bounds);

    public static AlphaStats ComputeAlphaStats(SKBitmap bmp)
    {
        var w = Math.Max(1, bmp.Width);
        var h = Math.Max(1, bmp.Height);

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        var count = 0;

        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                if (bmp.GetPixel(x, y).Alpha == 0) continue;

                count++;
                if (x < minX) minX = x;
                if (y < minY) minY = y;
                if (x > maxX) maxX = x;
                if (y > maxY) maxY = y;
            }
        }

        if (count == 0)
        {
            return new AlphaStats(0, SKRectI.Empty);
        }

        return new AlphaStats(count, new SKRectI(minX, minY, maxX + 1, maxY + 1));
    }

    private static string SanitizeFileName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
