using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MoeLoaderDelta
{
    public sealed class NullToCollapsedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type type = value.GetType();
            if (type == typeof(string))
            {
                return ((string)value).IsNullOrEmptyOrWhiteSpace() ? Visibility.Collapsed : Visibility.Visible;
            }
            else if (type == typeof(bool))
            {
                return ((bool)value) == true ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (type == typeof(bool?))
            {
                return ((bool?)value) == true ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (type == typeof(int?))
            {
                return ((int?)value) == null ? Visibility.Collapsed : Visibility.Visible;
            }
            return value.Equals(null) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
