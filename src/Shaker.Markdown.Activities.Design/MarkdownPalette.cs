using System.Windows.Media;

namespace Shaker.Markdown.Activities.Design
{
    /// <summary>The colours a rendered note is drawn in.</summary>
    /// <remarks>
    /// Studio ships a light and a dark theme, and a note that ignored them would be the one bright rectangle
    /// on a dark canvas. The designer picks the palette from the colours it inherits rather than asking
    /// Studio, because <c>IThemeService</c> is part of the wizard API and is not handed to a designer.
    /// </remarks>
    public sealed class MarkdownPalette
    {
        /// <summary>Body text.</summary>
        public Brush Text { get; set; }

        /// <summary>Quotes, captions and anything deliberately quieter than body text.</summary>
        public Brush Muted { get; set; }

        /// <summary>The note's own background.</summary>
        public Brush Background { get; set; }

        /// <summary>The background of code, table headers and other inset blocks.</summary>
        public Brush Subtle { get; set; }

        /// <summary>Rules, table grids and the line under a heading.</summary>
        public Brush Border { get; set; }

        /// <summary>Links.</summary>
        public Brush Link { get; set; }

        /// <summary>The palette for Studio's light theme.</summary>
        public static MarkdownPalette Light => new MarkdownPalette
        {
            Text = Frozen(0x24, 0x29, 0x2F),
            Muted = Frozen(0x57, 0x60, 0x6A),
            Background = Frozen(0xFF, 0xFF, 0xFF),
            Subtle = Frozen(0xF6, 0xF8, 0xFA),
            Border = Frozen(0xD0, 0xD7, 0xDE),
            Link = Frozen(0x09, 0x69, 0xDA)
        };

        /// <summary>The palette for Studio's dark theme.</summary>
        public static MarkdownPalette Dark => new MarkdownPalette
        {
            Text = Frozen(0xD4, 0xD4, 0xD4),
            Muted = Frozen(0x8B, 0x94, 0x9E),
            Background = Frozen(0x1E, 0x1E, 0x1E),
            Subtle = Frozen(0x2D, 0x2D, 0x30),
            Border = Frozen(0x3E, 0x3E, 0x42),
            Link = Frozen(0x4E, 0xA1, 0xF3)
        };

        /// <summary>
        /// A brush that can be shared across threads and never changes, which is what a static palette wants.
        /// </summary>
        private static Brush Frozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
