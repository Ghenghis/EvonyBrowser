using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CefSharp;
using CefSharp.Wpf;

namespace SvonyBrowser.Helpers
{

    /// <summary>
    /// Helper class for browser operations using CefSharp 84.4.10 API.
    /// </summary>
    public static class BrowserHelper
    {
        /// <summary>
        /// Creates a new ChromiumWebBrowser instance.
        /// </summary>
        public static ChromiumWebBrowser CreateBrowser(string initialUrl = "about:blank")
        {
            var browser = new ChromiumWebBrowser();
            browser.Address = initialUrl;
            return browser;
        }

        /// <summary>
        /// Sets the row for a browser in a Grid.
        /// </summary>
        public static void SetGridRow(object browser, int row)
        {
            if (browser is UIElement element)
                Grid.SetRow(element, row);
        }

        /// <summary>
        /// Loads HTML content into the browser.
        /// </summary>
        public static void LoadHtml(object browser, string html)
        {
            if (browser is IWebBrowser wb)
            {
                wb.LoadHtml(html, "http://localhost/");
            }
        }

        /// <summary>
        /// Reloads the browser.
        /// </summary>
        public static void Reload(object browser, bool ignoreCache = false)
        {
            if (browser is IWebBrowser wb)
            {
                wb.Reload(ignoreCache);
            }
        }

        /// <summary>
        /// Deletes all cookies.
        /// </summary>
        public static async Task DeleteAllCookiesAsync()
        {
            try
            {
                var cookieManager = CefSharp.Cef.GetGlobalCookieManager();
                if (cookieManager != null)
                {
                    await cookieManager.DeleteCookiesAsync("", "");
                }
            }
            catch (Exception ex)
            {
                App.Logger?.Error(ex, "Failed to delete cookies");
            }
        }

        /// <summary>
        /// Subscribes to FrameLoadEnd event.
        /// </summary>
        public static void OnFrameLoadEnd(object browser, Action<string, bool> callback)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                cwb.FrameLoadEnd += (sender, args) =>
                {
                    callback(args.Url, args.Frame?.IsMain ?? false);
                };
            }
        }

        /// <summary>
        /// Subscribes to LoadError event.
        /// </summary>
        public static void OnLoadError(object browser, Action<string, string, int> callback)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                cwb.LoadError += (sender, args) =>
                {
                    callback(args.FailedUrl, args.ErrorText, (int)args.ErrorCode);
                };
            }
        }

        /// <summary>
        /// Navigates to a URL.
        /// </summary>
        public static void Navigate(object browser, string url)
        {
            if (browser is IWebBrowser wb)
            {
                wb.Load(url);
            }
        }

        /// <summary>
        /// Gets the current URL.
        /// </summary>
        public static string GetAddress(object browser)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                return cwb.Address ?? string.Empty;
            }
            return string.Empty;
        }

        /// <summary>
        /// Checks if the browser is focused.
        /// </summary>
        public static bool IsFocused(object browser)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                return cwb.IsFocused;
            }
            return false;
        }

        /// <summary>
        /// Adds browser to a ContentControl.
        /// </summary>
        public static void AddToContainer(ContentControl container, object browser)
        {
            if (container != null && browser != null)
                container.Content = browser;
        }

        /// <summary>
        /// Goes back in browser history.
        /// </summary>
        public static void GoBack(object browser)
        {
            if (browser is IWebBrowser wb && wb.CanGoBack)
            {
                wb.Back();
            }
        }

        /// <summary>
        /// Goes forward in browser history.
        /// </summary>
        public static void GoForward(object browser)
        {
            if (browser is IWebBrowser wb && wb.CanGoForward)
            {
                wb.Forward();
            }
        }

        /// <summary>
        /// Executes JavaScript in the browser.
        /// </summary>
        public static void ExecuteScript(object browser, string script)
        {
            if (browser is IWebBrowser wb && wb.IsBrowserInitialized)
            {
                wb.GetMainFrame()?.ExecuteJavaScriptAsync(script);
            }
        }

        /// <summary>
        /// Disposes a browser instance.
        /// </summary>
        public static void DisposeBrowser(object browser)
        {
            if (browser is IDisposable d)
            {
                d.Dispose();
            }
        }

        /// <summary>
        /// Registers an event handler for address changes.
        /// </summary>
        public static void OnAddressChanged(object browser, Action<string> handler)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                cwb.AddressChanged += (s, e) => handler(cwb.Address ?? "");
            }
        }

        /// <summary>
        /// Registers an event handler for title changes.
        /// </summary>
        public static void OnTitleChanged(object browser, Action<string> handler)
        {
            if (browser is ChromiumWebBrowser cwb)
            {
                cwb.TitleChanged += (s, e) =>
                {
                    if (e is TitleChangedEventArgs tea)
                    {
                        handler(tea.Title ?? "");
                    }
                };
            }
        }
    }

}