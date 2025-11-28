namespace FaultDiagnosis.Core.Configuration
{
    public class FaultDiagnosisSettings
    {
        public string OllamaUrl { get; set; } = "http://127.0.0.1:11434";
        public string QdrantUrl { get; set; } = "http://127.0.0.1:6334";
        public string EmbeddingModel { get; set; } = "nomic-embed-text";
        public string GenerationModel { get; set; } = "llama3.1";
        
        public string LLMProvider { get; set; } = "Ollama"; // "Ollama" or "OpenAI"
        public string? ApiKey { get; set; }
        public string? OpenAIEndpoint { get; set; } // Optional, for Azure OpenAI
    }
}
