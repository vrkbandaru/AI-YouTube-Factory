using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.Extensions.Logging;

namespace AIYouTubeFactory.Infrastructure.Services;

public class VideoGenerationService : IVideoGenerationService
{
    private readonly IStoryboardAgent    _storyboardAgent;
    private readonly IImageAgent         _imageAgent;
    private readonly IVoiceAgent         _voiceAgent;
    private readonly ISubtitleAgent      _subtitleAgent;
    private readonly IVideoComposerAgent _composerAgent;
    private readonly ILogger<VideoGenerationService> _logger;
    private readonly string              _outputRoot;

    private static readonly Dictionary<Guid, GeneratedVideo> _videos = new();

    public VideoGenerationService(
        IStoryboardAgent    storyboardAgent,
        IImageAgent         imageAgent,
        IVoiceAgent         voiceAgent,
        ISubtitleAgent      subtitleAgent,
        IVideoComposerAgent composerAgent,
        ILogger<VideoGenerationService> logger,
        string outputRoot)
    {
        _storyboardAgent = storyboardAgent;
        _imageAgent      = imageAgent;
        _voiceAgent      = voiceAgent;
        _subtitleAgent   = subtitleAgent;
        _composerAgent   = composerAgent;
        _logger          = logger;
        _outputRoot      = outputRoot;
        Directory.CreateDirectory(outputRoot);
    }

    public async Task<GeneratedVideo> GenerateVideoAsync(
        YouTubeVideoScript script,
        VideoGenerationRequest request,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        var videoId   = Guid.NewGuid();
        var outputDir = Path.Combine(_outputRoot, videoId.ToString());
        Directory.CreateDirectory(outputDir);

        _logger.LogInformation("▶ Starting video: {Title} | Resolution:{Res} | Images:{Img} | Subs:{Sub}",
            script.Title, request.Resolution, request.GenerateImages, request.GenerateSubtitles);

        var pending = new GeneratedVideo
        {
            Id     = videoId,
            Title  = script.Title,
            Status = VideoGenerationStatus.Pending
        };
        _videos[videoId] = pending;

        try
        {
            // ── 1. Storyboard ─────────────────────────────────────────────────
            pending.Status = VideoGenerationStatus.GeneratingStoryboard;
            _logger.LogInformation("  [1/5] Generating storyboard...");
            var storyboard = await _storyboardAgent.GenerateStoryboardAsync(script, progress);
            _logger.LogInformation("  [1/5] Storyboard: {N} scenes", storyboard.Scenes.Count);

            // ── 2. Images ─────────────────────────────────────────────────────
            if (request.GenerateImages)
            {
                pending.Status = VideoGenerationStatus.GeneratingImages;
                _logger.LogInformation("  [2/5] Generating {N} scene images...", storyboard.Scenes.Count);
                var imagesDir = Path.Combine(outputDir, "images");
                await _imageAgent.GenerateSceneImagesAsync(
                    storyboard, imagesDir, request.ImageStyle, progress);
                var generated = storyboard.Scenes.Count(s => !string.IsNullOrEmpty(s.GeneratedImagePath));
                _logger.LogInformation("  [2/5] Images done: {N}/{Total}", generated, storyboard.Scenes.Count);
            }
            else
            {
                progress?.Report(new VideoProgressUpdate
                {
                    Stage           = "Image Agent",
                    Message         = "⏭️ Image generation skipped",
                    ProgressPercent = 40,
                    Status          = VideoGenerationStatus.GeneratingImages
                });
            }

            // ── 3. Voice ──────────────────────────────────────────────────────
            pending.Status = VideoGenerationStatus.GeneratingVoice;
            _logger.LogInformation("  [3/5] Generating voice narration...");
            var audioDir      = Path.Combine(outputDir, "audio");
            var audioSegments = await _voiceAgent.GenerateVoiceAsync(
                storyboard, audioDir,
                request.VoiceName, request.VoiceStyle,
                request.SpeechRate, progress);
            var withAudio = audioSegments.Count(s => !string.IsNullOrEmpty(s.FilePath));
            _logger.LogInformation("  [3/5] Audio: {With}/{Total} segments with audio",
                withAudio, audioSegments.Count);

            // ── 4. Subtitles ──────────────────────────────────────────────────
            SubtitleFile? subtitleFile = null;
            if (request.GenerateSubtitles)
            {
                pending.Status = VideoGenerationStatus.GeneratingSubtitles;
                _logger.LogInformation("  [4/5] Generating subtitles...");
                var subDir   = Path.Combine(outputDir, "subtitles");
                subtitleFile = await _subtitleAgent.GenerateSubtitlesAsync(
                    storyboard, audioSegments, subDir, progress);
                _logger.LogInformation("  [4/5] Subtitles: {N} entries", subtitleFile.Entries.Count);
            }
            else
            {
                progress?.Report(new VideoProgressUpdate
                {
                    Stage           = "Subtitle Agent",
                    Message         = "⏭️ Subtitles skipped",
                    ProgressPercent = 65,
                    Status          = VideoGenerationStatus.GeneratingSubtitles
                });
            }

            // ── 5. Compose ────────────────────────────────────────────────────
            pending.Status = VideoGenerationStatus.ComposingVideo;
            _logger.LogInformation("  [5/5] Composing final video...");
            var video = await _composerAgent.ComposeVideoAsync(
                storyboard, audioSegments, subtitleFile,
                outputDir, request.Resolution, progress);

            video.Id         = videoId;
            _videos[videoId] = video;

            if (video.Status == VideoGenerationStatus.Completed)
                _logger.LogInformation("✅ Video complete: {Title} | {MB:F1}MB | {Dur:mm\\:ss}",
                    video.Title,
                    video.FileSizeBytes / 1_048_576.0,
                    TimeSpan.FromSeconds(video.DurationSeconds));
            else
                _logger.LogWarning("⚠️ Video composer returned: {Status} — {Err}",
                    video.Status, video.ErrorMessage);

            return video;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Video generation failed for: {Title}", script.Title);
            var failed = new GeneratedVideo
            {
                Id           = videoId,
                Title        = script.Title,
                Status       = VideoGenerationStatus.Failed,
                ErrorMessage = ex.Message
            };
            _videos[videoId] = failed;
            return failed;
        }
    }

    public Task<GeneratedVideo?> GetVideoAsync(Guid videoId) =>
        Task.FromResult(_videos.TryGetValue(videoId, out var v) ? v : null);

    public Task<List<GeneratedVideo>> GetAllVideosAsync() =>
        Task.FromResult(_videos.Values.OrderByDescending(v => v.CreatedAt).ToList());

    public Task DeleteVideoAsync(Guid videoId)
    {
        if (_videos.TryGetValue(videoId, out var video))
        {
            var dir = Path.GetDirectoryName(video.FilePath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                try { Directory.Delete(dir, true); } catch { }
            _videos.Remove(videoId);
        }
        return Task.CompletedTask;
    }
}
