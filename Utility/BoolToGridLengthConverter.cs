using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SkyLineSQL.Utility
{
    public class BoolToGridLengthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool isVisible = values[0] is bool b && b;
            Window grid = values[1] as Window;

            var a = isVisible ? new GridLength(800, GridUnitType.Pixel)
                             : new GridLength(0, GridUnitType.Pixel);

            if (isVisible)
            {
                grid.Width += 800;
                grid.Height += 200;
            }
            else
            {
                grid.Width -= 800;
                grid.Height -= 200;
            }

            return a;
        }

        //public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        //{
        //    bool isVisible = value is bool b && b;
        //    return isVisible ? new GridLength(1, GridUnitType.Star)
        //                     : new GridLength(0);
        //}

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
    }
}
