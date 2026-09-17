using System;
using System.Activities;
using System.Activities.Presentation;
using System.Activities.Presentation.Model;
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
    /// The card Studio draws for a <c>Markdown Note</c>: an editor for the document, and the document
    /// itself, rendered, instead of the activity's properties.
    /// </summary>
    public class MarkdownNoteDesigner : ActivityDesigner
    {
        private readonly FlowDocumentScrollViewer _viewer;
        private readonly TextBox _editor;
        private readonly TextBlock _caption;
        private readonly Border _editorFrame;
        private readonly Border _card;
        private INotifyPropertyChanged _watched;

        /// <summary>True while this designer is the one changing the model, so it ignores the echo.</summary>
        private bool _writing;

        /// <summary>Builds the card. Studio creates one of these per note on the canvas.</summary>
        public MarkdownNoteDesigner()
        {
            // No Icon here: Studio finds MarkdownNoteIcon in Themes/Icons.xaml by name, the same way it does
            // for the activities that kept the stock card.
            _caption = new TextBlock
            {
                FontSize = 10.5,
                Margin = new Thickness(2, 0, 0, 4),
                TextTrimming = TextTrimming.CharacterEllipsis
            };

            _editor = new TextBox
            {
                AcceptsReturn = true,
                AcceptsTab = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11.5,
                MinLines = 4,
                MaxLength = 0,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6, 4, 6, 4)
            };

            // The preview follows the typing; the model is only written on the way out, so that one edit is
            // one undo step rather than one per keystroke.
            _editor.TextChanged += (sender, e) => Preview(_editor.Text);
            _editor.LostFocus += (sender, e) => Commit(_editor.Text);

            _editorFrame = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Margin = new Thickness(0, 0, 0, 8),
                MaxHeight = 260,
                Visibility = Visibility.Collapsed,
                Child = _editor
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
            layout.Children.Add(_editorFrame);
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

        private void OnModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!_writing)
                Refresh();
        }

        /// <summary>Re-reads the note and redraws it.</summary>
        private void Refresh()
        {
            try
            {
                MarkdownPalette palette = PaletteFromTheme();

                _card.Background = palette.Background;
                _card.BorderBrush = palette.Border;
                _caption.Foreground = palette.Muted;
                _editorFrame.BorderBrush = palette.Border;
                _editor.Background = palette.Subtle;
                _editor.Foreground = palette.Text;
                _editor.CaretBrush = palette.Text;

                object activity = ModelItem?.GetCurrentValue();

                // Studio builds a designer before it has an activity to show in it.
                if (!(activity is MarkdownNote note))
                {
                    _caption.Text = "Markdown Note";
                    _editorFrame.Visibility = Visibility.Collapsed;
                    _viewer.Document = new FlowDocument();
                    return;
                }

                _viewer.MaxHeight = note.MaxHeight > 0 ? note.MaxHeight : double.PositiveInfinity;

                string markdown = ReadMarkdown(note, out string basePath, out string caption, out bool editable);

                _caption.Text = caption;
                _basePath = basePath;
                _options = note.ToOptions();

                // Only the note's own literal text can be edited here. An expression, an annotation and a
                // file all have somewhere else that owns them.
                _editorFrame.Visibility = note.ShowEditor && editable ? Visibility.Visible : Visibility.Collapsed;

                // Never while the caret is in it: the model echo would move the cursor out from under the
                // person typing.
                if (editable && !_editor.IsKeyboardFocusWithin && _editor.Text != (markdown ?? string.Empty))
                    _editor.Text = markdown ?? string.Empty;

                Render(markdown);
            }
            catch (Exception exception)
            {
                // The canvas is not a place to throw from: a failure here would cost Studio the whole card.
                Debug.WriteLine("Markdown note could not be refreshed: " + exception);
            }
        }

        private string _basePath;
        private MarkdownOptions _options;

        /// <summary>Redraws the preview from text being typed, without touching the model.</summary>
        private void Preview(string markdown)
        {
            Render(string.IsNullOrWhiteSpace(markdown) ? EmptyHint : markdown);
        }

        private void Render(string markdown)
        {
            _viewer.Document = FlowDocumentMarkdownRenderer.Render(
                markdown, _options, PaletteFromTheme(), _basePath);
        }

        /// <summary>Writes the edited text back to the activity, as one undoable change.</summary>
        private void Commit(string markdown)
        {
            try
            {
                ModelProperty property = ModelItem?.Properties?.Find(nameof(MarkdownNote.Markdown));

                if (property == null)
                    return;

                ArgumentLiteral.Kind kind = ArgumentLiteral.Read(
                    property.ComputedValue as InArgument<string>, out string existing);

                // Never write over an expression. The editor is only offered for a literal, but the note's
                // Source can be changed while the editor still holds focus, and losing somebody's expression
                // to a stale editor would be the worst bug in this package.
                if (kind == ArgumentLiteral.Kind.Expression)
                    return;

                if (existing == markdown)
                    return;

                _writing = true;
                property.ComputedValue = ArgumentLiteral.From(markdown);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not save the edited text: " + exception);
            }
            finally
            {
                _writing = false;
            }
        }

        private const string EmptyHint =
            "*Type Markdown into the editor, or into the note's Markdown property, and it will be rendered here.*";

        /// <summary>
        /// Gets the note's text from wherever it says to get it, and says in the caption where that was —
        /// which matters most when the answer is "nowhere", and the note would otherwise just look empty.
        /// </summary>
        private string ReadMarkdown(MarkdownNote note, out string basePath, out string caption, out bool editable)
        {
            basePath = null;
            editable = false;

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
                    return ReadProperty(note, out caption, out editable);
            }
        }

        private string ReadProperty(MarkdownNote note, out string caption, out bool editable)
        {
            editable = false;

            switch (ArgumentLiteral.Read(note.Markdown, out string text))
            {
                case ArgumentLiteral.Kind.Expression:
                    caption = "Markdown Note · expression";
                    return "*This note's text comes from the expression `" + text + "`, which has no value " +
                           "until the process runs, so there is nothing to render here.*";

                case ArgumentLiteral.Kind.Literal:
                    caption = "Markdown Note";
                    editable = true;
                    return text;

                default:
                    caption = "Markdown Note";
                    editable = true;
                    return EmptyHint;
            }
        }

        private string ReadFile(MarkdownNote note, out string basePath, out string caption)
        {
            basePath = null;

            if (ArgumentLiteral.Read(note.FilePath, out string path) == ArgumentLiteral.Kind.Expression)
            {
                caption = "Markdown Note · file";
                return "*This note's file path is the expression `" + path + "`, which has no value until " +
                       "the process runs, so the file cannot be found from here.*";
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                caption = "Markdown Note · file";
                return "*This note renders a file, and no file path is set yet.*";
            }

            string resolved = ProjectLocator.Resolve(this, path);
            caption = "Markdown Note · " + path;

            if (resolved == null || !File.Exists(resolved))
                return "*There is no file at `" + path + "`.*";

            try
            {
                basePath = Path.GetDirectoryName(resolved);
                return File.ReadAllText(resolved, Encoding.UTF8);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not read " + resolved + ": " + exception);
                return "*`" + path + "` could not be read: " + exception.Message + "*";
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
