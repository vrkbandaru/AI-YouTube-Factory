using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Azure.AI.OpenAI;
using Azure;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace AIYouTubeFactory.Agents.Image;

public class ImageAgent : IImageAgent
{
    private readonly string _endpoint;
    private readonly string _apiKey;
    private readonly string _deployment;   // e.g. "MAI-Image-2.5" or "dall-e-3"
    private readonly HttpClient _http;

    // Fix 2: Accept any deployment name — works with dall-e-3, MAI-Image-2.5, etc.
    public ImageAgent(string endpoint, string apiKey, string deployment = "dall-e-3")
    {
        _endpoint   = endpoint.TrimEnd('/');
        _apiKey     = apiKey;
        _deployment = deployment;
        _http       = new HttpClient { Timeout = TimeSpan.FromMinutes(3) };
        _http.DefaultRequestHeaders.Add("api-key", _apiKey);
    }

    public async Task<List<StoryboardScene>> GenerateSceneImagesAsync(
        Storyboard storyboard,
        string outputDir,
        string style,
        IProgress<VideoProgressUpdate>? progress = null)
    {
        Directory.CreateDirectory(outputDir);
        var scenes = storyboard.Scenes;
        int total  = scenes.Count;

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Image Agent",
            Message         = $"🎨 Generating {total} scene images with {_deployment}...",
            ProgressPercent = 16,
            Status          = VideoGenerationStatus.GeneratingImages
        });

        for (int i = 0; i < total; i++)
        {
            var scene     = scenes[i];
            var imagePath = Path.Combine(outputDir, $"scene_{scene.Index:D3}.png");

            // Skip already-generated images (resume support)
            if (File.Exists(imagePath) && new FileInfo(imagePath).Length > 100)
            {
                scene.GeneratedImagePath = imagePath;
                continue;
            }

            int pct = 16 + (int)((double)i / total * 24);
            progress?.Report(new VideoProgressUpdate
            {
                Stage           = "Image Agent",
                Message         = $"🖼️ Scene {i + 1}/{total}: {scene.Title}",
                ProgressPercent = pct,
                Status          = VideoGenerationStatus.GeneratingImages
            });

            var fullPrompt = BuildPrompt(scene.ImagePrompt, style);

            try
            {
                var imageBytes = await GenerateImageAsync(fullPrompt);
                if (imageBytes?.Length > 0)
                {
                    await File.WriteAllBytesAsync(imagePath, imageBytes);
                    scene.GeneratedImagePath = imagePath;

                    progress?.Report(new VideoProgressUpdate
                    {
                        Stage            = "Image Agent",
                        Message          = $"✅ Scene {i + 1}/{total} image saved",
                        ProgressPercent  = pct,
                        Status           = VideoGenerationStatus.GeneratingImages,
                        PreviewImagePath = imagePath
                    });
                }
                else
                {
                    scene.GeneratedImagePath = await CreatePlaceholderAsync(imagePath, i);
                }

                // Rate limit pause between images
                if (i < total - 1)
                    await Task.Delay(TimeSpan.FromSeconds(12));
            }
            catch (Exception ex)
            {
                progress?.Report(new VideoProgressUpdate
                {
                    Stage           = "Image Agent",
                    Message         = $"⚠️ Scene {i + 1} image failed ({ex.Message}), using placeholder",
                    ProgressPercent = pct,
                    Status          = VideoGenerationStatus.GeneratingImages
                });
                scene.GeneratedImagePath = await CreatePlaceholderAsync(imagePath, i);
            }
        }

        progress?.Report(new VideoProgressUpdate
        {
            Stage           = "Image Agent",
            Message         = $"✅ All {total} scene images ready",
            ProgressPercent = 40,
            Status          = VideoGenerationStatus.GeneratingImages
        });

        return scenes;
    }

    // ── Fix 2: Generic REST call that works with any Azure image model ─────────
    private async Task<byte[]?> GenerateImageAsync(string prompt)
    {
        // Try Azure OpenAI image generations endpoint
        // Works with: dall-e-3, dall-e-2, MAI-Image-2.5 (if it has same API)
        var url = $"{_endpoint}/openai/deployments/{_deployment}/images/generations?api-version=2024-02-01";

        var body = JsonSerializer.Serialize(new
        {
            prompt,
            n               = 1,
            size            = "1792x1024",   // closest 16:9 for dall-e-3
            response_format = "b64_json"     // get bytes directly
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            var err = await response.Content.ReadAsStringAsync();

            // If 1792x1024 not supported (some models use 1024x1024 only), retry
            if (err.Contains("size") || err.Contains("1792"))
                return await GenerateImageWithSizeAsync(prompt, "1024x1024");

            throw new Exception($"Image API {response.StatusCode}: {err[..Math.Min(err.Length, 300)]}");
        }

        var json   = await response.Content.ReadAsStringAsync();
        var doc    = JsonDocument.Parse(json);
        var b64    = doc.RootElement
                       .GetProperty("data")[0]
                       .GetProperty("b64_json")
                       .GetString();

        return b64 != null ? Convert.FromBase64String(b64) : null;
    }

    private async Task<byte[]?> GenerateImageWithSizeAsync(string prompt, string size)
    {
        var url  = $"{_endpoint}/openai/deployments/{_deployment}/images/generations?api-version=2024-02-01";
        var body = JsonSerializer.Serialize(new
        {
            prompt,
            n               = 1,
            size,
            response_format = "b64_json"
        });

        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };

        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var doc  = JsonDocument.Parse(json);
        var b64  = doc.RootElement
                      .GetProperty("data")[0]
                      .GetProperty("b64_json")
                      .GetString();

        return b64 != null ? Convert.FromBase64String(b64) : null;
    }

    private static string BuildPrompt(string imagePrompt, string style) =>
        $"{imagePrompt}. Visual style: {style}. " +
        "16:9 composition, cinematic lighting, no text, no watermarks, no logos.";

    // Solid-colour PNG placeholder when image generation fails
    private static async Task<string> CreatePlaceholderAsync(string path, int index)
    {
        // Minimal valid 1×1 PNG — FFmpeg will scale to required resolution
        var bytes = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJ" +
            "AAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }
}
