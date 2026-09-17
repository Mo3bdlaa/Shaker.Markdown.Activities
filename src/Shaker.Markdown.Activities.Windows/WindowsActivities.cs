using System;
using System.Activities;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Shaker.Markdown.Core;
using Categories = Shaker.Markdown.Activities.Categories;

namespace Shaker.Markdown.Activities.Windows
{
    /// <summary>Convenience helpers for arguments a workflow may simply have left unbound.</summary>
    internal static class ArgumentExtensions
    {
        internal static T GetValue<T>(this InArgument<T> argument, ActivityContext context) =>
            argument == null ? default(T) : argument.Get(context);

        internal static void SetValue<T>(this OutArgument<T> argument, ActivityContext context, T value)
        {
            argument?.Set(context, value);
        }
    }

    /// <summary>What the two activities here have in common: a document, and how to read it.</summary>
    public abstract class MarkdownWindowActivity<TResult> : CodeActivity<TResult>
    {
        /// <summary>The document.</summary>
        [Category(Categories.Input)]
        [DisplayName("Markdown")]
        [Description("The Markdown to show. Leave blank and set File path instead to read it from a file.")]
        public InArgument<string> Markdown { get; set; }

        /// <summary>A file to read the document from, instead of passing the text.</summary>
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("A .md file to read instead of passing the text. Its folder is also where the document's images are looked for.")]
        public InArgument<string> FilePath { get; set; }

        /// <summary>Whether raw HTML in the document is rendered rather than escaped.</summary>
        [Category(Categories.Options)]
        [DisplayName("Allow HTML")]
        [Description("Render raw HTML embedded in the Markdown. Leave off for documents you did not write.")]
        public InArgument<bool> AllowHtml { get; set; }

        /// <summary>Which way round to colour the document.</summary>
        [Category(Categories.Appearance)]
        [DisplayName("Theme")]
        [Description("Light or dark. Printing is usually better on Light.")]
        public InArgument<DocumentTheme> Theme { get; set; }

        /// <summary>How long to allow before giving up, in seconds.</summary>
        [Category(Categories.Options)]
        [DisplayName("Timeout (seconds)")]
        [Description("How long to wait before giving up. Leave blank for 120 seconds — for Show Markdown this is how long the window may stay open.")]
        public InArgument<int> TimeoutSeconds { get; set; }

        /// <summary>Reads the document from wherever the card says, and says where its images live.</summary>
        protected string ReadDocument(CodeActivityContext context, out string folder)
        {
            folder = null;

            string path = FilePath.GetValue(context);

            if (!string.IsNullOrWhiteSpace(path))
            {
                string full = Path.GetFullPath(path);

                if (!File.Exists(full))
                    throw new FileNotFoundException("There is no Markdown file at " + full + ".", full);

                folder = Path.GetDirectoryName(full);
                return File.ReadAllText(full, System.Text.Encoding.UTF8);
            }

            string markdown = Markdown.GetValue(context);

            if (string.IsNullOrWhiteSpace(markdown))
                throw new ArgumentException("Set either Markdown or File path.", nameof(Markdown));

            return markdown;
        }

        /// <summary>The options the document should be read under.</summary>
        protected MarkdownOptions ReadOptions(CodeActivityContext context) =>
            new MarkdownOptions { AllowHtml = AllowHtml.GetValue(context) };

        /// <summary>The timeout, defaulted when the card leaves it blank.</summary>
        protected TimeSpan ReadTimeout(CodeActivityContext context)
        {
            int seconds = TimeoutSeconds.GetValue(context);
            return TimeSpan.FromSeconds(seconds > 0 ? seconds : 120);
        }
    }

    /// <summary>Shows a Markdown document to whoever is at the machine.</summary>
    [Category(Categories.Markdown)]
    [DisplayName("Show Markdown")]
    [Description("Shows rendered Markdown in a window and waits until it is closed. Attended only — it needs somebody at the machine.")]
    public sealed class ShowMarkdown : MarkdownWindowActivity<bool>
    {
        /// <summary>The window's title bar.</summary>
        [Category(Categories.Appearance)]
        [DisplayName("Title")]
        [Description("The window's title. Leave blank to use the document's first heading.")]
        public InArgument<string> Title { get; set; }

        /// <summary>The window's width in pixels.</summary>
        [Category(Categories.Appearance)]
        [DisplayName("Width")]
        [Description("The window's width in pixels. Leave blank for 900.")]
        public InArgument<double> Width { get; set; }

        /// <summary>The window's height in pixels.</summary>
        [Category(Categories.Appearance)]
        [DisplayName("Height")]
        [Description("The window's height in pixels. Leave blank for 650.")]
        public InArgument<double> Height { get; set; }

        /// <inheritdoc />
        /// <returns>True when the window was shown and closed normally.</returns>
        protected override bool Execute(CodeActivityContext context)
        {
            WebViewSurface.RequireRuntime();

            string markdown = ReadDocument(context, out string folder);
            MarkdownOptions options = ReadOptions(context);
            DocumentTheme theme = Theme.GetValue(context);
            string title = Title.GetValue(context);

            if (string.IsNullOrWhiteSpace(title))
                title = MarkdownEngine.GetTitle(markdown, options);

            double width = Width.GetValue(context);
            double height = Height.GetValue(context);

            return StaHost.Run(async () =>
            {
                using (var surface = new WebViewSurface())
                {
                    var dialog = new MarkdownDialog(surface, title, theme, width, height);

                    // Shown first, then loaded: WebView2 will not start until its control is in a window
                    // that has been realised.
                    dialog.Show();

                    await surface.LoadAsync(markdown, options, title, theme, folder).ConfigureAwait(true);

                    var closed = new TaskCompletionSource<bool>();
                    dialog.Closed += (sender, e) => closed.TrySetResult(true);

                    // Already gone by the time it finished loading, if somebody was quick.
                    if (!dialog.IsVisible)
                        closed.TrySetResult(true);

                    await closed.Task.ConfigureAwait(true);
                    return true;
                }
            }, ReadTimeout(context));
        }
    }

    /// <summary>Prints a Markdown document to a PDF file.</summary>
    [Category(Categories.Markdown)]
    [DisplayName("Markdown To PDF")]
    [Description("Renders Markdown and prints it to a PDF file. Needs the WebView2 runtime, which does the printing.")]
    public sealed class MarkdownToPdf : MarkdownWindowActivity<string>
    {
        /// <summary>Where to write the PDF.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Output path")]
        [Description("Where to write the PDF, for example \"Reports\\summary.pdf\".")]
        public InArgument<string> OutputPath { get; set; }

        /// <summary>Whether the page is wider than it is tall.</summary>
        [Category(Categories.Options)]
        [DisplayName("Landscape")]
        [Description("Print landscape rather than portrait. Useful for a document built around a wide table.")]
        public InArgument<bool> Landscape { get; set; }

        /// <inheritdoc />
        /// <returns>The full path of the PDF that was written.</returns>
        protected override string Execute(CodeActivityContext context)
        {
            WebViewSurface.RequireRuntime();

            string output = OutputPath.GetValue(context);

            if (string.IsNullOrWhiteSpace(output))
                throw new ArgumentException("Markdown To PDF needs an output path.", nameof(OutputPath));

            string full = Path.GetFullPath(output);
            string directory = Path.GetDirectoryName(full);

            // Created rather than demanded: a report written to a dated folder should not need a Create
            // Directory in front of it.
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string markdown = ReadDocument(context, out string folder);
            MarkdownOptions options = ReadOptions(context);
            DocumentTheme theme = Theme.GetValue(context);
            bool landscape = Landscape.GetValue(context);
            string title = MarkdownEngine.GetTitle(markdown, options);

            bool printed = StaHost.Run(async () =>
            {
                using (var surface = new WebViewSurface())
                {
                    // Off-screen rather than hidden: WebView2 does not start in a window that was never
                    // realised, and a window at these coordinates is on no monitor anybody has.
                    var host = new Window
                    {
                        Width = 900,
                        Height = 650,
                        Left = -32000,
                        Top = -32000,
                        ShowInTaskbar = false,
                        ShowActivated = false,
                        WindowStyle = WindowStyle.None,
                        Content = surface.Control
                    };

                    try
                    {
                        host.Show();
                        await surface.LoadAsync(markdown, options, title, theme, folder).ConfigureAwait(true);
                        return await surface.PrintToPdfAsync(full, landscape).ConfigureAwait(true);
                    }
                    finally
                    {
                        host.Close();
                    }
                }
            }, ReadTimeout(context));

            if (!printed)
                throw new MarkdownWindowException("WebView2 did not write a PDF to " + full + ".");

            return full;
        }
    }
}
