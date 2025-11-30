using System.Linq;
using System.Threading.Tasks;
using FaultDiagnosis.API.Models;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace FaultDiagnosis.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosisController : ControllerBase
    {
        private readonly ILLMClient _llmClient;
        private readonly IVectorStore _vectorStore;
        private readonly IValidator<DiagnosisRequest> _validator;

        public DiagnosisController(ILLMClient llmClient, IVectorStore vectorStore, IValidator<DiagnosisRequest> validator)
        {
            _llmClient = llmClient;
            _vectorStore = vectorStore;
            _validator = validator;
        }

        [HttpPost]
        public async Task<ActionResult<DiagnosisResult>> Diagnose([FromBody] DiagnosisRequest request)
        {
            var validationResult = await _validator.ValidateAsync(request);
            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            // 0. Query Expansion
            var expansionPrompt = $"Sen uzman bir otomotiv teknisyenisin. Bu belirti için arama sorgusunda kullanılabilecek 3-5 teknik eş anlamlı kelime, ilgili parça adı veya olası arıza modu öner. SADECE anahtar kelimeleri virgülle ayırarak döndür, başka hiçbir metin ekleme.\n\nAraç: {request.VehicleInfo}\nBelirti: {request.Symptom}";
            var expandedTerms = await _llmClient.GenerateCompletionAsync(expansionPrompt);
            var searchContext = $"{request.VehicleInfo} {request.Symptom} {expandedTerms}";

            // 1. Generate embedding for the expanded query
            var embedding = await _llmClient.GenerateEmbeddingAsync(searchContext);

            // 2. Retrieve relevant documents (Initial Retrieval)
            var initialChunks = await _vectorStore.SearchAsync(embedding, limit: 10);

            // 3. Re-ranking
            var rankingPrompt = "Sen yardımcı bir asistansın. Verilen döküman parçalarını sorguyla olan alaka düzeyine göre sırala.\n" +
                                $"Sorgu: {request.VehicleInfo} {request.Symptom}\n\n" +
                                "İşte döküman parçaları:\n" +
                                string.Join("\n", initialChunks.Select((c, i) => $"[{i}] {c.Content.Substring(0, Math.Min(100, c.Content.Length))}...")) +
                                "\n\nEn alakalı 3 parçanın indeks numaralarını virgülle ayrılmış bir liste olarak döndür (örneğin: 0,2,5). SADECE sayıları döndür.";
            
            var rankedIndicesStr = await _llmClient.GenerateCompletionAsync(rankingPrompt);
            var rankedIndices = (rankedIndicesStr ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                                                .Where(n => n >= 0 && n < initialChunks.Count)
                                                .Take(3)
                                                .ToList();

            var finalChunks = rankedIndices.Any() 
                ? rankedIndices.Select(i => initialChunks[i]).ToList() 
                : initialChunks.Take(3).ToList(); // Fallback to top 3 if re-ranking fails

            // 4. Construct Prompt
            var context = string.Join("\n\n", finalChunks.Select(c => $"Kaynak: {c.SourceFile}\nİçerik: {c.Content}"));
            
            var systemPrompt = "Sen uzman bir otomotiv arıza teşhis asistanısın. " +
                               "Verilen bağlamı (context) kullanarak sorunu teşhis et. " +
                               "Eğer bağlamda cevap yoksa, genel bilgini kullan ancak bunun kılavuzdan olmadığını açıkça belirt. " +
                               "Cevabını 'Olası Sebepler' ve 'Çözüm Adımları' başlıklarıyla net bir şekilde formatla. " +
                               "ÖNEMLİ: Her zaman Türkçe yanıt ver.";

            var userPrompt = $"Araç: {request.VehicleInfo}\nBelirti: {request.Symptom}\n\nBağlam:\n{context}";

            // 5. Generate Response
            var response = await _llmClient.GenerateCompletionAsync(userPrompt, systemPrompt);

            return Ok(new DiagnosisResult
            {
                Diagnosis = response,
                RelatedDocuments = finalChunks.Select(c => c.SourceFile).Distinct().ToList()
            });
        }
    }
}
