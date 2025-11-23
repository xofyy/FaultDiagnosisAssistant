using System.Linq;
using System.Threading.Tasks;
using FaultDiagnosis.API.Models;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FaultDiagnosis.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly ILLMClient _llmClient;
        private readonly IVectorStore _vectorStore;

        public DiagnosisController(ILLMClient llmClient, IVectorStore vectorStore)
        {
            _llmClient = llmClient;
            _vectorStore = vectorStore;
        }

        [HttpPost]
        public async Task<ActionResult<DiagnosisResult>> Diagnose([FromBody] DiagnosisRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Symptom))
            {
                return BadRequest("Symptom is required.");
            }

            // 1. Generate embedding for the symptom
            var embedding = await _llmClient.GenerateEmbeddingAsync($"{request.VehicleInfo} {request.Symptom}");

            // 2. Retrieve relevant documents
            var relevantChunks = await _vectorStore.SearchAsync(embedding, limit: 3);

            // 3. Construct Prompt
            var context = string.Join("\n\n", relevantChunks.Select(c => $"Source: {c.SourceFile}\nContent: {c.Content}"));
            
            var systemPrompt = "Sen uzman bir otomotiv arıza teşhis asistanısın. " +
                               "Verilen bağlamı (context) kullanarak sorunu teşhis et. " +
                               "Eğer bağlamda cevap yoksa, genel bilgini kullan ancak bunun kılavuzdan olmadığını açıkça belirt. " +
                               "Cevabını 'Olası Sebepler' ve 'Çözüm Adımları' başlıklarıyla net bir şekilde formatla. " +
                               "ÖNEMLİ: Her zaman Türkçe yanıt ver.";

            var userPrompt = $"Vehicle: {request.VehicleInfo}\nSymptom: {request.Symptom}\n\nContext:\n{context}";

            // 4. Generate Response
            var response = await _llmClient.GenerateCompletionAsync(userPrompt, systemPrompt);

            return Ok(new DiagnosisResult
            {
                Diagnosis = response,
                RelatedDocuments = relevantChunks.Select(c => c.SourceFile).Distinct().ToList()
            });
        }
    }
}
