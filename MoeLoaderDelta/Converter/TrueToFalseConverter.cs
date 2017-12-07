using System;
using System.Globalization;
using System.Windows.Data;

namespace MoeLoaderDelta
{
    /// <summary>
    /// 這是一個顛倒黑白的世界
    /// </summary>
    public sealed class TrueToFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var v = (bool)value;
            return !v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
