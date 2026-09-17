using System;
using System.Activities.Presentation.Metadata;
using System.ComponentModel;
using System.Diagnostics;
using Shaker.Markdown.Activities.Design;

namespace Shaker.Markdown.Activities.Windows.Design
{
    /// <summary>The icon for <see cref="ShowMarkdown"/>.</summary>
    public sealed class ShowMarkdownDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public ShowMarkdownDesigner() : base(typeof(ShowMarkdown))
        {
        }
    }

    /// <summary>The icon for <see cref="MarkdownToPdf"/>.</summary>
    public sealed class MarkdownToPdfDesigner : MarkdownActivityDesigner
    {
        /// <summary>Creates the designer.</summary>
        public MarkdownToPdfDesigner() : base(typeof(MarkdownToPdf))
        {
        }
    }

    /// <summary>
    /// Attaches the designers to this package's two activities. Studio looks for implementations of
    /// <see cref="IRegisterMetadata"/> when it loads the package and calls <see cref="Register"/> once.
    /// </summary>
    public sealed class WindowsDesignerMetadata : IRegisterMetadata
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

                builder.AddCustomAttributes(typeof(ShowMarkdown), new DesignerAttribute(typeof(ShowMarkdownDesigner)));
                builder.AddCustomAttributes(typeof(MarkdownToPdf), new DesignerAttribute(typeof(MarkdownToPdfDesigner)));

                Describe(builder, typeof(ShowMarkdown), "Result", "Closed",
                    "True once the window has been shown and closed.");

                Describe(builder, typeof(MarkdownToPdf), "Result", "PDF path",
                    "The full path of the PDF that was written.");

                MetadataStore.AddAttributeTable(builder.CreateTable());
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown Windows designers could not be registered: " + exception);
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
