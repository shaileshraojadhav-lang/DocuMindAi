using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace DocuMind.IngestFunction.Services;

public sealed class QdrantVectorService
{
    private readonly QdrantClient _client;
    private readonly IConfiguration _config;
    private readonly ILogger<QdrantVectorService> _logger;
    private readonly string _collection;

    public QdrantVectorService(
        QdrantClient client,
        IConfiguration config,
        ILogger<QdrantVectorService> logger)
    {
        _client = client;
        _config = config;
        _logger = logger;
        _collection = _config["Qdrant:Collection"] ?? "documind_chunks";
    }

    public async Task EnsureCollectionAsync(CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync(cancellationToken: ct);

        if (collections.Any(x => x == _collection))
        {
            _logger.LogInformation("Qdrant collection already exists: {Collection}", _collection);
            return;
        }

        await _client.CreateCollectionAsync(
            collectionName: _collection,
            vectorsConfig: new VectorParams
            {
                Size = 768,
                Distance = Distance.Cosine
            },
            cancellationToken: ct);

        _logger.LogInformation("Qdrant collection created: {Collection}", _collection);
    }

    public async Task UpsertChunksAsync(
        string blobName,
        string fileName,
        IReadOnlyList<string> chunks,
        IReadOnlyList<float[]> vectors,
        CancellationToken ct = default)
    {
        if (chunks.Count != vectors.Count)
            throw new InvalidOperationException("Chunks and vectors count mismatch.");

        var points = new List<PointStruct>();

        for (var i = 0; i < chunks.Count; i++)
        {
            points.Add(new PointStruct
            {
                Id = Guid.NewGuid(),
                Vectors = vectors[i],
                Payload =
                {
                    ["blobName"] = blobName,
                    ["fileName"] = fileName,
                    ["chunkIndex"] = i,
                    ["text"] = chunks[i],
                    ["sourceType"] = "document",
                    ["createdAt"] = DateTimeOffset.UtcNow.ToString("O")
                }
            });
        }

        await _client.UpsertAsync(
            collectionName: _collection,
            points: points,
            cancellationToken: ct);

        _logger.LogInformation(
            "Qdrant upsert completed. File={FileName}, Chunks={Count}",
            fileName,
            chunks.Count);
    }
}