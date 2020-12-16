using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using YouTubeStreamStarter.Models.Converters;

namespace YouTubeStreamStarter.Models
{
    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse); 
    public class Thumbnail
    {
        public string url { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class ThumbnailDetails
    {
        public List<Thumbnail> thumbnails { get; set; }
    }

    public class Metrics
    {
        public string viewCount { get; set; }
        public string commentCount { get; set; }
        public string likeCount { get; set; }
        public string dislikeCount { get; set; }
    }

    public class Permissions
    {
        public List<string> overallPermissions { get; set; }
    }

    public class ResponseStatus
    {
        public string statusCode { get; set; }
    }

    public class StatusDetails
    {
        public string detailFailed { get; set; }
    }

    public class CreativeCommonsLicense
    {
        public string status { get; set; }
    }

    public class RightsManagement
    {
        public string status { get; set; }
    }

    public class Features
    {
        public CreativeCommonsLicense creativeCommonsLicense { get; set; }
        public RightsManagement rightsManagement { get; set; }
    }

    public class UploadFailedState
    {
        public string detail { get; set; }
    }

    public class UploadFailedStatusDetail
    {
        public string detail { get; set; }
    }

    public class UserConfig
    {
        public string privacy { get; set; }
    }

    public class Visibility
    {
        public UploadFailedState uploadFailedState { get; set; }
        public string effectiveStatus { get; set; }
        public UploadFailedStatusDetail uploadFailedStatusDetail { get; set; }
        public UserConfig userConfig { get; set; }
    }

    public class SponsorsOnly
    {
        public bool isSponsorsOnly { get; set; }
    }

    public class AudienceRestriction
    {
        public string selfRating { get; set; }
        public string systemRating { get; set; }
        public bool overrideEnabled { get; set; }
        public string effectiveRating { get; set; }
        public string imposer { get; set; }
    }

    public class NonMonetizingRestrictions
    {
    }

    public class VideoResolutions
    {
        public string statusSd { get; set; }
        public string statusHd { get; set; }
        public string status4k { get; set; }
    }

    public class PrivateShare
    {
    }

    public class LoggingDirectives
    {
        public string trackingParams { get; set; }
    }

    public class VideoModel
    {
        public string videoId { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public PrivacyVideo privacy { get; set; }
        public string channelId { get; set; }
        public string timePublishedSeconds { get; set; }
        public ThumbnailDetails thumbnailDetails { get; set; }
        public Metrics metrics { get; set; }
        public string draftStatus { get; set; }
        public Permissions permissions { get; set; }
        public string lengthSeconds { get; set; }
        public string timeCreatedSeconds { get; set; }
        public string status { get; set; }
        public ResponseStatus responseStatus { get; set; }
        public StatusDetails statusDetails { get; set; }
        public Features features { get; set; }
        public string downloadUrl { get; set; }
        public string watchUrl { get; set; }
        public Visibility visibility { get; set; }
        public string origin { get; set; }
        public string inlineEditProcessingStatus { get; set; }
        public SponsorsOnly sponsorsOnly { get; set; }
        public bool unlistedExpired { get; set; }
        public AudienceRestriction audienceRestriction { get; set; }
        public NonMonetizingRestrictions nonMonetizingRestrictions { get; set; }
        public VideoResolutions videoResolutions { get; set; }
        public PrivateShare privateShare { get; set; }
        public LoggingDirectives loggingDirectives { get; set; }
    }


}
