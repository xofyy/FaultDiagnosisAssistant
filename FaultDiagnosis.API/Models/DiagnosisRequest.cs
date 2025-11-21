namespace FaultDiagnosis.API.Models
{
    public class DiagnosisRequest
    {
        public string Symptom { get; set; } = string.Empty;
        public string VehicleInfo { get; set; } = string.Empty;
    }
}
