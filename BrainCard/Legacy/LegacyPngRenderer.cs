using BrainCard.Models.FileFormatV2;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace BrainCard.Legacy;

public static class LegacyPngRenderer
{
    private const int TaperSegments = 3;
    private const float TaperMinScale = 0.35f;

    private const float HighlighterTipAspect = 3.0f;

    // Treat very short movements as stationary and stamp per point (prevents missing dots).
    private const float HighlighterStationaryEpsilonPx = 0.25f;

    // Minimum visible stamp dimensions in output pixels (prevents near-zero pressure dots from vanishing).
    private const float HighlighterMinShortSidePx = 0.8f;
    private const float HighlighterMinLongSidePx = 2.0f;

    // Denser stamping reduces periodic banding. Previously 0.25 produced visible striping.
    private const float HighlighterStampStepScale = 0.12f;

    // Ensure a minimum density even for small tips.
    private const float HighlighterMinStepPx = 0.35f;

    private const int HighlighterMaxStepsPerSegment = 512;

    // Observed: mouse-click strokes tend to report pressure?0.5 while the visible thickness matches the selected size.
    // Normalize so that pressure=0.5 maps to scale=1.0.
    private const double HighlighterPressureBaseline = 0.5;
    private const double HighlighterMinScale = 0.20;
    private const double HighlighterMaxScale = 1.60;

    // Legacy observation (UWP): highlighter pressure→visible line height(px)
    // SizeHeight=24: 0.9:38,0.8:34,0.7:30,0.6:26,0.5:24,0.4:20,0.3:16,0.2:12,0.1:8,0.05:6,0.01:5
    // (Reported: SizeWidth is fixed at 8, and tip rectangle is Height x (Height/3).)
    // We normalize to a height multiplier (height/baseSize) and use a monotone cubic (PCHIP) curve.
    private static readonly (double Pressure, double HeightScale)[] HighlighterPressureHeightScaleTable =
    [
        (0.01, 5.0 / 24.0),
        (0.05, 6.0 / 24.0),
        (0.10, 8.0 / 24.0),
        (0.20, 12.0 / 24.0),
        (0.30, 16.0 / 24.0),
        (0.40, 20.0 / 24.0),
        (0.50, 24.0 / 24.0),
        (0.60, 26.0 / 24.0),
        (0.70, 30.0 / 24.0),
        (0.80, 34.0 / 24.0),
        (0.90, 38.0 / 24.0),
        (1.00, 38.0 / 24.0)
    ];

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
                // Prefer legacy ink dimensions if present (closer to UWP rectangle tip behavior).
                var highlighterWidth = (double?)null;
                var highlighterHeight = (double?)null;

                // Bcf2Stroke may carry these fields; if not, they remain null.
                try
                {
                    var t = s.GetType();
                    highlighterWidth = t.GetProperty("SizeWidth")?.GetValue(s) as double?;
                    highlighterHeight = t.GetProperty("SizeHeight")?.GetValue(s) as double?;
                }
                catch { }

                DrawHighlighterStroke(canvas, s.Points, baseSize, baseColor, s.Opacity, highlighterWidth, highlighterHeight);
                continue;
            }

            DrawStrokeVariableWidth(canvas, s.Points, baseSize, baseColor, s.Opacity);
        }

        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data?.ToArray() ?? Array.Empty<byte>();
    }

    private static void DrawHighlighterStroke(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, SKColor baseColor, double? opacity, double? legacySizeWidth, double? legacySizeHeight)
    {
        // UWPのDrawAsHighlighterは白背景でのレンダリング＆白抜き後処理に依存していましたが、
        // ここでは背景色を固定せず、透過PNGのまま合成（BlendMode）で近似します。

        var a = Math.Min((int)baseColor.Alpha, 160);
        var color = baseColor.WithAlpha((byte)a);

        // If we have explicit height, use it as the baseSize (selected size corresponds to visible height).
        var effectiveBaseSize = legacySizeHeight.HasValue && legacySizeHeight.Value > 0.1 ? legacySizeHeight.Value : baseSize;

        // If we have explicit width+height, derive aspect; otherwise fallback.
        var aspect = HighlighterTipAspect;
        if (legacySizeWidth.HasValue && legacySizeWidth.Value > 0.1 && legacySizeHeight.HasValue && legacySizeHeight.Value > 0.1)
        {
            aspect = (float)(legacySizeHeight.Value / legacySizeWidth.Value);
            aspect = Math.Clamp(aspect, 1.25f, 10.0f);
        }

        // 1) Draw geometry into an alpha mask surface. Use Src to avoid accumulation (overlap should remain uniform).
        var bounds = canvas.DeviceClipBounds;
        using var maskSurface = SKSurface.Create(new SKImageInfo(bounds.Width, bounds.Height, SKColorType.Alpha8, SKAlphaType.Premul));
        var maskCanvas = maskSurface.Canvas;
        maskCanvas.Clear(SKColors.Transparent);

        using (var maskLayerPaint = new SKPaint { BlendMode = SKBlendMode.Src })
        {
            var save = maskCanvas.SaveLayer(maskLayerPaint);
            try
            {
                DrawHighlighterMask(maskCanvas, points, effectiveBaseSize, aspect);
            }
            finally
            {
                maskCanvas.RestoreToCount(save);
            }
        }

        // 2) Apply uniform color through the mask in a single draw.
        var effectiveColor = ApplyOpacity(color, opacity);
        using var mask = maskSurface.Snapshot();

        using var paint = new SKPaint
        {
            Color = effectiveColor,
            IsAntialias = true,
            BlendMode = SKBlendMode.SrcOver
        };

        canvas.DrawImage(mask, 0, 0, paint);
    }

    private static void DrawHighlighterMask(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, float tipAspect)
    {
        // Draw mask as solid alpha (white in Alpha8). Geometry only; no color.
        using var paint = new SKPaint
        {
            IsAntialias = true,
            Color = SKColors.White,
            Style = SKPaintStyle.Fill
        };

        // Reuse existing highlighter geometry routes.
        // Stationary points: stamp per point.
        var stationary = true;
        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;
            var dx0 = (float)(p1.X - p0.X);
            var dy0 = (float)(p1.Y - p0.Y);
            if (MathF.Sqrt(dx0 * dx0 + dy0 * dy0) > HighlighterStationaryEpsilonPx)
            {
                stationary = false;
                break;
            }
        }

        if (stationary)
        {
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                if (p == null) continue;

                var shortSide = (float)(baseSize * PressureToHighlighterScale(p.Pressure, tipAspect));
                var longSide = shortSide * tipAspect;

                shortSide = Math.Max(shortSide, HighlighterMinShortSidePx);
                longSide = Math.Max(longSide, HighlighterMinLongSidePx);

                DrawAxisAlignedRect(canvas, paint, (float)p.X, (float)p.Y, longSide, shortSide);
            }
            return;
        }

        // Moving: continuous strip (same as current), but rendered into the mask.
        var path = new SKPath();
        var hasAny = false;

        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;

            var dx = (float)(p1.X - p0.X);
            var dy = (float)(p1.Y - p0.Y);
            var len = MathF.Sqrt(dx * dx + dy * dy);

            if (len <= HighlighterStationaryEpsilonPx)
            {
                var pr0 = p1.Pressure;
                var short0 = (float)(baseSize * PressureToHighlighterScale(pr0, tipAspect));
                short0 *= (float)GetTaperScale(i - 1, points.Count - 1);
                var long0 = short0 * tipAspect;

                short0 = Math.Max(short0, HighlighterMinShortSidePx);
                long0 = Math.Max(long0, HighlighterMinLongSidePx);

                DrawAxisAlignedRect(canvas, paint, (float)p1.X, (float)p1.Y, long0, short0);
                hasAny = true;
                continue;
            }

            var pr = 0.5 * (p0.Pressure + p1.Pressure);
            var shortSide = (float)(baseSize * PressureToHighlighterScale(pr, tipAspect));
            shortSide *= (float)GetTaperScale(i - 1, points.Count - 1);

            var longSide = shortSide * tipAspect;

            shortSide = Math.Max(shortSide, HighlighterMinShortSidePx);
            longSide = Math.Max(longSide, HighlighterMinLongSidePx);

            var halfW = shortSide * 0.5f;
            var halfH = longSide * 0.5f;

            var x0 = (float)p0.X;
            var y0 = (float)p0.Y;
            var x1 = (float)p1.X;
            var y1 = (float)p1.Y;

            var r0_tl = new SKPoint(x0 - halfW, y0 - halfH);
            var r0_tr = new SKPoint(x0 + halfW, y0 - halfH);
            var r0_br = new SKPoint(x0 + halfW, y0 + halfH);
            var r0_bl = new SKPoint(x0 - halfW, y0 + halfH);

            var r1_tl = new SKPoint(x1 - halfW, y1 - halfH);
            var r1_tr = new SKPoint(x1 + halfW, y1 - halfH);
            var r1_br = new SKPoint(x1 + halfW, y1 + halfH);
            var r1_bl = new SKPoint(x1 - halfW, y1 + halfH);

            AddRectToPath(path, r0_tl, r0_tr, r0_br, r0_bl);
            AddRectToPath(path, r1_tl, r1_tr, r1_br, r1_bl);

            AddQuadToPath(path, r0_tl, r0_tr, r1_tr, r1_tl);
            AddQuadToPath(path, r0_bl, r0_br, r1_br, r1_bl);
            AddQuadToPath(path, r0_tl, r0_bl, r1_bl, r1_tl);
            AddQuadToPath(path, r0_tr, r0_br, r1_br, r1_tr);

            hasAny = true;
        }

        if (hasAny)
        {
            path.FillType = SKPathFillType.Winding;
            canvas.DrawPath(path, paint);
        }
    }

    private static void AddRectToPath(SKPath path, SKPoint tl, SKPoint tr, SKPoint br, SKPoint bl)
    {
        path.MoveTo(tl);
        path.LineTo(tr);
        path.LineTo(br);
        path.LineTo(bl);
        path.Close();
    }

    private static void AddQuadToPath(SKPath path, SKPoint a, SKPoint b, SKPoint c, SKPoint d)
    {
        path.MoveTo(a);
        path.LineTo(b);
        path.LineTo(c);
        path.LineTo(d);
        path.Close();
    }

    private static double Clamp01(double v)
        => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static void DrawAxisAlignedRect(SKCanvas canvas, SKPaint paint, float cx, float cy, float longSide, float shortSide)
    {
        // 縦長固定: longSideをY方向、shortSideをX方向へ
        var rect = new SKRect(
            cx - shortSide * 0.5f,
            cy - longSide * 0.5f,
            cx + shortSide * 0.5f,
            cy + longSide * 0.5f);

        canvas.DrawRect(rect, paint);
    }

    private static void DrawStrokeVariableWidth(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, SKColor color, double? opacity)
    {
        if (points == null || points.Count == 0) return;

        var effectiveColor = ApplyOpacity(color, opacity);

        // Single point: draw a dot as a circle using pressure.
        if (points.Count == 1)
        {
            var p = points[0];
            var w = (float)PressureToStrokeWidth(p?.Pressure ?? 1.0, baseSize);
            using var paint = CreateStrokePaint(effectiveColor, w);
            canvas.DrawCircle((float)p.X, (float)p.Y, w * 0.5f, paint);
            return;
        }

        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;

            var pr = 0.5 * (p0.Pressure + p1.Pressure);
            var w = (float)PressureToStrokeWidth(pr, baseSize);

            var taper = (float)GetTaperScale(i - 1, points.Count - 1);
            w *= taper;

            using var paint = CreateStrokePaint(effectiveColor, w);
            canvas.DrawLine((float)p0.X, (float)p0.Y, (float)p1.X, (float)p1.Y, paint);
        }
    }

    private static double GetTaperScale(int segmentIndex, int segmentCount)
    {
        if (segmentCount <= 0) return 1f;

        if (TaperSegments <= 0) return 1f;

        if (segmentIndex < TaperSegments)
        {
            var t = (segmentIndex + 1f) / TaperSegments;
            return Lerp(TaperMinScale, 1f, t);
        }

        var tailStart = Math.Max(0, segmentCount - TaperSegments);
        if (segmentIndex >= tailStart)
        {
            var t = (segmentCount - segmentIndex) / (float)TaperSegments;
            return Lerp(TaperMinScale, 1f, t);
        }

        return 1f;
    }

    private static float Lerp(float a, float b, float t)
        => a + (b - a) * Math.Clamp(t, 0f, 1f);

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

    private static double PressureToStrokeWidth(double pressure, double baseSize)
    {
        // UWP側の観察結果に合わせ、baseSizeを「最小幅」とみなし、筆圧で増加させる。
        // pressure=0 -> baseSize
        // pressure=1 -> baseSize * maxScale
        var pr = Clamp01(pressure);

        // 0付近?中圧の変化を緩やかにしつつ、最大付近で伸びるように。
        const double gamma = 1.35;
        var scaled = Math.Pow(pr, gamma);

        const double minScale = 1.0;
        const double maxScale = 1.70;

        var scale = minScale + (maxScale - minScale) * scaled;
        return Math.Max(0.1, baseSize * scale);
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

    private static double PressureToHighlighterScale(double pressure, float tipAspect)
    {
        var p = Clamp01(pressure);
        var heightScale = PchipInterpolate(p, HighlighterPressureHeightScaleTable);
        var shortSideScale = heightScale / Math.Max(0.0001f, tipAspect);
        return Math.Clamp(shortSideScale, 0.05, 10.0);
    }

    private static double PchipInterpolate(double x, (double X, double Y)[] points)
    {
        if (points == null || points.Length == 0) return 1.0;
        if (points.Length == 1) return points[0].Y;

        if (x <= points[0].X) return points[0].Y;
        if (x >= points[^1].X) return points[^1].Y;

        var i = 0;
        for (; i < points.Length - 2; i++)
        {
            if (x <= points[i + 1].X) break;
        }

        var x0 = points[i].X;
        var x1 = points[i + 1].X;
        var y0 = points[i].Y;
        var y1 = points[i + 1].Y;

        var h = x1 - x0;
        if (h <= 0) return y0;

        var m = ComputePchipSlopes(points);
        var m0 = m[i];
        var m1 = m[i + 1];

        var t = (x - x0) / h;
        t = Math.Clamp(t, 0.0, 1.0);

        var t2 = t * t;
        var t3 = t2 * t;

        var h00 = 2 * t3 - 3 * t2 + 1;
        var h10 = t3 - 2 * t2 + t;
        var h01 = -2 * t3 + 3 * t2;
        var h11 = t3 - t2;

        return h00 * y0 + h10 * h * m0 + h01 * y1 + h11 * h * m1;
    }

    private static double[] ComputePchipSlopes((double X, double Y)[] p)
    {
        var n = p.Length;
        var h = new double[n - 1];
        var delta = new double[n - 1];

        for (var i = 0; i < n - 1; i++)
        {
            h[i] = p[i + 1].X - p[i].X;
            delta[i] = h[i] > 0 ? (p[i + 1].Y - p[i].Y) / h[i] : 0.0;
        }

        var m = new double[n];

        if (n == 2)
        {
            m[0] = delta[0];
            m[1] = delta[0];
            return m;
        }

        for (var i = 1; i < n - 1; i++)
        {
            if (delta[i - 1] == 0.0 || delta[i] == 0.0 || Math.Sign(delta[i - 1]) != Math.Sign(delta[i]))
            {
                m[i] = 0.0;
                continue;
            }

            var w1 = 2 * h[i] + h[i - 1];
            var w2 = h[i] + 2 * h[i - 1];
            m[i] = (w1 + w2) / (w1 / delta[i - 1] + w2 / delta[i]);
        }

        m[0] = PchipEndpointSlope(h0: h[0], h1: h[1], d0: delta[0], d1: delta[1]);
        m[n - 1] = PchipEndpointSlope(h0: h[^1], h1: h[^2], d0: delta[^1], d1: delta[^2]);

        return m;
    }

    private static double PchipEndpointSlope(double h0, double h1, double d0, double d1)
    {
        if (h0 <= 0 || h1 <= 0) return d0;

        var m = ((2 * h0 + h1) * d0 - h0 * d1) / (h0 + h1);

        if (Math.Sign(m) != Math.Sign(d0)) return 0.0;
        if (Math.Sign(d0) != Math.Sign(d1) && Math.Abs(m) > Math.Abs(3 * d0)) return 3 * d0;
        if (Math.Abs(m) > Math.Abs(3 * d0)) return 3 * d0;

        return m;
    }
}
