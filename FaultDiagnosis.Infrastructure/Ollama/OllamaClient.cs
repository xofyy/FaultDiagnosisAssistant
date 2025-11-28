using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Configuration;
using FaultDiagnosis.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace FaultDiagnosis.Infrastructure.Ollama
{
    public class OllamaClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly FaultDiagnosisSettings _settings;

        public OllamaClient(HttpClient httpClient, IOptions<FaultDiagnosisSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = new
            {
                model = _settings.EmbeddingModel,
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
                model = _settings.GenerationModel,
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
