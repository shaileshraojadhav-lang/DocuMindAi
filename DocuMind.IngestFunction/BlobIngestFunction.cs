using DocuMind.IngestFunction.Services;
using DocuMind.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace DocuMind.IngestFunction;

public sealed class BlobIngestFunction
{
    private readonly DocumentParser _parser;
    private readonly TextChunker _chunker;
    private readonly SemanticKernelEmbeddingService _embedding;
    private readonly QdrantVectorService _qdrant;
    private readonly ILogger<BlobIngestFunction> _logger;

    public BlobIngestFunction(
        DocumentParser parser,
        TextChunker chunker,
        SemanticKernelEmbeddingService embedding,
        QdrantVectorService qdrant,
        ILogger<BlobIngestFunction> logger)
    {
        _parser = parser;
        _chunker = chunker;
        _embedding = embedding;
        _qdrant = qdrant;
        _logger = logger;
    }

    [Function(nameof(BlobIngestFunction))]
    public async Task Run(
        [BlobTrigger("documind-files/{name}", Connection = "AzureWebJobsStorage")]
        Stream stream,
        string name,
        CancellationToken ct)
    {
        _logger.LogInformation("Blob ingestion started. Name={Name}", name);

        var text = await _parser.ParseAsync(name, stream, ct);

        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("No text extracted from blob. Name={Name}", name);
            return;
        }

        var chunks = _chunker.Chunk(text);

        if (chunks.Count == 0)
        {
            _logger.LogWarning("No chunks created. Name={Name}", name);
            return;
        }

        _logger.LogInformation(
            "Document parsed and chunked. Name={Name}, Chunks={Chunks}",
            name,
            chunks.Count);

        await _qdrant.EnsureCollectionAsync(ct);

        var vectors = await _embedding.GenerateAsync(chunks, ct);

        await _qdrant.UpsertChunksAsync(
            blobName: name,
            fileName: Path.GetFileName(name),
            chunks: chunks,
            vectors: vectors,
            ct: ct);

        _logger.LogInformation(
            "Blob ingestion completed and stored in Qdrant. Name={Name}",
            name);
    }
}