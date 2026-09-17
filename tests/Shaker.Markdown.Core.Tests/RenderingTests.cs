using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Shaker.Markdown.Core.Tests
{
    public class RenderingTests
    {
        [Fact]
        public void HeadingsAndEmphasisRender()
        {
            string html = MarkdownEngine.ToHtml("# Title\n\nSome **bold** and *italic* text.");

            Assert.Contains("<h1", html);
            Assert.Contains("<strong>bold</strong>", html);
            Assert.Contains("<em>italic</em>", html);
        }

        [Fact]
        public void TablesRenderUnderTheAdvancedFlavor()
        {
            string html = MarkdownEngine.ToHtml("| a | b |\n|---|---|\n| 1 | 2 |");

            Assert.Contains("<table>", html);
            Assert.Contains("<td>1</td>", html);
        }

        [Fact]
        public void TablesAreNotAFeatureOfPlainCommonMark()
        {
            string html = MarkdownEngine.ToHtml(
                "| a | b |\n|---|---|\n| 1 | 2 |",
                new MarkdownOptions { Flavor = MarkdownFlavor.CommonMark });

            Assert.DoesNotContain("<table>", html);
        }

        [Fact]
        public void TaskListsRender()
        {
            string html = MarkdownEngine.ToHtml("- [x] done\n- [ ] not done");

            Assert.Contains("type=\"checkbox\"", html);
            Assert.Contains("checked", html);
        }

        [Fact]
        public void NullAndEmptyRenderToEmptyRatherThanThrowing()
        {
            Assert.Equal(string.Empty, MarkdownEngine.ToHtml(null));
            Assert.Equal(string.Empty, MarkdownEngine.ToHtml(string.Empty));
            Assert.Equal(string.Empty, MarkdownEngine.ToPlainText(null));
            Assert.Empty(MarkdownEngine.GetOutline(null));
        }

        [Fact]
        public void PlainTextKeepsTheWordsAndDropsTheMarkup()
        {
            string text = MarkdownEngine.ToPlainText("# Title\n\nSome **bold** text.");

            Assert.Contains("Title", text);
            Assert.Contains("bold", text);
            Assert.DoesNotContain("**", text);
            Assert.DoesNotContain("#", text);
        }

        [Fact]
        public void SoftBreaksJoinLinesUnlessAskedNotTo()
        {
            Assert.DoesNotContain("<br", MarkdownEngine.ToHtml("one\ntwo"));

            Assert.Contains("<br", MarkdownEngine.ToHtml(
                "one\ntwo", new MarkdownOptions { SoftBreakAsHardBreak = true }));
        }
    }

    public class HtmlSafetyTests
    {
        private const string Dangerous = "Hello <script>alert('x')</script> <img src=x onerror=alert(1)>";

        [Fact]
        public void RawHtmlIsEscapedByDefault()
        {
            string html = MarkdownEngine.ToHtml(Dangerous);

            Assert.DoesNotContain("<script>", html);
            Assert.Contains("&lt;script&gt;", html);
        }

        [Fact]
        public void RawHtmlSurvivesOnlyWhenItIsAskedFor()
        {
            string html = MarkdownEngine.ToHtml(Dangerous, new MarkdownOptions { AllowHtml = true });

            Assert.Contains("<script>", html);
        }

        [Fact]
        public void APageForbidsScriptUnlessScriptIsAllowed()
        {
            Assert.Contains("Content-Security-Policy", HtmlDocument.Build("<p>hi</p>"));
            Assert.Contains("default-src 'none'", HtmlDocument.Build("<p>hi</p>"));

            Assert.DoesNotContain(
                "Content-Security-Policy",
                HtmlDocument.Build("<p>hi</p>", allowScripts: true));
        }
    }

    public class OutlineTests
    {
        private const string Document = @"# Process
Intro text.

## Inputs
### Queue
## Outputs
";

        [Fact]
        public void HeadingsComeBackInOrderWithTheirLevels()
        {
            IList<MarkdownHeading> outline = MarkdownEngine.GetOutline(Document);

            Assert.Equal(
                new[] { "Process", "Inputs", "Queue", "Outputs" },
                outline.Select(h => h.Text).ToArray());

            Assert.Equal(new[] { 1, 2, 3, 2 }, outline.Select(h => h.Level).ToArray());
        }

        [Fact]
        public void HeadingsCarryTheAnchorTheRendererGivesThem()
        {
            MarkdownHeading queue = MarkdownEngine.GetOutline(Document).Single(h => h.Text == "Queue");

            Assert.Equal("queue", queue.Anchor);
        }

        [Fact]
        public void FormattingInsideAHeadingIsFlattenedAway()
        {
            MarkdownHeading heading = MarkdownEngine.GetOutline("## The `Result` property").Single();

            Assert.Equal("The Result property", heading.Text);
        }

        [Fact]
        public void TheTitleIsTheFirstHeadingWhateverItsLevel()
        {
            Assert.Equal("Process", MarkdownEngine.GetTitle(Document));
            Assert.Equal("Overview", MarkdownEngine.GetTitle("## Overview\n\ntext"));
        }

        [Fact]
        public void ADocumentWithNoHeadingsHasNoTitle()
        {
            Assert.Null(MarkdownEngine.GetTitle("just a paragraph"));
        }
    }

    public class PageTests
    {
        [Fact]
        public void ThemesChangeTheBackground()
        {
            Assert.Contains("#ffffff", HtmlDocument.Build("<p>hi</p>", theme: DocumentTheme.Light));
            Assert.Contains("#1e1e1e", HtmlDocument.Build("<p>hi</p>", theme: DocumentTheme.Dark));
        }

        [Fact]
        public void TheTitleIsEscapedRatherThanInterpolated()
        {
            string page = HtmlDocument.Build("<p>hi</p>", "<script>x</script>");

            Assert.Contains("&lt;script&gt;", page);
            Assert.DoesNotContain("<title><script>", page);
        }

        [Fact]
        public void AnExplicitBaseHrefWinsOverTheFolder()
        {
            string page = HtmlDocument.Build(
                "<p>hi</p>", basePath: "/tmp", baseHrefOverride: "https://markdown.invalid/");

            Assert.Contains("<base href=\"https://markdown.invalid/\"", page);
            Assert.DoesNotContain("file://", page);
        }

        [Fact]
        public void NoFolderAndNoOverrideMeansNoBaseElement()
        {
            Assert.DoesNotContain("<base", HtmlDocument.Build("<p>hi</p>"));
        }
    }
}
