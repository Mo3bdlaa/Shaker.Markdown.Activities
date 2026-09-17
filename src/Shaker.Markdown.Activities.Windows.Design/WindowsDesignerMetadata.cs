using System;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.Diagnostics;

namespace Shaker.Markdown.Activities.Windows.Design
{
    /// <summary>
    /// Puts this assembly's two activities in the same Activities panel folder as the rest of the pack, and
    /// names their outputs. Studio looks for implementations of <see cref="IRegisterMetadata"/> when it
    /// loads the package and calls <see cref="Register"/> once.
    /// </summary>
    /// <remarks>
    /// No designers. Both activities keep UiPath's stock card, which already shows their arguments well;
    /// their icons come from <c>Themes/Icons.xaml</c>, which Studio reads by activity name.
    /// </remarks>
    public sealed class WindowsDesignerMetadata : IRegisterMetadata
    {
        /// <summary>Registers the metadata.</summary>
        /// <remarks>
        /// A failure here would otherwise stop Studio from loading the package at all, so anything
        /// unexpected is swallowed: the activities then fall back to the stock metadata and keep working.
        /// </remarks>
        public void Register()
        {
            try
            {
                var builder = new AttributeTableBuilder();

                // The same folder as the rest of the pack, so these two land beside the activities they
                // belong with rather than starting a second folder of their own.
                var category = new CategoryAttribute(Shaker.Markdown.Activities.Categories.Markdown);

                builder.AddCustomAttributes(typeof(ShowMarkdown), category);
                builder.AddCustomAttributes(typeof(MarkdownToPdf), category);

                Describe(builder, typeof(ShowMarkdown), "Result", "Closed",
                    "True once the window has been shown and closed.");

                Describe(builder, typeof(MarkdownToPdf), "Result", "PDF path",
                    "The full path of the PDF that was written.");

                MetadataStore.AddAttributeTable(builder.CreateTable());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown Windows metadata could not be registered: " + exception);
            }
        }

        /// <summary>Gives one property a name, a description and a home in the properties panel.</summary>
        private static void Describe(AttributeTableBuilder builder, Type activity, string property, string name, string description)
        {
            builder.AddCustomAttributes(
                activity,
                property,
                new CategoryAttribute("Output"),
                new DisplayNameAttribute(name),
                new DescriptionAttribute(description));
        }
    }
}
