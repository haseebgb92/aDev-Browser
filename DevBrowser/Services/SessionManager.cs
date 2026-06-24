using System;
using System.Threading.Tasks;
using CefSharp;

namespace DevBrowser.Services
{
    public static class SessionManager
    {
        public static IRequestContext CreateIsolatedContext()
        {
            var requestContextSettings = new RequestContextSettings
            {
                CachePath = null, // Disk cache free
                PersistSessionCookies = false,
                PersistUserPreferences = false,
            };

            return new RequestContext(requestContextSettings);
        }

        public static async Task PurgeTabSession(IWebBrowser browser)
        {
            if (browser == null) return;

            var context = browser.RequestContext;
            if (context != null)
            {
                var cookieManager = context.GetCookieManager(null);
                if (cookieManager != null)
                {
                    await cookieManager.DeleteCookiesAsync(string.Empty, string.Empty);
                }

                context.ClearSchemeHandlerFactories();
            }

            // Note: browser.Dispose() and context.Dispose() should be called by the UI control disposal
        }
    }
}
