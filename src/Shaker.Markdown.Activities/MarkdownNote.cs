using System.Activities;
using System.ComponentModel;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities
{
    /// <summary>
    /// A block of Markdown drawn on the canvas, rendered rather than shown as its source. It does nothing
    /// when the process runs — it is there to be read by whoever opens the workflow next.
    /// </summary>
    /// <remarks>
    /// This is the activity the package exists for. Studio draws the rendered document itself through the
    /// designer in <c>Shaker.Markdown.Activities.Design</c>; in a cross-platform project, where Studio does
    /// not load WPF designers, the note still sits on the canvas and still carries its text, it is simply
    /// shown as source in the properties panel.
    /// </remarks>
    [DisplayName("Markdown Note")]
    [Description("Documents a workflow with rendered Markdown. Does nothing when the process runs.")]
    public sealed class MarkdownNote : CodeActivity
    {
        /// <summary>The document, when <see cref="Source"/> is <see cref="MarkdownSource.Property"/>.</summary>
        /// <remarks>
        /// An argument rather than a plain string, so the text can come from a variable or be built by an
        /// expression like any other property. The canvas can only draw what it can read without running the
        /// workflow, so a literal is rendered and an expression is reported as one — the note says which it
        /// is rather than appearing empty.
        /// </remarks>
        [Category(Categories.Note)]
        [DisplayName("Markdown")]
        [Description("The Markdown to render on the canvas. Headings, lists, tables, links and code all work. Use the editor on the card, or the … button here for the full expression editor.")]
        public InArgument<string> Markdown { get; set; }

        /// <summary>Where the Markdown comes from.</summary>
        [Category(Categories.Note)]
        [DisplayName("Source")]
        [Description("Take the text from this activity's Markdown property, from its annotation, or from a .md file in the project.")]
        public MarkdownSource Source { get; set; } = MarkdownSource.Property;

        /// <summary>
        /// The file to read, when <see cref="Source"/> is <see cref="MarkdownSource.File"/>. Relative paths
        /// are resolved against the project folder.
        /// </summary>
        [Category(Categories.Note)]
        [DisplayName("File path")]
        [Description("The .md file to render, for example \"Documentation\\process.md\". Relative to the project folder. Accepts a variable or an expression.")]
        public InArgument<string> FilePath { get; set; }

        /// <summary>Whether raw HTML inside the Markdown is rendered rather than shown as text.</summary>
        [Category(Categories.Note)]
        [DisplayName("Allow HTML")]
        [Description("Render raw HTML embedded in the Markdown. Leave off unless you wrote the document yourself.")]
        public bool AllowHtml { get; set; }

        /// <summary>How tall the rendered note may grow before it scrolls, in pixels.</summary>
        [Category(Categories.Appearance)]
        [DisplayName("Maximum height")]
        [Description("How tall the note may grow on the canvas before it scrolls. 0 lets it grow to fit.")]
        public double MaxHeight { get; set; } = 400d;

        /// <summary>Whether the card shows its editor as well as the rendered document.</summary>
        /// <remarks>
        /// Design time only, and stored on the activity rather than in the designer so that a note left open
        /// for editing is still open the next time the workflow is opened.
        /// </remarks>
        [Category(Categories.Appearance)]
        [DisplayName("Show editor")]
        [Description("Show the Markdown editor on the card, above the rendered document.")]
        public bool ShowEditor { get; set; }

        /// <summary>Does nothing. A note is documentation, not a step.</summary>
        protected override void Execute(CodeActivityContext context)
        {
        }

        /// <summary>The options this note's text should be read under.</summary>
        public MarkdownOptions ToOptions() => new MarkdownOptions { AllowHtml = AllowHtml };
    }
}
