using DocuMind.Hubs;
using DocuMind.Services;
using DocuMind.VideoIngestor;
using Microsoft.AspNetCore.Http.Connections;
using Qdrant.Client;
using Azure.Storage.Blobs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

builder.Services.AddSignalR(options =>
{
    options.EnableDetailedErrors = true;
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
    options.ClientTimeoutInterval = TimeSpan.FromMinutes(10);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
});

builder.Services.AddMemoryCache();

builder.Services.AddHttpClient();

builder.Services.AddHttpClient("ollama-chat", client =>
{
    client.Timeout = TimeSpan.FromMinutes(15);
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 1024L * 1024L * 1024L;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1024L * 1024L * 1024L;
});

// AI / Embedding
builder.Services.AddSingleton<SemanticKernelEmbeddingService>();

// Qdrant
var qdrantHost = builder.Configuration["Qdrant:Host"] ?? "localhost";
var qdrantPort = builder.Configuration.GetValue<int>("Qdrant:Port", 6334);

builder.Services.AddSingleton(_ => new QdrantClient(qdrantHost, qdrantPort));

// Azure Blob Storage
builder.Services.AddSingleton(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var connectionString = config["AzureStorage:ConnectionString"];

    if (string.IsNullOrWhiteSpace(connectionString))
        throw new InvalidOperationException("AzureStorage:ConnectionString is missing.");

    return new BlobServiceClient(connectionString);
});

builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();

// Core services
builder.Services.AddTransient<RagService>();
builder.Services.AddSingleton<VideoTranscriber>();
builder.Services.AddSingleton<DocumentParser>();
builder.Services.AddSingleton<DocumentRepository>();
builder.Services.AddSingleton<RetrievalService>();
builder.Services.AddSingleton<TextChunker>();
builder.Services.AddSingleton<VectorDbService>();
builder.Services.AddSingleton<DocumentStoreService>();

// Video services
builder.Services.AddSingleton<VideoQueue>();
builder.Services.AddTransient<VideoSplitter>();
builder.Services.AddTransient<AudioTranscriber>();
builder.Services.AddTransient<TranscriptSummariser>();
builder.Services.AddTransient<VideoPipeline>();
builder.Services.AddHostedService<VideoIngestorWorker>();
builder.Services.AddSingleton<YouTubeDownloader>();

var app = builder.Build();

app.Logger.LogInformation("DocuMind starting...");
app.Logger.LogInformation("Qdrant: {Host}:{Port}", qdrantHost, qdrantPort);
app.Logger.LogInformation("Ollama URL: {Url}", builder.Configuration["Ollama:BaseUrl"]);

using (var scope = app.Services.CreateScope())
{
    var vectorDb = scope.ServiceProvider.GetRequiredService<VectorDbService>();

    try
    {
        await vectorDb.EnsureCollectionAsync();
        app.Logger.LogInformation("Qdrant collection ready.");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Qdrant startup check failed.");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "DocuMind API v1");
    options.RoutePrefix = "docs-api";
});

app.UseStaticFiles();

app.UseCors("AllowAll");

app.UseRouting();

app.MapControllers();

app.MapHub<DocuMindHub>("/hubs/documind", options =>
{
    options.Transports = HttpTransportType.WebSockets | HttpTransportType.LongPolling;
});

app.MapGet("/health", () => Results.Ok("DocuMind is running."));

app.MapGet("/test-embedding", async (SemanticKernelEmbeddingService embeddingService) =>
{
    var vector = await embeddingService.GetEmbeddingAsync("hello world");
    return Results.Ok(new
    {
        dimensions = vector.Length,
        preview = vector.Take(5)
    });
});

app.MapGet("/qdrant-test", async (QdrantClient client) =>
{
    var collections = await client.ListCollectionsAsync();
    return Results.Ok(collections);
});

app.MapFallbackToFile("index.html");

app.Run();