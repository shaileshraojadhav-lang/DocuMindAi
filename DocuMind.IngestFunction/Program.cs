using DocuMind.IngestFunction.Services;
using DocuMind.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Qdrant.Client;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        services.AddHttpClient();

        services.AddSingleton<DocumentParser>();
        services.AddSingleton<TextChunker>();
        services.AddSingleton<SemanticKernelEmbeddingService>();

        var qdrantHost = context.Configuration["Qdrant:Host"] ?? "localhost";
        var qdrantPort = int.Parse(context.Configuration["Qdrant:Port"] ?? "6334");

        services.AddSingleton(_ => new QdrantClient(qdrantHost, qdrantPort));
        services.AddSingleton<QdrantVectorService>();
    })
    .Build();

host.Run();