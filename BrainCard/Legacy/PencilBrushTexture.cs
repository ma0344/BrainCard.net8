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

    // Mosaic-like grain: block size in source noise pixels.
    private const int MosaicBlockPx = 6;

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

        // Mosaic/block noise: each block shares a constant alpha value.
        // This matches the legacy "mosaic-like" grain better than smooth Perlin/value noise.
        var block = Math.Clamp(MosaicBlockPx, 1, sizePx);

        for (var by = 0; by < sizePx; by += block)
        {
            for (var bx = 0; bx < sizePx; bx += block)
            {
                // Pick one alpha per block.
                // Bias toward low alpha, but keep discrete steps.
                var r = rnd.NextDouble();
                var v = Math.Pow(r, 1.2); // slightly low-biased

                // Quantize to a few levels to emphasize mosaic steps.
                const int levels = 8;
                var q = Math.Round(v * (levels - 1)) / (levels - 1);
                var a = (byte)Math.Clamp((int)Math.Round(q * 255.0), 0, 255);

                var yMax = Math.Min(sizePx, by + block);
                var xMax = Math.Min(sizePx, bx + block);

                for (var y = by; y < yMax; y++)
                {
                    for (var x = bx; x < xMax; x++)
                    {
                        bmp.SetPixel(x, y, new SKColor(0, 0, 0, a));
                    }
                }
            }
        }

        return bmp;
    }
}
