using BrainCard.Models.FileFormatV2;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BrainCard.Legacy;

public static class LegacyPngRenderer
{
    public static byte[] RenderPng(IReadOnlyList<Bcf2Stroke> strokes, int width, int height, SKColor? background = null)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        strokes ??= Array.Empty<Bcf2Stroke>();

        var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var surface = SKSurface.Create(info);
        var canvas = surface.Canvas;

        canvas.Clear(background ?? SKColors.Transparent);

        foreach (var s in strokes)
        {
            if (s?.Points == null || s.Points.Count == 0) continue;

            var baseColor = ParseHexArgbOrDefault(s.Color, SKColors.Black);

            // Base stroke width. Pressure will modulate this per segment.
            var baseSize = Math.Max(0.5, s.Size);

            if (string.Equals(s.Tool, "highlighter", StringComparison.OrdinalIgnoreCase))
            {
                DrawHighlighterStroke(canvas, s.Points, baseSize, baseColor, s.Opacity);
                continue;
            }

            DrawStrokeVariableWidth(canvas, s.Points, baseSize, baseColor, s.Opacity);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? Array.Empty<byte>();
    }

    private static void DrawHighlighterStroke(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, SKColor baseColor, double? opacity)
    {
        // UWPのDrawAsHighlighterは白背景でのレンダリング＆白抜き後処理に依存していましたが、
        // ここでは背景色を固定せず、透過PNGのまま合成（BlendMode）で近似します。

        var a = Math.Min((int)baseColor.Alpha, 160);
        var color = baseColor.WithAlpha((byte)a);

        using var layerPaint = new SKPaint
        {
            BlendMode = SKBlendMode.Multiply
        };

        var saveCount = canvas.SaveLayer(layerPaint);
        try
        {
            DrawStrokeVariableWidth(canvas, points, baseSize, color, opacity);
        }
        finally
        {
            canvas.RestoreToCount(saveCount);
        }
    }

    private static void DrawStrokeVariableWidth(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, SKColor color, double? opacity)
    {
        if (points == null || points.Count == 0) return;

        var effectiveColor = ApplyOpacity(color, opacity);

        // Single point: draw a dot using pressure.
        if (points.Count == 1)
        {
            var p = points[0];
            var w = PressureToStrokeWidth(p?.Pressure ?? 1.0, baseSize);
            using var paint = CreateStrokePaint(effectiveColor, (float)w);
            canvas.DrawPoint((float)p.X, (float)p.Y, paint);
            return;
        }

        // Segment-based rendering: draw each segment with width derived from adjacent pressures.
        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;

            // Average pressure for the segment.
            var pr = 0.5 * ((p0.Pressure) + (p1.Pressure));
            var w = PressureToStrokeWidth(pr, baseSize);

            using var paint = CreateStrokePaint(effectiveColor, (float)w);
            canvas.DrawLine((float)p0.X, (float)p0.Y, (float)p1.X, (float)p1.Y, paint);
        }
    }

    private static SKPaint CreateStrokePaint(SKColor color, float strokeWidth)
        => new()
        {
            IsAntialias = true,
            Color = color,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = strokeWidth,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

    private static SKColor ApplyOpacity(SKColor baseColor, double? opacity)
    {
        if (opacity is null) return baseColor;

        var o = opacity.Value;
        if (o <= 0) return baseColor.WithAlpha(0);
        if (o >= 1) return baseColor;

        var a = (byte)Math.Clamp((int)Math.Round(baseColor.Alpha * o), 0, 255);
        return baseColor.WithAlpha(a);
    }

    private static double Clamp01(double v)
        => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static double PressureToStrokeWidth(double pressure, double baseSize)
    {
        // 0..1 pressure -> width multiplier.
        // Keep a visible minimum because some sources produce very low pressure.
        var pr = Clamp01(pressure);

        // gamma curve to emphasize differences around mid pressure.
        var gamma = 1.7;
        var scaled = Math.Pow(pr, gamma);

        // min 30% of baseSize, max 140% of baseSize
        var min = baseSize * 0.30;
        var max = baseSize * 1.40;
        return min + (max - min) * scaled;
    }

    private static SKColor ParseHexArgbOrDefault(string hex, SKColor fallback)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(hex)) return fallback;
            hex = hex.Trim();
            if (hex.Length != 9 || hex[0] != '#') return fallback;

            var a = byte.Parse(hex.Substring(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var r = byte.Parse(hex.Substring(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var g = byte.Parse(hex.Substring(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var b = byte.Parse(hex.Substring(7, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            return new SKColor(r, g, b, a);
        }
        catch
        {
            return fallback;
        }
    }
}
