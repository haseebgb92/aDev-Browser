# Build Prompt: aDevBrowser for Windows — Ephemeral Zero-Cache Developer Browser

## Project Summary

Build a Windows-only desktop browser for web developers. Every session is ephemeral: closing a tab or the app wipes all cookies, cache, localStorage, sessionStorage, IndexedDB, and service workers. No persistence. No exceptions. Full DevTools required. Includes a dedicated Responsive View tab that renders three device viewports side by side in a single tab for accurate cross-device testing.

---

## Platform

- **OS:** Windows 10 and Windows 11 only (64-bit)
- **Installer target:** under 80MB
- **RAM at idle (1 tab):** under 150MB
- **RAM with 5 active tabs:** under 500MB
- **Cold start to usable tab:** under 2.5 seconds

---

## Stack: CefSharp + .NET 8 (WPF)

Use **CefSharp.Wpf** (Chromium Embedded Framework, C# bindings) as the browser engine.

**Why CefSharp over Electron for Windows-only:**
- No Node.js runtime bundled — smaller install
- WPF shell is native Windows, lower overhead than Electron's UI renderer process
- Per-tab `IRequestContext` with `CachePath = null` gives true disk-cache-free sessions out of the box
- Full Chromium DevTools accessible via `ShowDevTools()` with no build flag changes
- Cuts 80-100MB idle RAM compared to Electron on the same machine

```xml
<!-- NuGet packages -->
<PackageReference Include="CefSharp.Wpf" Version="120.*" />
<PackageReference Include="CefSharp.Common" Version="120.*" />
```

.NET 8 (LTS). WPF project type.

---

## Zero Persistence Architecture

### Per-Tab Session Isolation

Each tab (including each pane inside the Responsive View tab) gets its own `IRequestContext`. No shared cookies, no shared cache, no shared storage between tabs or between device panes.

```csharp
var requestContextSettings = new RequestContextSettings
{
    CachePath = null,
    PersistSessionCookies = false,
    PersistUserPreferences = false,
};

var requestContext = new RequestContext(requestContextSettings);

var browser = new ChromiumWebBrowser(url)
{
    RequestContext = requestContext
};
```

### On Tab Close

```csharp
private async Task PurgeTabSession(ChromiumWebBrowser browser)
{
    var context = browser.RequestContext;

    var cookieManager = context.GetCookieManager(null);
    await cookieManager.DeleteCookiesAsync(string.Empty, string.Empty);

    context.ClearSchemeHandlerFactories();

    await browser.GetBrowser().GetHost().GetRequestContext()
        .ClearCertificateExceptionsAsync();

    browser.Dispose();
    context.Dispose();
}
```

For the Responsive View tab: call `PurgeTabSession` on all three device pane browsers independently before closing the tab.

### On App Exit

```csharp
protected override void OnExit(ExitEventArgs e)
{
    foreach (var tab in TabManager.AllTabs)
    {
        foreach (var browser in tab.GetAllBrowserInstances())
        {
            PurgeTabSession(browser).GetAwaiter().GetResult();
        }
    }
    Cef.Shutdown();
    base.OnExit(e);
}
```

---

## UI Requirements

### Standard Tab Shell

- Tab bar at top, horizontal, supports 20+ tabs
- Address bar: full-width URL input, Enter to load
- Toolbar: Back, Forward, Reload, Stop, Nuke Cache, Responsive View toggle, DevTools toggle
- Status bar: load progress + hovered URL
- Session badge on each tab: icon indicating zero-persistence mode

### Keyboard Shortcuts

| Action | Shortcut |
|---|---|
| New tab | Ctrl+T |
| Close tab | Ctrl+W |
| Reload | F5 / Ctrl+R |
| Hard reload | Ctrl+Shift+R |
| Open DevTools | F12 / Ctrl+Shift+I |
| Focus address bar | Ctrl+L / F6 |
| Nuke session cache | Ctrl+Shift+Delete |
| Open Responsive View tab | Ctrl+Shift+M |
| Next tab | Ctrl+Tab |
| Previous tab | Ctrl+Shift+Tab |
| Zoom in / out / reset | Ctrl+Plus / Ctrl+Minus / Ctrl+0 |

---

## Responsive View Tab

### Overview

The Responsive View is a dedicated tab type (not a mode overlay). When the user opens it (Ctrl+Shift+M or toolbar button), a new tab opens with three browser panes rendered side by side, each showing the same URL at a different device viewport and user-agent. All three panes load simultaneously.

This is not a CSS resize simulation. Each pane is a real `ChromiumWebBrowser` instance with its own viewport, its own user-agent string, and its own isolated request context. What you see is what the actual device browser renders.

### Default Device Panes

| Pane | Device Label | Viewport Width | Viewport Height | User-Agent String |
|---|---|---|---|---|
| Left | iPad Pro 12.9 | 1024px | 1366px | Mozilla/5.0 (iPad; CPU OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1 |
| Center | Samsung Galaxy S24 | 412px | 915px | Mozilla/5.0 (Linux; Android 14; SM-S921B) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36 |
| Right | iPhone 16 Pro | 393px | 852px | Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.5 Mobile/15E148 Safari/604.1 |

All device specs and user-agents must be editable in Settings. Users can swap any pane to a different preset or enter a custom viewport + UA.

### Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│  [←][→][↺]  [ https://example.com                              ] [⚡][⚙]  │
│  Responsive View                                          [Sync Scroll ○]  │
├──────────────────┬───────────────────┬──────────────────────────────┤
│  iPad Pro 12.9   │  Galaxy S24       │  iPhone 16 Pro               │
│  1024 x 1366     │  412 x 915        │  393 x 852                   │
│  [Edit]          │  [Edit]           │  [Edit]                       │
├──────────────────┼───────────────────┼──────────────────────────────┤
│                  │                   │                              │
│                  │                   │                              │
│   [browser pane] │   [browser pane]  │   [browser pane]             │
│                  │                   │                              │
│                  │                   │                              │
└──────────────────┴───────────────────┴──────────────────────────────┘
```

Each pane:
- Renders at its defined viewport size regardless of the actual pane width on screen. The pane scales (zooms out) to fit the available horizontal space while preserving the correct aspect ratio. A scale factor indicator shows below the device label (e.g., "Scale: 0.72x").
- Shows device label, resolution, and an Edit button at the top
- Has its own scrollbar
- Has its own DevTools accessible via right-click > Inspect or Ctrl+Shift+I while the pane is focused

### Viewport and UA Implementation

Set viewport size and user-agent per pane using CefSharp's `BrowserSettings` and `OnBeforeBrowse` interception:

```csharp
// Set user-agent per pane browser
var browserSettings = new BrowserSettings
{
    // UA set at request context level for full coverage
};

var requestContext = new RequestContext(new RequestContextSettings { CachePath = null });

// Override UA via request handler
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
```

Set the rendered viewport dimensions via JavaScript after page load:

```csharp
// Inject viewport meta and force layout width
private async Task SetViewportSize(ChromiumWebBrowser browser, int width, int height)
{
    await browser.GetMainFrame().EvaluateScriptAsync($@"
        (function() {{
            let meta = document.querySelector('meta[name=""viewport""]');
            if (!meta) {{
                meta = document.createElement('meta');
                meta.name = 'viewport';
                document.head.appendChild(meta);
            }}
            meta.content = 'width={width}, initial-scale=1';
        }})();
    ");
}
```

For accurate rendering, also set the `ChromiumWebBrowser` control's render size to the exact device dimensions and apply a `LayoutTransform` scale to fit it within the available pane area:

```csharp
// Set browser render size to exact device pixels
browser.Width = deviceWidth;
browser.Height = deviceHeight;

// Scale down to fit pane
double scaleX = paneAvailableWidth / deviceWidth;
double scaleY = paneAvailableHeight / deviceHeight;
double scale = Math.Min(scaleX, scaleY);

browser.LayoutTransform = new ScaleTransform(scale, scale);
scaleLabel.Text = $"Scale: {scale:F2}x";
```

This approach forces the browser to actually render at the device resolution, not just resize the container. Media queries, touch breakpoints, and responsive images all respond to the real viewport width.

### URL Synchronization

All three panes share the URL from the Responsive View tab's address bar. When the user navigates (types a new URL or clicks a link in any pane), all three panes navigate to the new URL.

```csharp
// Sync navigation from any pane to all panes
private void OnPaneAddressChanged(object sender, AddressChangedEventArgs e)
{
    if (_isSyncingNavigation) return;
    _isSyncingNavigation = true;

    foreach (var pane in _devicePanes.Where(p => p.Browser != sender))
    {
        pane.Browser.Load(e.Address);
    }

    AddressBar.Text = e.Address;
    _isSyncingNavigation = false;
}
```

### Reload Behavior

Pressing F5 or the Reload button reloads all three panes simultaneously. Ctrl+Shift+R hard-reloads all three (ignoreCache: true).

```csharp
private void ReloadAllPanes(bool ignoreCache = false)
{
    foreach (var pane in _devicePanes)
    {
        pane.Browser.Reload(ignoreCache);
    }
}
```

### Scroll Sync (Toggle)

A toggle in the Responsive View toolbar (default: off). When enabled, scrolling any pane sends a normalized scroll position (0.0 to 1.0) to the other two panes.

```csharp
// Inject scroll listener in each pane after load
private async Task InjectScrollSync(ChromiumWebBrowser browser, string paneId)
{
    await browser.GetMainFrame().EvaluateScriptAsync($@"
        window.__devBrowserPaneId = '{paneId}';
        window.addEventListener('scroll', function() {{
            var scrollY = window.scrollY;
            var maxScroll = document.body.scrollHeight - window.innerHeight;
            var ratio = maxScroll > 0 ? scrollY / maxScroll : 0;
            window.chrome.webview && window.chrome.webview.postMessage(
                JSON.stringify({{ type: 'scroll', paneId: '{paneId}', ratio: ratio }})
            );
        }}, {{ passive: true }});

        window.__applyScrollRatio = function(ratio) {{
            var maxScroll = document.body.scrollHeight - window.innerHeight;
            window.scrollTo(0, ratio * maxScroll);
        }};
    ");
}
```

Use CefSharp's `JavascriptMessageReceived` event or `IJavascriptCallback` to receive scroll events from the page and propagate to other panes via `EvaluateScriptAsync("window.__applyScrollRatio(" + ratio + ")")`.

### Editing a Device Pane

Clicking Edit on any pane opens a small inline panel above that pane with:

- Device preset dropdown (populated from Settings presets list)
- Custom viewport width (px) input
- Custom viewport height (px) input
- Custom user-agent text input
- Apply button (reloads that pane with new settings)

Changes apply to that session only. To make a custom device permanent, save it to the presets list in Settings.

### Nuke in Responsive View

The Nuke Cache button clears all three pane sessions simultaneously and reloads all three.

---

## Standard Developer Features (v1)

### Nuke Cache Button

Clears cookies, localStorage, sessionStorage, IndexedDB, and cache for the current tab's context, then reloads:

```csharp
private async Task NukeAndReload(ChromiumWebBrowser browser)
{
    var frame = browser.GetMainFrame();

    await frame.EvaluateScriptAsync(@"
        localStorage.clear();
        sessionStorage.clear();
        indexedDB.databases().then(dbs =>
            dbs.forEach(db => indexedDB.deleteDatabase(db.name))
        );
    ");

    var cookieManager = browser.RequestContext.GetCookieManager(null);
    await cookieManager.DeleteCookiesAsync(string.Empty, string.Empty);

    browser.Reload(ignoreCache: true);
}
```

### User-Agent Switcher

Dropdown in toolbar. Applies to current standard tab only. Presets: Windows Chrome, iPhone 16 Safari, Galaxy S24 Chrome, Custom.

### Custom Request Headers

Per-tab header injection panel. Key/value rows. Headers apply to all requests from that tab. Implemented via `IResourceRequestHandler`.

### Localhost Quick Bar

Pinned row below address bar (toggle). Configurable port shortcuts. Defaults: 3000, 4000, 5173, 8000, 8080.

### Disable JavaScript Toggle

Per-tab. Implemented via `BrowserSettings.JavascriptEnabled = CefState.Disabled`. Indicator on tab label when off.

### Network Throttle

Toolbar dropdown. Presets: None, Slow 3G (400kbps), Fast 3G (1.5Mbps), 4G (4Mbps).

### Console Always-Visible Mode

Split view: page top 70%, persistent console pane bottom 30%. Shows live JS errors and console.log output via `IDisplayHandler.OnConsoleMessage`. Full DevTools still accessible via F12.

---

## DevTools — Full, No Stripping

```csharp
browser.GetBrowser().GetHost().ShowDevTools(
    windowInfo: null,
    client: null,
    settings: new BrowserSettings(),
    inspectElementAt: new CefSharp.Structs.Point(0, 0)
);
```

Required panels: Console, Elements, Network, Sources, Application, Performance, Memory.

Must work in Release builds. Do not set any CEF flag that disables DevTools in release configuration. Set `CefSharpSettings.RuntimeStyle = RuntimeStyle.ChromeRuntime` for best DevTools compatibility in CefSharp 120+.

---

## Settings Panel

Stored in `%APPDATA%\DevBrowser\settings.json`. Never leaves the machine.

| Setting | Type | Default |
|---|---|---|
| Default new tab URL | Text | about:blank |
| DevTools default position | Bottom / Right / Detached | Bottom |
| Localhost quick bar ports | List | 3000, 5173, 8000, 8080 |
| User-agent presets | List (editable) | 4 defaults |
| Responsive View device presets | List (editable) | 3 defaults |
| Always-visible console | Toggle | Off |
| Scroll sync default state | Toggle | Off |
| Confirm before closing multiple tabs | Toggle | On |
| Startup behavior | Blank / Last URLs (no state restored) | Blank |

---

## Project Structure

```
DevBrowser/
├── DevBrowser.sln
├── DevBrowser/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── Tabs/
│   │   ├── TabManager.cs
│   │   ├── StandardTab.xaml
│   │   └── ResponsiveViewTab.xaml
│   ├── Views/
│   │   ├── TabStrip.xaml
│   │   ├── AddressBar.xaml
│   │   ├── DevicePane.xaml
│   │   ├── DevicePaneEditor.xaml
│   │   ├── DevToolsPanel.xaml
│   │   ├── SettingsPanel.xaml
│   │   └── ConsolePane.xaml
│   ├── Models/
│   │   ├── TabModel.cs
│   │   ├── DevicePreset.cs
│   │   └── AppSettings.cs
│   ├── Services/
│   │   ├── SessionManager.cs
│   │   ├── HeaderInjector.cs
│   │   ├── ThrottleService.cs
│   │   └── ScrollSyncService.cs
│   └── Handlers/
│       ├── DevicePaneRequestHandler.cs
│       ├── DevicePaneResourceHandler.cs
│       ├── DisplayHandler.cs
│       └── LifeSpanHandler.cs
└── Installer/
    └── setup.iss
```

---

## What to Exclude (v1)

- Extensions / plugins
- Password manager
- Sync or accounts
- PDF viewer
- DRM media
- Ad blocking
- History search
- Permanent bookmarks
- macOS / Linux builds
- Tauri / WebView2 path

---

## Success Criteria

1. Open a WordPress or Shopify staging URL. Make a server-side change. Press F5. New content loads with zero stale cache interference.
2. Open two standard tabs to the same URL. Log into one. The other tab shows no logged-in state.
3. Close a tab. Reopen the same URL. No cookies, no localStorage, no cached response.
4. Press F12. Full DevTools opens with working Console within 500ms.
5. Press Ctrl+Shift+Delete (Nuke). Page reloads. Application tab in DevTools shows empty storage.
6. Open Responsive View tab. All three device panes load the same URL simultaneously, each at the correct viewport width and user-agent. A site with a 768px tablet breakpoint shows the tablet layout in the iPad pane and the mobile layout in both phone panes.
7. Change the URL in the Responsive View address bar. All three panes navigate to the new URL.
8. Enable Scroll Sync. Scroll the Galaxy pane. iPhone and iPad panes scroll to the same relative position.
9. Click Edit on the iPhone pane. Change viewport to 430x932 (iPhone 16 Plus). Click Apply. Pane reloads at new dimensions. Scale factor updates.
10. Run for 4 hours with 5 tabs open (including one Responsive View tab). RAM stays under 700MB.
11. Cold start on a mid-range Windows 10 machine (i5, 8GB RAM): browser ready in under 2.5 seconds.

---

## Notes for Developers and AI Coding Tools Reading This Prompt

- CefSharp version must be 120 or higher.
- Do not use the global CEF cookie manager for per-tab operations. Always use `browser.RequestContext.GetCookieManager()`.
- Set `CefSharpSettings.RuntimeStyle = RuntimeStyle.ChromeRuntime` for best DevTools compatibility.
- WPF threading: all UI updates from CEF callbacks must be dispatched via `Application.Current.Dispatcher.Invoke`.
- Tab close must call `PurgeTabSession` before `Dispose`. For the Responsive View tab, purge all three pane browsers independently.
- The Nuke button clears the current tab context only (or all three panes if in Responsive View). It does not affect other open tabs.
- Responsive View panes must render at actual device pixel dimensions using `LayoutTransform` scale, not CSS zoom or container resize. Media queries must fire at the real viewport width.
- Scroll sync must use passive scroll listeners to avoid blocking the main thread.
- Zero persistence means zero. Nothing written to disk during a session except `settings.json`.