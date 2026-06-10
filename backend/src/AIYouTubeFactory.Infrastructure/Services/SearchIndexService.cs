using AIYouTubeFactory.Core.Interfaces;
using AIYouTubeFactory.Core.Models;
using Azure.Search.Documents;
using Azure.Search.Documents.Indexes;
using Azure.Search.Documents.Indexes.Models;
using Azure.Search.Documents.Models;
using Azure;

namespace AIYouTubeFactory.Infrastructure.Services;

public class SearchIndexService : ISearchIndexService
{
    private readonly SearchClient _searchClient;
    private readonly SearchIndexClient _indexClient;
    private const string IndexName = "youtube-factory-docs";

    public SearchIndexService(string searchEndpoint, string searchApiKey)
    {
        var credential = new AzureKeyCredential(searchApiKey);
        _indexClient = new SearchIndexClient(new Uri(searchEndpoint), credential);
        _searchClient = new SearchClient(new Uri(searchEndpoint), IndexName, credential);
        EnsureIndexExistsAsync().GetAwaiter().GetResult();
    }

    private async Task EnsureIndexExistsAsync()
    {
        try
        {
            var index = new SearchIndex(IndexName)
            {
                Fields = new List<SearchField>
                {
                    new SimpleField("id", SearchFieldDataType.String) { IsKey = true },
                    new SearchableField("content"),
                    new SearchableField("topic"),
                    new SimpleField("fileName", SearchFieldDataType.String) { IsFilterable = true },
                    new SimpleField("uploadedAt", SearchFieldDataType.DateTimeOffset) { IsSortable = true }
                }
            };
            await _indexClient.CreateOrUpdateIndexAsync(index);
        }
        catch { /* Index may already exist */ }
    }

    public async Task IndexDocumentAsync(UploadedDocument document)
    {
        var batch = IndexDocumentsBatch.Upload(new[]
        {
            new
            {
                id = document.Id.ToString(),
                content = document.ExtractedText[..Math.Min(document.ExtractedText.Length, 32000)],
                topic = document.Topic,
                fileName = document.FileName,
                uploadedAt = document.UploadedAt
            }
        });

        await _searchClient.IndexDocumentsAsync(batch);
    }

    public async Task<List<string>> SearchRelatedContentAsync(string query, int topK = 5)
    {
        var options = new SearchOptions { Size = topK, Select = { "content", "topic" } };
        var response = await _searchClient.SearchAsync<SearchDocument>(query, options);
        var results = new List<string>();

        await foreach (var result in response.Value.GetResultsAsync())
        {
            if (result.Document.TryGetValue("content", out var content))
                results.Add(content?.ToString() ?? "");
        }

        return results;
    }
}
