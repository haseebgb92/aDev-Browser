using System.Collections.Generic;
using CefSharp;
using CefSharp.Handler;

namespace DevBrowser.Handlers
{
    public class HeaderInjectionRequestHandler : RequestHandler
    {
        private readonly Dictionary<string, string> _customHeaders;

        public HeaderInjectionRequestHandler(Dictionary<string, string> customHeaders)
        {
            _customHeaders = customHeaders;
        }

        protected override IResourceRequestHandler GetResourceRequestHandler(
            IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
            IRequest request, bool isNavigation, bool isDownload,
            string requestInitiator, ref bool disableDefaultHandling)
        {
            return new HeaderInjectionResourceHandler(_customHeaders);
        }
    }

    public class HeaderInjectionResourceHandler : ResourceRequestHandler
    {
        private readonly Dictionary<string, string> _customHeaders;

        public HeaderInjectionResourceHandler(Dictionary<string, string> customHeaders)
        {
            _customHeaders = customHeaders;
        }

        protected override CefReturnValue OnBeforeResourceLoad(
            IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame,
            IRequest request, IRequestCallback callback)
        {
            var headers = request.Headers;
            foreach (var header in _customHeaders)
            {
                headers[header.Key] = header.Value;
            }
            request.Headers = headers;
            return CefReturnValue.Continue;
        }
    }
}
