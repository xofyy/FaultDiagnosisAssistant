using System.Threading.Tasks;

namespace FaultDiagnosis.Core.Interfaces
{
    public interface ILLMClient
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<string> GenerateCompletionAsync(string prompt, string systemPrompt = "");
    }
}
