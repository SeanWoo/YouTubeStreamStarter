using System.Runtime.Serialization;

namespace YouTubeStreamStarter.Models
{
    public enum PrivacyVideo
    {
        [EnumMember(Value = "")]
        None,
        [EnumMember(Value = "VIDEO_PRIVACY_PUBLIC")]
        Public,
        [EnumMember(Value = "VIDEO_PRIVACY_UNLISTED")]
        Unlisted,
        [EnumMember(Value = "VIDEO_PRIVACY_PRIVATE")]
        Private,
    }
}
