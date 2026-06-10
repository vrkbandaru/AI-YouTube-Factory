using AIYouTubeFactory.Agents.Script;
using AIYouTubeFactory.Agents.Thumbnail;
using AIYouTubeFactory.Agents.SEO;
using AIYouTubeFactory.Agents.Planner;
using AIYouTubeFactory.Agents.Social;
using AIYouTubeFactory.Agents.Storyboards;
using AIYouTubeFactory.Agents.Image;
using AIYouTubeFactory.Agents.Voice;
using AIYouTubeFactory.Agents.Subtitle;
using AIYouTubeFactory.Agents.VideoComposer;
using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Infrastructure.Parsers;
using AIYouTubeFactory.Infrastructure.Services;
using AIYouTubeFactory.API.Hubs;
using AIYouTubeFactory.API.Middleware;
using Azure;
using Azure.AI.OpenAI;
using Microsoft.SemanticKernel;
using FFMpegCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Logging ───────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// ── Configuration ─────────────────────────────────────────────────────────────
var azureOpenAIEndpoint   = builder.Configuration["AzureOpenAI:Endpoint"]
    ?? throw new InvalidOperationException("AzureOpenAI:Endpoint is required");
var azureOpenAIKey        = builder.Configuration["AzureOpenAI:ApiKey"]
    ?? throw new InvalidOperationException("AzureOpenAI:ApiKey is required");
var azureOpenAIDeployment = builder.Configuration["AzureOpenAI:DeploymentName"] ?? "gpt-4o-mini";

// Fix 2: Use configured image deployment (supports MAI-Image-2.5, dall-e-3, etc.)
var imageDeployment = builder.Configuration["AzureOpenAI:ImageDeployment"]
                  ?? builder.Configuration["AzureOpenAI:DalleDeployment"]
                  ?? "dall-e-3";

var speechKey       = builder.Configuration["AzureSpeech:Key"] ?? "";
var speechRegion    = builder.Configuration["AzureSpeech:Region"] ?? "eastus";
var videoOutputRoot = builder.Configuration["VideoOutput:RootPath"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "GeneratedVideos");

// Fix 1: Configure FFmpeg path from appsettings with auto-discovery fallback
var ffmpegPath = builder.Configuration["FFmpeg:BinaryFolder"] ?? "";
if (!string.IsNullOrWhiteSpace(ffmpegPath) && Directory.Exists(ffmpegPath))
{
    GlobalFFOptions.Configure(o => o.BinaryFolder = ffmpegPath);
    Log.Information("FFmpeg configured from appsettings: {Path}", ffmpegPath);
}
else
{
    // Auto-discover WinGet install path
    var wingetBase = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Microsoft", "WinGet", "Packages");

    if (Directory.Exists(wingetBase))
    {
        var discovered = Directory
            .GetDirectories(wingetBase, "Gyan.FFmpeg*", SearchOption.TopDirectoryOnly)
            .SelectMany(d => Directory.GetDirectories(d, "*full_build*", SearchOption.AllDirectories))
            .Select(d => Path.Combine(d, "bin"))
            .FirstOrDefault(Directory.Exists);

        if (discovered != null)
        {
            GlobalFFOptions.Configure(o => o.BinaryFolder = discovered);
            Log.Information("FFmpeg auto-discovered at: {Path}", discovered);
        }
        else
        {
            Log.Warning("FFmpeg not found via WinGet. Falling back to system PATH.");
        }
    }
}

// ── Azure OpenAI client ───────────────────────────────────────────────────────
var openAiCredential = new AzureKeyCredential(azureOpenAIKey);
var openAiClient     = new AzureOpenAIClient(new Uri(azureOpenAIEndpoint), openAiCredential);
builder.Services.AddSingleton(openAiClient);

// ── Semantic Kernel ───────────────────────────────────────────────────────────
builder.Services.AddSingleton(_ =>
{
    var kb = Kernel.CreateBuilder();
    kb.AddAzureOpenAIChatCompletion(azureOpenAIDeployment, openAiClient);
    return kb.Build();
});

// ── Content Agents ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IScriptAgent,       ScriptAgent>();
builder.Services.AddScoped<IThumbnailAgent,    ThumbnailAgent>();
builder.Services.AddScoped<ISEOAgent,          SEOAgent>();
builder.Services.AddScoped<IVideoPlannerAgent, VideoPlannerAgent>();
builder.Services.AddScoped<ISocialMediaAgent,  SocialMediaAgent>();

// ── Video Pipeline Agents ─────────────────────────────────────────────────────
builder.Services.AddScoped<IStoryboardAgent, AIYouTubeFactory.Agents.Storyboards.StoryboardAgent>();

// Fix 2: ImageAgent uses generic REST — works with MAI-Image-2.5, dall-e-3, etc.
builder.Services.AddScoped<IImageAgent>(_ =>
    new ImageAgent(azureOpenAIEndpoint, azureOpenAIKey, imageDeployment));

builder.Services.AddScoped<IVoiceAgent>(_ =>
    new VoiceAgent(speechKey, speechRegion));

builder.Services.AddScoped<ISubtitleAgent,      SubtitleAgent>();

// Fix 1: Pass ffmpegPath explicitly to VideoComposerAgent
builder.Services.AddScoped<IVideoComposerAgent>(_ =>
    new VideoComposerAgent(ffmpegPath));

builder.Services.AddScoped<IVideoGenerationService>(sp =>
    new VideoGenerationService(
        sp.GetRequiredService<IStoryboardAgent>(),
        sp.GetRequiredService<IImageAgent>(),
        sp.GetRequiredService<IVoiceAgent>(),
        sp.GetRequiredService<ISubtitleAgent>(),
        sp.GetRequiredService<IVideoComposerAgent>(),
        sp.GetRequiredService<ILogger<VideoGenerationService>>(),
        videoOutputRoot));

// ── Infrastructure ────────────────────────────────────────────────────────────
builder.Services.AddScoped<IDocumentParserService, DocumentParserService>();
builder.Services.AddScoped<IContentOrchestrator,   ContentOrchestrator>();

// ── Azure AI Search (optional) ────────────────────────────────────────────────
var searchEndpoint = builder.Configuration["AzureSearch:Endpoint"];
var searchKey      = builder.Configuration["AzureSearch:ApiKey"];
if (!string.IsNullOrEmpty(searchEndpoint) && !string.IsNullOrEmpty(searchKey))
    builder.Services.AddSingleton<ISearchIndexService>(
        new SearchIndexService(searchEndpoint, searchKey));

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR(o =>
{
    o.EnableDetailedErrors      = builder.Environment.IsDevelopment();
    o.MaximumReceiveMessageSize = 1_048_576;
    o.ClientTimeoutInterval     = TimeSpan.FromSeconds(60);
    o.KeepAliveInterval         = TimeSpan.FromSeconds(15);
});

// ── CORS ──────────────────────────────────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        if (builder.Environment.IsDevelopment())
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        else
            policy.WithOrigins(builder.Configuration["AllowedOrigin"] ?? "https://yourdomain.com")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
    c.SwaggerDoc("v1", new() { Title = "AI YouTube Content Factory API", Version = "v1" }));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapHub<ContentGenerationHub>("/hubs/content");
app.MapHealthChecks("/health");

app.Run();

// Expose implicit Program class for integration tests
public partial class Program { }
