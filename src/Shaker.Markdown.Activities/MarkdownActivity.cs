using System.Activities;

namespace Shaker.Markdown.Activities
{
    /// <summary>The names the properties panel and the activities panel group by.</summary>
    internal static class Categories
    {
        /// <summary>
        /// Where every activity in this pack appears in the Activities panel.
        /// </summary>
        /// <remarks>
        /// Deliberately not the package id. The package is installed and listed as
        /// <c>Shaker.Markdown.Activities</c>, which is what a project's dependencies should say; what
        /// somebody searching the panel is looking for is "Markdown", so that is the folder they sit in.
        /// </remarks>
        internal const string Markdown = "Markdown";

        internal const string Input = "Input";
        internal const string Output = "Output";
        internal const string Options = "Options";
        internal const string Note = "Note";
        internal const string Appearance = "Appearance";
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
