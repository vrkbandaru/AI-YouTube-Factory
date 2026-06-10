using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using AIYouTubeFactory.API.Hubs;

namespace AIYouTubeFactory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class VideoController : ControllerBase
{
    private readonly IVideoGenerationService _videoService;
    private readonly IHubContext<ContentGenerationHub> _hub;
    private readonly ILogger<VideoController> _logger;

    public VideoController(
        IVideoGenerationService videoService,
        IHubContext<ContentGenerationHub> hub,
        ILogger<VideoController> logger)
    {
        _videoService = videoService;
        _hub          = hub;
        _logger       = logger;
    }

    /// <summary>Start video generation</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateVideo([FromBody] VideoGenerationRequest request)
    {
        if (!ContentController.Results.TryGetValue(request.SessionId, out var contentResult))
            return NotFound("Content session not found. Please generate content first.");

        var script = contentResult.YouTubeScripts
            .FirstOrDefault(s => s.Index == request.ScriptIndex);

        if (script == null)
            return NotFound($"Script #{request.ScriptIndex} not found in session {request.SessionId}.");

        var videoSessionId = Guid.NewGuid().ToString();

        _ = Task.Run(async () =>
        {
            try
            {
                var reporter = new VideoProgressReporter(_hub, videoSessionId);
                var video    = await _videoService.GenerateVideoAsync(script, request, reporter);

                // Fix: include ALL fields including subtitleUrl and thumbnailUrl
                await _hub.Clients.Group(videoSessionId).SendAsync("VideoGenerationComplete", new
                {
                    videoId         = video.Id,
                    title           = video.Title,
                    status          = video.Status.ToString(),
                    durationSeconds = video.DurationSeconds,
                    fileSizeBytes   = video.FileSizeBytes,
                    resolution      = video.Resolution,
                    sceneCount      = video.SceneCount,
                    hasSubtitles    = video.HasSubtitles,
                    errorMessage    = video.ErrorMessage,
                    downloadUrl     = video.Status == VideoGenerationStatus.Completed
                        ? $"/api/video/download/{video.Id}" : null,
                    subtitleUrl     = video.HasSubtitles && video.SubtitlePath != null
                        ? $"/api/video/subtitle/{video.Id}" : null,
                    thumbnailUrl    = video.ThumbnailPath != null
                        ? $"/api/video/thumbnail/{video.Id}" : null
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Video generation error — session {SessionId}", videoSessionId);
                await _hub.Clients.Group(videoSessionId)
                    .SendAsync("VideoGenerationError", ex.Message);
            }
        });

        return Accepted(new
        {
            videoSessionId,
            scriptTitle = script.Title,
            message     = "Video generation started. Join SignalR session for live progress."
        });
    }

    /// <summary>Get video status and download URLs</summary>
    [HttpGet("{videoId}")]
    public async Task<IActionResult> GetVideo(Guid videoId)
    {
        var video = await _videoService.GetVideoAsync(videoId);
        if (video == null) return NotFound("Video not found.");

        return Ok(new
        {
            id              = video.Id,
            title           = video.Title,
            status          = video.Status.ToString(),
            durationSeconds = video.DurationSeconds,
            fileSizeBytes   = video.FileSizeBytes,
            resolution      = video.Resolution,
            sceneCount      = video.SceneCount,
            hasSubtitles    = video.HasSubtitles,
            createdAt       = video.CreatedAt,
            errorMessage    = video.ErrorMessage,
            downloadUrl     = video.Status == VideoGenerationStatus.Completed
                ? $"/api/video/download/{video.Id}" : null,
            subtitleUrl     = video.HasSubtitles
                ? $"/api/video/subtitle/{video.Id}" : null,
            thumbnailUrl    = video.ThumbnailPath != null
                ? $"/api/video/thumbnail/{video.Id}" : null
        });
    }

    /// <summary>List all videos</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllVideos()
    {
        var videos = await _videoService.GetAllVideosAsync();
        return Ok(videos.Select(v => new
        {
            id              = v.Id,
            title           = v.Title,
            status          = v.Status.ToString(),
            durationSeconds = v.DurationSeconds,
            fileSizeBytes   = v.FileSizeBytes,
            resolution      = v.Resolution,
            sceneCount      = v.SceneCount,
            hasSubtitles    = v.HasSubtitles,
            createdAt       = v.CreatedAt,
            errorMessage    = v.ErrorMessage,
            downloadUrl     = v.Status == VideoGenerationStatus.Completed
                ? $"/api/video/download/{v.Id}" : null,
            subtitleUrl     = v.HasSubtitles
                ? $"/api/video/subtitle/{v.Id}" : null,
            thumbnailUrl    = v.ThumbnailPath != null
                ? $"/api/video/thumbnail/{v.Id}" : null
        }));
    }

    /// <summary>Download MP4 — supports range requests for large files</summary>
    [HttpGet("download/{videoId}")]
    public async Task<IActionResult> DownloadVideo(Guid videoId)
    {
        var video = await _videoService.GetVideoAsync(videoId);

        if (video == null)
            return NotFound("Video not found.");
        if (video.Status != VideoGenerationStatus.Completed)
            return BadRequest($"Video is not ready yet. Status: {video.Status}");
        if (string.IsNullOrEmpty(video.FilePath) || !System.IO.File.Exists(video.FilePath))
            return NotFound("Video file missing from disk.");

        var fileInfo = new FileInfo(video.FilePath);
        if (fileInfo.Length == 0)
            return Problem("Video file is empty (0 bytes). Re-generate the video.");

        var fileName = $"{SanitizeFileName(video.Title)}.mp4";
        var stream   = System.IO.File.OpenRead(video.FilePath);

        // Add CORS headers so browser can access the download
        Response.Headers.Append("Access-Control-Expose-Headers", "Content-Disposition");
        Response.Headers.Append("Content-Disposition", $"attachment; filename=\"{fileName}\"");
        Response.Headers.Append("X-Video-Duration",    video.DurationSeconds.ToString("F2"));
        Response.Headers.Append("X-Video-Resolution",  video.Resolution);
        Response.Headers.Append("X-File-Size",         fileInfo.Length.ToString());

        return File(stream, "video/mp4", fileName, enableRangeProcessing: true);
    }

    /// <summary>Download SRT subtitle</summary>
    [HttpGet("subtitle/{videoId}")]
    public async Task<IActionResult> DownloadSubtitle(Guid videoId)
    {
        var video = await _videoService.GetVideoAsync(videoId);
        if (video?.SubtitlePath == null || !System.IO.File.Exists(video.SubtitlePath))
            return NotFound("Subtitle file not found.");

        var bytes    = await System.IO.File.ReadAllBytesAsync(video.SubtitlePath);
        var fileName = $"{SanitizeFileName(video.Title)}.srt";
        return File(bytes, "text/plain; charset=utf-8", fileName);
    }

    /// <summary>Get thumbnail JPEG</summary>
    [HttpGet("thumbnail/{videoId}")]
    public async Task<IActionResult> GetThumbnail(Guid videoId)
    {
        var video = await _videoService.GetVideoAsync(videoId);
        if (video?.ThumbnailPath == null || !System.IO.File.Exists(video.ThumbnailPath))
            return NotFound("Thumbnail not found.");

        var bytes = await System.IO.File.ReadAllBytesAsync(video.ThumbnailPath);
        return File(bytes, "image/jpeg");
    }

    /// <summary>Delete video and its files</summary>
    [HttpDelete("{videoId}")]
    public async Task<IActionResult> DeleteVideo(Guid videoId)
    {
        await _videoService.DeleteVideoAsync(videoId);
        return NoContent();
    }

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c))
              .Replace(' ', '_')[..Math.Min(name.Length, 80)];
}

public class VideoProgressReporter : IProgress<VideoProgressUpdate>
{
    private readonly IHubContext<ContentGenerationHub> _hub;
    private readonly string _sessionId;

    public VideoProgressReporter(IHubContext<ContentGenerationHub> hub, string sessionId)
    {
        _hub       = hub;
        _sessionId = sessionId;
    }

    public void Report(VideoProgressUpdate value)
    {
        _hub.Clients.Group(_sessionId).SendAsync("VideoProgress", value);
    }
}
