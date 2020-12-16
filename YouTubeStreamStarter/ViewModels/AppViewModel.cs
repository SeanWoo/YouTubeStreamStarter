using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
        public Channel Channel { get; set; }

        public AppViewModel(Window window) : base(window) {
            if (!Directory.Exists("Images"))
                Directory.CreateDirectory("Images");
        }

        private RelayCommand _saveChangesChannelData;
        public RelayCommand SaveChangesChannelData => _saveChangesChannelData ?? new RelayCommand(o => Channel?.SaveChangesAsync());

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
            Channel = new Channel(cookies);
            await Channel.UpdateAsync();
            OnPropertyChanged("Channel");
        }
    }
}
