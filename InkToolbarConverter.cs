using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BrainCard
{
    internal static class InkToolbarConverters
    {
        internal static readonly IValueConverter UwpToWpfStyle = new UwpToWpfStyleConverter();
        internal static readonly IValueConverter UwpToWpfFlowDirection = new UwpToWpfFlowDirectionConverter();
        internal static readonly IValueConverter UwpToWpfThickness = new UwpToWpfThicknessConverter();
        internal static readonly IValueConverter UwpToWpfHorizontalAlignment = new UwpToWpfHorizontalAlignmentConverter();
        internal static readonly IValueConverter UwpToWpfVerticalAlignment = new UwpToWpfVerticalAlignmentConverter();
        internal static readonly IValueConverter NullToUnsetValue = new NullToUnsetValueConverter();
    }

    internal sealed class UwpToWpfStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

#if BRAIN_CARD_DISABLE_XAML_ISLANDS
    internal sealed class UwpToWpfFlowDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal sealed class UwpToWpfThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal sealed class UwpToWpfHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }

    internal sealed class UwpToWpfVerticalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
#else
    internal sealed class UwpToWpfFlowDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Windows.UI.Xaml.FlowDirection uwp)
            {
                return uwp == Windows.UI.Xaml.FlowDirection.RightToLeft
                    ? System.Windows.FlowDirection.RightToLeft
                    : System.Windows.FlowDirection.LeftToRight;
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.FlowDirection wpf)
            {
                return wpf == System.Windows.FlowDirection.RightToLeft
                    ? Windows.UI.Xaml.FlowDirection.RightToLeft
                    : Windows.UI.Xaml.FlowDirection.LeftToRight;
            }

            return Binding.DoNothing;
        }
    }

    internal sealed class UwpToWpfThicknessConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Windows.UI.Xaml.Thickness t)
            {
                return new System.Windows.Thickness(t.Left, t.Top, t.Right, t.Bottom);
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.Thickness t)
            {
                return new Windows.UI.Xaml.Thickness(t.Left, t.Top, t.Right, t.Bottom);
            }

            return Binding.DoNothing;
        }
    }

    internal sealed class UwpToWpfHorizontalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Windows.UI.Xaml.HorizontalAlignment ha)
            {
                return ha switch
                {
                    Windows.UI.Xaml.HorizontalAlignment.Left => System.Windows.HorizontalAlignment.Left,
                    Windows.UI.Xaml.HorizontalAlignment.Center => System.Windows.HorizontalAlignment.Center,
                    Windows.UI.Xaml.HorizontalAlignment.Right => System.Windows.HorizontalAlignment.Right,
                    _ => System.Windows.HorizontalAlignment.Stretch,
                };
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.HorizontalAlignment ha)
            {
                return ha switch
                {
                    System.Windows.HorizontalAlignment.Left => Windows.UI.Xaml.HorizontalAlignment.Left,
                    System.Windows.HorizontalAlignment.Center => Windows.UI.Xaml.HorizontalAlignment.Center,
                    System.Windows.HorizontalAlignment.Right => Windows.UI.Xaml.HorizontalAlignment.Right,
                    _ => Windows.UI.Xaml.HorizontalAlignment.Stretch,
                };
            }

            return Binding.DoNothing;
        }
    }

    internal sealed class UwpToWpfVerticalAlignmentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Windows.UI.Xaml.VerticalAlignment va)
            {
                return va switch
                {
                    Windows.UI.Xaml.VerticalAlignment.Top => System.Windows.VerticalAlignment.Top,
                    Windows.UI.Xaml.VerticalAlignment.Center => System.Windows.VerticalAlignment.Center,
                    Windows.UI.Xaml.VerticalAlignment.Bottom => System.Windows.VerticalAlignment.Bottom,
                    _ => System.Windows.VerticalAlignment.Stretch,
                };
            }

            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Windows.VerticalAlignment va)
            {
                return va switch
                {
                    System.Windows.VerticalAlignment.Top => Windows.UI.Xaml.VerticalAlignment.Top,
                    System.Windows.VerticalAlignment.Center => Windows.UI.Xaml.VerticalAlignment.Center,
                    System.Windows.VerticalAlignment.Bottom => Windows.UI.Xaml.VerticalAlignment.Bottom,
                    _ => Windows.UI.Xaml.VerticalAlignment.Stretch,
                };
            }

            return Binding.DoNothing;
        }
    }
#endif

    internal sealed class NullToUnsetValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value ?? DependencyProperty.UnsetValue;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}
