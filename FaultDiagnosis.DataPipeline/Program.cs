using System;
using System.IO;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Interfaces;
using FaultDiagnosis.Infrastructure.Ollama;
using FaultDiagnosis.Infrastructure.Qdrant;
using FaultDiagnosis.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Qdrant.Client;

namespace FaultDiagnosis.DataPipeline
{
    class Program
    {
        static async Task Main(string[] args)
        {
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
                Directory.CreateDirectory(docsPath);
                logger.LogWarning($"Created docs directory at {docsPath}. Please put some text files there.");
                return;
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
            services.AddLogging(configure => configure.AddConsole());
            
            services.AddHttpClient<ILLMClient, OllamaClient>(client =>
            {
                client.BaseAddress = new Uri("http://127.0.0.1:11434");
                client.Timeout = TimeSpan.FromMinutes(5);
            });

            services.AddSingleton<QdrantClient>(sp => new QdrantClient(new Uri("http://127.0.0.1:6334")));
            services.AddSingleton<IVectorStore, QdrantVectorStore>();
            services.AddSingleton<IDocumentProcessor, TextDocumentProcessor>();
        }
    }
}
