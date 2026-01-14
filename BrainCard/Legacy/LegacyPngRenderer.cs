using BrainCard.Models.FileFormatV2;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics;
using System.IO;

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

    // Observed: mouse-click strokes tend to report pressure≈0.5 while the visible thickness matches the selected size.
    // Normalize so that pressure=0.5 maps to scale=1.0.
    private const double HighlighterPressureBaseline = 0.5;
    private const double HighlighterMinScale = 0.20;
    private const double HighlighterMaxScale = 1.60;

    // Pencil: soft brush approximation
    private const float PencilStationaryEpsilonPx = 0.25f;
    private const float PencilMinRadiusPx = 0.6f;
    private const float PencilStepScale = 0.22f;
    private const float PencilMinStepPx = 0.35f;
    private const int PencilMaxStepsPerSegment = 512;

    // Pencil texture: base size for the cached noise bitmap.
    private const int PencilNoiseTextureSizePx = 128;

    // Pencil: observation (legacy output) pressure→visible dot diameter(px)
    // SizeHeight=24
    // 0.01:5.0px 0.05:7.0px 0.10:8.0px 0.20:13.0px 0.30:18.0px 0.40:23.0px 0.50:28.0px
    // 0.60..1.00:28.0px (saturates)
    // Normalize to a diameter multiplier (diameter/baseSize) and use monotone cubic (PCHIP).
    private static readonly (double Pressure, double DiameterScale)[] PencilPressureDiameterScaleTable =
    [
        (0.01, 5.0 / 24.0),
        (0.05, 7.0 / 24.0),
        (0.10, 8.0 / 24.0),
        (0.20, 13.0 / 24.0),
        (0.30, 18.0 / 24.0),
        (0.40, 23.0 / 24.0),
        (0.50, 28.0 / 24.0),
        (0.60, 28.0 / 24.0),
        (0.70, 28.0 / 24.0),
        (0.80, 28.0 / 24.0),
        (0.90, 28.0 / 24.0),
        (1.00, 28.0 / 24.0)
    ];

    // Legacy observation (UWP): highlighter pressure→visible line height(px)
    // SizeHeight=24: 0.9:38,0.8:34,0.7:30,0.6:26,0.5:24,0.4:20,0.3:16,0.2:12,0.1:8,0.05:6,0.01:5
    // (Reported: SizeWidth is fixed at 8, and tip rectangle is Height x (Height/3).)
    // We normalize to a height multiplier (height/baseSize) and use a monotone cubic (PCHIP) curve.
    private static readonly (double Pressure, double HeightScale)[] HighlighterPressureHeightScaleTable =
    [
        (0.01, 46.0 / 192.0),
        (0.05, 58.0 / 192.0),
        (0.10, 72.0 / 192.0),
        (0.20, 99.0 / 192.0),
        (0.30, 126.0 / 192.0),
        (0.40, 154.0 / 192.0),
        (0.50, 181.0 / 192.0),
        (0.60, 208.0 / 192.0),
        (0.70, 236.0 / 192.0),
        (0.80, 263.0 / 192.0),
        (0.90, 291.0 / 192.0),
        (1.00, 318.0 / 192.0)
    ];

    // Ballpoint (pen): observation (UWP) pressure→visible width(px) for SizeHeight=24
    // 0.01:2px, 0.05:5px, 0.10:6px, 0.20:8px, 0.30:11.5px, 0.40:15px, 0.50:19px, 0.60:22px, 0.70:26px, 0.80:29.5px, 0.90:33px
    // Normalize to a width multiplier (width/baseSize) and use monotone cubic (PCHIP).
    private static readonly (double Pressure, double WidthScale)[] PenPressureWidthScaleTable =
    [
        (0.01, 18.0 / 72.0),
        (0.05, 23.0 / 72.0),
        (0.10, 28.0 / 72.0),
        (0.20, 39.0 / 72.0),
        (0.30, 51.5 / 72.0),
        (0.40, 61.0 / 72.0),
        (0.50, 72.0 / 72.0),
        (0.60, 82.0 / 72.0),
        (0.70, 93.0 / 72.0),
        (0.80, 104.0 / 72.0),
        (0.90, 114.0 / 72.0),
        (1.00, 125.0 / 72.0)
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

            var baseColor = ParseHexARGBOrDefault(s.Color, SKColors.Black);

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

            if (string.Equals(s.Tool, "pencil", StringComparison.OrdinalIgnoreCase))
            {
                DrawPencilStroke(canvas, s.Points, baseSize, baseColor, s.Opacity);
                continue;
            }

            DrawStrokeVariableWidth(canvas, s.Points, baseSize, baseColor, s.Opacity);
        }

        using var image = surface.Snapshot();

        // Debug: verify output pixels (helps diagnose empty PNG regression).
        if (SkiaDebugDump.TryGetDumpDir() != null)
        {
            try
            {
                var bmp = new SKBitmap(info);
                image.ReadPixels(info, bmp.GetPixels(), info.RowBytes, 0, 0);

                var st = SkiaDebugDump.ComputeAlphaStats(bmp);
                Debug.WriteLine($"[LegacyPngRenderer] alphaNonZero={st.NonZeroCount} bounds={st.Bounds.Left},{st.Bounds.Top},{st.Bounds.Right},{st.Bounds.Bottom} size={width}x{height}");

                if (st.NonZeroCount == 0)
                {
                    SkiaDebugDump.TryDumpBitmapPng(bmp, $"renderpng-empty-{width}x{height}.png");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LegacyPngRenderer] alpha check failed: {ex}");
            }
        }

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
        var p = Clamp01(pressure);
        var widthScale = PchipInterpolate(p, PenPressureWidthScaleTable);
        return Math.Max(0.1, baseSize * widthScale);
    }

    private static SKColor ParseHexARGBOrDefault(string hex, SKColor fallback)
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

    private static void DrawPencilStroke(SKCanvas canvas, IReadOnlyList<Bcf2Point> points, double baseSize, SKColor baseColor, double? opacity)
    {
        if (points == null || points.Count == 0) return;

        // Pencil tends to look lighter/softer than pen. Keep alpha but rely on soft mask.
        var effectiveColor = ApplyOpacity(baseColor, opacity);

        // Optional: override min radius for banding diagnostics.
        var minRadiusPx = (double)PencilMinRadiusPx;
        var envMinRadius = Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_MIN_RADIUS_PX");
        if (!string.IsNullOrWhiteSpace(envMinRadius) && double.TryParse(envMinRadius, NumberStyles.Float, CultureInfo.InvariantCulture, out var mr))
        {
            minRadiusPx = Math.Clamp(mr, 0.05, 50.0);
        }

        // Optional: override stamping density for banding diagnostics.
        var stepScale = (double)PencilStepScale;
        var minStepPx = (double)PencilMinStepPx;
        var envStepScale = Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_STEP_SCALE");
        if (!string.IsNullOrWhiteSpace(envStepScale) && double.TryParse(envStepScale, NumberStyles.Float, CultureInfo.InvariantCulture, out var ss))
        {
            stepScale = Math.Clamp(ss, 0.01, 10.0);
        }
        var envMinStep = Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_MIN_STEP_PX");
        if (!string.IsNullOrWhiteSpace(envMinStep) && double.TryParse(envMinStep, NumberStyles.Float, CultureInfo.InvariantCulture, out var ms))
        {
            minStepPx = Math.Clamp(ms, 0.01, 100.0);
        }

        // Normalize stamp alpha by step length and radius so repeated stamping doesn't over-darken.
        // Can be tuned by env var for parity work.
        var densityRef = 1.0;
        var envDensityRef = Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_DENSITY_REF");
        if (!string.IsNullOrWhiteSpace(envDensityRef) && double.TryParse(envDensityRef, NumberStyles.Float, CultureInfo.InvariantCulture, out var dr))
        {
            densityRef = Math.Clamp(dr, 0.05, 50.0);
        }

        bool shouldSkipFirstStampInSegment = false;

        var stationary = true;
        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;
            var dx0 = (float)(p1.X - p0.X);
            var dy0 = (float)(p1.Y - p0.Y);
            if (MathF.Sqrt(dx0 * dx0 + dy0 * dy0) > PencilStationaryEpsilonPx)
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

                var radius = (float)(0.5 * baseSize * PressureToPencilScale(p.Pressure));
                radius = Math.Max(radius, (float)minRadiusPx);
                radius *= (float)GetTaperScale(i, points.Count - 1);

                // Stationary strokes: avoid excessive dark dot by using a small, fixed density.
                var alphaMul = ComputePencilAlphaMultiplier(radius, stepPx: MathF.Max((float)minStepPx, radius * (float)stepScale), densityRef);
                StampSoftCircle(canvas, (float)p.X, (float)p.Y, radius, effectiveColor, alphaMul);
            }
            return;
        }

        for (var i = 1; i < points.Count; i++)
        {
            var p0 = points[i - 1];
            var p1 = points[i];
            if (p0 == null || p1 == null) continue;

            var x0 = (float)p0.X;
            var y0 = (float)p0.Y;
            var x1 = (float)p1.X;
            var y1 = (float)p1.Y;

            var dx = x1 - x0;
            var dy = y1 - y0;
            var len = MathF.Sqrt(dx * dx + dy * dy);

            // pressure baseline: 0.5 should roughly match selected size
            var pr = 0.5 * (p0.Pressure + p1.Pressure);
            var radiusBase = (float)(0.5 * baseSize * PressureToPencilScale(pr));
            radiusBase *= (float)GetTaperScale(i - 1, points.Count - 1);
            radiusBase = Math.Max(radiusBase, (float)minRadiusPx);

            if (len <= PencilStationaryEpsilonPx)
            {
                var alphaMul = ComputePencilAlphaMultiplier(radiusBase, stepPx: MathF.Max((float)minStepPx, radiusBase * (float)stepScale), densityRef);
                StampSoftCircle(canvas, x1, y1, radiusBase, effectiveColor, alphaMul);
                continue;
            }

            var step = Math.Max(minStepPx, radiusBase * stepScale);
            var steps = (int)MathF.Ceiling(len / (float)step);
            steps = Math.Clamp(steps, 1, PencilMaxStepsPerSegment);

            // Start point duplication: previous segment tends to end at the same coordinate.
            // Also avoid over-darkening at direction reversals (fold-back) by not stamping on t=0.
            var s0 = shouldSkipFirstStampInSegment ? 1 : 0;
            shouldSkipFirstStampInSegment = true;

            var alphaMulSeg = ComputePencilAlphaMultiplier(radiusBase, stepPx: (float)step, densityRef);

            for (var s = s0; s <= steps; s++)
            {
                var t = steps == 0 ? 0f : (s / (float)steps);
                var x = x0 + dx * t;
                var y = y0 + dy * t;
                StampSoftCircle(canvas, x, y, radiusBase, effectiveColor, alphaMulSeg);
            }
        }
    }

    private static float ComputePencilAlphaMultiplier(float radius, float stepPx, double densityRef)
    {
        // Larger radius and/or smaller step means more overlap, hence reduce per-stamp alpha.
        // Use a smooth curve to avoid sudden changes.
        var overlap = radius <= 0 ? 1f : (float)(stepPx / (radius * densityRef));
        overlap = Math.Clamp(overlap, 0.05f, 5.0f);

        // Map overlap to alpha multiplier: overlap=1 -> 1, smaller -> reduce.
        // sqrt softens response.
        return MathF.Sqrt(overlap);
    }

    private static void StampSoftCircle(SKCanvas canvas, float cx, float cy, float radius, SKColor color)
    {
        StampSoftCircle(canvas, cx, cy, radius, color, alphaMultiplier: 1.0f);
    }

    private static void StampSoftCircle(SKCanvas canvas, float cx, float cy, float radius, SKColor color, float alphaMultiplier)
    {
        if (radius <= 0) return;

        // Pencil: textured stamp (deterministic noise masked by a radial falloff).
        // This avoids ink/marker-like smooth gradients and yields a powdery look.

        const float opacityMultiplier = 0.10f;
        const byte maskInnerA = 100;
        const byte maskMidA = 70;
        const float maskMidPos = 0.8f;
        const float noiseScale = 0.50f;

        var am = Math.Clamp(alphaMultiplier, 0.01f, 10.0f);
        var a = (byte)Math.Clamp((int)Math.Round(color.Alpha * opacityMultiplier * am), 0, 255);
        if (a == 0) return;

        // Clamp mask radius to avoid pixel-quantization banding on small stamps.
        // This keeps the falloff smooth enough in device pixel space.
        var maskRadius = MathF.Max(radius, 2.0f);

        // On very small masks, keep the falloff gentler (reduce abrupt alpha changes).
        var midPos = maskRadius <= 3.0f ? 0.90f : maskMidPos;

        // Build brush shader (color with radial alpha falloff) to avoid DstIn radial mask banding.
        // Color alpha is applied at center and fades to 0 at edge.
        var colorAtCenter = color.WithAlpha(a);
        var colorAtMid = color.WithAlpha((byte)Math.Clamp((int)Math.Round(a * (maskMidA / 100.0)), 0, 255));
        using var brushShader = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy),
            maskRadius,
            new[] { colorAtCenter, colorAtMid, color.WithAlpha(0) },
            new[] { 0.0f, midPos, 1.0f },
            SKShaderTileMode.Clamp);

        using var paint = new SKPaint
        {
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            Shader = brushShader,
            BlendMode = SKBlendMode.SrcOver
        };

        // Radial mask (alpha) colors kept for optional legacy path + dump.
        var maskInner = new SKColor(0, 0, 0, maskInnerA);
        var maskMid = new SKColor(0, 0, 0, maskMidA);
        var maskOuter = new SKColor(0, 0, 0, 0);

        using var maskShader = SKShader.CreateRadialGradient(
            new SKPoint(cx, cy),
            maskRadius,
            new[] { maskInner, maskMid, maskOuter },
            new[] { 0.0f, midPos, 1.0f },
            SKShaderTileMode.Clamp);

        // Noise shader (alpha): tiled; sized in device space.
        // Add a phase offset per stamp to avoid periodic banding caused by fixed-origin tiling.
        var noiseBmp = PencilBrushTexture.GetAlphaNoise(PencilNoiseTextureSizePx);
        var sx = (radius * noiseScale) / (PencilNoiseTextureSizePx * 0.5f);
        var sy = (radius * noiseScale) / (PencilNoiseTextureSizePx * 0.5f);

        // Deterministic sub-pixel offset derived from stamp center.
        // Using a small offset (in source texture px) breaks alignment without adding randomness.
        float ox;
        float oy;
        if (EnvFlag("BRAIN_CARD_PENCIL_FIXED_NOISE_PHASE"))
        {
            ox = 0;
            oy = 0;
        }
        else
        {
            ox = (float)(((cx * 0.73f) + (cy * 0.37f)) % PencilNoiseTextureSizePx);
            oy = (float)(((cx * 0.41f) + (cy * 0.91f)) % PencilNoiseTextureSizePx);
        }

        var noiseMatrix = SKMatrix.Concat(
            SKMatrix.CreateTranslation(-ox, -oy),
            SKMatrix.CreateScale(sx, sy));

        using var noiseShader = SKShader.CreateBitmap(
            noiseBmp,
            SKShaderTileMode.Repeat,
            SKShaderTileMode.Repeat,
            noiseMatrix);

        // Optional: debug log occasionally (env var gated)
        if (SkiaDebugDump.TryGetDumpDir() != null)
        {
            var n = System.Threading.Interlocked.Increment(ref _pencilStampDumpCounter);
            if ((n % 200) == 0)
            {
                Debug.WriteLine($"[PencilStamp] n={n} cx={cx:F2} cy={cy:F2} r={radius:F2} sx={sx:F4} sy={sy:F4} ox={ox:F2} oy={oy:F2}");
            }
        }

        var rect = new SKRect(cx - maskRadius, cy - maskRadius, cx + maskRadius, cy + maskRadius);

        var dumpDir = SkiaDebugDump.TryGetDumpDir();
        var wantStampDump = dumpDir != null &&
            string.Equals(Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_STAMP_DUMP"), "1", StringComparison.OrdinalIgnoreCase) &&
            System.Threading.Interlocked.CompareExchange(ref _pencilStampDumpOnce, 1, 0) == 0;

        if (wantStampDump)
        {
            // Ensure the dump is visually inspectable even for very small radius (e.g., r<1).
            const int minDumpSizePx = 96;
            const int marginPx = 16;

            var diameter = (int)Math.Ceiling(radius * 2);
            var s = Math.Max(minDumpSizePx, diameter + marginPx * 2);

            var info = new SKImageInfo(s, s, SKColorType.Bgra8888, SKAlphaType.Premul);
            var c = s * 0.5f;

            // Optional: boost alpha for dump only.
            var boostEnv = Environment.GetEnvironmentVariable("BRAIN_CARD_PENCIL_STAMP_DUMP_ALPHA_BOOST");
            var alphaBoost = 1.0f;
            if (!string.IsNullOrWhiteSpace(boostEnv) && float.TryParse(boostEnv, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedBoost))
            {
                alphaBoost = Math.Clamp(parsedBoost, 1.0f, 1000.0f);
            }

            using var stage0 = SKSurface.Create(info);
            stage0.Canvas.Clear(SKColors.Transparent);
            stage0.Canvas.DrawCircle(c, c, radius, paint);

            using var stage1 = SKSurface.Create(info);
            stage1.Canvas.Clear(SKColors.Transparent);
            stage1.Canvas.DrawCircle(c, c, radius, paint);
            using (var mp = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(c, c), maskRadius, new[] { maskInner, maskMid, maskOuter }, new[] { 0.0f, midPos, 1.0f }, SKShaderTileMode.Clamp), BlendMode = SKBlendMode.DstIn, IsAntialias = true })
            {
                stage1.Canvas.DrawRect(new SKRect(0, 0, s, s), mp);
            }

            using var stage2 = SKSurface.Create(info);
            stage2.Canvas.Clear(SKColors.Transparent);
            stage2.Canvas.DrawCircle(c, c, radius, paint);
            using (var mp = new SKPaint { Shader = SKShader.CreateRadialGradient(new SKPoint(c, c), maskRadius, new[] { maskInner, maskMid, maskOuter }, new[] { 0.0f, midPos, 1.0f }, SKShaderTileMode.Clamp), BlendMode = SKBlendMode.DstIn, IsAntialias = true })
            {
                stage2.Canvas.DrawRect(new SKRect(0, 0, s, s), mp);
            }
            using (var np = new SKPaint
            {
                Shader = noiseShader,
                BlendMode = SKBlendMode.DstIn,
                IsAntialias = true
            })
            {
                stage2.Canvas.DrawRect(new SKRect(0, 0, s, s), np);
            }

            using var i0 = stage0.Snapshot();
            using var i1 = stage1.Snapshot();
            using var i2 = stage2.Snapshot();

            using var b0 = SKBitmap.FromImage(i0);
            using var b1 = SKBitmap.FromImage(i1);
            using var b2 = SKBitmap.FromImage(i2);

            // If alpha is too faint, output boosted-alpha variants for inspection.
            SKBitmap? bb0 = null;
            SKBitmap? bb1 = null;
            SKBitmap? bb2 = null;
            try
            {
                if (alphaBoost > 1.0f)
                {
                    bb0 = SkiaDebugDump.BoostAlpha(b0, alphaBoost);
                    bb1 = SkiaDebugDump.BoostAlpha(b1, alphaBoost);
                    bb2 = SkiaDebugDump.BoostAlpha(b2, alphaBoost);
                }

                var rText = radius.ToString("0.00", CultureInfo.InvariantCulture);
                var sxText = sx.ToString("0.0000", CultureInfo.InvariantCulture);
                var oxText = ox.ToString("0.00", CultureInfo.InvariantCulture);
                var oyText = oy.ToString("0.00", CultureInfo.InvariantCulture);

                var n0 = $"pencil-stamp-0-circle-r{rText}-s{s}.png";
                var n1 = $"pencil-stamp-1-circle+radial-r{rText}-s{s}.png";
                var n2 = $"pencil-stamp-2-circle+radial+noise-r{rText}-sx{sxText}-ox{oxText}-oy{oyText}-s{s}.png";

                SkiaDebugDump.TryDumpBitmapPng(b0, n0);
                SkiaDebugDump.TryDumpBitmapPng(b1, n1);
                SkiaDebugDump.TryDumpBitmapPng(b2, n2);

                // Also dump versions composited onto a white background for easy viewing in default image viewers.
                using (var wb0 = SkiaDebugDump.CompositeOnSolidBackground(b0, SKColors.White))
                using (var wb1 = SkiaDebugDump.CompositeOnSolidBackground(b1, SKColors.White))
                using (var wb2 = SkiaDebugDump.CompositeOnSolidBackground(b2, SKColors.White))
                {
                    SkiaDebugDump.TryDumpBitmapPng(wb0, Path.GetFileNameWithoutExtension(n0) + "-onwhite.png");
                    SkiaDebugDump.TryDumpBitmapPng(wb1, Path.GetFileNameWithoutExtension(n1) + "-onwhite.png");
                    SkiaDebugDump.TryDumpBitmapPng(wb2, Path.GetFileNameWithoutExtension(n2) + "-onwhite.png");
                }

                if (bb0 != null && bb1 != null && bb2 != null)
                {
                    var suffix = $"-boost{alphaBoost.ToString("0.##", CultureInfo.InvariantCulture)}";
                    SkiaDebugDump.TryDumpBitmapPng(bb0, Path.GetFileNameWithoutExtension(n0) + suffix + ".png");
                    SkiaDebugDump.TryDumpBitmapPng(bb1, Path.GetFileNameWithoutExtension(n1) + suffix + ".png");
                    SkiaDebugDump.TryDumpBitmapPng(bb2, Path.GetFileNameWithoutExtension(n2) + suffix + ".png");

                    using (var wbb0 = SkiaDebugDump.CompositeOnSolidBackground(bb0, SKColors.White))
                    using (var wbb1 = SkiaDebugDump.CompositeOnSolidBackground(bb1, SKColors.White))
                    using (var wbb2 = SkiaDebugDump.CompositeOnSolidBackground(bb2, SKColors.White))
                    {
                        SkiaDebugDump.TryDumpBitmapPng(wbb0, Path.GetFileNameWithoutExtension(n0) + suffix + "-onwhite.png");
                        SkiaDebugDump.TryDumpBitmapPng(wbb1, Path.GetFileNameWithoutExtension(n1) + suffix + "-onwhite.png");
                        SkiaDebugDump.TryDumpBitmapPng(wbb2, Path.GetFileNameWithoutExtension(n2) + suffix + "-onwhite.png");
                    }
                }

                Debug.WriteLine($"[PencilStampDump] wrote 3 PNGs: r={radius:F2} s={s} sx={sx:F4} sy={sy:F4} ox={ox:F2} oy={oy:F2} boost={alphaBoost:F2}");
            }
            finally
            {
                bb0?.Dispose();
                bb1?.Dispose();
                bb2?.Dispose();
            }
        }

        // === Actual draw: circle color -> radial alpha mask -> noise alpha mask ===
        // We draw in 3 steps using destination-in to apply masks.
        var save = canvas.SaveLayer(null);
        try
        {
            canvas.DrawCircle(cx, cy, radius, paint);

            // Optional legacy radial mask path (DstIn). Default disabled due to banding.
            if (EnvFlag("BRAIN_CARD_PENCIL_ENABLE_RADIAL_MASK"))
            {
                using var mp = new SKPaint { Shader = maskShader, BlendMode = SKBlendMode.DstIn, IsAntialias = true };
                canvas.DrawRect(rect, mp);
            }

            // Toggle: disable noise masking to diagnose banding/striping.
            if (!EnvFlag("BRAIN_CARD_PENCIL_DISABLE_NOISE"))
            {
                using var np = new SKPaint { Shader = noiseShader, BlendMode = SKBlendMode.DstIn, IsAntialias = true };
                canvas.DrawRect(rect, np);
            }
        }
        finally
        {
            canvas.RestoreToCount(save);
        }
    }

    private static double PressureToPencilScale(double pressure)
    {
        var p = Clamp01(pressure);
        var diameterScale = PchipInterpolate(p, PencilPressureDiameterScaleTable);
        return Math.Max(0.05, diameterScale);
    }

    private static int _pencilStampDumpCounter;
    private static int _pencilStampDumpOnce;

    private static bool EnvFlag(string name)
        => string.Equals(Environment.GetEnvironmentVariable(name), "1", StringComparison.OrdinalIgnoreCase);
}
