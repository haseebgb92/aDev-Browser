using CefSharp;
using CefSharp.Handler;

namespace DevBrowser.Handlers
{
    public class DevicePaneRequestHandler : RequestHandler
    {
        private readonly string _userAgent;

        public DevicePaneRequestHandler(string userAgent)
        {
            _userAgent = userAgent;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(
            IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
            IRequest request, bool isNavigation, bool isDownload,
            string requestInitiator, ref bool disableDefaultHandling)
        {
            return new DevicePaneResourceHandler(_userAgent);
        }
    }

    public class DevicePaneResourceHandler : ResourceRequestHandler
    {
        private readonly string _userAgent;

        public DevicePaneResourceHandler(string userAgent) => _userAgent = userAgent;

        protected override CefReturnValue OnBeforeResourceLoad(
            IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
            IRequest request, IRequestCallback callback)
        {
            var headers = request.Headers;
            headers["User-Agent"] = _userAgent;
            request.Headers = headers;
            return CefReturnValue.Continue;
        }
    }
}
