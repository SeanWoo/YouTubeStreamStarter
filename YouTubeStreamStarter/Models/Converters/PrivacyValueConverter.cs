using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace YouTubeStreamStarter.Models.Converters
{
    [ValueConversion(typeof(PrivacyVideo), typeof(string))]
    public class PrivacyValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return new string[] { };
            if (value is string)
                return "asd";
            return new string[] { PrivacyConverter.Convert((PrivacyVideo)value) };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return PrivacyConverter.Convert((string)value);
        }

    }
}
