using System;
using System.IO;
using System.Text;

namespace Shaker.Markdown.Core
{
    /// <summary>Which way round a rendered document is coloured.</summary>
    public enum DocumentTheme
    {
        /// <summary>Dark text on a light page.</summary>
        Light = 0,

        /// <summary>Light text on a dark page.</summary>
        Dark = 1
    }

    /// <summary>
    /// Wraps a rendered fragment into a page a browser control can show: the stylesheet, the theme, and the
    /// base address that makes a README's relative image paths resolve.
    /// </summary>
    public static class HtmlDocument
    {
        /// <summary>
        /// Builds the page.
        /// </summary>
        /// <param name="bodyHtml">The fragment from <see cref="MarkdownEngine.ToHtml"/>.</param>
        /// <param name="title">The page title, or null.</param>
        /// <param name="theme">Which way round to colour it.</param>
        /// <param name="basePath">
        /// The folder the Markdown was read from, so that <c>![](docs/diagram.png)</c> finds its image. Null
        /// for Markdown that came from a variable and has nowhere to be relative to.
        /// </param>
        /// <param name="baseHrefOverride">
        /// An explicit base address, used instead of deriving one from <paramref name="basePath"/>. The
        /// viewer passes its virtual host here, because a browser control serving a document from memory
        /// cannot reach the disk through a file:// base.
        /// </param>
        /// <param name="allowScripts">
        /// Whether script in the document may run. Off by default: the page carries a Content-Security-Policy
        /// that forbids it, which is the second half of the defence that
        /// <see cref="MarkdownOptions.AllowHtml"/> starts. A document is not trusted just because somebody
        /// turned raw HTML on to get their table to render.
        /// </param>
        public static string Build(
            string bodyHtml,
            string title = null,
            DocumentTheme theme = DocumentTheme.Light,
            string basePath = null,
            bool allowScripts = false,
            string baseHrefOverride = null)
        {
            var page = new StringBuilder();

            page.AppendLine("<!DOCTYPE html>");
            page.AppendLine("<html lang=\"en\">");
            page.AppendLine("<head>");
            page.AppendLine("<meta charset=\"utf-8\" />");

            if (!allowScripts)
            {
                // img-src allows local files so a README's diagrams still load; script is simply absent.
                page.AppendLine("<meta http-equiv=\"Content-Security-Policy\" " +
                                "content=\"default-src 'none'; img-src file: data: https:; " +
                                "style-src 'unsafe-inline'; font-src file: data:;\" />");
            }

            string baseHref = string.IsNullOrWhiteSpace(baseHrefOverride)
                ? ToBaseHref(basePath)
                : baseHrefOverride;
            if (baseHref != null)
                page.AppendLine("<base href=\"" + Escape(baseHref) + "\" />");

            page.AppendLine("<title>" + Escape(title ?? "Markdown") + "</title>");
            page.AppendLine("<style>");
            page.AppendLine(StyleSheet(theme));
            page.AppendLine("</style>");
            page.AppendLine("</head>");
            page.AppendLine("<body>");
            page.AppendLine(bodyHtml ?? string.Empty);
            page.AppendLine("</body>");
            page.AppendLine("</html>");

            return page.ToString();
        }

        /// <summary>
        /// Turns a folder into the address relative links resolve against.
        /// </summary>
        /// <remarks>
        /// The trailing separator is what makes this work: without it a browser treats the last segment as a
        /// file name and resolves <c>images/x.png</c> as a sibling of the folder rather than inside it.
        /// </remarks>
        private static string ToBaseHref(string basePath)
        {
            if (string.IsNullOrWhiteSpace(basePath))
                return null;

            try
            {
                string full = Path.GetFullPath(basePath);

                if (File.Exists(full))
                    full = Path.GetDirectoryName(full);

                if (string.IsNullOrEmpty(full))
                    return null;

                if (!full.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    full += Path.DirectorySeparatorChar;

                return new Uri(full).AbsoluteUri;
            }
            catch (Exception)
            {
                // A path that cannot be made absolute simply gets no base; the document still renders.
                return null;
            }
        }

        /// <summary>The stylesheet, in the requested theme.</summary>
        private static string StyleSheet(DocumentTheme theme)
        {
            bool dark = theme == DocumentTheme.Dark;

            string text = dark ? "#d4d4d4" : "#24292f";
            string muted = dark ? "#8b949e" : "#57606a";
            string background = dark ? "#1e1e1e" : "#ffffff";
            string subtle = dark ? "#2d2d30" : "#f6f8fa";
            string border = dark ? "#3e3e42" : "#d0d7de";
            string link = dark ? "#4ea1f3" : "#0969da";

            return @"
html, body { margin: 0; padding: 0; }
body {
  font-family: 'Segoe UI', -apple-system, sans-serif;
  font-size: 14px; line-height: 1.6;
  color: " + text + @"; background: " + background + @";
  padding: 16px 24px 48px 24px;
  word-wrap: break-word;
}
h1, h2, h3, h4, h5, h6 { font-weight: 600; line-height: 1.25; margin: 24px 0 16px; }
h1 { font-size: 1.85em; border-bottom: 1px solid " + border + @"; padding-bottom: .3em; }
h2 { font-size: 1.45em; border-bottom: 1px solid " + border + @"; padding-bottom: .3em; }
h3 { font-size: 1.2em; }
h4, h5, h6 { font-size: 1em; }
h6 { color: " + muted + @"; }
p, ul, ol, blockquote, table, pre { margin: 0 0 16px; }
a { color: " + link + @"; text-decoration: none; }
a:hover { text-decoration: underline; }
code {
  font-family: Consolas, 'Courier New', monospace; font-size: .9em;
  background: " + subtle + @"; border-radius: 4px; padding: .15em .4em;
}
pre {
  background: " + subtle + @"; border: 1px solid " + border + @"; border-radius: 6px;
  padding: 12px 16px; overflow: auto;
}
pre code { background: none; padding: 0; }
blockquote {
  margin-left: 0; padding: 0 1em; color: " + muted + @";
  border-left: .25em solid " + border + @";
}
table { border-collapse: collapse; display: block; overflow: auto; }
th, td { border: 1px solid " + border + @"; padding: 6px 13px; }
th { background: " + subtle + @"; font-weight: 600; }
img { max-width: 100%; }
hr { height: 1px; border: 0; background: " + border + @"; margin: 24px 0; }
ul, ol { padding-left: 2em; }
li + li { margin-top: .25em; }
li.task-list-item { list-style: none; margin-left: -1.6em; }
li.task-list-item input { margin-right: .5em; }
.footnotes { font-size: .9em; color: " + muted + @"; border-top: 1px solid " + border + @"; }
";
        }

        /// <summary>Escapes text going into an attribute or an element.</summary>
        private static string Escape(string value)
        {
            return (value ?? string.Empty)
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
