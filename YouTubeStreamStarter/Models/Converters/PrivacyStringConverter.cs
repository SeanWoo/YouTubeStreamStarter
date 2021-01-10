using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;

namespace YouTubeStreamStarter.Models.Converters
{
    [ValueConversion(typeof(string), typeof(string))]
    public class PrivacyStringConverter : IValueConverter
    {
        private readonly KeyValuePair<string, string>[] pairs = new KeyValuePair<string, string>[]{
            AppData.GetPair<string>("privacyValues.public"),
            AppData.GetPair<string>("privacyValues.unlisted"),
            AppData.GetPair<string>("privacyValues.private")
        };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string localResult = value as string;
            foreach (var pair in pairs)
                if (pair.Key == localResult)
                    return pair.Value;
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string localResult = value as string;
            foreach (var pair in pairs)
                if (pair.Value == localResult)
                    return pair.Key;
            
            return null;
        }

    }
}
