using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Infrastructure.Qdrant;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Qdrant.Client;
using Testcontainers.Qdrant;
using Xunit;

namespace FaultDiagnosis.Tests.Integration
{
    public class QdrantVectorStoreTests : IAsyncLifetime
    {
        private readonly QdrantContainer _qdrantContainer;
        private QdrantVectorStore _vectorStore;
        private QdrantClient _client;

        public QdrantVectorStoreTests()
        {
            _qdrantContainer = new QdrantBuilder()
                .WithImage("qdrant/qdrant:latest")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _qdrantContainer.StartAsync();
            
            // Testcontainers returns grpc connection string usually? 
            // Qdrant container exposes 6333 (http) and 6334 (grpc).
            // QdrantClient uses grpc by default.
            // Let's use the mapped port for 6334.
            var grpcPort = _qdrantContainer.GetMappedPublicPort(6334);
            var host = _qdrantContainer.Hostname;
            
            _client = new QdrantClient(new Uri($"http://{host}:{grpcPort}"));
            
            _vectorStore = new QdrantVectorStore(_client);
        }

        public async Task DisposeAsync()
        {
            await _qdrantContainer.DisposeAsync();
        }

        [Fact]
        public async Task UpsertAsync_ShouldAddChunkToCollection()
        {
            // Arrange
            await _vectorStore.CreateCollectionIfNotExistsAsync();
            var chunk = new DocumentChunk
            {
                Id = Guid.NewGuid(),
                Content = "Test content",
                SourceFile = "test.txt",
                Embedding = new float[768] // Dimension 768
            };
            // Fill with some data
            Array.Fill(chunk.Embedding, 0.1f);

            // Act
            await _vectorStore.UpsertAsync(chunk);

            // Assert
            // We can verify by searching
            var results = await _vectorStore.SearchAsync(chunk.Embedding, 1);
            results.Should().NotBeEmpty();
            results.First().Content.Should().Be("Test content");
        }
    }
}
