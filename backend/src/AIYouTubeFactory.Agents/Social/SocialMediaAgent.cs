using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Social;

public class SocialMediaAgent : ISocialMediaAgent
{
    private readonly Kernel _kernel;

    public SocialMediaAgent(Kernel kernel)
    {
        _kernel = kernel;
    }

    public async Task<List<LinkedInPost>> GenerateLinkedInPostsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Social Media Agent",
            Status = "running",
            Message = $"Crafting {count} LinkedIn posts...",
            ProgressPercent = 20
        });

        var prompt = $@"You are a LinkedIn thought leader and content strategist.
Based on the content about ""{topic}"", write {count} high-performing LinkedIn posts.

Content Summary:
{TruncateContent(content, 2000)}

LinkedIn posts that perform best:
- Start with a bold, controversial, or story-driven first line
- Use line breaks (short paragraphs, 1-2 lines each)
- Include a lesson, framework, or insight
- End with a question or CTA
- Mix post types: stories, lists, frameworks, opinions, how-tos

Return ONLY a JSON array:
[{{ 
  ""title"": ""internal title"",
  ""content"": ""full post with line breaks using \n"",
  ""hashtags"": [""#Tag1"", ""#Tag2"", ""#Tag3""],
  ""callToAction"": ""question or CTA"",
  ""postType"": ""story|list|framework|opinion|howto""
}}]
";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 6000, Temperature = 0.75 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var posts = JsonSerializer.Deserialize<List<LinkedInPost>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            for (int i = 0; i < posts.Count; i++) posts[i].Index = i + 1;

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Social Media Agent",
                Status = "completed",
                Message = $"✅ {posts.Count} LinkedIn posts ready",
                ProgressPercent = 60,
                Data = posts
            });

            return posts;
        }
        catch
        {
            return new List<LinkedInPost>();
        }
    }

    public async Task<List<TwitterThread>> GenerateTwitterThreadsAsync(
        string content, string topic, int count,
        IProgress<AgentProgressUpdate>? progress = null)
    {
        progress?.Report(new AgentProgressUpdate
        {
            AgentName = "Social Media Agent",
            Status = "running",
            Message = $"Writing {count} Twitter/X threads...",
            ProgressPercent = 70
        });

        var prompt = $@"You are a viral Twitter/X thread writer.
Create {count} threads about ""{topic}"" from this content:

{TruncateContent(content, 1500)}

Thread rules:
- Tweet 1: Hook that makes people NEED to read on
- Tweets 2-8: Value-packed points, one per tweet
- Last tweet: Summary + CTA + follow request
- Each tweet max 280 chars
- Use numbers (1/, 2/ style)

Return ONLY a JSON array:
[{{ 
  ""topic"": ""thread topic"",
  ""tweets"": [""tweet 1 text"", ""tweet 2 text"", ...],
  ""hashtags"": [""#tag1"", ""#tag2""]
}}]
";

        var result = await _kernel.InvokePromptAsync(prompt, new KernelArguments(
            new AzureOpenAIPromptExecutionSettings { MaxTokens = 4000, Temperature = 0.8 }));

        try
        {
            var json = ExtractJson(result.ToString());
            var threads = JsonSerializer.Deserialize<List<TwitterThread>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

            for (int i = 0; i < threads.Count; i++) threads[i].Index = i + 1;

            progress?.Report(new AgentProgressUpdate
            {
                AgentName = "Social Media Agent",
                Status = "completed",
                Message = $"✅ {threads.Count} Twitter threads ready",
                ProgressPercent = 100,
                Data = threads
            });

            return threads;
        }
        catch
        {
            return new List<TwitterThread>();
        }
    }

    private static string TruncateContent(string content, int maxChars)
        => content.Length > maxChars ? content[..maxChars] + "..." : content;

    private static string ExtractJson(string text)
    {
        int start = text.IndexOf('[');
        if (start == -1) start = text.IndexOf('{');
        if (start == -1) return "[]";
        char open = text[start], close = open == '[' ? ']' : '}';
        int depth = 0;
        for (int i = start; i < text.Length; i++)
        {
            if (text[i] == open) depth++;
            else if (text[i] == close) { depth--; if (depth == 0) return text[start..(i + 1)]; }
        }
        return "[]";
    }
}
