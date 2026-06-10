using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using AIYouTubeFactory.Infrastructure.Services;
using FluentAssertions;
using Moq;
using Xunit;

namespace AIYouTubeFactory.Tests.Infrastructure;

public class ContentOrchestratorTests
{
    private readonly Mock<IScriptAgent>       _scriptAgent   = new();
    private readonly Mock<IThumbnailAgent>    _thumbAgent    = new();
    private readonly Mock<ISEOAgent>          _seoAgent      = new();
    private readonly Mock<IVideoPlannerAgent> _plannerAgent  = new();
    private readonly Mock<ISocialMediaAgent>  _socialAgent   = new();

    private ContentOrchestrator CreateSut() =>
        new(_scriptAgent.Object, _thumbAgent.Object,
            _seoAgent.Object, _plannerAgent.Object, _socialAgent.Object);

    private static UploadedDocument MakeDoc(string topic = "Microservices") => new()
    {
        Id            = Guid.NewGuid(),
        FileName      = "test.md",
        FileType      = "md",
        ExtractedText = "Sample content about " + topic,
        Topic         = topic
    };

    private static ContentGenerationRequest MakeRequest(Guid docId) => new()
    {
        DocumentId              = docId,
        Topic                   = "Microservices",
        YouTubeVideoCount       = 2,
        ShortsCount             = 3,
        LinkedInPostCount       = 1,
        TwitterThreadCount      = 1,
        GenerateThumbnailPrompts = true,
        GenerateSEO             = true,
        TargetAudience          = "developers",
        ContentStyle            = "educational"
    };

    [Fact]
    public async Task OrchestrateAsync_ReturnsResult_WithAllContent()
    {
        var doc     = MakeDoc();
        var request = MakeRequest(doc.Id);

        _scriptAgent.Setup(x => x.GenerateYouTubeScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<YouTubeVideoScript>
            {
                new() { Index = 1, Title = "Microservices Explained", EstimatedDurationMinutes = 12 },
                new() { Index = 2, Title = "API Gateway Pattern",     EstimatedDurationMinutes = 10 }
            });

        _scriptAgent.Setup(x => x.GenerateShortsScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<ShortsScript>
            {
                new() { Index = 1, Title = "What is a Microservice?" },
                new() { Index = 2, Title = "Service Discovery in 60s" },
                new() { Index = 3, Title = "Docker vs VM" }
            });

        _seoAgent.Setup(x => x.EnrichWithSEOAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<string>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<SEOData> { new() { PrimaryKeyword = "microservices tutorial" } });

        _thumbAgent.Setup(x => x.GenerateThumbnailPromptsAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<ThumbnailPrompt> { new() { VideoTitle = "Microservices Explained" } });

        _socialAgent.Setup(x => x.GenerateLinkedInPostsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<LinkedInPost> { new() { Index = 1, Title = "The microservices mindset" } });

        _socialAgent.Setup(x => x.GenerateTwitterThreadsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<TwitterThread> { new() { Index = 1, Topic = "Microservices tips" } });

        _plannerAgent.Setup(x => x.GenerateContentPlanAsync(
            It.IsAny<string>(), It.IsAny<List<YouTubeVideoScript>>(),
            It.IsAny<List<ShortsScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new VideoContentPlan { OverallStrategy = "Build authority in microservices" });

        var sut    = CreateSut();
        var result = await sut.OrchestrateAsync(doc, request);

        result.Should().NotBeNull();
        result.Topic.Should().Be("Microservices");
        result.YouTubeScripts.Should().HaveCount(2);
        result.ShortsScripts.Should().HaveCount(3);
        result.LinkedInPosts.Should().HaveCount(1);
        result.TwitterThreads.Should().HaveCount(1);
        result.ThumbnailPrompts.Should().HaveCount(1);
        result.ContentPlan.OverallStrategy.Should().NotBeEmpty();
        result.SessionId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OrchestrateAsync_CallsAllAgents()
    {
        var doc = MakeDoc();
        SetupAllMocksWithDefaults();

        var sut = CreateSut();
        await sut.OrchestrateAsync(doc, MakeRequest(doc.Id));

        _scriptAgent.Verify(x => x.GenerateYouTubeScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);

        _scriptAgent.Verify(x => x.GenerateShortsScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);

        _seoAgent.Verify(x => x.EnrichWithSEOAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<string>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);

        _thumbAgent.Verify(x => x.GenerateThumbnailPromptsAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);

        _socialAgent.Verify(x => x.GenerateLinkedInPostsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);

        _plannerAgent.Verify(x => x.GenerateContentPlanAsync(
            It.IsAny<string>(), It.IsAny<List<YouTubeVideoScript>>(),
            It.IsAny<List<ShortsScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()), Times.Once);
    }

    [Fact]
    public async Task OrchestrateAsync_UsesDocumentTopic_WhenRequestTopicEmpty()
    {
        var doc     = MakeDoc("Docker Deep Dive");
        var request = MakeRequest(doc.Id);
        request.Topic = "";
        SetupAllMocksWithDefaults();

        var result = await CreateSut().OrchestrateAsync(doc, request);
        result.Topic.Should().Be("Docker Deep Dive");
    }

    [Fact]
    public async Task OrchestrateAsync_ReportsProgress()
    {
        var doc      = MakeDoc();
        var updates  = new List<AgentProgressUpdate>();
        var progress = new Progress<AgentProgressUpdate>(u => updates.Add(u));
        SetupAllMocksWithDefaults();

        await CreateSut().OrchestrateAsync(doc, MakeRequest(doc.Id), progress);

        updates.Should().NotBeEmpty();
        updates.Should().Contain(u => u.Status == "completed");
    }

    private void SetupAllMocksWithDefaults()
    {
        _scriptAgent.Setup(x => x.GenerateYouTubeScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<YouTubeVideoScript> { new() { Index = 1, Title = "Test Video" } });

        _scriptAgent.Setup(x => x.GenerateShortsScriptsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<ShortsScript> { new() { Index = 1, Title = "Test Short" } });

        _seoAgent.Setup(x => x.EnrichWithSEOAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<string>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<SEOData>());

        _thumbAgent.Setup(x => x.GenerateThumbnailPromptsAsync(
            It.IsAny<List<YouTubeVideoScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<ThumbnailPrompt>());

        _socialAgent.Setup(x => x.GenerateLinkedInPostsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<LinkedInPost>());

        _socialAgent.Setup(x => x.GenerateTwitterThreadsAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(),
            It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new List<TwitterThread>());

        _plannerAgent.Setup(x => x.GenerateContentPlanAsync(
            It.IsAny<string>(), It.IsAny<List<YouTubeVideoScript>>(),
            It.IsAny<List<ShortsScript>>(), It.IsAny<IProgress<AgentProgressUpdate>>()))
            .ReturnsAsync(new VideoContentPlan());
    }
}
