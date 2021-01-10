using YouTubeStreamStarter.Models;
using Xunit;
using System.IO;

namespace Confuguration_Test
{
    public class UnitTest1
    {
        [Fact]
        public void GetValue_Test()
        {
            var result = AppData.GetValue<string>("privacyValues.public.Value");
            Assert.Equal("���������", result);
        }
        [Fact]
        public void GetPair_Test()
        {
            var result = AppData.GetPair<string>("privacyValues.public");
            Assert.Equal("VIDEO_PRIVACY_PUBLIC", result.Key);
            Assert.Equal("���������", result.Value);
        }
    }
}
