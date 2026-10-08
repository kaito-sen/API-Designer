using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using API_Integarated.Core.Models;

namespace API_Integarated.UI.Converters
{
    public class MethodToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ApiMethod method)
            {
                return method switch
                {
                    ApiMethod.GET => new SolidColorBrush(Color.FromRgb(46, 125, 50)),      // Green #2E7D32
                    ApiMethod.POST => new SolidColorBrush(Color.FromRgb(230, 81, 0)),     // Orange #E65100
                    ApiMethod.PUT => new SolidColorBrush(Color.FromRgb(21, 101, 192)),     // Blue #1565C0
                    ApiMethod.DELETE => new SolidColorBrush(Color.FromRgb(198, 40, 40)),   // Red #C62828
                    ApiMethod.PATCH => new SolidColorBrush(Color.FromRgb(106, 27, 154)),   // Purple #6A1B9A
                    ApiMethod.HEAD => new SolidColorBrush(Color.FromRgb(69, 90, 100)),     // Slate #455A64
                    ApiMethod.OPTIONS => new SolidColorBrush(Color.FromRgb(97, 97, 97)),   // Grey #616161
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class StatusCodeToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int code)
            {
                if (code >= 200 && code < 300)
                    return new SolidColorBrush(Color.FromRgb(46, 125, 50)); // Success green
                if (code >= 300 && code < 400)
                    return new SolidColorBrush(Color.FromRgb(2, 119, 189)); // Redirect blue
                if (code >= 400 && code < 500)
                    return new SolidColorBrush(Color.FromRgb(230, 81, 0));  // Client error orange
                if (code >= 500)
                    return new SolidColorBrush(Color.FromRgb(198, 40, 40));  // Server error red
            }
            return new SolidColorBrush(Color.FromRgb(198, 40, 40));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class EqualityToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return Visibility.Visible;
            if (value == null || parameter == null) return Visibility.Collapsed;

            var valStr = value.ToString();
            var paramStr = parameter.ToString();

            return string.Equals(valStr, paramStr, StringComparison.OrdinalIgnoreCase)
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class InverseBooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b)
                return b ? Visibility.Collapsed : Visibility.Visible;
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
    }

    public class EqualityToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null && parameter == null) return true;
            if (value == null || parameter == null) return false;

            var valStr = value.ToString();
            var paramStr = parameter.ToString();

            return string.Equals(valStr, paramStr, StringComparison.OrdinalIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool b && b && parameter != null)
            {
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, parameter.ToString()!, true);
                }
                return parameter;
            }
            return Binding.DoNothing;
        }
    }
}
