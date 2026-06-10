using System.Text;
using AIYouTubeFactory.Infrastructure.Parsers;
using FluentAssertions;
using Xunit;

namespace AIYouTubeFactory.Tests.Infrastructure;

public class DocumentParserServiceTests
{
    private readonly DocumentParserService _sut = new();

    [Fact]
    public async Task ParseAsync_PlainText_ExtractsContent()
    {
        var text    = "Microservices Architecture\nService Discovery\nAPI Gateway pattern";
        var stream  = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var result  = await _sut.ParseAsync(stream, "notes.txt", "text/plain");

        result.Should().NotBeNull();
        result.ExtractedText.Should().Contain("Microservices");
        result.FileType.Should().Be("txt");
        result.FileName.Should().Be("notes.txt");
    }

    [Fact]
    public async Task ParseAsync_Markdown_ExtractsContent()
    {
        var md     = "# Docker Basics\n\n## Containers\nContainers are lightweight...\n\n## Images\nImages are blueprints...";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(md));
        var result = await _sut.ParseAsync(stream, "docker.md", "text/markdown");

        result.ExtractedText.Should().Contain("Docker");
        result.FileType.Should().Be("md");
        result.Sections.Should().NotBeEmpty();
    }

    [Fact]
    public async Task ParseAsync_UnsupportedType_ThrowsNotSupportedException()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var act    = async () => await _sut.ParseAsync(stream, "file.xyz", "application/octet-stream");

        await act.Should().ThrowAsync<NotSupportedException>();
    }

    [Fact]
    public async Task ParseAsync_SetsTopicFromFileName()
    {
        var text   = "Some content here about microservices";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var result = await _sut.ParseAsync(stream, "microservices-notes.txt", "text/plain");

        result.Topic.Should().Contain("microservices");
    }

    [Fact]
    public async Task ParseAsync_LargeText_SplitsIntoSections()
    {
        var text   = string.Join("\n", Enumerable.Range(1, 100).Select(i => $"Section {i}: " + new string('x', 50)));
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        var result = await _sut.ParseAsync(stream, "large.txt", "text/plain");

        result.Sections.Count.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task ParseAsync_AssignsNewGuidId()
    {
        var stream  = new MemoryStream(Encoding.UTF8.GetBytes("test content"));
        var result1 = await _sut.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes("content1")), "a.txt", "text/plain");
        var result2 = await _sut.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes("content2")), "b.txt", "text/plain");

        result1.Id.Should().NotBe(result2.Id);
    }
}
