using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using YouTubeStreamStarter.Models;

namespace YouTubeStreamStarter.ViewModels
{
    public abstract class BaseViewModel : INotifyPropertyChanged
    {
        private Window _window;

        public BaseViewModel(Window window)
        {
            _window = window;
        }

        protected RelayCommand _exit;
        protected RelayCommand _minimize;
        protected RelayCommand Exit => _exit ?? new RelayCommand((o) => _window.Close());
        protected RelayCommand Minimize => _minimize ?? new RelayCommand((o) => _window.WindowState = WindowState.Minimized);
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName]string name = "") => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
