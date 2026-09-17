using System;
using System.Activities;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities
{
    /// <summary>
    /// Shared plumbing for the activities that read Markdown at run time: the two knobs that decide how a
    /// document is parsed, in one place so they mean the same thing on every card.
    /// </summary>
    public abstract class MarkdownActivityBase<TResult> : CodeActivity<TResult>
    {
        /// <summary>Whether raw HTML inside the Markdown survives into the output.</summary>
        [Category(Categories.Options)]
        [DisplayName("Allow HTML")]
        [Description("Pass through raw HTML embedded in the Markdown instead of escaping it. Leave off for documents you did not write.")]
        public InArgument<bool> AllowHtml { get; set; }

        /// <summary>Whether a single newline ends the line, as it does in chat apps and issue trackers.</summary>
        [Category(Categories.Options)]
        [DisplayName("Line breaks are literal")]
        [Description("Treat a single newline as a line break, the way chat apps and issue trackers do, rather than joining the lines into one paragraph.")]
        public InArgument<bool> SoftBreakAsHardBreak { get; set; }

        /// <summary>Reads the options off the card.</summary>
        protected MarkdownOptions ReadOptions(CodeActivityContext context) => new MarkdownOptions
        {
            AllowHtml = AllowHtml.GetValue(context),
            SoftBreakAsHardBreak = SoftBreakAsHardBreak.GetValue(context)
        };
    }

    /// <summary>Renders Markdown to HTML.</summary>
    [DisplayName("Markdown To HTML")]
    [Description("Renders Markdown to HTML, either as a fragment to drop into an email body or as a complete styled page.")]
    public sealed class MarkdownToHtml : MarkdownActivityBase<string>
    {
        /// <summary>The document to render.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Markdown")]
        [Description("The Markdown text to render.")]
        public InArgument<string> Markdown { get; set; }

        /// <summary>
        /// Whether to produce a whole page rather than a fragment.
        /// </summary>
        /// <remarks>
        /// A fragment is what an HTML email body wants. A whole page — doctype, stylesheet and all — is what
        /// a browser control or a saved <c>.html</c> file wants, and it is the only one of the two that
        /// comes out looking like the document you wrote.
        /// </remarks>
        [Category(Categories.Options)]
        [DisplayName("Complete page")]
        [Description("Produce a full HTML page with a stylesheet, rather than a bare fragment. Turn this on when saving to a file or showing in a browser.")]
        public InArgument<bool> StandalonePage { get; set; }

        /// <summary>Which way round to colour the page, when producing a complete one.</summary>
        [Category(Categories.Options)]
        [DisplayName("Theme")]
        [Description("The colours of the complete page. Ignored when producing a fragment.")]
        public InArgument<DocumentTheme> Theme { get; set; }

        /// <summary>
        /// The folder relative image links are resolved against, when producing a complete page.
        /// </summary>
        [Category(Categories.Options)]
        [DisplayName("Base folder")]
        [Description("The folder a document's relative image paths point into. Usually the folder the .md file came from.")]
        public InArgument<string> BaseFolder { get; set; }

        /// <inheritdoc />
        protected override string Execute(CodeActivityContext context)
        {
            string markdown = Markdown.GetValue(context) ?? string.Empty;
            MarkdownOptions options = ReadOptions(context);
            string fragment = MarkdownEngine.ToHtml(markdown, options);

            if (!StandalonePage.GetValue(context))
                return fragment;

            return HtmlDocument.Build(
                fragment,
                MarkdownEngine.GetTitle(markdown, options),
                Theme.GetValue(context),
                BaseFolder.GetValue(context));
        }
    }

    /// <summary>Strips Markdown down to its words.</summary>
    [DisplayName("Markdown To Text")]
    [Description("Strips the markup out of Markdown, leaving the words. Useful for a log line, a subject line or a plain-text email part.")]
    public sealed class MarkdownToText : MarkdownActivityBase<string>
    {
        /// <summary>The document to flatten.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Markdown")]
        [Description("The Markdown text to strip.")]
        public InArgument<string> Markdown { get; set; }

        /// <inheritdoc />
        protected override string Execute(CodeActivityContext context)
        {
            return MarkdownEngine.ToPlainText(Markdown.GetValue(context), ReadOptions(context));
        }
    }

    /// <summary>Reads a <c>.md</c> file from disk.</summary>
    [DisplayName("Read Markdown File")]
    [Description("Reads a .md file and reports its text, its title and how many headings it has.")]
    public sealed class ReadMarkdownFile : MarkdownActivityBase<string>
    {
        /// <summary>The file to read.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("File path")]
        [Description("The .md file to read, for example \"Documentation\\process.md\".")]
        public InArgument<string> FilePath { get; set; }

        /// <summary>The document's first heading, or an empty string when it has none.</summary>
        [Category(Categories.Output)]
        [DisplayName("Title")]
        [Description("The document's first heading, which is what a reader would call it. Empty when the document has no headings.")]
        public OutArgument<string> Title { get; set; }

        /// <summary>The folder the file came from, ready to hand to Markdown To HTML as its base folder.</summary>
        [Category(Categories.Output)]
        [DisplayName("Base folder")]
        [Description("The folder the file was read from. Hand this to Markdown To HTML so the document's images resolve.")]
        public OutArgument<string> BaseFolder { get; set; }

        /// <inheritdoc />
        protected override string Execute(CodeActivityContext context)
        {
            string path = FilePath.GetValue(context);

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Read Markdown File needs a file path.", nameof(FilePath));

            string full = Path.GetFullPath(path);

            if (!File.Exists(full))
                throw new FileNotFoundException("There is no Markdown file at " + full + ".", full);

            // Detects a BOM and falls back to UTF-8, which is what a .md file in a repository will be.
            string text = File.ReadAllText(full, Encoding.UTF8);

            Title.SetValue(context, MarkdownEngine.GetTitle(text, ReadOptions(context)) ?? string.Empty);
            BaseFolder.SetValue(context, Path.GetDirectoryName(full) ?? string.Empty);

            return text;
        }
    }

    /// <summary>Lists a document's headings.</summary>
    [DisplayName("Get Markdown Outline")]
    [Description("Lists a document's headings in order, for building a table of contents or checking that a template was filled in.")]
    public sealed class GetMarkdownOutline : MarkdownActivityBase<IList<MarkdownHeading>>
    {
        /// <summary>The document to read.</summary>
        [RequiredArgument]
        [Category(Categories.Input)]
        [DisplayName("Markdown")]
        [Description("The Markdown text whose headings you want.")]
        public InArgument<string> Markdown { get; set; }

        /// <inheritdoc />
        protected override IList<MarkdownHeading> Execute(CodeActivityContext context)
        {
            return MarkdownEngine.GetOutline(Markdown.GetValue(context), ReadOptions(context));
        }
    }
}
