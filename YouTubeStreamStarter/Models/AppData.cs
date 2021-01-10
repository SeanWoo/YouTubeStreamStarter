using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace YouTubeStreamStarter.Models
{
    public static class AppData
    {
        public static T GetValue<T>(string path)
        {
            var json = File.ReadAllText(Directory.GetCurrentDirectory() + @"\appsettings.json");
            return JObject.Parse(json).SelectToken(path).ToObject<T>();
        }
        public static KeyValuePair<string, T> GetPair<T>(string path)
        {
            var json = File.ReadAllText(Directory.GetCurrentDirectory() + @"\appsettings.json");
            return JObject.Parse(json).SelectToken(path).ToObject<KeyValuePair<string, T>>();
        }
    }
}
