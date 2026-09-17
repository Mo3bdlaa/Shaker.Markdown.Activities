using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Shaker.Markdown.Activities.Design;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities.Wizard
{
    /// <summary>
    /// The window behind the ribbon's <c>Markdown</c> button: the project's <c>.md</c> files on the left,
    /// the one you picked rendered on the right.
    /// </summary>
    /// <remarks>
    /// This is as close as a package can get to "open a .md file in Studio". Studio has no extension point
    /// for registering a viewer for a file type — <c>IWorkflowDesignApi</c> offers wizards, settings and
    /// analyzer rules, and nothing that owns an editor tab — so the ribbon is the door, and the window finds
    /// the files itself.
    /// </remarks>
    public sealed class MarkdownViewerWindow : Window
    {
        private readonly ListBox _files = new ListBox { BorderThickness = new Thickness(0) };
        private readonly ContentControl _surface = new ContentControl();
        private readonly TextBlock _status = new TextBlock { Margin = new Thickness(8, 4, 8, 4), FontSize = 11 };
        private readonly FlowDocumentScrollViewer _fallback;
        private readonly WebView2Host _browser;
        private readonly string _root;
        private bool _dark;

        /// <summary>Opens the viewer over a project folder.</summary>
        /// <param name="projectFolder">The project to list, or null to start empty and let the user browse.</param>
        public MarkdownViewerWindow(string projectFolder)
        {
            _root = projectFolder;

            Title = "Markdown";
            Width = 1000;
            Height = 700;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            _fallback = new FlowDocumentScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                IsToolBarVisible = false,
                IsSelectionEnabled = true,
                Padding = new Thickness(16),
                BorderThickness = new Thickness(0)
            };

            // WebView2 when the machine has it, the canvas renderer when it does not. The status bar says
            // which, because the difference is visible and a reader should not have to guess why.
            _browser = WebView2Host.TryCreate();
            _surface.Content = _browser != null ? _browser.Element : (UIElement)_fallback;

            Content = BuildLayout();

            _files.SelectionChanged += (sender, e) => Show(_files.SelectedItem as MarkdownFileEntry);

            ApplyTheme();

            // Deferred to Loaded rather than run here. EnsureCoreWebView2Async needs the control to be in
            // the visual tree, and failing that first call would send the window to the fallback renderer
            // permanently, on a machine where WebView2 works perfectly well.
            Loaded += (sender, e) => Reload();
        }

        private UIElement BuildLayout()
        {
            var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 6, 6, 6) };
            toolbar.Children.Add(Button("Refresh", (sender, e) => Reload()));
            toolbar.Children.Add(Button("Open file…", (sender, e) => Browse()));
            toolbar.Children.Add(Button("Light / dark", (sender, e) => ToggleTheme()));

            var left = new DockPanel { Width = 280 };
            var heading = new TextBlock
            {
                Text = "Markdown in this project",
                Margin = new Thickness(8, 6, 8, 6),
                FontWeight = FontWeights.SemiBold
            };
            DockPanel.SetDock(heading, Dock.Top);
            left.Children.Add(heading);
            left.Children.Add(_files);

            var split = new Grid();
            split.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            split.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            Grid.SetColumn(left, 0);
            Grid.SetColumn(_surface, 1);
            split.Children.Add(left);
            split.Children.Add(_surface);

            var root = new DockPanel();
            DockPanel.SetDock(toolbar, Dock.Top);
            DockPanel.SetDock(_status, Dock.Bottom);
            root.Children.Add(toolbar);
            root.Children.Add(_status);
            root.Children.Add(split);

            return root;
        }

        private static Button Button(string text, RoutedEventHandler onClick)
        {
            var button = new Button { Content = text, Padding = new Thickness(10, 3, 10, 3), Margin = new Thickness(0, 0, 6, 0) };
            button.Click += onClick;
            return button;
        }

        /// <summary>Re-reads the project's file list, keeping whatever was open selected.</summary>
        private void Reload()
        {
            string selected = (_files.SelectedItem as MarkdownFileEntry)?.FullPath;

            IList<MarkdownFileEntry> found = MarkdownFileEntry.Find(_root);
            _files.ItemsSource = found;

            if (found.Count == 0)
            {
                _status.Text = _root == null
                    ? "No project folder was found. Use Open file to pick a .md file."
                    : "No .md files under " + _root + ".";
                return;
            }

            MarkdownFileEntry restored = null;
            foreach (MarkdownFileEntry entry in found)
            {
                if (string.Equals(entry.FullPath, selected, StringComparison.OrdinalIgnoreCase))
                    restored = entry;
            }

            _files.SelectedItem = restored ?? found[0];
        }

        /// <summary>Opens a file from anywhere, for documentation that lives outside the project.</summary>
        private void Browse()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Markdown (*.md;*.markdown)|*.md;*.markdown|All files (*.*)|*.*",
                InitialDirectory = _root ?? string.Empty
            };

            if (dialog.ShowDialog(this) == true)
            {
                _files.SelectedItem = null;
                Show(new MarkdownFileEntry(dialog.FileName, Path.GetFileName(dialog.FileName)));
            }
        }

        private void ToggleTheme()
        {
            _dark = !_dark;
            ApplyTheme();
            Show(_current);
        }

        private void ApplyTheme()
        {
            MarkdownPalette palette = _dark ? MarkdownPalette.Dark : MarkdownPalette.Light;

            Background = palette.Background;
            _files.Background = palette.Subtle;
            _files.Foreground = palette.Text;
            _status.Foreground = palette.Muted;
            _fallback.Background = palette.Background;
        }

        private MarkdownFileEntry _current;

        /// <summary>Renders a file into whichever surface this window ended up with.</summary>
        private async void Show(MarkdownFileEntry entry)
        {
            _current = entry;

            if (entry == null)
                return;

            string markdown;

            try
            {
                markdown = File.ReadAllText(entry.FullPath, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown viewer could not read " + entry.FullPath + ": " + exception);
                _status.Text = entry.RelativePath + " could not be read: " + exception.Message;
                return;
            }

            // The viewer reads other people's files, so raw HTML stays off and script stays off with it.
            var options = new MarkdownOptions { AllowHtml = false };
            string folder = Path.GetDirectoryName(entry.FullPath);
            string title = MarkdownEngine.GetTitle(markdown, options) ?? entry.RelativePath;

            bool shown = false;

            if (_browser != null)
            {
                shown = await _browser.ShowAsync(
                    MarkdownEngine.ToHtml(markdown, options), title, _dark, folder);

                // One failure is enough: swap the surface for good rather than retrying on every click.
                if (!shown)
                    _surface.Content = _fallback;
            }

            if (!shown)
            {
                _fallback.Document = FlowDocumentMarkdownRenderer.Render(
                    markdown, options, _dark ? MarkdownPalette.Dark : MarkdownPalette.Light, folder);
            }

            _status.Text = entry.FullPath + "   ·   " + (shown ? "WebView2" : "built-in renderer");
        }
    }
}
