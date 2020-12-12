using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows;
using YouTubeStreamStarter.Models;

namespace YouTubeStreamStarter.ViewModels
{
    public class AppViewModel : BaseViewModel
    {
        private string _channelCookie;

        public string ChannelCookie { get => _channelCookie; set => ParseCookie(value); }
        public Channel Channel { get; set; }

        public AppViewModel(Window window) : base(window) { }

        private void ParseCookie(string value)
        {
            _channelCookie = value;

            var cookies = new CookieContainer();
            var lines = ChannelCookie.Split('\n');
            foreach (var line in lines)
            {
                var datas = line.Split('\t', ' ');
                if (datas.Length > 6)
                    cookies.Add(new Cookie(datas[6], datas[7], datas[2], datas[0]));
                
            }
            if(cookies.Count == 0)
            {
                MessageBox.Show("Не верные куки");
                return;
            }
            Channel = new Channel(cookies);
            Channel.Update();
        }
    }
}
