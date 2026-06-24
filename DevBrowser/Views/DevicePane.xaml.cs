using System.Windows.Controls;
using DevBrowser.Handlers;
using DevBrowser.Services;
using CefSharp;
using CefSharp.Wpf;

namespace DevBrowser.Views
{
    public partial class DevicePane : UserControl
    {
        public DevicePane()
        {
            InitializeComponent();
        }

        public ChromiumWebBrowser WebBrowser => Browser;

        public void Initialize(string label, int width, int height, string userAgent)
        {
            DeviceLabel.Text = label;
            ResolutionLabel.Text = $"{width} x {height}";
            
            // Set exact dimensions for the browser
            Browser.Width = width;
            Browser.Height = height;

            // Set isolated context
            Browser.RequestContext = SessionManager.CreateIsolatedContext();
            
            // Set User-Agent
            Browser.RequestHandler = new DevicePaneRequestHandler(userAgent);

            // In WPF, we use Viewbox for scaling, which is easier than LayoutTransform calculations
            // since it handles the aspect ratio automatically.
        }
    }
}
