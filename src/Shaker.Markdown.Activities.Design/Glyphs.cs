using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>
    /// The icons on the activity cards and in the Activities panel, drawn as geometry so the package carries
    /// no image files and stays crisp at any zoom.
    /// </summary>
    /// <remarks>
    /// Every icon is a document page with an accent mark inside it saying what the activity does to the
    /// document, so the pack reads as one family while each activity is still told apart at a glance. The
    /// note keeps the Markdown mark itself, since it is the one activity that shows a document rather than
    /// converting one.
    /// </remarks>
    internal static class Glyphs
    {
        private static readonly Color Accent = Color.FromRgb(0x1F, 0x6F, 0xB2);
        private static readonly Color Muted = Color.FromRgb(0x5A, 0x6B, 0x7A);

        /// <summary>The page every icon but the note is built on, and the corner it folds over.</summary>
        private const string Page = "M3.5,1.5 H9.5 L12.5,4.5 V14.5 H3.5 Z";
        private const string Fold = "M9.5,1.5 V4.5 H12.5";

        private static readonly Dictionary<string, DrawingBrush> Cache = new Dictionary<string, DrawingBrush>();

        /// <summary>The icon for an activity, by type name. Unknown names get the plain page.</summary>
        internal static DrawingBrush For(string activityName)
        {
            lock (Cache)
            {
                if (Cache.TryGetValue(activityName, out DrawingBrush cached))
                    return cached;

                DrawingBrush brush = Build(activityName);
                Cache[activityName] = brush;
                return brush;
            }
        }

        private static DrawingBrush Build(string activityName)
        {
            switch (activityName)
            {
                // The Markdown mark: the rounded box around an M and a caret, same as the package icon.
                case "MarkdownNote":
                    return Icon(
                        Stroke("M1.5,3.5 H14.5 V12.5 H1.5 Z", Muted, 1.2, radius: 1.6),
                        Stroke("M4,10 V6 L6,8.5 L8,6 V10", Accent, 1.3),
                        Stroke("M11,6 V9.5 M9.5,8.5 L11,10 L12.5,8.5", Accent, 1.3));

                // Angle brackets, for HTML.
                case "MarkdownToHtml":
                    return Page_(Stroke("M7,7.5 L5,10 L7,12.5 M10,7.5 L12,10 L10,12.5", Accent, 1.3));

                // A capital T, for plain text.
                case "MarkdownToText":
                    return Page_(Stroke("M5.5,8 H10.5 M8,8 V12.5", Accent, 1.3));

                // An arrow out of the page, for reading one in.
                case "ReadMarkdownFile":
                    return Page_(Stroke("M8,7 V11.5 M6,9.5 L8,11.8 L10,9.5", Accent, 1.4));

                // Bullets and rules, for a table of contents.
                case "GetMarkdownOutline":
                    return Page_(
                        Stroke("M5.6,7.6 H11 M6.6,10 H11 M7.6,12.4 H11", Accent, 1.2),
                        Fill("M4.6,7.1 H5.2 V7.7 H4.6 Z M5.6,9.5 H6.2 V10.1 H5.6 Z M6.6,11.9 H7.2 V12.5 H6.6 Z", Accent));

                // A grid, for a table.
                case "GetDataTableFromMarkdown":
                    return Page_(
                        Stroke("M4.6,7.4 H11.6 M4.6,9.8 H11.6 M4.6,12.2 H11.6", Accent, 1.1),
                        Stroke("M7,7.4 V12.2 M9.4,7.4 V12.2", Accent, 1.1));

                // A window with a title bar, for showing a document to somebody.
                case "ShowMarkdown":
                    return Page_(
                        Stroke("M4.4,7.4 H11.8 V12.6 H4.4 Z", Accent, 1.2),
                        Fill("M4.4,7.4 H11.8 V8.8 H4.4 Z", Accent));

                // An arrow onto a baseline, for printing to a file.
                case "MarkdownToPdf":
                    return Page_(
                        Stroke("M8,6.8 V10.6 M6.2,8.9 L8,10.9 L9.8,8.9", Accent, 1.4),
                        Stroke("M5,12.8 H11", Accent, 1.4));

                default:
                    return Page_();
            }
        }

        /// <summary>A page, with whatever mark belongs on it.</summary>
        private static DrawingBrush Page_(params Drawing[] marks)
        {
            var parts = new List<Drawing>
            {
                Stroke(Page, Muted, 1.2),
                Stroke(Fold, Muted, 1.2)
            };

            parts.AddRange(marks);

            return Icon(parts.ToArray());
        }

        /// <summary>Assembles the parts into a brush over one fixed 16×16 coordinate space.</summary>
        private static DrawingBrush Icon(params Drawing[] parts)
        {
            var group = new DrawingGroup();

            // An invisible square first, so every icon reports the same bounds and Uniform stretch scales
            // them all identically. Without it an icon with a smaller mark would be drawn larger.
            group.Children.Add(new GeometryDrawing(
                Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 16, 16))));

            foreach (Drawing part in parts)
                group.Children.Add(part);

            var brush = new DrawingBrush(group) { Stretch = Stretch.Uniform };
            brush.Freeze();
            return brush;
        }

        /// <summary>An outlined shape.</summary>
        private static Drawing Stroke(string path, Color colour, double thickness, double radius = 0)
        {
            Geometry geometry = radius > 0
                ? RoundedFrom(path, radius)
                : Geometry.Parse(path);

            var pen = new Pen(Frozen(colour), thickness)
            {
                StartLineCap = PenLineCap.Round,
                EndLineCap = PenLineCap.Round,
                LineJoin = PenLineJoin.Round
            };
            pen.Freeze();

            var drawing = new GeometryDrawing(null, pen, geometry);
            drawing.Freeze();
            return drawing;
        }

        /// <summary>A filled shape.</summary>
        private static Drawing Fill(string path, Color colour)
        {
            var drawing = new GeometryDrawing(Frozen(colour), null, Geometry.Parse(path));
            drawing.Freeze();
            return drawing;
        }

        /// <summary>
        /// The rounded box the note's mark sits in. Path markup has no corner radius, so the one shape that
        /// needs one is rebuilt as a rounded rectangle from the bounds of its path.
        /// </summary>
        private static Geometry RoundedFrom(string path, double radius)
        {
            Rect bounds = Geometry.Parse(path).Bounds;
            var geometry = new RectangleGeometry(bounds, radius, radius);
            geometry.Freeze();
            return geometry;
        }

        private static Brush Frozen(Color colour)
        {
            var brush = new SolidColorBrush(colour);
            brush.Freeze();
            return brush;
        }
    }
}
