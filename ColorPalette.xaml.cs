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
    /// <summary>
    /// ColorPalette.xaml の相互作用ロジック
    /// </summary>
    public partial class ColorPalette : UserControl
    {
        public ColorPalette()
        {
            InitializeComponent();
        }
        private void ColorButton_Click(object sender, RoutedEventArgs e)
        {
            // Extract the color of the button that was clicked.
            ColorButton clickedColorButton = (ColorButton)sender;
            Color clickedColor = clickedColorButton.Color;

            // Raise an event or set a property to notify the color change
        }
    }
}
