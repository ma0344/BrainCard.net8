using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

#if BRAIN_CARD_ENABLE_WINRT_INK
using Windows.UI.Input.Inking;
#endif

namespace BrainCard.Legacy;

public static class LegacyInkDump
{
#if BRAIN_CARD_ENABLE_WINRT_INK
    public sealed class InkDumpRoot
    {
        public List<InkDumpStroke> Strokes { get; set; } = new();
    }

    public sealed class InkDumpStroke
    {
        public string Tool { get; set; }
        public bool DrawAsHighlighter { get; set; }
        public string Color { get; set; }
        public double SizeWidth { get; set; }
        public double SizeHeight { get; set; }
        public string PenTip { get; set; }
        public double? PenTipTransformM11 { get; set; }
        public double? PenTipTransformM22 { get; set; }
        public List<InkDumpPoint> Points { get; set; } = new();
    }

    public sealed class InkDumpPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Pressure { get; set; }
    }

    private static string ToHexArgb(Windows.UI.Color c)
        => $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";

    public static string DumpToJson(InkStrokeContainer container)
    {
        var root = new InkDumpRoot();
        var strokes = container.GetStrokes();
        if (container != null)
        {
            foreach (var stroke in container.GetStrokes())
            {
                if (stroke == null) continue;

                var s = new InkDumpStroke();
                try
                {
                    var da = stroke.DrawingAttributes;
                    if (da != null)
                    {
                        s.DrawAsHighlighter = da.DrawAsHighlighter;
                        s.Color = ToHexArgb(da.Color);
                        s.SizeWidth = da.Size.Width;
                        s.SizeHeight = da.Size.Height;
                        s.PenTip = da.PenTip.ToString();

                        try
                        {
                            var m = da.PenTipTransform;
                            s.PenTipTransformM11 = m.M11;
                            s.PenTipTransformM22 = m.M22;
                        }
                        catch
                        {
                        }
                    }
                }
                catch
                {
                }

                try
                {
                    var points = stroke.GetInkPoints();
                    if (points != null)
                    {
                        foreach (var p in points)
                        {
                            s.Points.Add(new InkDumpPoint
                            {
                                X = p.Position.X,
                                Y = p.Position.Y,
                                Pressure = p.Pressure
                            });
                        }
                    }
                }
                catch
                {
                }

                // Tool label as a hint (best-effort)
                s.Tool = s.DrawAsHighlighter ? "highlighter" : "pen";

                root.Strokes.Add(s);
            }
        }

        return JsonConvert.SerializeObject(root, Formatting.Indented);
    }

    public static void DumpToFile(InkStrokeContainer container, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath)) return;

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(filePath, DumpToJson(container));
    }
#else
    public static string DumpToJson(object _)
        => "{\"error\":\"BRAIN_CARD_ENABLE_WINRT_INK not set\"}";
#endif
}
