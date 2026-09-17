using System.Activities;

namespace Shaker.Markdown.Activities
{
    /// <summary>The names the properties panel and the activities panel group by.</summary>
    public static class Categories
    {
        /// <summary>
        /// Where every activity in this pack appears in the Activities panel.
        /// </summary>
        /// <remarks>
        /// Deliberately not the package id. The package is installed and listed as
        /// <c>Shaker.Markdown.Activities</c>, which is what a project's dependencies should say; what
        /// somebody searching the panel is looking for is "Markdown", so that is the folder they sit in.
        ///
        /// Studio reads this from the attribute table the design assembly registers, not from an attribute
        /// on the activity class, so <c>DesignerMetadata</c> is where it takes effect. Dots would nest it:
        /// one name with no dots is one folder at the top level.
        /// </remarks>
        public const string Markdown = "Markdown";

        /// <summary>What the activity is given.</summary>
        public const string Input = "Input";

        /// <summary>What the activity hands back.</summary>
        public const string Output = "Output";

        /// <summary>Knobs that change how the work is done, all of which have a sensible default.</summary>
        public const string Options = "Options";

        /// <summary>The note's own text and where it comes from.</summary>
        public const string Note = "Note";

        /// <summary>How something is drawn rather than what it does.</summary>
        public const string Appearance = "Appearance";
    }

    /// <summary>Convenience helpers for arguments a workflow may simply have left unbound.</summary>
    /// <remarks>
    /// An optional property nobody filled in is null rather than an argument holding a default, and calling
    /// <c>Get</c> on it throws. Every optional argument in this pack is read through here so that leaving one
    /// blank means "the default" rather than an exception at run time.
    /// </remarks>
    internal static class ArgumentExtensions
    {
        internal static T GetValue<T>(this InArgument<T> argument, ActivityContext context) =>
            argument == null ? default(T) : argument.Get(context);

        internal static void SetValue<T>(this OutArgument<T> argument, ActivityContext context, T value)
        {
            argument?.Set(context, value);
        }
    }
}
