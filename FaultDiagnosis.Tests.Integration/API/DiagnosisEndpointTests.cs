using System.Net;
using System.Net.Http.Json;
using FaultDiagnosis.API;
using FaultDiagnosis.API.Models;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace FaultDiagnosis.Tests.Integration.API
{
    public class DiagnosisEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly Mock<IVectorStore> _vectorStoreMock;
        private readonly Mock<ILLMClient> _llmClientMock;

        public DiagnosisEndpointTests(WebApplicationFactory<Program> factory)
        {
            _vectorStoreMock = new Mock<IVectorStore>();
            _llmClientMock = new Mock<ILLMClient>();

            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(_vectorStoreMock.Object);
                    services.AddSingleton(_llmClientMock.Object);
                });
            });
        }

        [Fact]
        public async Task Diagnose_ShouldReturnDiagnosis_WhenRequestIsValid()
        {
            // Arrange
            var request = new DiagnosisRequest 
            { 
                Symptom = "Engine overheating",
                VehicleInfo = "2020 Toyota Corolla"
            };
            
            _vectorStoreMock.Setup(s => s.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>()))
                .ReturnsAsync(new List<DocumentChunk> 
                { 
                    new DocumentChunk { Content = "Check coolant level" } 
                });

            _llmClientMock.Setup(c => c.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new float[768]);

            _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Based on the symptoms, check the coolant level.");

            var client = _factory.CreateClient();

            // Act
            var response = await client.PostAsJsonAsync("/api/diagnosis", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var result = await response.Content.ReadFromJsonAsync<DiagnosisResponse>();
            result.Should().NotBeNull();
            result.Diagnosis.Should().Contain("coolant level");
        }
    }
}
