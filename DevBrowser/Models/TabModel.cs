using System.ComponentModel;
using CefSharp.Wpf;

namespace DevBrowser.Models
{
    public class TabModel : INotifyPropertyChanged
    {
        private string _title = "New Tab";
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(nameof(Title)); }
        }

        private string _url = "about:blank";
        public string Url
        {
            get => _url;
            set { _url = value; OnPropertyChanged(nameof(Url)); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); }
        }

        public System.Windows.Input.ICommand? SelectCommand { get; set; }

        public System.Windows.Controls.UserControl View { get; set; } = null!;
        public ChromiumWebBrowser Browser { get; set; } = null!;
        
        public bool IsResponsiveView { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
