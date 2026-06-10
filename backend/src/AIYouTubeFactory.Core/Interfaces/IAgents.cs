using AIYouTubeFactory.Core.Models;

namespace AIYouTubeFactory.Core.Interfaces;

public interface IScriptAgent
{
    Task<List<YouTubeVideoScript>> GenerateYouTubeScriptsAsync(
        string content, string topic, int count, string audience, string style,
        IProgress<AgentProgressUpdate>? progress = null);

    Task<List<ShortsScript>> GenerateShortsScriptsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface IThumbnailAgent
{
    Task<List<ThumbnailPrompt>> GenerateThumbnailPromptsAsync(
        List<YouTubeVideoScript> scripts,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface ISEOAgent
{
    Task<List<SEOData>> EnrichWithSEOAsync(
        List<YouTubeVideoScript> scripts, string topic,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface IVideoPlannerAgent
{
    Task<VideoContentPlan> GenerateContentPlanAsync(
        string topic, List<YouTubeVideoScript> scripts, List<ShortsScript> shorts,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface ISocialMediaAgent
{
    Task<List<LinkedInPost>> GenerateLinkedInPostsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null);

    Task<List<TwitterThread>> GenerateTwitterThreadsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface IDocumentParserService
{
    Task<UploadedDocument> ParseAsync(Stream fileStream, string fileName, string contentType);
}

public interface IContentOrchestrator
{
    Task<ContentGenerationResult> OrchestrateAsync(
        UploadedDocument document, ContentGenerationRequest request,
        IProgress<AgentProgressUpdate>? progress = null);
}

public interface ISearchIndexService
{
    Task IndexDocumentAsync(UploadedDocument document);
    Task<List<string>> SearchRelatedContentAsync(string query, int topK = 5);
}
