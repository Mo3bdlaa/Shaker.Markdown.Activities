using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Shaker.Markdown.Core;
using Block = System.Windows.Documents.Block;
using Inline = System.Windows.Documents.Inline;
using MdBlock = Markdig.Syntax.Block;
using MdContainerInline = Markdig.Syntax.Inlines.ContainerInline;
using MdInline = Markdig.Syntax.Inlines.Inline;
using MdTable = Markdig.Extensions.Tables.Table;
using MdTableCell = Markdig.Extensions.Tables.TableCell;
using MdTableRow = Markdig.Extensions.Tables.TableRow;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// Draws Markdown as a WPF <see cref="FlowDocument"/>, which is what puts a rendered document on the
    /// Studio canvas.
    /// </summary>
    /// <remarks>
    /// A FlowDocument rather than a browser control, and deliberately: the canvas redraws a note on every
    /// keystroke, there may be a dozen of them in one workflow, and a WebView2 per note would cost a browser
    /// process each and a runtime that may not be installed. The trade is fidelity — no syntax highlighting,
    /// no raw HTML — and that is what the viewer window is for.
    /// </remarks>
    public static class FlowDocumentMarkdownRenderer
    {
        /// <summary>Renders a document.</summary>
        /// <param name="markdown">The Markdown. Null or empty gives an empty document, not null.</param>
        /// <param name="options">How to read it.</param>
        /// <param name="palette">The colours to draw it in.</param>
        /// <param name="basePath">
        /// The folder relative image paths point into, or null when the Markdown came from somewhere with no
        /// folder of its own.
        /// </param>
        public static FlowDocument Render(
            string markdown,
            MarkdownOptions options,
            MarkdownPalette palette,
            string basePath = null)
        {
            palette = palette ?? MarkdownPalette.Light;

            var document = new FlowDocument
            {
                FontFamily = new FontFamily("Segoe UI"),
                FontSize = 12.5,
                Foreground = palette.Text,
                Background = Brushes.Transparent,
                PagePadding = new Thickness(0),
                LineHeight = 18,
                // Justification and hyphenation both fight with code and tables; neither earns its place here.
                IsOptimalParagraphEnabled = false,
                IsHyphenationEnabled = false
            };

            if (string.IsNullOrWhiteSpace(markdown))
                return document;

            try
            {
                MarkdownDocument parsed = MarkdownEngine.Parse(markdown, options);
                var context = new RenderContext(palette, basePath);

                foreach (Block block in ConvertBlocks(parsed, context))
                    document.Blocks.Add(block);
            }
            catch (Exception exception)
            {
                // A note that cannot be rendered says so on the canvas rather than blanking or throwing into
                // the designer, which would cost Studio the whole card.
                Debug.WriteLine("Markdown note could not be rendered: " + exception);
                document.Blocks.Clear();
                document.Blocks.Add(new Paragraph(new Run("This Markdown could not be rendered: " + exception.Message))
                {
                    Foreground = palette.Muted,
                    FontStyle = FontStyles.Italic
                });
            }

            return document;
        }

        /// <summary>What the walk needs to carry with it: the colours, and where images are relative to.</summary>
        private sealed class RenderContext
        {
            internal RenderContext(MarkdownPalette palette, string basePath)
            {
                Palette = palette;
                BasePath = basePath;
            }

            internal MarkdownPalette Palette { get; }

            internal string BasePath { get; }
        }

        private static IEnumerable<Block> ConvertBlocks(IEnumerable<MdBlock> blocks, RenderContext context)
        {
            foreach (MdBlock block in blocks)
            {
                Block converted = ConvertBlock(block, context);
                if (converted != null)
                    yield return converted;
            }
        }

        private static Block ConvertBlock(MdBlock block, RenderContext context)
        {
            switch (block)
            {
                case HeadingBlock heading:
                    return Heading(heading, context);

                case ParagraphBlock paragraph:
                    return new Paragraph { Margin = new Thickness(0, 0, 0, 8) }
                        .WithInlines(ConvertInlines(paragraph.Inline, context));

                case ListBlock list:
                    return List(list, context);

                case QuoteBlock quote:
                    return Quote(quote, context);

                case CodeBlock code:
                    return Code(code, context);

                case ThematicBreakBlock _:
                    return Rule(context);

                case MdTable table:
                    return Table(table, context);

                case HtmlBlock html:
                    // Only reachable when the note allows raw HTML. A FlowDocument cannot host it, so the
                    // markup is shown as what it is rather than silently dropped.
                    return Monospace(LinesOf(html.Lines), context, context.Palette.Muted);

                case ContainerBlock container:
                    // Anything else that holds blocks — a custom container, a footnote group — is flattened
                    // rather than skipped, so its contents still reach the reader.
                    var section = new Section();
                    foreach (Block child in ConvertBlocks(container, context))
                        section.Blocks.Add(child);
                    return section.Blocks.Count > 0 ? section : null;

                default:
                    return null;
            }
        }

        private static Block Heading(HeadingBlock heading, RenderContext context)
        {
            // Sized down the deeper the heading sits; h5 and h6 stop shrinking and lean on weight instead.
            double[] sizes = { 1.8, 1.45, 1.2, 1.05, 1.0, 1.0 };
            int level = Math.Min(Math.Max(heading.Level, 1), 6);

            var paragraph = new Paragraph
            {
                FontSize = 12.5 * sizes[level - 1],
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, level == 1 ? 0 : 14, 0, 6),
                Foreground = level == 6 ? context.Palette.Muted : context.Palette.Text
            };

            // The rule under h1 and h2, the same way a README renders.
            if (level <= 2)
            {
                paragraph.BorderBrush = context.Palette.Border;
                paragraph.BorderThickness = new Thickness(0, 0, 0, 1);
                paragraph.Padding = new Thickness(0, 0, 0, 4);
            }

            return paragraph.WithInlines(ConvertInlines(heading.Inline, context));
        }

        private static Block List(ListBlock list, RenderContext context)
        {
            var rendered = new List
            {
                MarkerStyle = list.IsOrdered ? TextMarkerStyle.Decimal : TextMarkerStyle.Disc,
                Margin = new Thickness(0, 0, 0, 8),
                Padding = new Thickness(18, 0, 0, 0)
            };

            if (list.IsOrdered && int.TryParse(list.OrderedStart, out int start) && start > 0)
                rendered.StartIndex = start;

            foreach (MdBlock item in list)
            {
                if (!(item is ListItemBlock itemBlock))
                    continue;

                var listItem = new ListItem();
                foreach (Block child in ConvertBlocks(itemBlock, context))
                    listItem.Blocks.Add(child);

                // A tight list still needs its items to sit together rather than a paragraph apart.
                if (listItem.Blocks.LastBlock != null)
                    listItem.Blocks.LastBlock.Margin = new Thickness(0);

                rendered.ListItems.Add(listItem);
            }

            return rendered;
        }

        private static Block Quote(QuoteBlock quote, RenderContext context)
        {
            var section = new Section
            {
                BorderBrush = context.Palette.Border,
                BorderThickness = new Thickness(3, 0, 0, 0),
                Padding = new Thickness(12, 0, 0, 0),
                Margin = new Thickness(0, 0, 0, 8),
                Foreground = context.Palette.Muted
            };

            foreach (Block child in ConvertBlocks(quote, context))
                section.Blocks.Add(child);

            return section;
        }

        private static Block Code(CodeBlock code, RenderContext context) =>
            Monospace(LinesOf(code.Lines), context, context.Palette.Text);

        /// <summary>A block of preformatted text, inset and boxed the way a code fence renders.</summary>
        private static Block Monospace(string text, RenderContext context, Brush foreground)
        {
            return new Paragraph(new Run(text))
            {
                FontFamily = new FontFamily("Consolas, Courier New"),
                FontSize = 11.5,
                Foreground = foreground,
                Background = context.Palette.Subtle,
                BorderBrush = context.Palette.Border,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(10, 8, 10, 8),
                Margin = new Thickness(0, 0, 0, 8),
                LineHeight = 16
            };
        }

        private static Block Rule(RenderContext context)
        {
            return new Paragraph
            {
                BorderBrush = context.Palette.Border,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Margin = new Thickness(0, 6, 0, 14)
            };
        }

        private static Block Table(MdTable table, RenderContext context)
        {
            var rendered = new Table { CellSpacing = 0, Margin = new Thickness(0, 0, 0, 10) };
            int columns = table.OfType<MdTableRow>().Select(row => row.Count).DefaultIfEmpty(0).Max();

            for (int column = 0; column < columns; column++)
                rendered.Columns.Add(new TableColumn { Width = GridLength.Auto });

            var group = new TableRowGroup();
            rendered.RowGroups.Add(group);

            foreach (MdTableRow row in table.OfType<MdTableRow>())
            {
                var renderedRow = new TableRow();

                if (row.IsHeader)
                {
                    renderedRow.FontWeight = FontWeights.SemiBold;
                    renderedRow.Background = context.Palette.Subtle;
                }

                foreach (MdTableCell cell in row.OfType<MdTableCell>())
                {
                    var renderedCell = new TableCell
                    {
                        BorderBrush = context.Palette.Border,
                        BorderThickness = new Thickness(1),
                        Padding = new Thickness(8, 4, 8, 4)
                    };

                    foreach (Block child in ConvertBlocks(cell, context))
                    {
                        child.Margin = new Thickness(0);
                        renderedCell.Blocks.Add(child);
                    }

                    renderedRow.Cells.Add(renderedCell);
                }

                group.Rows.Add(renderedRow);
            }

            return rendered;
        }

        private static IEnumerable<Inline> ConvertInlines(MdContainerInline container, RenderContext context)
        {
            if (container == null)
                yield break;

            for (MdInline inline = container.FirstChild; inline != null; inline = inline.NextSibling)
            {
                foreach (Inline converted in ConvertInline(inline, context))
                    yield return converted;
            }
        }

        private static IEnumerable<Inline> ConvertInline(MdInline inline, RenderContext context)
        {
            switch (inline)
            {
                case LiteralInline literal:
                    yield return new Run(literal.Content.ToString());
                    break;

                case EmphasisInline emphasis:
                    yield return Emphasis(emphasis, context);
                    break;

                case CodeInline code:
                    yield return new Run(code.Content)
                    {
                        FontFamily = new FontFamily("Consolas, Courier New"),
                        FontSize = 11.5,
                        Background = context.Palette.Subtle
                    };
                    break;

                case LinkInline link:
                    yield return link.IsImage ? Image(link, context) : Link(link, context);
                    break;

                case AutolinkInline autolink:
                    yield return Hyperlink(autolink.Url, new Run(autolink.Url), context);
                    break;

                case TaskList task:
                    // Rendered as the box itself, since a FlowDocument has no checkbox in running text.
                    yield return new Run(task.Checked ? "☑ " : "☐ ");
                    break;

                case LineBreakInline lineBreak:
                    // A soft break is a space in Markdown; only a hard break ends the line.
                    yield return lineBreak.IsHard ? (Inline)new LineBreak() : new Run(" ");
                    break;

                case HtmlEntityInline entity:
                    yield return new Run(entity.Transcoded.ToString());
                    break;

                case HtmlInline html:
                    yield return new Run(html.Tag) { Foreground = context.Palette.Muted };
                    break;

                case MdContainerInline container:
                    foreach (Inline child in ConvertInlines(container, context))
                        yield return child;
                    break;
            }
        }

        private static Inline Emphasis(EmphasisInline emphasis, RenderContext context)
        {
            Span span;

            switch (emphasis.DelimiterChar)
            {
                case '~':
                    span = new Span { TextDecorations = TextDecorations.Strikethrough };
                    break;

                // One delimiter is italic, two are bold, three are both — the CommonMark rule.
                default:
                    span = emphasis.DelimiterCount >= 2 ? (Span)new Bold() : new Italic();
                    if (emphasis.DelimiterCount >= 3)
                        span = new Bold(new Italic());
                    break;
            }

            Span target = span is Bold bold && bold.Inlines.FirstInline is Italic italic ? italic : span;

            foreach (Inline child in ConvertInlines(emphasis, context))
                target.Inlines.Add(child);

            return span;
        }

        private static Inline Link(LinkInline link, RenderContext context)
        {
            var content = new Span();

            foreach (Inline child in ConvertInlines(link, context))
                content.Inlines.Add(child);

            if (content.Inlines.Count == 0)
                content.Inlines.Add(new Run(link.Url ?? string.Empty));

            return Hyperlink(link.Url, content, context);
        }

        private static Inline Hyperlink(string url, Inline content, RenderContext context)
        {
            var hyperlink = new Hyperlink(content) { Foreground = context.Palette.Link };

            if (string.IsNullOrWhiteSpace(url))
                return hyperlink;

            hyperlink.ToolTip = url;

            // A relative link has nothing to resolve against on the canvas, so only absolute ones open.
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri target))
            {
                hyperlink.NavigateUri = target;
                hyperlink.RequestNavigate += (sender, e) =>
                {
                    e.Handled = true;
                    Open(e.Uri);
                };
            }

            return hyperlink;
        }

        /// <summary>Hands a link to whatever the machine opens it with.</summary>
        private static void Open(Uri uri)
        {
            try
            {
                // http, https and mailto only. A note is a document, and a document should not be able to
                // launch a local executable because somebody wrote file:///… in a link.
                if (uri.Scheme != Uri.UriSchemeHttp &&
                    uri.Scheme != Uri.UriSchemeHttps &&
                    uri.Scheme != Uri.UriSchemeMailto)
                    return;

                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not open " + uri + ": " + exception);
            }
        }

        /// <summary>
        /// An image, when it is a local file that can be found, and its alt text when it is not.
        /// </summary>
        /// <remarks>
        /// Local files only. A remote image would mean the canvas making a network request every time a note
        /// is redrawn, on somebody else's schedule and into somebody else's logs, which is not a thing a
        /// designer should do quietly.
        /// </remarks>
        private static Inline Image(LinkInline link, RenderContext context)
        {
            string alt = string.Concat(link.Descendants<LiteralInline>().Select(l => l.Content.ToString()));
            Inline fallback = new Run(string.IsNullOrWhiteSpace(alt) ? "[image]" : "[" + alt + "]")
            {
                Foreground = context.Palette.Muted,
                FontStyle = FontStyles.Italic
            };

            if (string.IsNullOrWhiteSpace(context.BasePath) || string.IsNullOrWhiteSpace(link.Url))
                return fallback;

            try
            {
                if (Uri.TryCreate(link.Url, UriKind.Absolute, out Uri absolute) && !absolute.IsFile)
                    return fallback;

                string path = Path.GetFullPath(Path.Combine(context.BasePath, link.Url));

                if (!File.Exists(path))
                    return fallback;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                // Read the bytes now and let go of the file: holding it open would lock an image in the
                // user's own project for as long as the workflow stays open.
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();

                return new InlineUIContainer(new System.Windows.Controls.Image
                {
                    Source = bitmap,
                    Stretch = Stretch.Uniform,
                    MaxWidth = bitmap.PixelWidth,
                    ToolTip = path
                });
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Markdown note could not load the image " + link.Url + ": " + exception);
                return fallback;
            }
        }

        /// <summary>The text of a code or HTML block, with the trailing blank line trimmed off.</summary>
        private static string LinesOf(Markdig.Helpers.StringLineGroup lines)
        {
            return string.Join(Environment.NewLine,
                Enumerable.Range(0, lines.Count).Select(i => lines.Lines[i].Slice.ToString())).TrimEnd();
        }

        /// <summary>Fills a paragraph, and hands it back so it can be returned in one expression.</summary>
        private static Paragraph WithInlines(this Paragraph paragraph, IEnumerable<Inline> inlines)
        {
            foreach (Inline inline in inlines)
                paragraph.Inlines.Add(inline);

            return paragraph;
        }
    }
}
