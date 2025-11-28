using FaultDiagnosis.Core.Configuration;
using FaultDiagnosis.Infrastructure.Ollama;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using Xunit;

namespace FaultDiagnosis.Tests.Integration
{
    public class OllamaClientTests : IDisposable
    {
        private readonly WireMockServer _server;
        private readonly OllamaClient _client;
        private readonly HttpClient _httpClient;

        public OllamaClientTests()
        {
            _server = WireMockServer.Start();
            _httpClient = new HttpClient { BaseAddress = new Uri(_server.Urls[0]) };
            
            var settings = new FaultDiagnosisSettings 
            { 
                EmbeddingModel = "nomic-embed-text",
                GenerationModel = "llama3.1" 
            };
            var optionsMock = new Mock<IOptions<FaultDiagnosisSettings>>();
            optionsMock.Setup(o => o.Value).Returns(settings);

            _client = new OllamaClient(_httpClient, optionsMock.Object);
        }

        public void Dispose()
        {
            _server.Stop();
            _server.Dispose();
            _httpClient.Dispose();
        }

        [Fact]
        public async Task GenerateEmbeddingAsync_ShouldReturnEmbedding_WhenApiReturnsSuccess()
        {
            // Arrange
            _server.Given(
                Request.Create().WithPath("/api/embeddings").UsingPost()
            )
            .RespondWith(
                Response.Create()
                    .WithStatusCode(200)
                    .WithBody(@"{ ""embedding"": [0.1, 0.2, 0.3] }")
            );

            // Act
            var result = await _client.GenerateEmbeddingAsync("test");

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().Be(0.1f);
        }
    }
}
