using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace BrainCard.Overlay
{
    public partial class StrokeOverlayWindow : Window
    {
        public StrokeOverlayWindow()
        {
            InitializeComponent();
            SizeChanged += (_, __) =>
            {
                RootCanvas.Width = ActualWidth;
                RootCanvas.Height = ActualHeight;
            };
        }

        public void SetStrokes(IReadOnlyList<IReadOnlyList<Point>> strokes, IReadOnlyList<Point> currentStroke, bool includeCurrent)
        {
            RootCanvas.Width = ActualWidth;
            RootCanvas.Height = ActualHeight;
            RootCanvas.Children.Clear();

            if (strokes != null)
            {
                foreach (var s in strokes)
                {
                    AddStroke(s);
                }
            }

            if (includeCurrent && currentStroke != null && currentStroke.Count > 1)
            {
                AddStroke(currentStroke);
            }
        }

        private void AddStroke(IReadOnlyList<Point> dipPoints)
        {
            if (dipPoints == null || dipPoints.Count < 2)
            {
                return;
            }

            var pl = new Polyline
            {
                Stroke = Brushes.Black,
                StrokeThickness = 2.0,
                SnapsToDevicePixels = true,
                StrokeLineJoin = PenLineJoin.Round,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round
            };

            foreach (var p in dipPoints)
            {
                pl.Points.Add(p);
            }

            RootCanvas.Children.Add(pl);
        }

        public void Clear()
        {
            RootCanvas.Children.Clear();
        }
    }
}
