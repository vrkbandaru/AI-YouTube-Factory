using Microsoft.AspNetCore.Mvc;
using AIYouTubeFactory.Core.Models;
using System.Text;

namespace AIYouTubeFactory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    // Shared results store (same as ContentController — in production use Redis/DB)
    private static readonly Dictionary<Guid, ContentGenerationResult> _results =
        ContentController.Results;

    /// <summary>Export all scripts as Markdown</summary>
    [HttpGet("{sessionId}/markdown")]
    public IActionResult ExportMarkdown(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound();

        var sb = new StringBuilder();
        sb.AppendLine($"# 🎬 AI YouTube Content Factory — {result.Topic}");
        sb.AppendLine($"> Generated: {result.GeneratedAt:yyyy-MM-dd HH:mm} UTC");
        sb.AppendLine();

        // YouTube Scripts
        sb.AppendLine("---");
        sb.AppendLine($"## 📹 YouTube Video Scripts ({result.YouTubeScripts.Count})");
        foreach (var script in result.YouTubeScripts)
        {
            sb.AppendLine($"\n### Video {script.Index}: {script.Title}");
            sb.AppendLine($"**Duration:** ~{script.EstimatedDurationMinutes} minutes");
            sb.AppendLine($"\n**Description:**\n{script.Description}");
            sb.AppendLine($"\n**🎣 Hook (0:00–0:30):**\n{script.Hook}");
            sb.AppendLine($"\n**📖 Introduction:**\n{script.Introduction}");
            foreach (var section in script.MainContent)
            {
                sb.AppendLine($"\n**🔹 {section.Title}** (~{section.DurationSeconds}s)");
                sb.AppendLine(section.Content);
                if (!string.IsNullOrEmpty(section.VisualNote))
                    sb.AppendLine($"> 🎥 Visual: {section.VisualNote}");
            }
            sb.AppendLine($"\n**📢 Call to Action:**\n{script.CallToAction}");
            sb.AppendLine($"\n**👋 Outro:**\n{script.Outro}");
            if (script.SEO?.Tags?.Any() == true)
            {
                sb.AppendLine($"\n**🔍 SEO Tags:** {string.Join(", ", script.SEO.Tags)}");
                sb.AppendLine($"**🎯 Primary Keyword:** {script.SEO.PrimaryKeyword}");
            }
        }

        // Shorts
        sb.AppendLine("\n---");
        sb.AppendLine($"## ⚡ YouTube Shorts ({result.ShortsScripts.Count})");
        foreach (var s in result.ShortsScripts)
        {
            sb.AppendLine($"\n### Short {s.Index}: {s.Title}");
            sb.AppendLine($"**Hook:** {s.Hook}");
            sb.AppendLine($"**Main Point:** {s.MainPoint}");
            sb.AppendLine($"**CTA:** {s.CallToAction}");
            sb.AppendLine($"**Visual:** {s.VisualConcept}");
            sb.AppendLine($"**Hashtags:** {string.Join(" ", s.Hashtags)}");
        }

        // LinkedIn
        sb.AppendLine("\n---");
        sb.AppendLine($"## 💼 LinkedIn Posts ({result.LinkedInPosts.Count})");
        foreach (var p in result.LinkedInPosts)
        {
            sb.AppendLine($"\n### Post {p.Index}: {p.Title} [{p.PostType}]");
            sb.AppendLine(p.Content);
            sb.AppendLine($"\n{string.Join(" ", p.Hashtags)}");
        }

        // Twitter Threads
        sb.AppendLine("\n---");
        sb.AppendLine($"## 🐦 Twitter/X Threads ({result.TwitterThreads.Count})");
        foreach (var t in result.TwitterThreads)
        {
            sb.AppendLine($"\n### Thread {t.Index}: {t.Topic}");
            for (int i = 0; i < t.Tweets.Count; i++)
                sb.AppendLine($"\n**{i + 1}/** {t.Tweets[i]}");
        }

        // Thumbnail Prompts
        sb.AppendLine("\n---");
        sb.AppendLine($"## 🖼️ Thumbnail Prompts ({result.ThumbnailPrompts.Count})");
        foreach (var tp in result.ThumbnailPrompts)
        {
            sb.AppendLine($"\n### {tp.VideoTitle}");
            sb.AppendLine($"**Main Text:** {tp.MainText}");
            sb.AppendLine($"**Color Scheme:** {tp.ColorScheme}");
            sb.AppendLine($"**DALL-E Prompt:**\n> {tp.DallePrompt}");
            sb.AppendLine($"**Midjourney Prompt:**\n> {tp.MidjourneyPrompt}");
        }

        // Content Plan
        sb.AppendLine("\n---");
        sb.AppendLine("## 📅 Content Strategy & Publishing Plan");
        sb.AppendLine($"\n**Strategy:** {result.ContentPlan.OverallStrategy}");
        sb.AppendLine($"\n**Content Pillars:** {string.Join(", ", result.ContentPlan.ContentPillars)}");
        sb.AppendLine($"\n**Growth Strategy:**\n{result.ContentPlan.ChannelGrowthStrategy}");
        sb.AppendLine("\n**Publishing Schedule:**");
        sb.AppendLine("| Week | Platform | Content Type | Title | Day |");
        sb.AppendLine("|------|----------|-------------|-------|-----|");
        foreach (var item in result.ContentPlan.PublishingSchedule)
            sb.AppendLine($"| {item.WeekNumber} | {item.Platform} | {item.ContentType} | {item.Title} | {item.PublishDay} |");

        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        return File(bytes, "text/markdown", $"content-factory-{result.Topic}-{DateTime.UtcNow:yyyyMMdd}.md");
    }

    /// <summary>Export as JSON</summary>
    [HttpGet("{sessionId}/json")]
    public IActionResult ExportJson(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound();

        var bytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result,
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

        return File(bytes, "application/json",
            $"content-factory-{result.Topic}-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>Export summary stats</summary>
    [HttpGet("{sessionId}/summary")]
    public IActionResult GetSummary(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound();

        return Ok(new
        {
            topic = result.Topic,
            generatedAt = result.GeneratedAt,
            counts = new
            {
                youtubeVideos = result.YouTubeScripts.Count,
                shorts = result.ShortsScripts.Count,
                linkedInPosts = result.LinkedInPosts.Count,
                twitterThreads = result.TwitterThreads.Count,
                thumbnailPrompts = result.ThumbnailPrompts.Count,
                totalTweets = result.TwitterThreads.Sum(t => t.Tweets.Count),
                estimatedVideoHours = result.YouTubeScripts.Sum(s => s.EstimatedDurationMinutes) / 60.0
            },
            contentPlan = result.ContentPlan
        });
    }
}
