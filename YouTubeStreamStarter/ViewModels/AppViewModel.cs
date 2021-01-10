using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using YouTubeStreamStarter.Models;

namespace YouTubeStreamStarter.ViewModels
{
    public class AppViewModel : BaseViewModel
    {
        private Channel _channel;
        private string _channelCookie;

        public string ChannelCookie { get => _channelCookie; set => ParseCookieAsync(value); }
        public bool IsAllChecked {
            get 
            {
                var selected = Channel?.Videos.Select(item => item.IsChecked).Distinct().ToList();
                return selected?.Count == 1 ? selected.Single() : false;
            }
            set 
            {
                if (Channel?.Videos != null)
                    foreach (var video in Channel?.Videos)
                        video.IsChecked = value;

            }
        }
        public Dictionary<string, string> PrivacyVideoVariants { get; set; }
        public ObservableCollection<VideoModel> SelectedVideoItems { get; set; }
        public string a { get; set; }
        public Channel Channel { get; set; }

        public AppViewModel(Window window) : base(window) {
            PrivacyVideoVariants = new Dictionary<string, string>();
            var pair = AppData.GetPair<string>("privacyValues.public");
            PrivacyVideoVariants.Add(pair.Key, pair.Value);
            pair = AppData.GetPair<string>("privacyValues.unlisted");
            PrivacyVideoVariants.Add(pair.Key, pair.Value);
            pair = AppData.GetPair<string>("privacyValues.private");
            PrivacyVideoVariants.Add(pair.Key, pair.Value);

            if (!Directory.Exists("Images"))
                Directory.CreateDirectory("Images");
        }

        private RelayCommand _openChannel;
        public RelayCommand OpenChannel => _openChannel ?? new RelayCommand(o => Process.Start("cmd", "/C start " + Channel.Address), (o) => Channel != null);
        private RelayCommand _saveChangesChannelData;
        public RelayCommand SaveChangesChannelData => _saveChangesChannelData ?? new RelayCommand( o => Channel.SaveChangesAsync(), (o) => Channel != null);

        private RelayCommand _openImageAvatar;
        public RelayCommand OpenImageAvatar => _openImageAvatar ?? new RelayCommand(o => {
            var fd = new OpenFileDialog();
            if(fd.ShowDialog() == true)
            {
                File.Copy(fd.FileName, "Images/clientAvatar.jpg", true);
            }
            OnPropertyChanged("Channel");
        });
        private RelayCommand _openImageBanner;
        public RelayCommand OpenImageBanner => _openImageBanner ?? new RelayCommand(o => {
            var fd = new OpenFileDialog();
            if (fd.ShowDialog() == true)
            {
                File.Copy(fd.FileName, "Images/clientBanner.jpg", true);
            }
            OnPropertyChanged("Channel");
        });
        private async void ParseCookieAsync(string value)
        {
            _channelCookie = value;

            var cookies = new CookieContainer();
            await Task.Run(() =>
            {
                var lines = ChannelCookie.Split('\n');
                foreach (var line in lines)
                {
                    var datas = line.Split('\t', ' ');
                    if (datas.Length > 6)
                        cookies.Add(new Cookie(datas[6], datas[7], datas[2], datas[0]));
                }
            });
            if (cookies.Count == 0)
            {
                MessageBox.Show("Не верные куки");
                return;
            }

            //TODO: Временное решение
            if (File.Exists("proxys.txt"))
            {
                var rnd = new Random();
                var proxys = File.ReadAllLines("proxys.txt");
                foreach (var proxy in proxys)
                {
                    var channel = new Channel(cookies, proxy);

                    channel.Description = rnd.Next().ToString();

                    await channel.UpdateAsync();
                    if (channel.IsSaved)
                    {
                        Channel = channel;
                        break;
                    }
                }
            }
            else
            {
                Channel = new Channel(cookies);
            }

            await Channel.UpdateAsync();
            OnPropertyChanged("Channel");
        }
    }
}
