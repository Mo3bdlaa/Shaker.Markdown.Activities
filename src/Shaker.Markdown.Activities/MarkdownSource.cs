using System.ComponentModel;

namespace Shaker.Markdown.Activities
{
    /// <summary>Where a note on the canvas takes its Markdown from.</summary>
    public enum MarkdownSource
    {
        /// <summary>From the note's own <c>Markdown</c> property, typed into the card.</summary>
        [Description("Typed into the activity")]
        Property = 0,

        /// <summary>
        /// From the note's own annotation, so the documentation lives where Studio already keeps it.
        /// </summary>
        [Description("This activity's annotation")]
        Annotation = 1,

        /// <summary>From a <c>.md</c> file in the project, so one document serves the repository and the canvas.</summary>
        [Description("A .md file in the project")]
        File = 2
    }
}
