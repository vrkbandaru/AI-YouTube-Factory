namespace AIYouTubeFactory.Core.Models;

public class UploadedDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // pdf, pptx, docx, md
    public string ExtractedText { get; set; } = string.Empty;
    public List<string> Sections { get; set; } = new();
    public string Topic { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

public class ContentGenerationRequest
{
    public Guid DocumentId { get; set; }
    public string Topic { get; set; } = string.Empty;
    public int YouTubeVideoCount { get; set; } = 10;
    public int ShortsCount { get; set; } = 20;
    public int LinkedInPostCount { get; set; } = 5;
    public int TwitterThreadCount { get; set; } = 5;
    public bool GenerateThumbnailPrompts { get; set; } = true;
    public bool GenerateSEO { get; set; } = true;
    public string TargetAudience { get; set; } = "developers and tech enthusiasts";
    public string ContentStyle { get; set; } = "educational"; // educational, entertaining, inspirational
}

public class ContentGenerationResult
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string Topic { get; set; } = string.Empty;
    public List<YouTubeVideoScript> YouTubeScripts { get; set; } = new();
    public List<ShortsScript> ShortsScripts { get; set; } = new();
    public List<LinkedInPost> LinkedInPosts { get; set; } = new();
    public List<TwitterThread> TwitterThreads { get; set; } = new();
    public List<ThumbnailPrompt> ThumbnailPrompts { get; set; } = new();
    public VideoContentPlan ContentPlan { get; set; } = new();
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

public class YouTubeVideoScript
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public string Introduction { get; set; } = string.Empty;
    public List<ScriptSection> MainContent { get; set; } = new();
    public string CallToAction { get; set; } = string.Empty;
    public string Outro { get; set; } = string.Empty;
    public int EstimatedDurationMinutes { get; set; }
    public SEOData SEO { get; set; } = new();
    public ThumbnailPrompt ThumbnailPrompt { get; set; } = new();
}

public class ScriptSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int DurationSeconds { get; set; }
    public string VisualNote { get; set; } = string.Empty;
}

public class ShortsScript
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Hook { get; set; } = string.Empty;
    public string MainPoint { get; set; } = string.Empty;
    public string CallToAction { get; set; } = string.Empty;
    public int DurationSeconds { get; set; } = 60;
    public List<string> Hashtags { get; set; } = new();
    public string VisualConcept { get; set; } = string.Empty;
}

public class LinkedInPost
{
    public int Index { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public List<string> Hashtags { get; set; } = new();
    public string CallToAction { get; set; } = string.Empty;
    public string PostType { get; set; } = string.Empty; // article, thought-leadership, tips, story
}

public class TwitterThread
{
    public int Index { get; set; }
    public string Topic { get; set; } = string.Empty;
    public List<string> Tweets { get; set; } = new();
    public List<string> Hashtags { get; set; } = new();
}

public class ThumbnailPrompt
{
    public string VideoTitle { get; set; } = string.Empty;
    public string MainText { get; set; } = string.Empty;
    public string BackgroundDescription { get; set; } = string.Empty;
    public string ColorScheme { get; set; } = string.Empty;
    public string FaceExpression { get; set; } = string.Empty;
    public string DallePrompt { get; set; } = string.Empty;
    public string MidjourneyPrompt { get; set; } = string.Empty;
}

public class SEOData
{
    public string PrimaryKeyword { get; set; } = string.Empty;
    public List<string> SecondaryKeywords { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public string OptimizedTitle { get; set; } = string.Empty;
    public string OptimizedDescription { get; set; } = string.Empty;
    public List<string> Chapters { get; set; } = new();
}

public class VideoContentPlan
{
    public string OverallStrategy { get; set; } = string.Empty;
    public List<string> ContentPillars { get; set; } = new();
    public List<PublishingScheduleItem> PublishingSchedule { get; set; } = new();
    public List<string> SeriesIdeas { get; set; } = new();
    public string ChannelGrowthStrategy { get; set; } = string.Empty;
}

public class PublishingScheduleItem
{
    public int WeekNumber { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string PublishDay { get; set; } = string.Empty;
}

public class AgentProgressUpdate
{
    public string AgentName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty; // running, completed, error
    public string Message { get; set; } = string.Empty;
    public int ProgressPercent { get; set; }
    public object? Data { get; set; }
}
