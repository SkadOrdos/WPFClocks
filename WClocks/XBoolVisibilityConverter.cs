using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace WClocks
{
    public class XBoolVisibilityConverter : IValueConverter
    {
        public XBoolVisibilityConverter()
        {
        }

        public Visibility Convert(bool value)
        {
            return (Visibility)Convert(value, typeof(Visibility), null, System.Globalization.CultureInfo.InvariantCulture);
        }


        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return System.Convert.ToBoolean(value) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
