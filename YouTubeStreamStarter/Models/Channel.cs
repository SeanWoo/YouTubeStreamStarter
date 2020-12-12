using System.Collections.Generic;
using System.Net;
using System.Linq;
using Leaf.xNet;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Windows;
using System;
using System.Security.Cryptography;
using System.Text;

namespace YouTubeStreamStarter.Models
{
    public class Channel
    {
        private HttpRequest _request;
        private YouTubeParams _params;

        public bool IsValid { get; private set; }
        public string Name { get; set; }
        public string Description { get; set; }

        public Channel(CookieContainer cookies) 
        {
            _request = new HttpRequest();
            _request.IgnoreProtocolErrors = true;
            _request.Cookies = new CookieStorage(container: cookies);
            _request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36";

            _request[HttpHeader.AcceptLanguage] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";
            _request[HttpHeader.Referer] = "https://www.youtube.com/";

            var systemProxy = WebRequest.GetSystemWebProxy().GetProxy(new Uri("https://google.com")).Authority;
            if (!string.IsNullOrWhiteSpace(systemProxy))
                _request.Proxy = HttpProxyClient.Parse(systemProxy);

            _params.ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[1].ToString();
            _params.userAgent = _request.UserAgent;
        }

        public void Update()
        {
            if (!GetYouTubeSettings()) return;
            if (!GetChannelLink()) return;
            if (!GetChannelData()) return;
        }

        public void SaveChanges()
        {

        }

        public async void UpdateAsync() => await Task.Run(Update);
        public async void SaveChangesAsync() => await Task.Run(SaveChanges);

        private bool GetYouTubeSettings()
        {
            var response = _request.Get("https://www.youtube.com/");
            var isLogged_in = Match(response.ToString(), "\"LOGGED_IN\":(.*?),");
            if (isLogged_in != "true")
            {
                MessageBox.Show("Account is not valid");
                IsValid = false;
                return false;
            }

            _params.INNERTUBE_API_KEY = Match(response.ToString(), "\"INNERTUBE_API_KEY\":\"(.*?)\"");
            _params.hl = Match(response.ToString(), "\"hl\":\"(.*?)\"");
            _params.gl = Match(response.ToString(), "\"gl\":\"(.*?)\"");
            _params.geo = Match(response.ToString(), "\"geo\":\"(.*?)\"");
            _params.clientVersion = Match(response.ToString(), "\"clientVersion\":\"(.*?)\"");
            _params.visitorData = Match(response.ToString(), "\"visitorData\":\"(.*?)\"");

            return true;
        }
        private bool GetChannelLink()
        {
            var response = _request.Get("https://www.youtube.com/channel_switcher");
            _params.channelAddress = Match(response.ToString(), "href=\"https://studio.youtube.com/channel/(.*?)/livestreaming", RegexOptions.Singleline);


            if (string.IsNullOrWhiteSpace(_params.channelAddress))
            {
                IsValid = false;
                return false;
            }
            return true;
        }
        private bool GetChannelData()
        {
            var response = _request.Get("https://studio.youtube.com/channel/UC/livestreaming/manage");
            _params.visitorData = Match(response.ToString(), "\"visitorData\":\"(.*?)\"");
            _params.clientVersion = Match(response.ToString(), "\"clientVersion\":\"(.*?)\"");
            _params.externalChannelId = Match(response.ToString(), "\"externalChannelId\":\"(.*?)\"");
            _params.DELEGATED_SESSION_ID = Match(response.ToString(), "\"DELEGATED_SESSION_ID\":\"(.*?)\"");
            _params.EVENT_ID = Match(response.ToString(), "\"EVENT_ID\":\"(.*?)\"");
            _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT = Match(response.ToString(), "\"INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT\":\"(.*?)\"");

            var unixTimestamp = GetUnixTimestamp();
            var rawHash = unixTimestamp + " " + _request.Cookies.GetCookies("https://google.com").Where(x => x.Name == "SAPISID").Select(x => x.Value).First() + " https://studio.youtube.com";
            _params.SAPISIDHASH = $"SAPISIDHASH {unixTimestamp}_{GetHash(rawHash)}";

            _request.AddHeader(HttpHeader.ContentType, "application/json");
            _request.AddHeader("Authorization", _params.SAPISIDHASH);
            _request.AddHeader("accept", "*/*");
            _request.AddHeader("sec-fetch-dest", "empty");
            _request.AddHeader("sec-fetch-mode", "same-origin");
            _request.AddHeader("sec-fetch-site", "same-origin");
            _request.AddHeader("x-client-data", "CIa2yQEIorbJAQjEtskBCKmdygEIrMfKAQj1x8oBCPjHygEItMvKAQjc1coBCMGcywE=");
            _request.AddHeader("Origin", "https://studio.youtube.com");
            _request.AddHeader("x-origin", "https://studio.youtube.com");

            var content = new StringContent("{'externalChannelId':'" + _params.externalChannelId + "','context':{'client':{'clientName':62,'clientVersion':'1.20201210.01.00','hl':'" + _params.hl + "','gl':'" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_route_delete_playlist_to_outertube','value':'false'},{'key':'force_live_chat_merchandise_upsell','value':'false'}]},'user':{'delegationContext':{'externalChannelId':'" + _params.externalChannelId + "','roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'}},'serializedDelegationContext':'" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce':'" + _params.EVENT_ID + "'}}");
            response = _request.Post("https://studio.youtube.com/youtubei/v1/channel_edit/get_channel_page_settings?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            return true;
        }
        private string Match(string input, string pattern, RegexOptions options)
        {
            var match = Regex.Match(input, pattern, options);
            if (match.Success)
                return match.Groups[match.Groups.Count - 1].Value;
            return null;
        }
        private string Match(string input, string pattern) => Match(input, pattern, RegexOptions.None);
        private string GetHash(string input)
        {
            var hash = "";
            using (var sha = new SHA1Managed())
            {
                var bytes = sha.ComputeHash(Encoding.Default.GetBytes(input));
                var sb = new StringBuilder();

                foreach (byte b in bytes)
                    sb.Append(b.ToString("x2"));

                hash = sb.ToString();
            }
            return hash;
        }
        private int GetUnixTimestamp() => (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        
    }
}
