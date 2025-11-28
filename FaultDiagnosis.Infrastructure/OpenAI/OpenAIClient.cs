using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Configuration;
using FaultDiagnosis.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace FaultDiagnosis.Infrastructure.OpenAI
{
    public class OpenAIClient : ILLMClient
    {
        private readonly HttpClient _httpClient;
        private readonly FaultDiagnosisSettings _settings;

        public OpenAIClient(HttpClient httpClient, IOptions<FaultDiagnosisSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            if (!string.IsNullOrEmpty(_settings.ApiKey))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            var request = new
            {
                model = _settings.EmbeddingModel, // e.g., "text-embedding-3-small"
                input = text
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("v1/embeddings", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIEmbeddingResponse>(json);

            return result?.Data?[0]?.Embedding ?? Array.Empty<float>();
        }

        public async Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = "")
        {
            var messages = new List<object>();
            if (!string.IsNullOrEmpty(systemPrompt))
            {
                messages.Add(new { role = "system", content = systemPrompt });
            }
            messages.Add(new { role = "user", content = prompt });

            var request = new
            {
                model = _settings.GenerationModel, // e.g., "gpt-4o"
                messages = messages
            };

            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("v1/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OpenAIChatResponse>(json);

            return result?.Choices?[0]?.Message?.Content ?? string.Empty;
        }

        // Response Models
        private class OpenAIEmbeddingResponse
        {
            [JsonPropertyName("data")]
            public List<EmbeddingData> Data { get; set; }
        }

        private class EmbeddingData
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; }
        }

        private class OpenAIChatResponse
        {
            [JsonPropertyName("choices")]
            public List<ChatChoice> Choices { get; set; }
        }

        private class ChatChoice
        {
            [JsonPropertyName("message")]
            public ChatMessage Message { get; set; }
        }

        private class ChatMessage
        {
            [JsonPropertyName("content")]
            public string Content { get; set; }
        }
    }
}
