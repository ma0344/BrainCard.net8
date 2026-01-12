using BrainCard.Models.FileFormatV2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#if BRAIN_CARD_ENABLE_WINRT_INK
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.UI.Input.Inking;
#endif

namespace BrainCard.Legacy;

public static class LegacyInkToV2Converter
{
#if BRAIN_CARD_ENABLE_WINRT_INK
    private static string ToHexArgb(Windows.UI.Color c)
        => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    private static double Clamp01(double v)
        => v < 0 ? 0 : (v > 1 ? 1 : v);

    private static double CoerceSize(double? size)
    {
        if (size == null) return 2.0;
        var v = size.Value;
        if (double.IsNaN(v) || double.IsInfinity(v) || v <= 0) return 2.0;
        return v;
    }

    private static string MapTool(InkStroke stroke)
    {
        try
        {
            // Pencil/pen/highlighterなどの厳密マッピングは後続Issue。
            // まずはhighlighterを判別できる範囲で対応し、他はpen。
            var da = stroke?.DrawingAttributes;
            if (da != null && da.DrawAsHighlighter)
            {
                return "highlighter";
            }
        }
        catch
        {
        }

        return "pen";
    }

    public static async Task<IReadOnlyList<Bcf2Stroke>> TryConvertIsfBytesToV2StrokesAsync(byte[] isfBytes)
    {
        if (isfBytes == null || isfBytes.Length == 0)
            return Array.Empty<Bcf2Stroke>();

        InkStrokeContainer container;
        try
        {
            using var ms = new MemoryStream(isfBytes);
            using IRandomAccessStream stream = ms.AsRandomAccessStream();
            container = new InkStrokeContainer();
            await container.LoadAsync(stream);
        }
        catch
        {
            return Array.Empty<Bcf2Stroke>();
        }

        try
        {
            var result = new List<Bcf2Stroke>();

            foreach (var stroke in container.GetStrokes())
            {
                if (stroke == null) continue;

                var points = stroke.GetInkPoints();
                if (points == null || points.Count == 0) continue;

                var v2 = new Bcf2Stroke
                {
                    Id = Guid.NewGuid().ToString("N"),
                    Tool = MapTool(stroke),
                    DeviceKind = "unknown"
                };

                try
                {
                    var da = stroke.DrawingAttributes;
                    if (da != null)
                    {
                        v2.Color = ToHexArgb(da.Color);

                        // v2にはcolor(ARGB)とopacityがあるが、初期はcolorのAに集約する。
                        v2.Opacity = null;

                        // Size: InkDrawingAttributes.Size が使える場合は優先。
                        // （形状や単位差は後段で調整）
                        var size = Math.Max(da.Size.Width, da.Size.Height);
                        v2.Size = CoerceSize(size);
                    }
                }
                catch
                {
                    // keep defaults
                    if (string.IsNullOrWhiteSpace(v2.Color)) v2.Color = "#FF000000";
                    if (v2.Size <= 0) v2.Size = 2.0;
                }

                if (string.IsNullOrWhiteSpace(v2.Color)) v2.Color = "#FF000000";
                if (v2.Size <= 0) v2.Size = 2.0;

                var t = 0;
                foreach (var p in points)
                {
                    v2.Points.Add(new Bcf2Point
                    {
                        X = p.Position.X,
                        Y = p.Position.Y,
                        Pressure = Clamp01(p.Pressure),
                        T = t
                    });
                    t += 16;
                }

                // Ensure t is monotonic and starts at 0.
                try
                {
                    LegacyTimeReconstruction.EnsureMonotonicRelativeMs(v2.Points, defaultStepMs: 16);
                }
                catch
                {
                }

                result.Add(v2);
            }

            return result;
        }
        catch
        {
            return Array.Empty<Bcf2Stroke>();
        }
    }
#else
    public static Task<IReadOnlyList<Bcf2Stroke>> TryConvertIsfBytesToV2StrokesAsync(byte[] isfBytes)
        => Task.FromResult((IReadOnlyList<Bcf2Stroke>)Array.Empty<Bcf2Stroke>());
#endif
}
