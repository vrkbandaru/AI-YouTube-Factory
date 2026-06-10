using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Thumbnail;

public class ThumbnailAgent : IThumbnailAgent
{
    private readonly Kernel _kernel;

    public ThumbnailAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<List<ThumbnailPrompt>> GenerateThumbnailPromptsAsync(
        List<YouTubeVideoScript> scripts,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Thumbnail Agent",
            Status = "running",
            Message = "Designing thumbnail concepts...",
            ProgressPercent = 10
        });

        var scriptSummaries = scripts.Select((s, i) => $"{i + 1}. {s.Title}").ToList();

        var prompt = $@"You are a YouTube thumbnail design expert who has studied MrBeast, Marques Brownlee, and Fireship thumbnails.
Create compelling thumbnail prompts for these {scripts.Count} YouTube videos:

{string.Join("\n", scriptSummaries)}

For each video, design a thumbnail that:
- Uses bold, high-contrast colors
- Has 3-5 words of text MAX
- Creates curiosity or urgency
- Works at small sizes (mobile)

Return ONLY a JSON array:
[{{ 
  ""videoTitle"": ""matching video title"",
  ""mainText"": ""3-5 word overlay text"",
  ""backgroundDescription"": ""detailed scene description"",
  ""colorScheme"": ""primary and accent colors"",
  ""faceExpression"": ""shocked/excited/curious/serious/none"",
  ""dallePrompt"": ""detailed DALL-E 3 generation prompt"",
  ""midjourneyPrompt"": ""Midjourney-style prompt with parameters""
}}]
";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 4000, Temperature = 0.8 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var prompts = JsonSerializer.Deserialize<List<ThumbnailPrompt>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Thumbnail Agent",
                Status = "completed",
                Message = $"✅ Generated {prompts.Count} thumbnail prompts",
                ProgressPercent = 100,
                Data = prompts
            });

            return prompts;
        }
        catch
        {
            return scripts.Select(s => new ThumbnailPrompt
            {
                VideoTitle = s.Title,
                MainText = s.Title.Split(' ').Take(3).Aggregate((a, b) => $"{a} {b}").ToUpper(),
                DallePrompt = $"YouTube thumbnail: {s.Title}, dramatic lighting, bold text",
                MidjourneyPrompt = $"YouTube thumbnail {s.Title} --ar 16:9 --v 6"
            }).ToList();
        }
    }

    private static string ExtractJson(string text)
    {
        int start = text.IndexOf('[');
        if (start == -1) start = text.IndexOf('{');
        if (start == -1) return "[]";
        int end = text.LastIndexOf(text[start] == '[' ? ']' : '}');
        return end > start ? text[start..(end + 1)] : "[]";
    }
}
