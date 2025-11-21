using System.Collections.Generic;

namespace FaultDiagnosis.Core.Entities
{
    public class DiagnosisResult
    {
        public string Diagnosis { get; set; } = string.Empty;
        public List<string> RelatedDocuments { get; set; } = new();
    }
}
