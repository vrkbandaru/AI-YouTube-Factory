using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Planner;

public class VideoPlannerAgent : IVideoPlannerAgent
{
    private readonly Kernel _kernel;

    public VideoPlannerAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<VideoContentPlan> GenerateContentPlanAsync(
        string topic, List<YouTubeVideoScript> scripts, List<ShortsScript> shorts,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Video Planner Agent",
            Status = "running",
            Message = "Building 90-day content calendar...",
            ProgressPercent = 20
        });

        var videoTitles = string.Join("\n", scripts.Select((s, i) => $"Video {i + 1}: {s.Title}"));
        var shortTitles = string.Join("\n", shorts.Take(10).Select((s, i) => $"Short {i + 1}: {s.Title}"));

        var prompt = $@"You are a YouTube channel growth strategist. Create a complete content strategy for:

Topic: {topic}

Long Videos Available:
{videoTitles}

Shorts Available:
{shortTitles}

Return ONLY a JSON object:
{{
  ""overallStrategy"": ""2-3 sentence channel positioning and growth strategy"",
  ""contentPillars"": [""pillar1"", ""pillar2"", ""pillar3"", ""pillar4""],
  ""publishingSchedule"": [
    {{
      ""weekNumber"": 1,
      ""contentType"": ""Long Video"",
      ""title"": ""matching video title"",
      ""platform"": ""YouTube"",
      ""publishDay"": ""Tuesday""
    }},
    ... for 12 weeks mixing long videos, shorts, and LinkedIn
  ],
  ""seriesIdeas"": [""series idea 1"", ""series idea 2"", ""series idea 3""],
  ""channelGrowthStrategy"": ""detailed 300-word growth playbook including community posts, end screens, playlists, collaboration ideas""
}}";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 3000, Temperature = 0.6 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var plan = JsonSerializer.Deserialize<VideoContentPlan>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Video Planner Agent",
                Status = "completed",
                Message = "✅ Content plan ready",
                ProgressPercent = 100,
                Data = plan
            });

            return plan;
        }
        catch
        {
            return new VideoContentPlan
            {
                OverallStrategy = $"Build authority in {topic} through consistent long-form and short-form content.",
                ContentPillars = new() { "Tutorials", "Deep Dives", "Quick Tips", "Case Studies" },
                ChannelGrowthStrategy = "Post 2x per week consistently. Engage with comments. Create playlists."
            };
        }
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
