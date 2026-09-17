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

                // The note draws its own card. The rest keep UiPath's, which already shows their arguments
                // well; what they were missing was an icon, and Themes/Icons.xaml supplies that by name.
                builder.AddCustomAttributes(typeof(MarkdownNote), new DesignerAttribute(typeof(MarkdownNoteDesigner)));

                // Where the pack sits in the Activities panel.
                //
                // This is the registration that decides it. A [Category] on the activity class is read for
                // the properties panel and not for this, so without this every activity fell back to being
                // grouped by package id — Shaker.Markdown.Activities, which Studio splits on the dots into
                // Shaker > Markdown. One name with no dots in it is one folder.
                var category = new CategoryAttribute(Categories.Markdown);

                foreach (Type activity in new[]
                         {
                             typeof(MarkdownNote),
                             typeof(MarkdownToHtml),
                             typeof(MarkdownToText),
                             typeof(ReadMarkdownFile),
                             typeof(GetMarkdownOutline),
                             typeof(GetDataTableFromMarkdown)
                         })
                {
                    builder.AddCustomAttributes(activity, category);
                }

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

                Describe(builder, typeof(GetDataTableFromMarkdown), "Result", "DataTable",
                    "The table, every column a string. Hand it to For Each Row or Write Range.");

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
