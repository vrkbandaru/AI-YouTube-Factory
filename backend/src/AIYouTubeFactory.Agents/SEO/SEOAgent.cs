using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.SEO;

public class SEOAgent : ISEOAgent
{
    private readonly Kernel _kernel;

    public SEOAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<List<SEOData>> EnrichWithSEOAsync(
        List<YouTubeVideoScript> scripts, string topic,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "SEO Agent",
            Status = "running",
            Message = "Researching keywords and optimizing for YouTube search...",
            ProgressPercent = 15
        });

        var titles = string.Join("\n", scripts.Select((s, i) => $"{i + 1}. {s.Title}"));

        var prompt = $@"You are a YouTube SEO expert with deep knowledge of the YouTube algorithm, 
            keyword research, and video optimization.

            Topic: {topic}
            Videos:
            {titles}

            For each video, provide comprehensive SEO optimization.
            Return ONLY a JSON array (one entry per video in same order):
            [{{ 
              ""primaryKeyword"": ""main keyword phrase (high volume, medium competition)"",
              ""secondaryKeywords"": [""kw1"", ""kw2"", ""kw3"", ""kw4"", ""kw5""],
              ""tags"": [""tag1"", ""tag2"", ... up to 15 tags],
              ""optimizedTitle"": ""SEO-optimized title (under 60 chars, keyword first)"",
              ""optimizedDescription"": ""500-char description with keyword in first 125 chars, timestamps placeholder, links section"",
              ""chapters"": [""0:00 - Intro"", ""1:30 - Section 1"", ...]
            }}]

            Focus on real YouTube search terms people actually use.
            ";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 5000, Temperature = 0.4 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var seoData = JsonSerializer.Deserialize<List<SEOData>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            // Enrich scripts with SEO data
            for (int i = 0; i < Math.Min(seoData.Count, scripts.Count); i++)
            {
                scripts[i].SEO = seoData[i];
                if (!string.IsNullOrEmpty(seoData[i].OptimizedTitle))
                    scripts[i].Title = seoData[i].OptimizedTitle;
            }

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "SEO Agent",
                Status = "completed",
                Message = "✅ SEO optimization complete",
                ProgressPercent = 100,
                Data = seoData
            });

            return seoData;
        }
        catch
        {
            return scripts.Select(s => new SEOData
            {
                PrimaryKeyword = topic,
                Tags = new List<string> { topic, "tutorial", "guide" }
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
