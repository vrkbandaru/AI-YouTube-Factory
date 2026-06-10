using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using AIYouTubeFactory.API.Hubs;

namespace AIYouTubeFactory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContentController : ControllerBase
{
    private readonly IDocumentParserService _parser;
    private readonly IContentOrchestrator _orchestrator;
    private readonly IHubContext<ContentGenerationHub> _hub;
    private readonly ILogger<ContentController> _logger;

    // In-memory store (replace with Redis/DB in production)
    private static readonly Dictionary<Guid, UploadedDocument> _documents = new();
    internal static readonly Dictionary<Guid, ContentGenerationResult> Results = new();
    private static readonly Dictionary<Guid, ContentGenerationResult> _results = Results;

    public ContentController(
        IDocumentParserService parser,
        IContentOrchestrator orchestrator,
        IHubContext<ContentGenerationHub> hub,
        ILogger<ContentController> logger)
    {
        _parser = parser;
        _orchestrator = orchestrator;
        _hub = hub;
        _logger = logger;
    }

    /// <summary>Upload a document (PDF, PPTX, DOCX, MD)</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> UploadDocument(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        var allowedTypes = new[] { ".pdf", ".pptx", ".docx", ".md", ".markdown", ".txt", ".ppt", ".doc" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedTypes.Contains(ext))
            return BadRequest($"Unsupported file type. Allowed: {string.Join(", ", allowedTypes)}");

        try
        {
            await using var stream = file.OpenReadStream();
            var document = await _parser.ParseAsync(stream, file.FileName, file.ContentType);
            _documents[document.Id] = document;

            _logger.LogInformation("Document uploaded: {FileName}, {CharCount} chars extracted",
                file.FileName, document.ExtractedText.Length);

            return Ok(new
            {
                documentId = document.Id,
                fileName = document.FileName,
                topic = document.Topic,
                extractedChars = document.ExtractedText.Length,
                sectionCount = document.Sections.Count,
                preview = document.ExtractedText[..Math.Min(500, document.ExtractedText.Length)]
            });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing document");
            return StatusCode(500, "Error processing document.");
        }
    }

    /// <summary>Start content generation for an uploaded document</summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateContent([FromBody] ContentGenerationRequest request)
    {
        if (!_documents.TryGetValue(request.DocumentId, out var document))
            return NotFound("Document not found. Please upload first.");

        var sessionId = Guid.NewGuid().ToString();

        // Fire and forget - progress sent via SignalR
        _ = Task.Run(async () =>
        {
            try
            {
                var reporter = new AgentProgressReporter(_hub, sessionId);
                var result = await _orchestrator.OrchestrateAsync(document, request, reporter);
                _results[result.SessionId] = result;

                await _hub.Clients.Group(sessionId).SendAsync("GenerationComplete", new
                {
                    sessionId = result.SessionId,
                    summary = new
                    {
                        youtubeScripts = result.YouTubeScripts.Count,
                        shortsScripts = result.ShortsScripts.Count,
                        linkedInPosts = result.LinkedInPosts.Count,
                        twitterThreads = result.TwitterThreads.Count,
                        thumbnailPrompts = result.ThumbnailPrompts.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating content for session {SessionId}", sessionId);
                await _hub.Clients.Group(sessionId).SendAsync("GenerationError", ex.Message);
            }
        });

        return Accepted(new { sessionId, message = "Generation started. Connect to SignalR for progress." });
    }

    /// <summary>Get generation results</summary>
    [HttpGet("results/{sessionId}")]
    public IActionResult GetResults(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound("Results not found or generation still in progress.");

        return Ok(result);
    }

    /// <summary>Get a specific script</summary>
    [HttpGet("results/{sessionId}/script/{index}")]
    public IActionResult GetScript(Guid sessionId, int index)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound();

        var script = result.YouTubeScripts.FirstOrDefault(s => s.Index == index);
        return script == null ? NotFound() : Ok(script);
    }

    /// <summary>Export results as JSON</summary>
    [HttpGet("results/{sessionId}/export")]
    public IActionResult ExportResults(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
            return NotFound();

        return File(
            System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(result,
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true }),
            "application/json",
            $"content-factory-{result.Topic}-{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>Health check</summary>
    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy", timestamp = DateTime.UtcNow });

    /// <summary>Poll generation status (alternative to SignalR)</summary>
    [HttpGet("status/{sessionId}")]
    public IActionResult GetStatus(Guid sessionId)
    {
        if (!_results.TryGetValue(sessionId, out var result))
        {
            // Check if session exists but not done
            return Ok(new { status = "processing", message = "Generation in progress..." });
        }
        return Ok(new
        {
            status = "completed",
            topic = result.Topic,
            counts = new
            {
                youtubeScripts = result.YouTubeScripts.Count,
                shortsScripts = result.ShortsScripts.Count,
                linkedInPosts = result.LinkedInPosts.Count,
                twitterThreads = result.TwitterThreads.Count
            }
        });
    }
}
