using System;
using System.IO;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Configuration;
using FaultDiagnosis.Core.Interfaces;
using FaultDiagnosis.Infrastructure.Ollama;
using FaultDiagnosis.Infrastructure.OpenAI;
using FaultDiagnosis.Infrastructure.Qdrant;
using FaultDiagnosis.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Qdrant.Client;
using System.Net.Http;
using Serilog;

namespace FaultDiagnosis.DataPipeline
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Allow gRPC over HTTP (unencrypted)
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

            var services = new ServiceCollection();
            ConfigureServices(services);
            var serviceProvider = services.BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var processor = serviceProvider.GetRequiredService<IDocumentProcessor>();
            var llm = serviceProvider.GetRequiredService<ILLMClient>();
            var vectorStore = serviceProvider.GetRequiredService<IVectorStore>();

            string docsPath = Path.Combine(Directory.GetCurrentDirectory(), "docs");
            if (!Directory.Exists(docsPath))
            {
                // Try parent directory (for local dev where docs is in root)
                var parentDocs = Path.Combine(Directory.GetCurrentDirectory(), "..", "docs");
                if (Directory.Exists(parentDocs))
                {
                    docsPath = parentDocs;
                }
                else
                {
                    Directory.CreateDirectory(docsPath);
                    logger.LogWarning($"Created docs directory at {docsPath}. Please put some text files there.");
                    return;
                }
            }

            // Ensure collection exists
            try 
            {
                await vectorStore.CreateCollectionIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to connect to Qdrant. Make sure it is running.");
                return;
            }

            var files = Directory.GetFiles(docsPath, "*.txt");
            foreach (var file in files)
            {
                logger.LogInformation($"Processing {Path.GetFileName(file)}...");
                var chunks = await processor.ProcessFileAsync(file);

                foreach (var chunk in chunks)
                {
                    logger.LogInformation($"Generating embedding for chunk ID {chunk.Id}...");
                    chunk.Embedding = await llm.GenerateEmbeddingAsync(chunk.Content);
                    
                    logger.LogInformation($"Upserting chunk ID {chunk.Id} to Qdrant...");
                    await vectorStore.UpsertAsync(chunk);
                }
                logger.LogInformation($"Finished {Path.GetFileName(file)}");
            }

            logger.LogInformation("Pipeline completed.");
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // Configuration
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // Serilog
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            services.AddLogging(configure => configure.AddSerilog());

            services.Configure<FaultDiagnosisSettings>(configuration.GetSection("FaultDiagnosis"));
            var settings = configuration.GetSection("FaultDiagnosis").Get<FaultDiagnosisSettings>();

            if (settings.LLMProvider.Equals("OpenAI", StringComparison.OrdinalIgnoreCase))
            {
                services.AddHttpClient<ILLMClient, OpenAIClient>(client =>
                {
                    client.BaseAddress = new Uri(settings.OpenAIEndpoint ?? "https://api.openai.com/");
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
                .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
            }
            else
            {
                services.AddHttpClient<ILLMClient, OllamaClient>(client =>
                {
                    client.BaseAddress = new Uri(settings.OllamaUrl);
                    client.Timeout = TimeSpan.FromMinutes(5);
                })
                .AddTransientHttpErrorPolicy(builder => builder.WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
                .AddTransientHttpErrorPolicy(builder => builder.CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)));
            }

            services.AddSingleton<QdrantClient>(sp => 
            {
                var uri = new Uri(settings.QdrantUrl);
                return new QdrantClient(
                    host: uri.Host, 
                    port: uri.Port, 
                    https: uri.Scheme == "https"
                );
            });
            services.AddSingleton<IVectorStore, QdrantVectorStore>();
            services.AddSingleton<IDocumentProcessor, TextDocumentProcessor>();
        }
    }
}
