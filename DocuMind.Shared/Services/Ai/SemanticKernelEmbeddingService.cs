using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DocuMind.Services;

public sealed class SemanticKernelEmbeddingService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<SemanticKernelEmbeddingService> _logger;
    private readonly string _baseUrl;
    private readonly string _model;

    public SemanticKernelEmbeddingService(
        IHttpClientFactory factory,
        IConfiguration config,
        ILogger<SemanticKernelEmbeddingService> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;

        _baseUrl = (_config["Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/');
        _model = _config["Embedding:Model"] ?? "nomic-embed-text";

        _logger.LogInformation("[SK-EMBED] Ready model={Model} url={Url}", _model, _baseUrl);
    }

    public async Task<float[]> GetEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var result = await GenerateAsync([text], cancellationToken);
        return result.First();
    }

    public async Task<List<float[]>> GenerateAsync(
        IEnumerable<string> values,
        CancellationToken cancellationToken = default)
    {
        var inputs = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToList();

        if (inputs.Count == 0)
            return [];

        using var http = _factory.CreateClient();
        http.Timeout = TimeSpan.FromMinutes(5);

        var request = new OllamaEmbedRequest
        {
            Model = _model,
            Input = inputs
        };

        var url = $"{_baseUrl}/api/embed";

        _logger.LogInformation("[SK-EMBED] Calling Ollama {Url}, count={Count}", url, inputs.Count);

        var response = await http.PostAsJsonAsync(url, request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Embedding failed HTTP {(int)response.StatusCode}. Body: {body}");

        var data = System.Text.Json.JsonSerializer.Deserialize<OllamaEmbedResponse>(
            body,
            new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        var vectors = new List<float[]>();

        if (data?.Embeddings is { Count: > 0 })
            vectors.AddRange(data.Embeddings);

        if (data?.Embedding is { Length: > 0 })
            vectors.Add(data.Embedding);

        if (vectors.Count == 0)
            throw new InvalidOperationException("Ollama returned empty embedding response.");

        return vectors;
    }

    private sealed class OllamaEmbedRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("input")]
        public List<string> Input { get; set; } = [];
    }

    private sealed class OllamaEmbedResponse
    {
        [JsonPropertyName("embeddings")]
        public List<float[]>? Embeddings { get; set; }

        [JsonPropertyName("embedding")]
        public float[]? Embedding { get; set; }
    }
}