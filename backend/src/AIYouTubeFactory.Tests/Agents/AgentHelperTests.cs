using AIYouTubeFactory.Core.Models;
using FluentAssertions;
using Xunit;

namespace AIYouTubeFactory.Tests.Agents;

/// <summary>
/// Tests for agent JSON parsing helpers — these run without Azure OpenAI.
/// Integration tests that call actual LLM require environment variables.
/// </summary>
public class AgentHelperTests
{
    [Fact]
    public void ExtractJson_FromTextWithLeadingProse_ReturnsJsonArray()
    {
        var text   = "Sure! Here are the ideas:\n```json\n[{\"title\":\"Video 1\"},{\"title\":\"Video 2\"}]\n```";
        var result = ExtractJsonPublic(text);
        result.Should().StartWith("[");
        result.Should().EndWith("]");
        result.Should().Contain("Video 1");
    }

    [Fact]
    public void ExtractJson_FromCleanJson_ReturnsSameJson()
    {
        var json   = "[{\"title\":\"Test\",\"angle\":\"Beginners\"}]";
        var result = ExtractJsonPublic(json);
        result.Should().Be(json);
    }

    [Fact]
    public void ExtractJson_FromObject_ReturnsObject()
    {
        var text   = "Here is the result: {\"strategy\":\"Build authority\",\"pillars\":[\"Education\"]}";
        var result = ExtractJsonPublic(text);
        result.Should().StartWith("{");
        result.Should().Contain("Build authority");
    }

    [Fact]
    public void ExtractJson_FromEmpty_ReturnsFallback()
    {
        var result = ExtractJsonPublic("No JSON here at all.");
        result.Should().BeOneOf("[]", "{}");
    }

    [Theory]
    [InlineData("educational")]
    [InlineData("entertaining")]
    [InlineData("tutorial")]
    public void ContentGenerationRequest_ValidStyles_AreAccepted(string style)
    {
        var req = new ContentGenerationRequest { ContentStyle = style };
        req.ContentStyle.Should().Be(style);
    }

    [Fact]
    public void AgentProgressUpdate_DefaultValues_AreCorrect()
    {
        var update = new AgentProgressUpdate();
        update.ProgressPercent.Should().Be(0);
        update.Status.Should().BeEmpty();
    }

    [Fact]
    public void YouTubeVideoScript_MainContent_InitializesEmpty()
    {
        var script = new YouTubeVideoScript();
        script.MainContent.Should().NotBeNull();
        script.MainContent.Should().BeEmpty();
    }

    [Fact]
    public void UploadedDocument_NewGuid_AssignedOnCreation()
    {
        var doc1 = new UploadedDocument();
        var doc2 = new UploadedDocument();
        doc1.Id.Should().NotBe(doc2.Id);
        doc1.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void ContentGenerationResult_DefaultCounts_AreZero()
    {
        var result = new ContentGenerationResult();
        result.YouTubeScripts.Should().BeEmpty();
        result.ShortsScripts.Should().BeEmpty();
        result.LinkedInPosts.Should().BeEmpty();
        result.TwitterThreads.Should().BeEmpty();
        result.ThumbnailPrompts.Should().BeEmpty();
    }

    [Fact]
    public void ShortsScript_DefaultDuration_Is60Seconds()
    {
        var s = new ShortsScript();
        s.DurationSeconds.Should().Be(60);
    }

    // Expose the private ExtractJson pattern for testing
    private static string ExtractJsonPublic(string text)
    {
        // Strip markdown code blocks
        text = System.Text.RegularExpressions.Regex.Replace(text, @"```(?:json)?", "").Trim();

        int start = -1;
        char openChar = '{', closeChar = '}';
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '[' || text[i] == '{')
            {
                openChar  = text[i];
                closeChar = text[i] == '[' ? ']' : '}';
                start     = i;
                break;
            }
        }
        if (start == -1) return "[]";

        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == openChar)  depth++;
            else if (text[i] == closeChar) { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return "[]";
    }
}
