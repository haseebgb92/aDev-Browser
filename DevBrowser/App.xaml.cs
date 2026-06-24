using System.Windows;
using CefSharp;
using CefSharp.Wpf;

namespace DevBrowser
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var settings = new CefSettings()
            {
                // By default CefSharp will use an in-memory cache if no cache path is provided
                // but we want to be explicit about it for zero persistence.
                CachePath = null,
                PersistSessionCookies = false,
                PersistUserPreferences = false
            };

            // Initialize CEF with the provided settings
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Cef.Shutdown();
            base.OnExit(e);
        }
    }
}
