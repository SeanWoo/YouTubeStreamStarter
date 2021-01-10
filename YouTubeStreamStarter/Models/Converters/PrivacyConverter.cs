using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace YouTubeStreamStarter.Models.Converters
{
    public static class PrivacyConverter
    {
        public static PrivacyVideo Convert(string value)
        {
            return (PrivacyVideo)Enum.Parse(typeof(PrivacyVideo), value);
        }

        public static string Convert(PrivacyVideo value)
        {
            var enumType = typeof(PrivacyVideo);
            var memberInfos = enumType.GetMember(value.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false);
            return ((EnumMemberAttribute)valueAttributes[0]).Value;
        }
    }
}
