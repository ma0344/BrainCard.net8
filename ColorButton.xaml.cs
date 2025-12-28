using ModernWpf.Controls;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;
using Windows.UI.ViewManagement;
// TODO PenColorButtonのコントロール化
namespace BrainCard
{
    public partial class ColorButton : ToggleButton
    {

        public ColorButton()
        {
            InitializeComponent();
            this.Click += ColorButton_Click;
            this.Checked += ColorButton_Checked;

            AccentColorBrush = new SolidColorBrush(GetSystemAccentColor());
        }

        // Colorプロパティの定義
        public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set
            {
                SetValue(ColorProperty, value);
            }
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(ColorButton),
                new PropertyMetadata(Colors.Black, new PropertyChangedCallback(OnColorChanged)));
        public static readonly DependencyProperty ColorBrushProperty =
            DependencyProperty.Register("ColorBrush", typeof(Brush), typeof(ColorButton),
                new PropertyMetadata(new SolidColorBrush(Colors.Black)));

        public Brush ColorBrush
        {
            get { return (Brush)GetValue(ColorBrushProperty); }
            private set { SetValue(ColorBrushProperty, value); }
        }



        public Brush AccentColorBrush
        { get; private set; }

        private void ColorButton_Checked(object sender, RoutedEventArgs e)
        {
            var parent = VisualTreeHelper.GetParent(this) as Panel;
            if (parent != null)
            {
                foreach (var child in parent.Children)
                {
                    if (child is ColorButton button && button != this)
                    {
                        button.IsChecked = false;
                    }
                }
            }
        }


        private static void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // 必要に応じてクリック時の処理を追加
        }

        private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ColorButton button = d as ColorButton;
            if (button != null)
            {
                Color newColor = (Color)e.NewValue;
                button.ColorBrush = new SolidColorBrush(newColor);
            }
        }
        private Color GetSystemAccentColor()
        {
            var uiSettings = new UISettings();
            var accentColor = uiSettings.GetColorValue(UIColorType.Accent);
            return Color.FromArgb(accentColor.A, accentColor.R, accentColor.G, accentColor.B);
        }

    }
}
