using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CefSharp;
using CefSharp.Wpf;
using DevBrowser.Models;
using DevBrowser.Tabs;
using DevBrowser.Services;
using DevBrowser.Handlers;
using System.Linq;
using System.Threading.Tasks;

namespace DevBrowser
{
    public partial class MainWindow : Window
    {
        public TabManager TabManager { get; } = new TabManager();
        public AppSettings Settings { get; private set; } = null!;
        public ObservableCollection<HeaderModel> CustomHeaders { get; } = new ObservableCollection<HeaderModel>();

        public ICommand NewTabCommand { get; }
        public ICommand CloseTabCommand { get; }
        public ICommand ReloadCommand { get; }
        public ICommand HardReloadCommand { get; }
        public ICommand DevToolsCommand { get; }
        public ICommand FocusAddressBarCommand { get; }
        public ICommand NukeCommand { get; }
        public ICommand ResponsiveViewCommand { get; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            TabStripControl.ItemsSource = TabManager.Tabs;

            Settings = AppSettings.Load();
            DefaultUrlBox.Text = Settings.DefaultNewTabUrl;
            ConsoleVisibleCheck.IsChecked = Settings.AlwaysVisibleConsole;
            
            NewTabCommand = new RelayCommand(_ => AddNewTab(Settings.DefaultNewTabUrl));
            CloseTabCommand = new RelayCommand(_ => CloseSelectedTab());
            ReloadCommand = new RelayCommand(_ => TabManager.SelectedTab?.Browser.Reload());
            HardReloadCommand = new RelayCommand(_ => TabManager.SelectedTab?.Browser.Reload(true));
            DevToolsCommand = new RelayCommand(_ => TabManager.SelectedTab?.Browser.ShowDevTools());
            FocusAddressBarCommand = new RelayCommand(_ => AddressBar.Focus());
            NukeCommand = new RelayCommand(_ => NukeSelectedTab());
            ResponsiveViewCommand = new RelayCommand(_ => OpenResponsiveView());

            // Add initial tab
            AddNewTab(Settings.DefaultNewTabUrl);
        }

        private void AddNewTab(string url)
        {
            var tabView = new StandardTab(url);
            var browser = tabView.WebBrowser;
            
            var tabModel = new TabModel
            {
                Title = "Loading...",
                Url = url,
                Browser = browser,
                View = tabView
            };

            tabModel.SelectCommand = new RelayCommand(_ => SelectTab(tabModel));

            browser.AddressChanged += (s, e) => {
                Dispatcher.Invoke(() => {
                    var newAddress = (string)e.NewValue;
                    tabModel.Url = newAddress;
                    if (TabManager.SelectedTab == tabModel)
                        AddressBar.Text = newAddress;
                });
            };

            browser.TitleChanged += (s, e) => {
                Dispatcher.Invoke(() => tabModel.Title = (string)e.NewValue);
            };

            TabManager.AddTab(tabModel);
            SelectTab(tabModel);
        }

        private void SelectTab(TabModel tab)
        {
            if (TabManager.SelectedTab != null)
                TabManager.SelectedTab.IsSelected = false;

            TabManager.SelectedTab = tab;
            tab.IsSelected = true;
            
            TabContent.Content = tab.View;
            AddressBar.Text = tab.Url;
        }

        private void CloseSelectedTab()
        {
            if (TabManager.SelectedTab != null)
                RemoveTab(TabManager.SelectedTab);
        }

        private void CloseTab_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is TabModel tab)
                RemoveTab(tab);
        }

        private void RemoveTab(TabModel tab)
        {
            TabManager.RemoveTab(tab);
            if (TabManager.Tabs.Count == 0)
                Close();
            else if (TabManager.SelectedTab != null)
                SelectTab(TabManager.SelectedTab);
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void Maximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        private void AddressBar_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var url = AddressBar.Text;
                if (!string.IsNullOrWhiteSpace(url) && !url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("about:"))
                {
                    url = "https://" + url;
                    AddressBar.Text = url;
                }
                TabManager.SelectedTab?.Browser.Load(url);
            }
        }

        private void QuickBar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string port)
            {
                TabManager.SelectedTab?.Browser.Load($"http://localhost:{port}");
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e) => TabManager.SelectedTab?.Browser.Back();
        private void Forward_Click(object sender, RoutedEventArgs e) => TabManager.SelectedTab?.Browser.Forward();
        private void Reload_Click(object sender, RoutedEventArgs e) => TabManager.SelectedTab?.Browser.Reload();
        private void DevTools_Click(object sender, RoutedEventArgs e) => TabManager.SelectedTab?.Browser.ShowDevTools();

        private async void NukeSelectedTab()
        {
            var tab = TabManager.SelectedTab;
            if (tab == null) return;

            var browsers = new List<ChromiumWebBrowser>();
            if (tab.IsResponsiveView && tab.Browser.Parent is ResponsiveViewTab rv)
            {
                browsers.Add(rv.PaneLeft.WebBrowser);
                browsers.Add(rv.PaneCenter.WebBrowser);
                browsers.Add(rv.PaneRight.WebBrowser);
            }
            else
            {
                browsers.Add(tab.Browser);
            }

            foreach (var browser in browsers)
            {
                await browser.GetMainFrame().EvaluateScriptAsync(@"
                    localStorage.clear();
                    sessionStorage.clear();
                    indexedDB.databases().then(dbs =>
                        dbs.forEach(db => indexedDB.deleteDatabase(db.name))
                    );
                ");

                var context = browser.RequestContext;
                var cookieManager = context?.GetCookieManager(null);
                if (cookieManager != null)
                {
                    await cookieManager.DeleteCookiesAsync(string.Empty, string.Empty);
                }

                browser.Reload(ignoreCache: true);
            }
        }

        private void OpenResponsiveView()
        {
            var currentUrl = TabManager.SelectedTab?.Url ?? Settings.DefaultNewTabUrl;
            var tabView = new ResponsiveViewTab(currentUrl);
            
            var tabModel = new TabModel
            {
                Title = "Responsive View",
                Url = currentUrl,
                Browser = tabView.PaneLeft.WebBrowser,
                View = tabView,
                IsResponsiveView = true
            };

            tabModel.SelectCommand = new RelayCommand(_ => SelectTab(tabModel));

            tabView.PaneLeft.WebBrowser.AddressChanged += (s, ev) => {
                Dispatcher.Invoke(() => {
                    var newAddress = (string)ev.NewValue;
                    tabModel.Url = newAddress;
                    if (TabManager.SelectedTab == tabModel)
                        AddressBar.Text = newAddress;
                });
            };

            TabManager.AddTab(tabModel);
            SelectTab(tabModel);
        }

        private void ToggleHeaders_Click(object sender, RoutedEventArgs e)
        {
            HeadersPanel.Visibility = HeadersPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            if (HeadersPanel.Visibility == Visibility.Visible)
            {
                SettingsPanel.Visibility = Visibility.Collapsed;
                HeadersList.ItemsSource = CustomHeaders;
            }
        }

        private void AddHeader_Click(object sender, RoutedEventArgs e) => CustomHeaders.Add(new HeaderModel { Key = "X-New-Header", Value = "Value" });

        private void RemoveHeader_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is HeaderModel header)
                CustomHeaders.Remove(header);
        }

        private void ApplyHeaders_Click(object sender, RoutedEventArgs e)
        {
            var headerDict = CustomHeaders.ToDictionary(h => h.Key, h => h.Value);
            var tab = TabManager.SelectedTab;
            if (tab != null)
            {
                tab.Browser.RequestHandler = new HeaderInjectionRequestHandler(headerDict);
                tab.Browser.Reload();
            }
            HeadersPanel.Visibility = Visibility.Collapsed;
        }

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = SettingsPanel.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            if (SettingsPanel.Visibility == Visibility.Visible)
                HeadersPanel.Visibility = Visibility.Collapsed;
            else
            {
                Settings.DefaultNewTabUrl = DefaultUrlBox.Text;
                Settings.Save();
            }
        }

        private void ConsoleVisible_Click(object sender, RoutedEventArgs e)
        {
            Settings.AlwaysVisibleConsole = ConsoleVisibleCheck.IsChecked ?? false;
        }

        private void ThrottleBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ThrottleBox.SelectedItem is ComboBoxItem item)
                ApplyThrottling(item.Tag as string);
        }

        private async void ApplyThrottling(string? config)
        {
            var browser = TabManager.SelectedTab?.Browser;
            if (browser == null) return;

            bool offline = false;
            double latency = 0;
            double downloadThroughput = -1;
            double uploadThroughput = -1;

            if (config != null)
            {
                var parts = config.Split(',');
                if (parts.Length == 3)
                {
                    uploadThroughput = double.Parse(parts[0]) * 1024;
                    downloadThroughput = double.Parse(parts[1]) * 1024;
                    latency = double.Parse(parts[2]);
                    offline = (downloadThroughput == 0 && uploadThroughput == 0);
                }
            }

            var parameters = new Dictionary<string, object>
            {
                { "offline", offline },
                { "latency", latency },
                { "downloadThroughput", downloadThroughput },
                { "uploadThroughput", uploadThroughput }
            };

            await browser.GetBrowser().ExecuteDevToolsMethodAsync(0, "Network.emulateNetworkConditions", parameters);
        }

        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.ContextMenu != null)
            {
                button.ContextMenu.PlacementTarget = button;
                button.ContextMenu.IsOpen = true;
            }
        }

        private void NewTab_Click(object sender, RoutedEventArgs e) => AddNewTab(Settings.DefaultNewTabUrl);
        private void ResponsiveView_Click(object sender, RoutedEventArgs e) => OpenResponsiveView();
        private void Nuke_Click(object sender, RoutedEventArgs e) => NukeSelectedTab();
    }
}
