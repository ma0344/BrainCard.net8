using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BrainCard
{
    public class ColorChangedEventArgs : EventArgs
    {
        public Color SelectedColor { get; }
        public Brush SelectedColorBrush { get; }
        public ColorChangedEventArgs(Color newColor)
        {
            SelectedColor = newColor;
        }
    }
    /// <summary>
    /// ColorPickerButton.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPickerButton : UserControl
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(ColorPickerButton),
                new PropertyMetadata(Colors.Black, OnSelectedColorChanged));

        public Color SelectedColor
        {
            get { return (Color)GetValue(SelectedColorProperty); }
            set { SetValue(SelectedColorProperty, value); }
        }

        public static readonly DependencyProperty SelectedColorBrushProperty =
    DependencyProperty.Register(
        nameof(SelectedColorBrush),
        typeof(Brush),
        typeof(ColorPickerButton),
        new PropertyMetadata(new SolidColorBrush(Colors.Black), OnSelectedColorChanged));

        public Brush SelectedColorBrush
        {
            get
            {
                return new SolidColorBrush(SelectedColor);
            }
            set
            {
                if (value is SolidColorBrush brush)
                {
                    SelectedColor = brush.Color;
                }
            }
        }

        public bool SelectAfterFlyoutClose
        {
            get { return (bool)GetValue(SelectedAuterFlyoutCloseProperty); }
            set { SetValue(SelectedAuterFlyoutCloseProperty, value); }
        }

        public static readonly DependencyProperty SelectedAuterFlyoutCloseProperty =
            DependencyProperty.Register(
                nameof(SelectAfterFlyoutClose),
                typeof(bool),
                typeof(ColorPickerButton),
                new PropertyMetadata(true));

        public event EventHandler<ColorChangedEventArgs> ColorChanged;

        public ColorPickerButton()
        {
            InitializeComponent();
        }

        private static void OnSelectedColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ColorPickerButton colorPickerButton)
            {
                colorPickerButton.OnSelectedColorChanged((Color)e.NewValue);
            }
        }




        private void OnSelectedColorChanged(Color newColor)
        {
            CurrentColorRect.Fill = new SolidColorBrush(newColor);
            ColorChanged?.Invoke(this, new ColorChangedEventArgs(newColor));
        }

        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            ColorButton clickedColorButton = (ColorButton)sender;
            Color clickedColor = clickedColorButton.Color;

            CurrentColorRect.Fill = new SolidColorBrush(clickedColor);

            ColorChanged?.Invoke(this, new ColorChangedEventArgs(clickedColor));
            if (SelectAfterFlyoutClose)
            {
                ColoPickerFlyout.Hide();
            }

            // Raise an event or set a property to notify the color change
        }

    }
}
