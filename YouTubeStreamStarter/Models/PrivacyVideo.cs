using System.Runtime.Serialization;

namespace YouTubeStreamStarter.Models
{
    public enum PrivacyVideo : byte
    {
        None = 0,
        [EnumMember(Value = "VIDEO_PRIVACY_PUBLIC")]
        Public = 1,
        [EnumMember(Value = "VIDEO_PRIVACY_UNLISTED")]
        Unlisted = 2,
        [EnumMember(Value = "VIDEO_PRIVACY_PRIVATE")]
        Private = 3,
    }
}
