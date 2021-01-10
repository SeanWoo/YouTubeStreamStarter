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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;

namespace YouTubeStreamStarter.Models
{
    public class Channel : INotifyPropertyChanged
    {
        private HttpRequest _request;
        private YouTubeParams _params;
        private bool _isSaved = true;
        private bool _differentAvatars = false;
        private bool _differentBanners = false;
        private readonly Uri _clientAvatar = new Uri(Directory.GetCurrentDirectory() + @"\Images\clientAvatar.jpg");
        private readonly Uri _serverAvatar = new Uri(Directory.GetCurrentDirectory() + @"\Images\serverAvatar.jpg");
        private readonly Uri _clientBanner = new Uri(Directory.GetCurrentDirectory() + @"\Images\clientBanner.jpg");
        private readonly Uri _serverBanner = new Uri(Directory.GetCurrentDirectory() + @"\Images\serverBanner.jpg");
        private string _clientName;
        private string _serverName;
        private string _clientDescription;
        private string _serverDescription;
        private ObservableCollection<VideoModel> _clientVideos;
        private ObservableCollection<VideoModel> _serverVideos;

        public bool IsValid { get; private set; }
        public string Address { get => "https://www.youtube.com/channel/" + _params.channelAddress; }
        public bool IsSaved { 
            get => _isSaved; 
            private set
            {
                _isSaved = value;
                OnPropertyChanged();
            }
        }
        public ObservableCollection<VideoModel> Videos
        {
            get => _clientVideos;
            private set
            {
                _clientVideos = value;
                CheckIsSaved();
                OnPropertyChanged();
            }
        }

        public BitmapImage Avatar
        {
            get
            {
                CheckIsSaved();
                return LoadBitmapImage(_clientAvatar.LocalPath);
            }
        }
        public BitmapImage Banner
        {
            get
            {
                CheckIsSaved();
                return LoadBitmapImage(_clientBanner.LocalPath);
            }
        }
        public string Name
        {
            get => _clientName;
            set
            {
                _clientName = value;
                CheckIsSaved();
                OnPropertyChanged();
            }
        }
        public string Description
        {
            get => _clientDescription;
            set
            {
                _clientDescription = value;
                CheckIsSaved();
                OnPropertyChanged();
            }
        }

        public Channel(CookieContainer cookies)
        {
            _request = new HttpRequest();
            _request.IgnoreProtocolErrors = true;
            _request.Cookies = new CookieStorage(container: cookies);
            _request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/86.0.4240.111 Safari/537.36";

            _request[HttpHeader.Accept] = "*/*";
            _request[HttpHeader.AcceptLanguage] = "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3";
            _request[HttpHeader.Referer] = "https://www.youtube.com/";

            var systemProxy = WebRequest.GetSystemWebProxy().GetProxy(new Uri("https://google.com"));
            if (systemProxy != null)
                _request.Proxy = HttpProxyClient.Parse(systemProxy.Authority);

            _params.ip = Dns.GetHostByName(Dns.GetHostName()).AddressList[1].ToString();
            _params.userAgent = _request.UserAgent;
        }
        public Channel(CookieContainer cookies, string proxy) : this(cookies)
        {
            _request.Proxy = HttpProxyClient.Parse(proxy);
        }

        public void Update()
        {
            if (!GetYouTubeSettings()) return;
            if (!GetYouTubeAPISettings()) return;
            if (!GetChannelLink()) return;
            if (!GetChannelData()) return;

            GenerateSAPSIDHASH("https://studio.youtube.com");

            if (!GetSessionToken()) return;
            if (!GetVideos()) return;

            IsValid = true;
        }

        public void SaveChanges()
        {
            if (IsSaved) return;

            if (!SaveChangesChannelData()) return;
            if (!SaveChangesVideosAsync()) return;
        }

        public async Task UpdateAsync() => await Task.Run(Update);
        public async Task SaveChangesAsync() => await Task.Run(SaveChanges);

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
            var response = _request.Get("https://www.youtube.com/channel/" + _params.channelAddress + "/about");
            _serverName = Match(response.ToString(), "{\"channelMetadataRenderer\":{\"title\":\"(.*?)\"");
            _serverDescription = Match(response.ToString(), "{\"channelMetadataRenderer\":{\"title\":\".*?\",\"description\":\"(.*?)\"");
            var urlAvatars = JsonConvert.DeserializeObject<List<YouTubeImage>>("[" + Match(response.ToString(), "{\"channelMetadataRenderer\":{.*?\"avatar\":{\"thumbnails\":\\[(.*?)\\]") + "]");
            var urlAvatar = urlAvatars.Last().url;

            var urlBanners = JsonConvert.DeserializeObject<List<YouTubeImage>>("[" + Match(response.ToString(), "\"banner\":{\"thumbnails\":\\[(.*?)\\]") + "]");
            var urlBanner = urlBanners.Last().url;

            if (!string.IsNullOrWhiteSpace(urlAvatar))
            {
                response = _request.Get(urlAvatar);
                if (response.IsOK)
                {
                    var bytes = response.ToBytes();
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "/Images/serverAvatar.jpg", bytes);
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "/Images/clientAvatar.jpg", bytes);
                }
                OnPropertyChanged("Avatar");
            }
            if (!string.IsNullOrWhiteSpace(urlBanner))
            {
                response = _request.Get(urlBanner);
                if (response.IsOK)
                {
                    var bytes = response.ToBytes();
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "/Images/serverBanner.jpg", bytes);
                    File.WriteAllBytes(Directory.GetCurrentDirectory() + "/Images/clientBanner.jpg", bytes);
                }
                OnPropertyChanged("Banner");
            }
            if (string.IsNullOrWhiteSpace(_clientName))
                _clientName = _serverName;
            if (string.IsNullOrWhiteSpace(_clientDescription))
                _clientDescription = _serverDescription;
            return true;
        }
        private bool GetYouTubeAPISettings()
        {
            var response = _request.Get("https://studio.youtube.com/");
            _params.visitorData = Match(response.ToString(), "\"visitorData\":\"(.*?)\"");
            _params.clientName = Match(response.ToString(), "\"clientName\":(\\d+)") ?? "62";
            _params.clientVersion = Match(response.ToString(), "\"clientVersion\":\"(.*?)\"");
            _params.externalChannelId = Match(response.ToString(), "\"externalChannelId\":\"(.*?)\"");
            _params.onBehalfOfUser = Match(response.ToString(), "\"DELEGATED_SESSION_ID\":\"(.*?)\"");
            _params.EVENT_ID = Match(response.ToString(), "\"EVENT_ID\":\"(.*?)\"");
            _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT = Match(response.ToString(), "\"INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT\":\"(.*?)\"");
            return true;
        }
        private bool GetSessionToken()
        {
            AddAPIHeaders();
            var content = new StringContent("{'engagementType':'ENGAGEMENT_TYPE_CREATOR_STUDIO_ACTION','ids':[{'externalChannelId':'" + _params.externalChannelId + "'}], 'context':{ 'client':{'clientName':'" + _params.clientName + "','clientVersion': '" + _params.clientVersion + "','hl': '" + _params.hl + "','gl': '" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_route_delete_playlist_to_outertube','value':'false'},{'key':'force_live_chat_merchandise_upsell','value':'false'}]},'user':{'delegationContext':{'roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'},'externalChannelId': '" + _params.externalChannelId + "'},'serializedDelegationContext': '" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce': '" + _params.EVENT_ID + "'}}");
            var response = _request.Post("https://studio.youtube.com/youtubei/v1/att/get?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            _params.challenge = Match(response.ToString(), "\"challenge\": \"(.*?)\"");
            _params.botguardData = Match(response.ToString(), "\"program\": \"(.*?)\"");

            var inter = Match(response.ToString(), "\"interpreterUrl\": \"(.*?)\"");
            
            _params.botguardData = _params.botguardData.Substring(3);
            _params.botguardData = _params.botguardData.Substring(0, _params.botguardData.LastIndexOf("//"));
            AddAPIHeaders();

            content = new StringContent("{'challenge':'a=5&a2=3&b=y76lp5V5SBoPJtIoAngoNNQFzHs&c=1608416431&d=62&e3=UC-6PU5Yrqd-RIv9tmCvNeDg&c1a=1&hh=itTvXg4nds0hbiVAm3nSgNAklGQNKWOzyTgJX5hG6iQ','botguardResponse':'!CgmlCSDNAAXj0ixo40JlW7Gzehm0s1ghvbX4tvxQLQIAAAA_UgAAACloAQcKAReGNWzximOR3H7aWhqcwt2rL8RSzt6XS97KiqTKwnHbHzTJ2Fe062oeXSqmwZMHz4vbTO9spai7u5sEhREhjC0VLjonVIFY1ydjA0Mh6cEjGUUvf5sN3pxK59CqFjobWaIeAtWWXU8PmROioQJ7Qsz4_HFh8l3xfmg9DoLX4n53MHdeS3bbPV3aFmd1aUqGZYTvvQZsIOhQloJxA3oLWjQumiMwrejkgyO0sozIm1SQHPRvBogZhdVVYlSLuTv3F_WCvJkV50T0BmHMQEwMAJIw7jb3bAICYPGA4JcUFPsekzU2ttlIQ0DFUX2-D5LPQswQtjTnDpPeS_cOOfIdze4rtWoJ8huvPX8Fs2h_ZWUNHegCCFlFYieZApoaSMXNW4xOXeFAh1OBTiqnowH6lhFmdbknUZOBawoAE49j5KAG81QP7KFAjqnKXPS1Jpa7KMMlW2KYhB7HYvCDuDkVtu3kgXtblWiS0IEBuLrAZ3RlUpug3tyOZxY5BEMSHuN_Kg9w548q9qVzk8NDN7QgEMf4lxABeprgto4_tw3mllbrJI2xEhGL6XrFmjSXYiW1KDgJP01gAgmVCw9gbFiaZIN10C3VUl7ebkwMtXYfZCe0iNZG3vfy_LQmEM2FMvRmSk32Uso-ApC8YJdFL7dRHcLZAgpMkacQ0OiEv1s9W01Ehclgh6snzNMqeBAbB06XB8eGUyrmBbPyMvjHQALlwjDeztnj3-a_94FuPHN8tgz5se_fyi-Jl_BD1A7wpzONUKmIdlMO4PoCst_YGVnlHWUsdgH__mMFtb_71cbZuzaCnkTjmIkN-RAO5WrwKua2SzyhLHI9ISkgjdg9L-O4taE61-KCFL3kwu9k3XdEUBCC1nddelsST2VNmYpeBZp2jIN31bG0ElMUD4kbGnaPXHiPCjJBsBrOQz1qI9IiI4OWP0wAfvq9cKymYgJyZJ899H0ylNM0kO-44XplaGScnpxLGV0JYlLWoTBjYe2com7jnrtz5KNEwheCYfYiwHtaa9HFCjzPAAy4vdueUW6OUaUFz3xzkcpeZ1lApDci_7XpV9X7UMI3f3-sJcoMrz-1x-9e_Q2Ydi1E7cAsM8rYrhVmmdV5DhdiTGOc__d2TpzGTZ5V9_dGu21AhUzarKgpbFIXXI17t-sy7kNVSU9mRUA-JSMaIsInrCURkL7SAjTj6e79MHh1ZnjgWsBK_s6231dgVan4mS-Px_usmPKogDoto4P7zkGJUo01WLzMQeL_l47lLIk','xguardClientStatus':0, 'context':{'client':{'clientName':'" + _params.clientName + "','clientVersion': '" + _params.clientVersion + "','hl': '" + _params.hl + "','gl': '" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_route_delete_playlist_to_outertube','value':'false'},{'key':'force_live_chat_merchandise_upsell','value':'false'}]},'user':{'delegationContext':{'roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'},'externalChannelId': '" + _params.externalChannelId + "'},'serializedDelegationContext': '" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce': '" + _params.EVENT_ID + "'}}");
            response = _request.Post("https://studio.youtube.com/youtubei/v1/att/esr?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            _params.sessionToken = Match(response.ToString(), "\"sessionToken\": \"(.*?)\"");
            return _params.sessionToken is null ? false : true;
        }
        private bool GetVideos()
        {
            AddAPIHeaders();
            var content = new StringContent("{'filter':{'and':{'operands':[{'channelIdIs':{'value':'" + _params.channelAddress + "'}},{'videoOriginIs':{'value':'VIDEO_ORIGIN_UPLOAD'}}]}},'order':'VIDEO_ORDER_DISPLAY_TIME_DESC','pageSize':30,'mask':{'channelId':true,'videoId':true,'lengthSeconds':true,'premiere':{'all':true},'status':true,'thumbnailDetails':{'all':true},'title':true,'draftStatus':true,'downloadUrl':true,'watchUrl':true,'permissions':{'all':true},'timeCreatedSeconds':true,'timePublishedSeconds':true,'origin':true,'livestream':{'all':true},'privacy':true,'features':{'all':true},'responseStatus':{'all':true},'statusDetails':{'all':true},'description':true,'metrics':{'all':true},'titleFormattedString':{'all':true},'descriptionFormattedString':{'all':true},'audienceRestriction':{'all':true},'inlineEditProcessingStatus':true,'nonMonetizingRestrictions':{'all':true},'videoResolutions':{'all':true},'scheduledPublishingDetails':{'all':true},'visibility':{'all':true},'privateShare':{'all':true},'sponsorsOnly':{'all':true},'unlistedExpired':true},'context':{'client':{'clientName':" + _params.clientName + ",'clientVersion':'" + _params.clientVersion + "','hl':'" + _params.hl + "','gl':'" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_live_chat_merchandise_upsell','value':'false'},{'key':'force_route_delete_playlist_to_outertube','value':'false'}],'sessionInfo':{'token':'" + _params.sessionToken + "'}},'user':{'onBehalfOfUser':'" + _params.onBehalfOfUser + "', 'delegationContext':{'externalChannelId':'" + _params.externalChannelId + "','roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'}},'serializedDelegationContext':'" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce':'" + _params.EVENT_ID + "'}}");
            var response = _request.Post("https://studio.youtube.com/youtubei/v1/creator/list_creator_videos?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            _serverVideos = JsonConvert.DeserializeObject<ObservableCollection<VideoModel>>("[" + Match(response.ToString(), "\"videos\": \\[(.*?)\\],...\"videosTotalSize", RegexOptions.Singleline) + "]");
            
            if(_clientVideos == null || _clientVideos.Count == 0)
                _clientVideos = JsonConvert.DeserializeObject<ObservableCollection<VideoModel>>("[" + Match(response.ToString(), "\"videos\": \\[(.*?)\\],...\"videosTotalSize", RegexOptions.Singleline) + "]");

            foreach (var item in _clientVideos)
                item.PropertyChanged += VideoModelHandler;
            
            return true;
        }

        private bool SaveChangesVideosAsync()
        {
            var publicKey = AppData.GetPair<string>("privacyUpdateValues.public");
            var unlistedKey = AppData.GetPair<string>("privacyUpdateValues.unlisted");
            var privateKey = AppData.GetPair<string>("privacyUpdateValues.private");

            var dict = new Dictionary<string, List<string>>()
            {
                [publicKey.Value] = new List<string>(),
                [unlistedKey.Value] = new List<string>(),
                [privateKey.Value] = new List<string>(),
            };
            for (int i = 0; i < _clientVideos.Count; i++)
            {
                if(_clientVideos[i].privacy != _serverVideos[i].privacy)
                {
                    if (_clientVideos[i].privacy == publicKey.Key)
                        dict[publicKey.Value].Add(_clientVideos[i].videoId);
                    if (_clientVideos[i].privacy == unlistedKey.Key)
                        dict[unlistedKey.Value].Add(_clientVideos[i].videoId);
                    if (_clientVideos[i].privacy == privateKey.Key)
                        dict[privateKey.Value].Add(_clientVideos[i].videoId);
                }
            }

            foreach (var item in dict.Where(x => x.Value.Count > 0))
            {
                AddAPIHeaders();
                var content = new StringContent("{'channelId':'" + _params.channelAddress + "','videoUpdate':{'privacyState':{'privacy':'" + "VIDEO_UPDATE_PRIVACY_SETTING_PUBLIC" + "'}},'videos':{'videoIds':[\"ghW4hIxilkA\"]},'context':{'client':{'clientName':" + _params.clientName + ",'clientVersion':'" + _params.clientVersion + "','hl':'" + _params.hl + "','gl':'" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_live_chat_merchandise_upsell','value':'false'},{'key':'force_route_delete_playlist_to_outertube','value':'false'}],'sessionInfo':{'token':'" + _params.sessionToken + "'}},'user':{'onBehalfOfUser':'" + _params.onBehalfOfUser + "', 'delegationContext':{'externalChannelId':'" + _params.externalChannelId + "','roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'}},'serializedDelegationContext':'" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce':'" + _params.EVENT_ID + "'}}");
                _request.Post("https://studio.youtube.com/youtubei/v1/creator/enqueue_creator_bulk_action?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            }
            GetVideos();
            return true;
        }
        private bool SaveChangesChannelData()
        {
            var strContentBegin = "{'externalChannelId':'" + _params.externalChannelId + "',";
            var strContentEnd = "'context':{'client':{'clientName':" + _params.clientName + ",'clientVersion':'" + _params.clientVersion + "','hl':'" + _params.hl + "','gl':'" + _params.gl + "','experimentsToken':'','utcOffsetMinutes':240},'request':{'returnLogEntry':true,'internalExperimentFlags':[{'key':'force_live_chat_merchandise_upsell','value':'false'},{'key':'force_route_delete_playlist_to_outertube','value':'false'}],'sessionInfo':{'token':'" + _params.sessionToken + "'}},'user':{'onBehalfOfUser':'" + _params.onBehalfOfUser + "', 'delegationContext':{'externalChannelId':'" + _params.externalChannelId + "','roleType':{'channelRoleType':'CREATOR_CHANNEL_ROLE_TYPE_OWNER'}},'serializedDelegationContext':'" + _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT + "'},'clientScreenNonce':'" + _params.EVENT_ID + "'}}";

            var strContentName = "'titleUpdate':{'personalTitle':{'givenName':'" + Name + "','familyName':'','translations':{'original':'" + Name + "','originalLanguage':'','messages':[]}}},";
            var strContentDescription = "'descriptionUpdate':{'description':{'original':'" + Description + "','originalLanguage':'','messages':[]}},";
            var strContentAvatar = "'avatarImageUpdate':";
            var strContentBanner = "'bannerImageUpdate':";

            var strContent = strContentBegin;
            if (_clientName != _serverName)
                strContent += strContentName;
            if (_clientDescription != _serverDescription)
                strContent += strContentDescription;
            if (_differentAvatars)
            {
                var imageID = UploadImage(_clientAvatar.LocalPath);
                strContent += strContentAvatar + imageID + ",";
            }
            if (_differentBanners)
            {
                var imageID = UploadImage(_clientBanner.LocalPath);
                strContent += strContentBanner + imageID + ",";
            }
            if (strContent != strContentBegin)
            {
                strContent += strContentEnd;

                AddAPIHeaders();
                var content = new StringContent(strContent);
                _request.Post("https://studio.youtube.com/youtubei/v1/channel_edit/update_channel_page_settings?alt=json&key=" + _params.INNERTUBE_API_KEY, content);
            }
            GetChannelData();
            CheckIsSaved();
            return true;
        }

        private void CheckIsSaved()
        {
            if (_clientName != _serverName || _clientDescription != _serverDescription)
            {
                IsSaved = false;
                return;
            }
            var a1 = GetBytesFromImage(_clientAvatar.LocalPath);
            var a2 = GetBytesFromImage(_serverAvatar.LocalPath);

            if (!a1.AsSpan().SequenceEqual(a2))
            {
                IsSaved = false;
                _differentAvatars = true;
                return;
            }
            else
            { 
                _differentAvatars = false; 
            }


            var b1 = GetBytesFromImage(_clientBanner.LocalPath);
            var b2 = GetBytesFromImage(_serverBanner.LocalPath);

            if (!b1.AsSpan().SequenceEqual(b2))
            {
                IsSaved = false;
                return;
            }

            if (!_serverVideos.SequenceEqual(_clientVideos))
            {
                IsSaved = false;
                _differentBanners = true;
                return;
            }
            else
            {
                _differentBanners = false;
            }

            IsSaved = true;
        }

        private string UploadImage(string pathToImage)
        {
            var content = new FileContent(pathToImage);

            //AddAPIHeaders();
            _request.AddHeader("sec-fetch-dest", "empty");
            _request.AddHeader("sec-fetch-mode", "cors");
            _request.AddHeader("sec-fetch-site", "same-site");
            _request.AddHeader("Referer", "https://studio.youtube.com/channel/" + _params.channelAddress + "/editing/images");
            _request.AddHeader("access-control-request-headers", "x-goog-upload-command,x-goog-upload-header-content-length,x-goog-upload-protocol,x-youtube-channelid");
            _request.AddHeader("access-control-request-method", "POST");
            _request.AddHeader("X-Client-Data", "CIa2yQEIorbJAQjEtskBCKmdygEIrMfKAQj1x8oBCPjHygEItMvKAQjc1coBCMGcywE=");
            _request.AddHeader("X-Goog-Upload-Command", "start");
            _request.AddHeader("X-Goog-Upload-Header-Content-Length", content.CalculateContentLength().ToString());
            _request.AddHeader("X-Goog-Upload-Protocol", "resumable");
            _request.AddHeader("X-YouTube-ChannelId", _params.channelAddress);

            string upload_id = "";
            var enumer = _request.Post("https://www.youtube.com/channel_image_upload/profile").EnumerateHeaders();
            do
            {
                if (enumer.Current.Key == "X-GUploader-UploadID")
                {
                    upload_id = enumer.Current.Value;
                    break;
                }
            }
            while (enumer.MoveNext());

            //AddAPIHeaders();
            _request.AddHeader("sec-fetch-dest", "empty");
            _request.AddHeader("sec-fetch-mode", "cors");
            _request.AddHeader("sec-fetch-site", "same-site");
            _request.AddHeader("Referer", "https://studio.youtube.com/channel/" + _params.channelAddress + "/editing/images");
            _request.AddHeader("X-Client-Data", "CIa2yQEIorbJAQjEtskBCKmdygEIrMfKAQj1x8oBCPjHygEItMvKAQjc1coBCMGcywE=");
            _request.AddHeader("X-Goog-Upload-Command", "upload, finalize");
            _request.AddHeader("X-Goog-Upload-Offset", "0");
            _request.AddHeader("X-YouTube-ChannelId", _params.channelAddress);
            var result = _request.Post("https://www.youtube.com/channel_image_upload/profile?upload_id=" + upload_id + "&upload_protocol=resumable", content).ToString();

            return result;
        }
        private void GenerateSAPSIDHASH(string referer)
        {
            var unixTimestamp = GetUnixTimestamp();
            var rawHash = unixTimestamp + " " + _request.Cookies.GetCookies("https://google.com").Where(x => x.Name == "SAPISID").Select(x => x.Value).First() + " " + referer;
            _params.SAPISIDHASH = $"SAPISIDHASH {unixTimestamp}_{GetHash(rawHash)}";
        }
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
        private void AddAPIHeaders()
        {
            _request.AddHeader(HttpHeader.ContentType, "application/json");
            _request.AddHeader("Authorization", _params.SAPISIDHASH);
            _request.AddHeader("accept", "*/*");
            _request.AddHeader("sec-fetch-dest", "empty");
            _request.AddHeader("sec-fetch-mode", "same-origin");
            _request.AddHeader("sec-fetch-site", "same-origin");
            _request.AddHeader("x-client-data", "CIa2yQEIorbJAQjEtskBCKmdygEIrMfKAQj1x8oBCPjHygEItMvKAQijzcoBCNzVygEIk5rLAQjRmssBCMGcywEI1JzLAQ==");
            _request.AddHeader("Origin", "https://studio.youtube.com");
            _request.AddHeader("x-origin", "https://studio.youtube.com");
            //_request.AddHeader("sec-ch-ua", "Google Chrome\";v=\"87\", \" Not;A Brand\";v=\"99\", \"Chromium\";v=\"87\"");
            //_request.AddHeader("sec-ch-ua-mobile", "?0");
            _request.AddHeader("X-YouTube-Delegation-Context", _params.INNERTUBE_CONTEXT_SERIALIZED_DELEGATION_CONTEXT);
            _request.AddHeader("X-YouTube-Client-Name", _params.clientName);
            _request.AddHeader("X-YouTube-Client-Version", _params.clientVersion);
            //_request.AddHeader("X-Goog-AuthUser", "0");
            //_request.AddHeader("X-YouTube-Page-CL", "347838303");
            //_request.AddHeader("X-YouTube-Page-Label", "youtube.studio.web_20201216_03_RC01");
            //_request.AddHeader("X-Goog-Visitor-Id", "CgtwM3FrV1Y2THZkMCic8vn-BQ%3D%3D");
            //_request.AddHeader("X-YouTube-Time-Zone", "Europe/Saratov");
            //_request.AddHeader("X-YouTube-Ad-Signals", "dt=1608415514732&flash=0&frm&u_tz=240&u_his=2&u_java&u_h=1080&u_w=1920&u_ah=1080&u_aw=1858&u_cd=24&u_nplug=3&u_nmime=4&bc=31&bih=1009&biw=1858&brdim=62%2C0%2C62%2C0%2C1858%2C0%2C1858%2C1080%2C1858%2C1009&vis=1&wgl=true&ca_type=image");
        }
        private byte[] GetBytesFromImage(string path)
        {
            var bitmapClientImage = LoadBitmapImage(path);
            int bytesPerPixel = bitmapClientImage.Format.BitsPerPixel / 8;
            var stride = bitmapClientImage.PixelWidth * bytesPerPixel;
            int arrayLength = stride * bitmapClientImage.PixelHeight;
            var result = new byte[arrayLength];
            bitmapClientImage.CopyPixels(result, stride, 0);

            return result;
        }
        private string Match(string input, string pattern, RegexOptions options)
        {
            var match = Regex.Match(input, pattern, options);
            if (match.Success)
                return match.Groups[match.Groups.Count - 1].Value;
            return null;
        }
        private string Match(string input, string pattern) => Match(input, pattern, RegexOptions.None);
        public static BitmapImage LoadBitmapImage(string fileName)
        {
            using (var stream = new FileStream(fileName, FileMode.Open))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                return bitmapImage;
            }
        }
        private void VideoModelHandler(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "IsChecked")
            {
                var videoSaved = true;
                if (!_serverVideos.SequenceEqual(_clientVideos))
                {
                    IsSaved = false;
                    return;
                }
                if (videoSaved)
                    CheckIsSaved();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
