using System.Net;
using System.Net.Http.Json;
using System.Text;
using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace AIYouTubeFactory.Tests.API;

public class ContentControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly Mock<IDocumentParserService>   _parserMock   = new();
    private readonly Mock<IContentOrchestrator>     _orchMock     = new();

    public ContentControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Replace real services with mocks
                var parserDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IDocumentParserService));
                if (parserDesc != null) services.Remove(parserDesc);
                services.AddScoped<IDocumentParserService>(_ => _parserMock.Object);

                var orchDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IContentOrchestrator));
                if (orchDesc != null) services.Remove(orchDesc);
                services.AddScoped<IContentOrchestrator>(_ => _orchMock.Object);
            });
        });
    }

    [Fact]
    public async Task GET_Health_Returns200()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync("/api/content/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task POST_Upload_WithValidFile_Returns200()
    {
        _parserMock.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new UploadedDocument
            {
                Id            = Guid.NewGuid(),
                FileName      = "test.md",
                FileType      = "md",
                ExtractedText = "# Microservices\nThis is test content about microservices architecture.",
                Topic         = "Microservices",
                Sections      = new List<string> { "Section 1", "Section 2" }
            });

        var client  = _factory.CreateClient();
        var content = new MultipartFormDataContent();
        var file    = new ByteArrayContent(Encoding.UTF8.GetBytes("# Test content\nSome markdown text"));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/markdown");
        content.Add(file, "file", "test.md");

        var response = await client.PostAsync("/api/content/upload", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<dynamic>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task POST_Upload_WithUnsupportedType_Returns400()
    {
        _parserMock.Setup(p => p.ParseAsync(
            It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new NotSupportedException("File type .exe is not supported."));

        var client  = _factory.CreateClient();
        var content = new MultipartFormDataContent();
        var file    = new ByteArrayContent(new byte[] { 0x4D, 0x5A });
        content.Add(file, "file", "virus.exe");

        var response = await client.PostAsync("/api/content/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task POST_Upload_WithNoFile_Returns400()
    {
        var client   = _factory.CreateClient();
        var content  = new MultipartFormDataContent();
        var response = await client.PostAsync("/api/content/upload", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GET_Results_WithInvalidSession_Returns404()
    {
        var client   = _factory.CreateClient();
        var response = await client.GetAsync($"/api/content/results/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task POST_Generate_WithInvalidDocumentId_Returns404()
    {
        var client  = _factory.CreateClient();
        var payload = new
        {
            documentId             = Guid.NewGuid(),
            topic                  = "Test",
            youTubeVideoCount      = 2,
            shortsCount            = 3,
            linkedInPostCount      = 1,
            twitterThreadCount     = 1,
            generateThumbnailPrompts = true,
            generateSEO            = true,
            targetAudience         = "developers",
            contentStyle           = "educational"
        };

        var response = await client.PostAsJsonAsync("/api/content/generate", payload);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
