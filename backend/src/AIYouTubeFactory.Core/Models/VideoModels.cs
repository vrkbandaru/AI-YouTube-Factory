namespace AIYouTubeFactory.Core.Models;

// ── Video Generation Request ───────────────────────────────────────────────────
public class VideoGenerationRequest
{
    public Guid   SessionId        { get; set; }  // from content generation
    public int    ScriptIndex      { get; set; } = 1;
    public string VoiceName        { get; set; } = "en-US-JennyNeural";
    public string VoiceStyle       { get; set; } = "newscast";
    public float  SpeechRate       { get; set; } = 1.0f;
    public bool   GenerateSubtitles{ get; set; } = true;
    public bool   GenerateImages   { get; set; } = true;
    public string ImageStyle       { get; set; } = "cinematic, professional, high quality";
    public string OutputFormat     { get; set; } = "mp4";
    public VideoResolution Resolution { get; set; } = VideoResolution.HD1080p;
}

public enum VideoResolution
{
    HD720p,   // 1280x720
    HD1080p,  // 1920x1080
    UHD4K     // 3840x2160
}

// ── Storyboard ────────────────────────────────────────────────────────────────
public class Storyboard
{
    public string              VideoTitle { get; set; } = string.Empty;
    public List<StoryboardScene> Scenes   { get; set; } = new();
    public int                 TotalEstimatedSeconds { get; set; }
}

public class StoryboardScene
{
    public int    Index           { get; set; }
    public string Title           { get; set; } = string.Empty;
    public string Narration       { get; set; } = string.Empty;  // text spoken aloud
    public string ImagePrompt     { get; set; } = string.Empty;  // DALL-E prompt
    public string VisualDirection { get; set; } = string.Empty;  // camera/style notes
    public int    DurationSeconds { get; set; }
    public string TransitionType  { get; set; } = "fade";        // fade, cut, dissolve
    public string? GeneratedImagePath { get; set; }
    public string? GeneratedAudioPath { get; set; }
    public double AudioDurationSeconds { get; set; }
}

// ── Voice / Audio ─────────────────────────────────────────────────────────────
public class AudioSegment
{
    public int    SceneIndex   { get; set; }
    public string Text         { get; set; } = string.Empty;
    public string FilePath     { get; set; } = string.Empty;
    public double DurationSeconds { get; set; }
    public long   FileSizeBytes { get; set; }
}

// ── Subtitles ─────────────────────────────────────────────────────────────────
public class SubtitleFile
{
    public string         Format   { get; set; } = "srt";  // srt or vtt
    public string         FilePath { get; set; } = string.Empty;
    public string         Content  { get; set; } = string.Empty;
    public List<SubtitleEntry> Entries { get; set; } = new();
}

public class SubtitleEntry
{
    public int      Index     { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime   { get; set; }
    public string   Text      { get; set; } = string.Empty;
}

// ── Final Video ────────────────────────────────────────────────────────────────
public class GeneratedVideo
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public string   Title           { get; set; } = string.Empty;
    public string   FilePath        { get; set; } = string.Empty;
    public string   FileName        { get; set; } = string.Empty;
    public long     FileSizeBytes   { get; set; }
    public double   DurationSeconds { get; set; }
    public string   Resolution      { get; set; } = string.Empty;
    public int      SceneCount      { get; set; }
    public bool     HasSubtitles    { get; set; }
    public string?  SubtitlePath    { get; set; }
    public string?  ThumbnailPath   { get; set; }
    public DateTime CreatedAt       { get; set; } = DateTime.UtcNow;
    public VideoGenerationStatus Status { get; set; } = VideoGenerationStatus.Pending;
    public string?  ErrorMessage    { get; set; }
}

public enum VideoGenerationStatus
{
    Pending,
    GeneratingStoryboard,
    GeneratingImages,
    GeneratingVoice,
    GeneratingSubtitles,
    ComposingVideo,
    Completed,
    Failed
}

// ── Progress ──────────────────────────────────────────────────────────────────
public class VideoProgressUpdate
{
    public string               Stage        { get; set; } = string.Empty;
    public string               Message      { get; set; } = string.Empty;
    public int                  ProgressPercent { get; set; }
    public VideoGenerationStatus Status       { get; set; }
    public string?              PreviewImagePath { get; set; }
}
