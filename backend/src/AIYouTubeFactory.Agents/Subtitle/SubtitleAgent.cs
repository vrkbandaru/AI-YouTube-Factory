using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using System.Text;

namespace AIYouTubeFactory.Agents.Subtitle;

public class SubtitleAgent : ISubtitleAgent
{
    // Max characters per subtitle line
    private const int MaxCharsPerLine  = 42;
    // Max words per subtitle entry
    private const int MaxWordsPerEntry = 10;

    public async Task<SubtitleFile> GenerateSubtitlesAsync(
        Storyboard storyboard,
        List<AudioSegment> audioSegments,
        string outputDir,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        Directory.CreateDirectory(outputDir);

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Subtitle Agent",
            Message         = "📝 Generating SRT subtitles...",
            ProgressPercent = 61,
            Status          = VideoGenerationStatus.GeneratingSubtitles
        });

        var entries     = BuildSubtitleEntries(storyboard, audioSegments);
        var srtContent  = BuildSRT(entries);
        var vttContent  = BuildVTT(entries);

        var srtPath = Path.Combine(outputDir, "subtitles.srt");
        var vttPath = Path.Combine(outputDir, "subtitles.vtt");

        await File.WriteAllTextAsync(srtPath, srtContent, Encoding.UTF8);
        await File.WriteAllTextAsync(vttPath, vttContent, Encoding.UTF8);

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Subtitle Agent",
            Message         = $"✅ Subtitles ready — {entries.Count} entries",
            ProgressPercent = 65,
            Status          = VideoGenerationStatus.GeneratingSubtitles
        });

        return new SubtitleFile
        {
            Format   = "srt",
            FilePath = srtPath,
            Content  = srtContent,
            Entries  = entries
        };
    }

    private static List<SubtitleEntry> BuildSubtitleEntries(
        Storyboard storyboard,
        List<AudioSegment> audioSegments)
    {
        var entries      = new List<SubtitleEntry>();
        int entryIndex   = 1;
        double timeOffset = 0;

        foreach (var scene in storyboard.Scenes)
        {
            var segment = audioSegments.FirstOrDefault(a => a.SceneIndex == scene.Index);
            var text    = scene.Narration;
            if (string.IsNullOrWhiteSpace(text)) { timeOffset += scene.DurationSeconds; continue; }

            var sceneDuration = segment?.DurationSeconds > 0
                ? segment.DurationSeconds
                : scene.DurationSeconds;

            // Split narration into word chunks
            var words      = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var chunks     = SplitIntoChunks(words, MaxWordsPerEntry);
            var timePerChunk = sceneDuration / Math.Max(chunks.Count, 1);

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunkText  = string.Join(" ", chunks[i]);
                var startTime  = TimeSpan.FromSeconds(timeOffset + i * timePerChunk);
                var endTime    = TimeSpan.FromSeconds(timeOffset + (i + 1) * timePerChunk - 0.1);

                // Wrap long lines
                var displayText = WrapText(chunkText, MaxCharsPerLine);

                entries.Add(new SubtitleEntry
                {
                    Index     = entryIndex++,
                    StartTime = startTime,
                    EndTime   = endTime,
                    Text      = displayText
                });
            }

            timeOffset += sceneDuration;
        }

        return entries;
    }

    private static List<List<string>> SplitIntoChunks(string[] words, int chunkSize)
    {
        var chunks = new List<List<string>>();
        for (int i = 0; i < words.Length; i += chunkSize)
            chunks.Add(words.Skip(i).Take(chunkSize).ToList());
        return chunks;
    }

    private static string WrapText(string text, int maxChars)
    {
        if (text.Length <= maxChars) return text;
        var mid   = text.Length / 2;
        var space = text.LastIndexOf(' ', mid);
        if (space < 0) return text;
        return text[..space] + "\n" + text[(space + 1)..];
    }

    private static string BuildSRT(List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine(e.Index.ToString());
            sb.AppendLine($"{FormatTimeSRT(e.StartTime)} --> {FormatTimeSRT(e.EndTime)}");
            sb.AppendLine(e.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string BuildVTT(List<SubtitleEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WEBVTT");
        sb.AppendLine();
        foreach (var e in entries)
        {
            sb.AppendLine($"{e.Index}");
            sb.AppendLine($"{FormatTimeVTT(e.StartTime)} --> {FormatTimeVTT(e.EndTime)}");
            sb.AppendLine(e.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string FormatTimeSRT(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2},{t.Milliseconds:D3}";

    private static string FormatTimeVTT(TimeSpan t) =>
        $"{(int)t.TotalHours:D2}:{t.Minutes:D2}:{t.Seconds:D2}.{t.Milliseconds:D3}";
}
