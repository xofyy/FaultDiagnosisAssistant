using FaultDiagnosis.API.Models;
using FaultDiagnosis.API.Validators;
using FluentValidation.TestHelper;
using Xunit;

namespace FaultDiagnosis.Tests.Unit
{
    public class DiagnosisRequestValidatorTests
    {
        private readonly DiagnosisRequestValidator _validator;

        public DiagnosisRequestValidatorTests()
        {
            _validator = new DiagnosisRequestValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Symptom_Is_Empty()
        {
            var model = new DiagnosisRequest { Symptom = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Symptom);
        }

        [Fact]
        public void Should_Have_Error_When_Symptom_Is_Too_Short()
        {
            var model = new DiagnosisRequest { Symptom = "short" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.Symptom);
        }

        [Fact]
        public void Should_Not_Have_Error_When_Symptom_Is_Valid()
        {
            var model = new DiagnosisRequest { Symptom = "This is a valid symptom description." };
            var result = _validator.TestValidate(model);
            result.ShouldNotHaveValidationErrorFor(x => x.Symptom);
        }

        [Fact]
        public void Should_Have_Error_When_VehicleInfo_Is_Empty()
        {
            var model = new DiagnosisRequest { VehicleInfo = "" };
            var result = _validator.TestValidate(model);
            result.ShouldHaveValidationErrorFor(x => x.VehicleInfo);
        }
    }
}
