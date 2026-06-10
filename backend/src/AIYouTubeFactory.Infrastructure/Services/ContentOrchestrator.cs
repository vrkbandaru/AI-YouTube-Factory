using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;

namespace AIYouTubeFactory.Infrastructure.Services;

public class ContentOrchestrator : IContentOrchestrator
{
    private readonly IScriptAgent _scriptAgent;
    private readonly IThumbnailAgent _thumbnailAgent;
    private readonly ISEOAgent _seoAgent;
    private readonly IVideoPlannerAgent _plannerAgent;
    private readonly ISocialMediaAgent _socialAgent;

    public ContentOrchestrator(
        IScriptAgent scriptAgent,
        IThumbnailAgent thumbnailAgent,
        ISEOAgent seoAgent,
        IVideoPlannerAgent plannerAgent,
        ISocialMediaAgent socialAgent)
    {
        _scriptAgent = scriptAgent;
        _thumbnailAgent = thumbnailAgent;
        _seoAgent = seoAgent;
        _plannerAgent = plannerAgent;
        _socialAgent = socialAgent;
    }

    public async Task<ContentGenerationResult> OrchestrateAsync(
        UploadedDocument document, ContentGenerationRequest request,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        var result = new ContentGenerationResult
        {
            Topic = request.Topic ?? document.Topic
        };

        var content = document.ExtractedText;

        // Phase 1: Scripts (long videos + shorts) - run in parallel
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Orchestrator",
            Status = "running",
            Message = "🚀 Phase 1: Generating scripts...",
            ProgressPercent = 5
        });

        var scriptsTask = _scriptAgent.GenerateYouTubeScriptsAsync(
            content, result.Topic, request.YouTubeVideoCount,
            request.TargetAudience, request.ContentStyle, progress);

        var shortsTask = _scriptAgent.GenerateShortsScriptsAsync(
            content, result.Topic, request.ShortsCount, progress);

        await Task.WhenAll(scriptsTask, shortsTask);

        result.YouTubeScripts = await scriptsTask;
        result.ShortsScripts = await shortsTask;

        // Phase 2: SEO + Thumbnails + Social - run in parallel
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Orchestrator",
            Status = "running",
            Message = "⚡ Phase 2: SEO, thumbnails, and social content...",
            ProgressPercent = 55
        });

        var seoTask = request.GenerateSEO
            ? _seoAgent.EnrichWithSEOAsync(result.YouTubeScripts, result.Topic, progress)
            : Task.FromResult(new List<SEOData>());

        var thumbTask = request.GenerateThumbnailPrompts
            ? _thumbnailAgent.GenerateThumbnailPromptsAsync(result.YouTubeScripts, progress)
            : Task.FromResult(new List<ThumbnailPrompt>());

        var linkedInTask = _socialAgent.GenerateLinkedInPostsAsync(
            content, result.Topic, request.LinkedInPostCount, progress);

        var twitterTask = _socialAgent.GenerateTwitterThreadsAsync(
            content, result.Topic, request.TwitterThreadCount, progress);

        await Task.WhenAll(seoTask, thumbTask, linkedInTask, twitterTask);

        result.ThumbnailPrompts = await thumbTask;
        result.LinkedInPosts = await linkedInTask;
        result.TwitterThreads = await twitterTask;

        // Phase 3: Content Plan
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Orchestrator",
            Status = "running",
            Message = "📅 Phase 3: Building content strategy...",
            ProgressPercent = 88
        });

        result.ContentPlan = await _plannerAgent.GenerateContentPlanAsync(
            result.Topic, result.YouTubeScripts, result.ShortsScripts, progress);

        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Orchestrator",
            Status = "completed",
            Message = $"🎉 Complete! Generated {result.YouTubeScripts.Count} videos, {result.ShortsScripts.Count} shorts, {result.LinkedInPosts.Count} LinkedIn posts, {result.TwitterThreads.Count} Twitter threads",
            ProgressPercent = 100,
            Data = result
        });

        return result;
    }
}
