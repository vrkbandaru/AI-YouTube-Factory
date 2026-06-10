using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Script;

public class ScriptAgent : IScriptAgent
{
    private readonly Kernel _kernel;

    public ScriptAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<List<YouTubeVideoScript>> GenerateYouTubeScriptsAsync(
        string content, string topic, int count, string audience, string style,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Script Agent",
            Status = "running",
            Message = $"Analyzing content and planning {count} YouTube video scripts...",
            ProgressPercent = 5
        });

        // Step 1: Generate video ideas/outline
        var ideasPrompt = $@"You are an expert YouTube content strategist. Analyze the following content about ""{topic}"" 
and generate {count} unique, engaging YouTube video ideas.

Content:
{TruncateContent(content, 3000)}

Target Audience: {audience}
Content Style: {style}

Return ONLY a JSON array of objects with these fields:
- title: string (clickbait-worthy but accurate)
- angle: string (unique angle/hook for this video)
- keyPoints: string[] (3-5 main points to cover)
- estimatedDurationMinutes: number (8-20)

Make each video distinct with different angles, depths, and formats.
";

        var ideasResult = await _kernel.InvokePromptAsync(ideasPrompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 3000, Temperature = 0.8 }));

        var ideasJson = ExtractJson(ideasResult.ToString());
        var ideas = JsonSerializer.Deserialize<List<VideoIdea>>(ideasJson,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var scripts = new List<YouTubeVideoScript>();
        int totalIdeas = Math.Min(ideas.Count, count);

        for (int i = 0; i < totalIdeas; i++)
        {
            var idea = ideas[i];
            int pct = 10 + (int)((double)i / totalIdeas * 80);
            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Script Agent",
                Status = "running",
                Message = $"Writing script {i + 1}/{totalIdeas}: {idea.Title}",
                ProgressPercent = pct
            });

            var script = await GenerateSingleScriptAsync(idea, content, topic, audience, style);
            script.Index = i + 1;
            scripts.Add(script);
        }

        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Script Agent",
            Status = "completed",
            Message = $"✅ Generated {scripts.Count} YouTube scripts",
            ProgressPercent = 100,
            Data = scripts
        });

        return scripts;
    }

    private async Task<YouTubeVideoScript> GenerateSingleScriptAsync(
        VideoIdea idea, string content, string topic, string audience, string style)
    {
        var scriptPrompt = $@"You are a professional YouTube scriptwriter. Write a complete, engaging script for:

Title: {idea.Title}
Angle: {idea.Angle}
Key Points: {string.Join(", ", idea.KeyPoints ?? new())}
Duration: ~{idea.EstimatedDurationMinutes} minutes
Audience: {audience}
Style: {style}

Source Content:
{TruncateContent(content, 2000)}

Return ONLY a JSON object with:
{{
  ""title"": ""final title"",
  ""description"": ""YouTube description (150 words)"",
  ""hook"": ""attention-grabbing first 30 seconds script"",
  ""introduction"": ""intro section (60 seconds)"",
  ""mainContent"": [
    {{""title"": ""section name"", ""content"": ""full script"", ""durationSeconds"": 120, ""visualNote"": ""what to show on screen""}}
  ],
  ""callToAction"": ""CTA script text"",
  ""outro"": ""outro script"",
  ""estimatedDurationMinutes"": {idea.EstimatedDurationMinutes}
}}";

        var result = await _kernel.InvokePromptAsync(scriptPrompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 4000, Temperature = 0.7 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var script = JsonSerializer.Deserialize<YouTubeVideoScript>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
            return script;
        }
        catch
        {
            return new YouTubeVideoScript
            {
                Title = idea.Title,
                Description = $"A comprehensive guide to {idea.Angle}",
                Hook = $"Have you ever wondered about {idea.Title}?",
                Introduction = $"Welcome! Today we're diving deep into {idea.Title}.",
                MainContent = idea.KeyPoints?.Select((kp, idx) => new ScriptSection
                {
                    Title = kp,
                    Content = $"Let's explore {kp} in detail...",
                    DurationSeconds = 180
                }).ToList() ?? new(),
                CallToAction = "If you found this helpful, smash that like button and subscribe!",
                Outro = "See you in the next video!",
                EstimatedDurationMinutes = idea.EstimatedDurationMinutes
            };
        }
    }

    public async Task<List<ShortsScript>> GenerateShortsScriptsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Script Agent",
            Status = "running",
            Message = $"Creating {count} YouTube Shorts scripts...",
            ProgressPercent = 10
        });

        var prompt = $@"You are a viral YouTube Shorts expert. Based on this content about ""{topic}"":

{TruncateContent(content, 2500)}

Generate {count} unique YouTube Shorts scripts (under 60 seconds each).
Each short should be punchy, hook-first, and deliver ONE clear value.

Return ONLY a JSON array:
[{{ 
  ""title"": ""Short title"",
  ""hook"": ""first 3 seconds (must stop scroll)"",
  ""mainPoint"": ""core content (40 seconds)"",
  ""callToAction"": ""end CTA"",
  ""durationSeconds"": 58,
  ""hashtags"": [""#tag1"", ""#tag2"", ""#tag3""],
  ""visualConcept"": ""what to film/show""
}}]";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 5000, Temperature = 0.85 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var shorts = JsonSerializer.Deserialize<List<ShortsScript>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            for (int i = 0; i < shorts.Count; i++) shorts[i].Index = i + 1;

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Script Agent",
                Status = "completed",
                Message = $"✅ Generated {shorts.Count} Shorts scripts",
                ProgressPercent = 100,
                Data = shorts
            });

            return shorts;
        }
        catch
        {
            return new List<ShortsScript>();
        }
    }

    private static string TruncateContent(string content, int maxChars)
        => content.Length > maxChars ? content[..maxChars] + "..." : content;

    private static string ExtractJson(string text)
    {
        // Find first [ or { and last ] or }
        int start = -1;
        char openChar = '{', closeChar = '}';
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '[' || text[i] == '{')
            {
                openChar = text[i];
                closeChar = text[i] == '[' ? ']' : '}';
                start = i;
                break;
            }
        }
        if (start == -1) return "[]";

        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == openChar) depth++;
            else if (text[i] == closeChar) { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return "[]";
    }

    private class VideoIdea
    {
        public string Title { get; set; } = string.Empty;
        public string Angle { get; set; } = string.Empty;
        public List<string>? KeyPoints { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 12;
    }
}
