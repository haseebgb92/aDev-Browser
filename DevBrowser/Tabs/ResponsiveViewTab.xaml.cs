using System.Windows.Controls;
using System.Collections.Generic;
using CefSharp;
using System.Linq;

namespace DevBrowser.Tabs
{
    public partial class ResponsiveViewTab : UserControl
    {
        private bool _isSyncingNavigation = false;

        public ResponsiveViewTab(string url)
        {
            InitializeComponent();

            // iPad Pro 12.9
            PaneLeft.Initialize("iPad Pro 12.9", 1024, 1366, 
                "Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1");
            
            // Galaxy S24
            PaneCenter.Initialize("Galaxy S24", 412, 915, 
                "Mozilla/5.0 (Linux; Android 14; SM-S921B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36");

            // iPhone 16 Pro
            PaneRight.Initialize("iPhone 16 Pro", 393, 852, 
                "Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1");

            var browsers = new[] { PaneLeft.WebBrowser, PaneCenter.WebBrowser, PaneRight.WebBrowser };

            foreach (var browser in browsers)
            {
                browser.AddressChanged += (s, e) => {
                    if (_isSyncingNavigation) return;
                    _isSyncingNavigation = true;
                    
                    Dispatcher.Invoke(() => {
                        var newAddress = (string)e.NewValue;
                        foreach (var b in browsers.Where(x => x != browser))
                        {
                            b.Load(newAddress);
                        }
                    });

                    _isSyncingNavigation = false;
                };

                browser.Load(url);
            }
        }
    }
}
