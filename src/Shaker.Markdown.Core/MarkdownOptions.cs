using System;

namespace Shaker.Markdown.Core
{
    /// <summary>Which set of Markdown rules to read a document under.</summary>
    public enum MarkdownFlavor
    {
        /// <summary>Plain CommonMark, and nothing else.</summary>
        CommonMark = 0,

        /// <summary>
        /// CommonMark plus the extensions people expect from a README: tables, task lists, footnotes,
        /// strikethrough, automatic links and heading anchors.
        /// </summary>
        Advanced = 1
    }

    /// <summary>How a piece of Markdown should be read.</summary>
    public sealed class MarkdownOptions
    {
        /// <summary>The rules to read the document under. Advanced by default, as READMEs are written.</summary>
        public MarkdownFlavor Flavor { get; set; } = MarkdownFlavor.Advanced;

        /// <summary>
        /// Whether raw HTML embedded in the Markdown is passed through rather than escaped.
        /// </summary>
        /// <remarks>
        /// Off by default, and deliberately so. Markdown that reaches this library usually comes from a file
        /// in somebody's repository or from a variable filled at run time, and raw HTML in either can carry
        /// script that the WebView2 viewer would happily execute. Turn it on only for Markdown you wrote.
        /// </remarks>
        public bool AllowHtml { get; set; }

        /// <summary>Whether a single newline ends the line, as it does in chat apps and issue trackers.</summary>
        public bool SoftBreakAsHardBreak { get; set; }

        /// <summary>A copy of these options, so a caller cannot mutate one held elsewhere.</summary>
        public MarkdownOptions Clone() => new MarkdownOptions
        {
            Flavor = Flavor,
            AllowHtml = AllowHtml,
            SoftBreakAsHardBreak = SoftBreakAsHardBreak
        };

        /// <summary>The options used when a caller does not say. Never handed out directly.</summary>
        public static MarkdownOptions Default => new MarkdownOptions();
    }
}
