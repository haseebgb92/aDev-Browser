using System.Windows.Controls;
using DevBrowser.Services;
using CefSharp;
using CefSharp.Wpf;

namespace DevBrowser.Tabs
{
    public partial class StandardTab : UserControl
    {
        public StandardTab(string url)
        {
            InitializeComponent();
            
            // Create isolated context for this tab
            Browser.RequestContext = SessionManager.CreateIsolatedContext();
            Browser.Load(url);
        }

        public ChromiumWebBrowser WebBrowser => Browser;
    }
}
