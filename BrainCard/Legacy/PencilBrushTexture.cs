using SkiaSharp;
using System;

namespace BrainCard.Legacy;

internal static class PencilBrushTexture
{
    private static readonly object Gate = new();

    // Cache one base texture. It will be scaled at draw time.
    private static SKBitmap? _alphaNoise;

    // Use a fixed seed for deterministic results.
    private const int Seed = 1337;

    private static int _dumpedSize = -1;

    public static void ClearCache()
    {
        lock (Gate)
        {
            _alphaNoise?.Dispose();
            _alphaNoise = null;
            _dumpedSize = -1;
        }
    }

    public static SKBitmap GetAlphaNoise(int sizePx)
    {
        if (sizePx < 8) sizePx = 8;
        if (sizePx > 256) sizePx = 256;

        lock (Gate)
        {
            if (_alphaNoise != null && _alphaNoise.Width == sizePx && _alphaNoise.Height == sizePx)
            {
                TryDumpIfEnabled(_alphaNoise, sizePx);
                return _alphaNoise;
            }

            _alphaNoise?.Dispose();
            _alphaNoise = CreateAlphaNoise(sizePx, Seed);
            TryDumpIfEnabled(_alphaNoise, sizePx);
            return _alphaNoise;
        }
    }

    private static void TryDumpIfEnabled(SKBitmap alpha8, int sizePx)
    {
        // Dump once per size to avoid spamming.
        if (_dumpedSize == sizePx) return;

        var dir = SkiaDebugDump.TryGetDumpDir();
        if (dir == null) return;

        _dumpedSize = sizePx;

        // Alpha8 is hard to see. Upscale to BGRA for inspection.
        using var bgra = SkiaDebugDump.UpscaleAlpha8ToBgra(alpha8, scale: 8);
        SkiaDebugDump.TryDumpBitmapPng(bgra, $"pencil-noise-alpha8-{sizePx}px-up8.png");
    }

    private static SKBitmap CreateAlphaNoise(int sizePx, int seed)
    {
        // Alpha8 noise; later combined with a radial mask.
        var bmp = new SKBitmap(new SKImageInfo(sizePx, sizePx, SKColorType.Alpha8, SKAlphaType.Premul));

        var rnd = new Random(seed);

        // Simple value-noise with a light blur via neighborhood averaging.
        // This avoids harsh salt-and-pepper.
        Span<byte> raw = sizePx * sizePx <= 0 ? Span<byte>.Empty : new byte[sizePx * sizePx];

        for (var y = 0; y < sizePx; y++)
        {
            for (var x = 0; x < sizePx; x++)
            {
                // Bias toward low alpha; pencil should be mostly transparent spec.
                // Use squared random to increase probability of small values.
                var r = rnd.NextDouble();
                var v = r * r;
                var a = (byte)Math.Clamp((int)Math.Round(v * 255.0), 0, 255);
                raw[y * sizePx + x] = a;
            }
        }

        // 1-pass box blur (3x3) into bitmap.
        for (var y = 0; y < sizePx; y++)
        {
            for (var x = 0; x < sizePx; x++)
            {
                var sum = 0;
                var count = 0;

                for (var oy = -1; oy <= 1; oy++)
                {
                    var yy = y + oy;
                    if ((uint)yy >= (uint)sizePx) continue;

                    for (var ox = -1; ox <= 1; ox++)
                    {
                        var xx = x + ox;
                        if ((uint)xx >= (uint)sizePx) continue;

                        sum += raw[yy * sizePx + xx];
                        count++;
                    }
                }

                var a = (byte)(count == 0 ? 0 : (sum / count));
                bmp.SetPixel(x, y, new SKColor(0, 0, 0, a));
            }
        }

        return bmp;
    }
}
