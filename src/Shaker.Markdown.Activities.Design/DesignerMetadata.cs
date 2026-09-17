using System;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.Diagnostics;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// Attaches the designers to the activities. Studio looks for implementations of
    /// <see cref="IRegisterMetadata"/> when it loads the package and calls <see cref="Register"/> once.
    /// </summary>
    public sealed class DesignerMetadata : IRegisterMetadata
    {
        /// <summary>Registers the designers.</summary>
        /// <remarks>
        /// A failure here would otherwise stop Studio from loading the package at all, so anything
        /// unexpected is swallowed: the activities then fall back to the stock designers and keep working.
        /// </remarks>
        public void Register()
        {
            try
            {
                var builder = new AttributeTableBuilder();

                builder.AddCustomAttributes(typeof(MarkdownNote), new DesignerAttribute(typeof(MarkdownNoteDesigner)));

                // Result arrives from CodeActivity<T> with no category of its own, which lands it under Misc,
                // away from the outputs it belongs with.
                Describe(builder, typeof(MarkdownToHtml), "Result", "HTML",
                    "The rendered HTML: a fragment by default, or a complete styled page when Complete page is on.");

                Describe(builder, typeof(MarkdownToText), "Result", "Text",
                    "The document with its markup stripped out, leaving the words.");

                Describe(builder, typeof(ReadMarkdownFile), "Result", "Markdown",
                    "The text of the file, ready to hand to Markdown To HTML.");

                Describe(builder, typeof(GetMarkdownOutline), "Result", "Headings",
                    "The document's headings in order. Each one carries its Level, its Text and its Anchor.");

                MetadataStore.AddAttributeTable(builder.CreateTable());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown designers could not be registered: " + exception);
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
