namespace Shaker.Markdown.Core
{
    /// <summary>One heading found in a document.</summary>
    public sealed class MarkdownHeading
    {
        /// <summary>Creates a heading.</summary>
        public MarkdownHeading(int level, string text, string anchor)
        {
            Level = level;
            Text = text ?? string.Empty;
            Anchor = anchor ?? string.Empty;
        }

        /// <summary>How deep the heading sits, 1 for <c>#</c> through 6 for <c>######</c>.</summary>
        public int Level { get; }

        /// <summary>The heading's text, with any formatting inside it flattened away.</summary>
        public string Text { get; }

        /// <summary>The anchor the renderer gives the heading, for linking to it.</summary>
        public string Anchor { get; }

        /// <inheritdoc />
        public override string ToString() => new string('#', Level) + " " + Text;
    }
}
