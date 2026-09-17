using System;
using System.Activities.Presentation;
using System.Diagnostics;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// The stock card, with this pack's icon on it. Everything except the note uses it: the properties panel
    /// already shows their arguments well, and what they were missing was an icon.
    /// </summary>
    public class MarkdownActivityDesigner : ActivityDesigner
    {
        /// <summary>Creates a designer for an activity.</summary>
        /// <param name="activity">
        /// The activity this designer belongs to. Passed by the subclass rather than read from the model,
        /// because the Activities panel asks a designer type for its icon before any activity exists.
        /// </param>
        protected MarkdownActivityDesigner(Type activity)
        {
            try
            {
                Icon = Glyphs.For(activity?.Name ?? string.Empty);
            }
            catch (Exception exception)
            {
                // An icon is not worth a card. Studio falls back to its own glyph.
                Debug.WriteLine("Markdown icon could not be built for " + activity + ": " + exception);
            }
        }
    }

    // One designer per activity. They differ only in which activity they belong to, but the activities panel
    // asks a designer type for its icon before any activity exists, so the type itself has to know.

    /// <summary>The icon for <see cref="MarkdownToHtml"/>.</summary>
    public sealed class MarkdownToHtmlDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public MarkdownToHtmlDesigner() : base(typeof(MarkdownToHtml))
        {
        }
    }

    /// <summary>The icon for <see cref="MarkdownToText"/>.</summary>
    public sealed class MarkdownToTextDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public MarkdownToTextDesigner() : base(typeof(MarkdownToText))
        {
        }
    }

    /// <summary>The icon for <see cref="ReadMarkdownFile"/>.</summary>
    public sealed class ReadMarkdownFileDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public ReadMarkdownFileDesigner() : base(typeof(ReadMarkdownFile))
        {
        }
    }

    /// <summary>The icon for <see cref="GetMarkdownOutline"/>.</summary>
    public sealed class GetMarkdownOutlineDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetMarkdownOutlineDesigner() : base(typeof(GetMarkdownOutline))
        {
        }
    }

    /// <summary>The icon for <see cref="GetDataTableFromMarkdown"/>.</summary>
    public sealed class GetDataTableFromMarkdownDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public GetDataTableFromMarkdownDesigner() : base(typeof(GetDataTableFromMarkdown))
        {
        }
    }
}
