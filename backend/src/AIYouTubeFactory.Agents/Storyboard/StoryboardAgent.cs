using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Storyboards;

public class StoryboardAgent : IStoryboardAgent
{
    private readonly Kernel _kernel;

    public StoryboardAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<Storyboard> GenerateStoryboardAsync(
        YouTubeVideoScript script,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Storyboard Agent",
            Message         = $"📋 Breaking '{script.Title}' into scenes...",
            ProgressPercent = 5,
            Status          = VideoGenerationStatus.GeneratingStoryboard
        });

        // Build full script text
        var fullScript = BuildFullScript(script);

        var prompt = $@"You are a professional video storyboard director.
Break the following YouTube video script into individual scenes for video production.

Video Title: {script.Title}
Estimated Duration: {script.EstimatedDurationMinutes} minutes

Full Script:
{fullScript}

Create a detailed storyboard where each scene:
- Has 15-45 seconds of narration
- Has a specific visual that can be generated as an image
- Has clear camera/style direction

Return ONLY a JSON object:
{{
  ""videoTitle"": ""{script.Title}"",
  ""totalEstimatedSeconds"": {script.EstimatedDurationMinutes * 60},
  ""scenes"": [
    {{
      ""index"": 1,
      ""title"": ""Scene title"",
      ""narration"": ""Exact words to be spoken aloud for this scene"",
      ""imagePrompt"": ""Detailed DALL-E image generation prompt: subject, style, lighting, composition. Cinematic, 16:9, no text"",
      ""visualDirection"": ""Camera angle, movement, mood description"",
      ""durationSeconds"": 30,
      ""transitionType"": ""fade""
    }}
  ]
}}

Rules:
- narration must be natural spoken English, no markdown
- imagePrompt must be detailed, visual-only, no text in image
- transitionType: ""fade"", ""cut"", or ""dissolve""
- Create enough scenes to cover the full script
- Each scene narration should flow naturally into the next
";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 6000, Temperature = 0.6 }));

        try
        {
            var json       = ExtractJson(result.ToString());
            var storyboard = JsonSerializer.Deserialize<Storyboard>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? BuildFallbackStoryboard(script);

            // Re-index scenes
            for (int i = 0; i < storyboard.Scenes.Count; i++)
                storyboard.Scenes[i].Index = i + 1;

            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Storyboard Agent",
                Message         = $"✅ Storyboard ready: {storyboard.Scenes.Count} scenes, ~{storyboard.TotalEstimatedSeconds}s",
                ProgressPercent = 15,
                Status          = VideoGenerationStatus.GeneratingStoryboard
            });

            return storyboard;
        }
        catch
        {
            return BuildFallbackStoryboard(script);
        }
    }

    private static string BuildFullScript(YouTubeVideoScript script)
    {
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(script.Hook))         parts.Add($"HOOK:\n{script.Hook}");
        if (!string.IsNullOrEmpty(script.Introduction)) parts.Add($"INTRO:\n{script.Introduction}");
        foreach (var s in script.MainContent)           parts.Add($"{s.Title.ToUpper()}:\n{s.Content}");
        if (!string.IsNullOrEmpty(script.CallToAction)) parts.Add($"CTA:\n{script.CallToAction}");
        if (!string.IsNullOrEmpty(script.Outro))        parts.Add($"OUTRO:\n{script.Outro}");
        return string.Join("\n\n", parts);
    }

    private static Storyboard BuildFallbackStoryboard(YouTubeVideoScript script)
    {
        var scenes = new List<StoryboardScene>();
        int idx    = 1;

        void AddScene(string title, string narration, string imgHint, int secs)
        {
            scenes.Add(new StoryboardScene
            {
                Index           = idx++,
                Title           = title,
                Narration       = narration,
                ImagePrompt     = $"Cinematic professional video frame: {imgHint}, 16:9, dramatic lighting, high quality",
                VisualDirection = "Medium shot, professional lighting",
                DurationSeconds = secs,
                TransitionType  = "fade"
            });
        }

        AddScene("Hook",         script.Hook,         $"eye-catching scene about {script.Title}", 20);
        AddScene("Introduction", script.Introduction, $"presenter introducing {script.Title}",     30);
        foreach (var s in script.MainContent)
            AddScene(s.Title, s.Content[..Math.Min(s.Content.Length, 300)],
                     $"visual representation of {s.Title}", s.DurationSeconds);
        AddScene("Call to Action", script.CallToAction, "subscribe button, thumbs up, social media", 15);
        AddScene("Outro",          script.Outro,         $"outro screen for {script.Title}", 10);

        return new Storyboard
        {
            VideoTitle             = script.Title,
            Scenes                 = scenes,
            TotalEstimatedSeconds  = scenes.Sum(s => s.DurationSeconds)
        };
    }

    private static string ExtractJson(string text)
    {
        int start = text.IndexOf('{');
        if (start == -1) return "{}";
        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == '{') depth++;
            else if (text[i] == '}') { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return "{}";
    }
}
