using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace Shaker.Markdown.Activities.Wizard
{
    /// <summary>
    /// The viewer's browser surface, and the only place in this package that touches WebView2.
    /// </summary>
    /// <remarks>
    /// WebView2 is optional. The Evergreen runtime is installed on most Windows machines and is not
    /// guaranteed on any of them, and a Studio without it must still open the viewer. So every entry point
    /// here is wrapped, marked <see cref="MethodImplOptions.NoInlining"/> so that a missing assembly faults
    /// this method rather than its caller, and returns null or false instead of throwing. The window falls
    /// back to the same FlowDocument renderer the canvas uses.
    /// </remarks>
    internal sealed class WebView2Host
    {
        /// <summary>The name the document's own folder is served under while it is being shown.</summary>
        /// <remarks>
        /// A page held in memory has no folder to be relative to, and a file:// base is not reachable from
        /// it. Mapping the folder to a host name gives the document's images an address that resolves, and
        /// confines what the page can read to that one folder.
        /// </remarks>
        private const string VirtualHost = "markdown.invalid";

        private readonly WebView2 _control;
        private string _mappedFolder;

        private WebView2Host(WebView2 control) => _control = control;

        /// <summary>The control to put in the window.</summary>
        internal FrameworkElement Element => _control;

        /// <summary>
        /// Creates the surface, or returns null when WebView2 is not usable on this machine.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static WebView2Host TryCreate()
        {
            try
            {
                // Throws when the Evergreen runtime is absent, and returns empty when it is too old.
                string version = CoreWebView2Environment.GetAvailableBrowserVersionString();

                if (string.IsNullOrEmpty(version))
                    return null;

                return new WebView2Host(new WebView2());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer is falling back from WebView2: " + exception);
                return null;
            }
        }

        /// <summary>
        /// Shows a rendered page, serving <paramref name="folder"/> so the document's own images load.
        /// </summary>
        /// <returns>False when the page could not be shown, so the caller can fall back.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal async Task<bool> ShowAsync(string bodyHtml, string title, bool dark, string folder)
        {
            try
            {
                await _control.EnsureCoreWebView2Async().ConfigureAwait(true);

                CoreWebView2 core = _control.CoreWebView2;

                if (core == null)
                    return false;

                // Nothing in a Markdown document has any business opening a dev tools window, a context
                // menu of browser commands, or a second window.
                core.Settings.AreDevToolsEnabled = false;
                core.Settings.AreDefaultContextMenusEnabled = false;
                core.Settings.IsScriptEnabled = false;
                core.Settings.IsStatusBarEnabled = false;

                MapFolder(core, folder);

                string page = Shaker.Markdown.Core.HtmlDocument.Build(
                    bodyHtml,
                    title,
                    dark ? Shaker.Markdown.Core.DocumentTheme.Dark : Shaker.Markdown.Core.DocumentTheme.Light,
                    basePath: null,
                    allowScripts: false,
                    baseHrefOverride: folder == null ? null : "https://" + VirtualHost + "/");

                core.NavigateToString(page);
                return true;
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer could not show the document in WebView2: " + exception);
                return false;
            }
        }

        /// <summary>Points the virtual host at the folder the document being shown came from.</summary>
        private void MapFolder(CoreWebView2 core, string folder)
        {
            if (string.Equals(_mappedFolder, folder, StringComparison.OrdinalIgnoreCase))
                return;

            if (_mappedFolder != null)
                core.ClearVirtualHostNameToFolderMapping(VirtualHost);

            _mappedFolder = null;

            if (string.IsNullOrEmpty(folder))
                return;

            // Read-only: the page may load what is in the folder and may not reach outside it.
            core.SetVirtualHostNameToFolderMapping(VirtualHost, folder, CoreWebView2HostResourceAccessKind.DenyCors);
            _mappedFolder = folder;
        }
    }
}
