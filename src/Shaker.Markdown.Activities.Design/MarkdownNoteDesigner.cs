using System;
using System.Activities.Presentation;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Shaker.Markdown.Core;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// The card Studio draws for a <c>Markdown Note</c>: the document itself, rendered, instead of the
    /// activity's properties.
    /// </summary>
    public class MarkdownNoteDesigner : ActivityDesigner
    {
        private readonly FlowDocumentScrollViewer _viewer;
        private readonly TextBlock _caption;
        private readonly Border _card;
        private INotifyPropertyChanged _watched;

        /// <summary>Builds the card. Studio creates one of these per note on the canvas.</summary>
        public MarkdownNoteDesigner()
        {
            _caption = new TextBlock
            {
                FontSize = 10.5,
                Margin = new Thickness(2, 0, 0, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            _viewer = new FlowDocumentScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                IsToolBarVisible = false,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent,
                // Selection is what makes a rendered note as useful as the source it replaced: text in it
                // can still be copied out.
                IsSelectionEnabled = true
            };

            var layout = new StackPanel();
            layout.Children.Add(_caption);
            layout.Children.Add(_viewer);

            _card = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10, 8, 10, 8),
                MinWidth = 260,
                Child = layout
            };

            Content = _card;

            // The palette is read from the colours Studio has by then handed down, which is not yet true in
            // the constructor.
            Loaded += (sender, e) => Refresh();
        }

        /// <summary>Follows the note as its properties are edited, so the canvas keeps up with the typing.</summary>
        protected override void OnModelItemChanged(object newItem)
        {
            base.OnModelItemChanged(newItem);

            if (_watched != null)
                _watched.PropertyChanged -= OnModelPropertyChanged;

            _watched = newItem as INotifyPropertyChanged;

            if (_watched != null)
                _watched.PropertyChanged += OnModelPropertyChanged;

            Refresh();
        }

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e) => Refresh();

        /// <summary>Re-reads the note and redraws it.</summary>
        private void Refresh()
        {
            try
            {
                MarkdownPalette palette = PaletteFromTheme();

                _card.Background = palette.Background;
                _card.BorderBrush = palette.Border;
                _caption.Foreground = palette.Muted;

                object activity = ModelItem?.GetCurrentValue();

                // Studio builds a designer before it has an activity to show in it.
                if (!(activity is MarkdownNote note))
                {
                    _caption.Text = "Markdown Note";
                    _viewer.Document = new FlowDocument();
                    return;
                }

                _viewer.MaxHeight = note.MaxHeight > 0 ? note.MaxHeight : double.PositiveInfinity;

                string basePath;
                string markdown = ReadMarkdown(note, out basePath, out string caption);

                _caption.Text = caption;
                _viewer.Document = FlowDocumentMarkdownRenderer.Render(
                    markdown, note.ToOptions(), palette, basePath);
            }
            catch (Exception exception)
            {
                // The canvas is not a place to throw from: a failure here would cost Studio the whole card.
                Debug.WriteLine("Markdown note could not be refreshed: " + exception);
            }
        }

        /// <summary>
        /// Gets the note's text from wherever it says to get it, and says in the caption where that was —
        /// which matters most when the answer is "nowhere", and the note would otherwise just look empty.
        /// </summary>
        private string ReadMarkdown(MarkdownNote note, out string basePath, out string caption)
        {
            basePath = null;

            switch (note.Source)
            {
                case MarkdownSource.Annotation:
                    caption = "Markdown Note · annotation";
                    string annotation = AnnotationReader.Read(ModelItem);

                    if (string.IsNullOrWhiteSpace(annotation))
                        return "*This note renders its own annotation, and there is no annotation yet. " +
                               "Right-click the activity and choose Annotations › Add Annotation.*";

                    return annotation;

                case MarkdownSource.File:
                    return ReadFile(note, out basePath, out caption);

                default:
                    caption = "Markdown Note";

                    if (string.IsNullOrWhiteSpace(note.Markdown))
                        return "*Type Markdown into the note's Markdown property and it will be rendered here.*";

                    return note.Markdown;
            }
        }

        private string ReadFile(MarkdownNote note, out string basePath, out string caption)
        {
            basePath = null;

            if (string.IsNullOrWhiteSpace(note.FilePath))
            {
                caption = "Markdown Note · file";
                return "*This note renders a file, and no file path is set yet.*";
            }

            string resolved = ProjectLocator.Resolve(this, note.FilePath);
            caption = "Markdown Note · " + note.FilePath;

            if (resolved == null || !File.Exists(resolved))
                return "*There is no file at `" + note.FilePath + "`.*";

            try
            {
                basePath = Path.GetDirectoryName(resolved);
                return File.ReadAllText(resolved, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read " + resolved + ": " + exception);
                return "*`" + note.FilePath + "` could not be read: " + exception.Message + "*";
            }
        }

        /// <summary>
        /// Picks the palette from the colour Studio is drawing text in.
        /// </summary>
        /// <remarks>
        /// A designer is not given the theme service, so it infers: pale inherited text means the surface
        /// behind it is dark. Anything it cannot read falls back to the light palette, which is Studio's
        /// default.
        /// </remarks>
        private MarkdownPalette PaletteFromTheme()
        {
            try
            {
                if (TextElement.GetForeground(this) is SolidColorBrush foreground)
                {
                    Color colour = foreground.Color;

                    // Rec. 601 luma: green carries most of what the eye reads as brightness.
                    double luma = (0.299 * colour.R + 0.587 * colour.G + 0.114 * colour.B) / 255d;

                    return luma > 0.5 ? MarkdownPalette.Dark : MarkdownPalette.Light;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read the Studio theme: " + exception);
            }

            return MarkdownPalette.Light;
        }
    }
}
