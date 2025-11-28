using FaultDiagnosis.API.Controllers;
using FaultDiagnosis.API.Models;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace FaultDiagnosis.Tests.Unit
{
    public class DiagnosisControllerTests
    {
        private readonly Mock<ILLMClient> _llmClientMock;
        private readonly Mock<IVectorStore> _vectorStoreMock;
        private readonly Mock<IValidator<DiagnosisRequest>> _validatorMock;
        private readonly DiagnosisController _controller;

        public DiagnosisControllerTests()
        {
            _llmClientMock = new Mock<ILLMClient>();
            _vectorStoreMock = new Mock<IVectorStore>();
            _validatorMock = new Mock<IValidator<DiagnosisRequest>>();

            _controller = new DiagnosisController(
                _llmClientMock.Object, 
                _vectorStoreMock.Object, 
                _validatorMock.Object);
        }

        [Fact]
        public async Task Diagnose_ShouldReturnBadRequest_WhenValidationFails()
        {
            // Arrange
            var request = new DiagnosisRequest { Symptom = "" };
            var validationResult = new ValidationResult(new[] { new ValidationFailure("Symptom", "Required") });
            
            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            // Act
            var result = await _controller.Diagnose(request);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Diagnose_ShouldReturnOk_WhenValidationPasses()
        {
            // Arrange
            var request = new DiagnosisRequest { Symptom = "Engine noise", VehicleInfo = "Clio" };
            _validatorMock.Setup(v => v.ValidateAsync(request, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _llmClientMock.Setup(c => c.GenerateEmbeddingAsync(It.IsAny<string>()))
                .ReturnsAsync(new float[] { 0.1f, 0.2f });

            _vectorStoreMock.Setup(s => s.SearchAsync(It.IsAny<float[]>(), It.IsAny<int>()))
                .ReturnsAsync(new List<DocumentChunk>());

            _llmClientMock.Setup(c => c.GenerateCompletionAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync("Diagnosis result");

            // Act
            var result = await _controller.Diagnose(request);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var response = okResult.Value as DiagnosisResult;
            response.Diagnosis.Should().Be("Diagnosis result");
        }
    }
}
