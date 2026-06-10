using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig;
using UglyToad.PdfPig;
using System.Text;
using System.Text.RegularExpressions;

namespace AIYouTubeFactory.Infrastructure.Parsers;

public class DocumentParserService : IDocumentParserService
{
    public async Task<UploadedDocument> ParseAsync(Stream fileStream, string fileName, string contentType)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var doc = new UploadedDocument
        {
            FileName = fileName,
            FileType = ext.TrimStart('.')
        };

        // Copy stream to memory for multi-read
        using var ms = new MemoryStream();
        await fileStream.CopyToAsync(ms);
        ms.Position = 0;

        doc.ExtractedText = ext switch
        {
            ".pdf" => ExtractFromPdf(ms),
            ".pptx" or ".ppt" => ExtractFromPptx(ms),
            ".docx" or ".doc" => ExtractFromDocx(ms),
            ".md" or ".markdown" => ExtractFromMarkdown(ms),
            ".txt" => new StreamReader(ms).ReadToEnd(),
            _ => throw new NotSupportedException($"File type {ext} is not supported.")
        };

        doc.Sections = SplitIntoSections(doc.ExtractedText);
        doc.Topic = InferTopic(doc.ExtractedText, fileName);
        return doc;
    }

    private static string ExtractFromPdf(Stream stream)
    {
        var sb = new StringBuilder();
        using var pdf = PdfDocument.Open(stream);
        foreach (var page in pdf.GetPages())
        {
            sb.AppendLine(page.Text);
        }
        return sb.ToString();
    }

    private static string ExtractFromPptx(Stream stream)
    {
        var sb = new StringBuilder();
        using var prs = PresentationDocument.Open(stream, false);
        var presentation = prs.PresentationPart?.Presentation;
        if (presentation?.SlideIdList == null) return string.Empty;

        int slideNum = 1;
        foreach (SlideId slideId in presentation.SlideIdList)
        {
            var slidePart = (SlidePart)prs.PresentationPart!.GetPartById(slideId.RelationshipId!);
            sb.AppendLine($"--- Slide {slideNum++} ---");
            foreach (var para in slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Paragraph>())
            {
                var text = string.Concat(para.Descendants<DocumentFormat.OpenXml.Drawing.Run>()
                    .Select(r => r.Text?.Text ?? ""));
                if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
            }
        }
        return sb.ToString();
    }

    private static string ExtractFromDocx(Stream stream)
    {
        var sb = new StringBuilder();
        using var doc = WordprocessingDocument.Open(stream, false);
        var body = doc.MainDocumentPart?.Document.Body;
        if (body == null) return string.Empty;

        foreach (var para in body.Descendants<Paragraph>())
        {
            var text = para.InnerText;
            if (!string.IsNullOrWhiteSpace(text)) sb.AppendLine(text);
        }
        return sb.ToString();
    }

    private static string ExtractFromMarkdown(Stream stream)
    {
        var md = new StreamReader(stream).ReadToEnd();
        // Strip markdown formatting, keep text
        var pipeline = new MarkdownPipelineBuilder().Build();
        var doc = Markdown.Parse(md, pipeline);
        // Return raw markdown text (LLM can handle it)
        return md;
    }

    private static List<string> SplitIntoSections(string text)
    {
        // Split by slide markers, headers, or every ~500 words
        var sections = new List<string>();
        var parts = Regex.Split(text, @"(---\s*Slide\s*\d+\s*---|#{1,3}\s+\w)");

        var current = new StringBuilder();
        int wordCount = 0;
        foreach (var part in parts)
        {
            current.Append(part);
            wordCount += part.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            if (wordCount >= 400)
            {
                sections.Add(current.ToString().Trim());
                current.Clear();
                wordCount = 0;
            }
        }
        if (current.Length > 0) sections.Add(current.ToString().Trim());

        return sections.Where(s => s.Length > 50).ToList();
    }

    private static string InferTopic(string text, string fileName)
    {
        // Use filename as hint, fall back to first meaningful words
        var name = Path.GetFileNameWithoutExtension(fileName)
            .Replace("-", " ").Replace("_", " ");
        return string.IsNullOrWhiteSpace(name) ? "Technology" : name;
    }
}
