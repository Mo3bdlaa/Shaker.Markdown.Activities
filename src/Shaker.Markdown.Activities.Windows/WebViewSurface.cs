using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities.Windows
{
    /// <summary>
    /// A WebView2 showing a rendered Markdown document, and the one place this package touches the browser.
    /// </summary>
    /// <remarks>
    /// Unlike the viewer in Studio's ribbon, there is no FlowDocument fallback here. The two activities in
    /// this package exist to show a document faithfully and to print one, and a fallback renderer would do
    /// the first badly and the second not at all — so a missing runtime is reported rather than worked
    /// around.
    /// </remarks>
    internal sealed class WebViewSurface : IDisposable
    {
        /// <summary>The name the document's own folder is served under while it is being shown.</summary>
        private const string VirtualHost = "markdown.invalid";

        private readonly WebView2 _control = new WebView2();
        private TaskCompletionSource<bool> _navigated;

        /// <summary>The control, to put in a window.</summary>
        internal WebView2 Control => _control;

        /// <summary>Checks that WebView2 can run at all, and says what to do when it cannot.</summary>
        internal static void RequireRuntime()
        {
            string version;

            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            }
            catch (Exception exception)
            {
                throw new MarkdownWindowException(
                    "The WebView2 runtime is needed to show or print Markdown, and it could not be found. " +
                    "Install the Microsoft Edge WebView2 Evergreen Runtime on this machine.", exception);
            }

            if (string.IsNullOrEmpty(version))
            {
                throw new MarkdownWindowException(
                    "The WebView2 runtime is needed to show or print Markdown, and it is not installed. " +
                    "Install the Microsoft Edge WebView2 Evergreen Runtime on this machine.");
            }
        }

        /// <summary>Starts the browser and loads a document, returning once it has finished rendering.</summary>
        internal async Task LoadAsync(string markdown, MarkdownOptions options, string title, DocumentTheme theme, string folder)
        {
            await _control.EnsureCoreWebView2Async().ConfigureAwait(true);

            CoreWebView2 core = _control.CoreWebView2
                ?? throw new MarkdownWindowException("WebView2 started but gave no browser to draw into.");

            core.Settings.AreDevToolsEnabled = false;
            core.Settings.AreDefaultContextMenusEnabled = false;
            core.Settings.IsScriptEnabled = false;
            core.Settings.IsStatusBarEnabled = false;

            if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
            {
                // Read-only, and scoped to the one folder the document came from: the page may load the
                // images beside it and may not reach anywhere else on the disk.
                core.SetVirtualHostNameToFolderMapping(
                    VirtualHost, folder, CoreWebView2HostResourceAccessKind.DenyCors);
            }
            else
            {
                folder = null;
            }

            string page = HtmlDocument.Build(
                MarkdownEngine.ToHtml(markdown, options),
                title,
                theme,
                basePath: null,
                allowScripts: false,
                baseHrefOverride: folder == null ? null : "https://" + VirtualHost + "/");

            _navigated = new TaskCompletionSource<bool>();
            core.NavigationCompleted += OnNavigationCompleted;
            core.NavigateToString(page);

            // Printing before the page has laid out produces a blank sheet, so both callers wait here.
            await _navigated.Task.ConfigureAwait(true);
            core.NavigationCompleted -= OnNavigationCompleted;
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _navigated?.TrySetResult(e.IsSuccess);
        }

        /// <summary>Prints what is loaded to a PDF file.</summary>
        internal async Task<bool> PrintToPdfAsync(string path, bool landscape)
        {
            CoreWebView2PrintSettings settings = _control.CoreWebView2.Environment.CreatePrintSettings();
            settings.ShouldPrintBackgrounds = true;
            settings.Orientation = landscape
                ? CoreWebView2PrintOrientation.Landscape
                : CoreWebView2PrintOrientation.Portrait;

            return await _control.CoreWebView2.PrintToPdfAsync(path, settings).ConfigureAwait(true);
        }

        /// <inheritdoc />
        public void Dispose() => _control.Dispose();
    }
}
