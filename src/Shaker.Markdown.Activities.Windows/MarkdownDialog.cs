using System.Windows;
using System.Windows.Media;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities.Windows
{
    /// <summary>The window <c>Show Markdown</c> puts on screen.</summary>
    internal sealed class MarkdownDialog : Window
    {
        /// <summary>Builds the window around a browser surface.</summary>
        internal MarkdownDialog(WebViewSurface surface, string title, DocumentTheme theme, double width, double height)
        {
            Title = string.IsNullOrWhiteSpace(title) ? "Markdown" : title;
            Width = width > 0 ? width : 900;
            Height = height > 0 ? height : 650;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Matched to the document so the window does not flash white around a dark page while it loads.
            Background = theme == DocumentTheme.Dark
                ? new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E))
                : Brushes.White;

            Content = surface.Control;
        }
    }
}
