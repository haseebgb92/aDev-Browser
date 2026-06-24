using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace DevBrowser.Converters
{
    public class SelectedTabBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isSelected && isSelected)
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFFFF"));
            }
            return new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
