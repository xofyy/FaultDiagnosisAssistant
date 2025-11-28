using FaultDiagnosis.API.Models;
using FluentValidation;

namespace FaultDiagnosis.API.Validators
{
    public class DiagnosisRequestValidator : AbstractValidator<DiagnosisRequest>
    {
        public DiagnosisRequestValidator()
        {
            RuleFor(x => x.Symptom)
                .NotEmpty().WithMessage("Symptom is required.")
                .MinimumLength(10).WithMessage("Symptom must be at least 10 characters long.");

            RuleFor(x => x.VehicleInfo)
                .NotEmpty().WithMessage("Vehicle info is required.");
        }
    }
}
