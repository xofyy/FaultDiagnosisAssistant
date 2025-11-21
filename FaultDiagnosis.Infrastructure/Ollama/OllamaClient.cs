using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Interfaces;

namespace FaultDiagnosis.Infrastructure.Ollama
{
    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private const string EmbeddingModel = "nomic-embed-text";
        private const string GenerationModel = "llama3.1";

        public OllamaClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
            // Ensure BaseAddress is set in Program.cs/DI (e.g., http://localhost:11434)
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = new
            {
                model = EmbeddingModel,
                prompt = text
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/embeddings", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<EmbeddingResponse>(json);

            return result?.Embedding ?? Array.Empty<float>();
        }

        public async Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = "")
        {
            var fullPrompt = string.IsNullOrEmpty(systemPrompt) ? prompt : $"{systemPrompt}\n\n{prompt}";
            
            var request = new
            {
                model = GenerationModel,
                prompt = fullPrompt,
                stream = false
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/generate", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GenerateResponse>(json);

            return result?.Response ?? string.Empty;
        }

        private class EmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; }
        }

        private class GenerateResponse
        {
            [JsonPropertyName("response")]
            public string Response { get; set; }
        }
    }
}
