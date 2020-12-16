using System;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Data;

namespace YouTubeStreamStarter.Models.Converters
{
    public class PrivacyConverter
    {
        public PrivacyVideo Convert(string value)
        {
            var enumType = typeof(PrivacyVideo);
            foreach(var member in enumType.GetMembers())
            {
                var valueAttributes = member.GetCustomAttributes(typeof(EnumMemberAttribute), false);
                var memberValue = ((EnumMemberAttribute)valueAttributes[0]).Value;
                if (value == memberValue)
                    return (PrivacyVideo)Enum.Parse(typeof(PrivacyVideo), member.Name);
            }
            return PrivacyVideo.None;
        }

        public string Convert(PrivacyVideo value)
        {
            var enumType = typeof(PrivacyVideo);
            var memberInfos = enumType.GetMember(value.ToString());
            var enumValueMemberInfo = memberInfos.FirstOrDefault(m => m.DeclaringType == enumType);
            var valueAttributes = enumValueMemberInfo.GetCustomAttributes(typeof(EnumMemberAttribute), false);
            return ((EnumMemberAttribute)valueAttributes[0]).Value;
        }
    }
}
