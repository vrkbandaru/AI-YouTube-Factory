using AIYouTubeFactory.Core.Models;

namespace AIYouTubeFactory.Core.Interfaces;

public interface IStoryboardAgent
{
    Task<Storyboard> GenerateStoryboardAsync(
        YouTubeVideoScript script,
        IProgress<VideoProgressUpdate>? progress = null);
}

public interface IImageAgent
{
    Task<List<StoryboardScene>> GenerateSceneImagesAsync(
        Storyboard storyboard,
        string outputDir,
        string style,
        IProgress<VideoProgressUpdate>? progress = null);
}

public interface IVoiceAgent
{
    Task<List<AudioSegment>> GenerateVoiceAsync(
        Storyboard storyboard,
        string outputDir,
        string voiceName,
        string voiceStyle,
        float speechRate,
        IProgress<VideoProgressUpdate>? progress = null);
}

public interface ISubtitleAgent
{
    Task<SubtitleFile> GenerateSubtitlesAsync(
        Storyboard storyboard,
        List<AudioSegment> audioSegments,
        string outputDir,
        IProgress<VideoProgressUpdate>? progress = null);
}

public interface IVideoComposerAgent
{
    Task<GeneratedVideo> ComposeVideoAsync(
        Storyboard storyboard,
        List<AudioSegment> audioSegments,
        SubtitleFile? subtitleFile,
        string outputDir,
        VideoResolution resolution,
        IProgress<VideoProgressUpdate>? progress = null);
}

public interface IVideoGenerationService
{
    Task<GeneratedVideo> GenerateVideoAsync(
        YouTubeVideoScript script,
        VideoGenerationRequest request,
        IProgress<VideoProgressUpdate>? progress = null);

    Task<GeneratedVideo?> GetVideoAsync(Guid videoId);
    Task<List<GeneratedVideo>> GetAllVideosAsync();
    Task DeleteVideoAsync(Guid videoId);
}
