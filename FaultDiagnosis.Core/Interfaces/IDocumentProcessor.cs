using System.Collections.Generic;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Entities;

namespace FaultDiagnosis.Core.Interfaces
{
    public interface IDocumentProcessor
    {
        Task<List<DocumentChunk>> ProcessFileAsync(string filePath);
    }
}
