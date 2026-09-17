using System;
using System.Collections.Generic;
using System.Linq;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Shaker.Markdown.Core
{
    /// <summary>
    /// Reads Markdown. Every surface in this package — the activities, the designers on the canvas and the
    /// viewer window — comes through here, so a document looks the same wherever it is shown.
    /// </summary>
    public static class MarkdownEngine
    {
        /// <summary>
        /// Builds the parser for a set of options.
        /// </summary>
        /// <remarks>
        /// Pipelines are immutable once built and Markdig is happy to share one across threads, so they are
        /// cached: the designers rebuild their content on every keystroke and building a pipeline each time
        /// would be the expensive part of that.
        /// </remarks>
        public static MarkdownPipeline BuildPipeline(MarkdownOptions options)
        {
            options = options ?? MarkdownOptions.Default;

            string key = options.Flavor + "|" + options.AllowHtml + "|" + options.SoftBreakAsHardBreak;

            lock (PipelineLock)
            {
                if (Pipelines.TryGetValue(key, out MarkdownPipeline cached))
                    return cached;

                var builder = new MarkdownPipelineBuilder();

                if (options.Flavor == MarkdownFlavor.Advanced)
                    builder = builder.UseAdvancedExtensions();
                else
                    builder = builder.UseAutoIdentifiers();

                if (options.SoftBreakAsHardBreak)
                    builder = builder.UseSoftlineBreakAsHardlineBreak();

                if (!options.AllowHtml)
                    builder = builder.DisableHtml();

                MarkdownPipeline pipeline = builder.Build();
                Pipelines[key] = pipeline;
                return pipeline;
            }
        }

        private static readonly Dictionary<string, MarkdownPipeline> Pipelines = new Dictionary<string, MarkdownPipeline>();

        private static readonly object PipelineLock = new object();

        /// <summary>Renders Markdown to an HTML fragment — no <c>html</c> or <c>body</c> element around it.</summary>
        /// <param name="markdown">The document. Null and empty both render as an empty string.</param>
        /// <param name="options">How to read it, or null for the defaults.</param>
        public static string ToHtml(string markdown, MarkdownOptions options = null)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            return Markdig.Markdown.ToHtml(markdown, BuildPipeline(options));
        }

        /// <summary>
        /// Strips the document down to the words in it, for a log line or a tooltip where markup would only
        /// be noise.
        /// </summary>
        public static string ToPlainText(string markdown, MarkdownOptions options = null)
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            return Markdig.Markdown.ToPlainText(markdown, BuildPipeline(options));
        }

        /// <summary>Parses the document into the tree the renderers walk.</summary>
        public static MarkdownDocument Parse(string markdown, MarkdownOptions options = null)
        {
            return Markdig.Markdown.Parse(markdown ?? string.Empty, BuildPipeline(options));
        }

        /// <summary>
        /// Lists the document's headings in the order they appear, which is enough to build a table of
        /// contents or to name a document by its first heading.
        /// </summary>
        public static IList<MarkdownHeading> GetOutline(string markdown, MarkdownOptions options = null)
        {
            var headings = new List<MarkdownHeading>();

            if (string.IsNullOrEmpty(markdown))
                return headings;

            foreach (HeadingBlock heading in Parse(markdown, options).Descendants<HeadingBlock>())
            {
                headings.Add(new MarkdownHeading(
                    heading.Level,
                    FlattenToText(heading.Inline),
                    heading.GetAttributes().Id ?? string.Empty));
            }

            return headings;
        }

        /// <summary>
        /// The document's title: its first heading, or null when it has none.
        /// </summary>
        /// <remarks>
        /// The first heading rather than the shallowest. A README whose first line is <c>## Overview</c> is
        /// titled by that, which is what a reader would call it, and looking for an <c>h1</c> that may not
        /// exist would only leave the common case untitled.
        /// </remarks>
        public static string GetTitle(string markdown, MarkdownOptions options = null)
        {
            MarkdownHeading first = GetOutline(markdown, options).FirstOrDefault();
            return string.IsNullOrEmpty(first?.Text) ? null : first.Text;
        }

        /// <summary>
        /// Reduces formatted inline content to its text, so that <c>## The `Result` property</c> reads as
        /// "The Result property" wherever a heading is shown outside a renderer.
        /// </summary>
        private static string FlattenToText(ContainerInline container)
        {
            if (container == null)
                return string.Empty;

            var text = new System.Text.StringBuilder();

            foreach (Inline inline in container.Descendants())
            {
                switch (inline)
                {
                    case LiteralInline literal:
                        text.Append(literal.Content.ToString());
                        break;

                    case CodeInline code:
                        text.Append(code.Content);
                        break;

                    // A line break inside a heading is still a word boundary to whoever reads it back.
                    case LineBreakInline _:
                        text.Append(' ');
                        break;
                }
            }

            return text.ToString().Trim();
        }
    }
}
