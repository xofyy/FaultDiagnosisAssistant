using System.Collections.Generic;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Entities;

namespace FaultDiagnosis.Core.Interfaces
{
    public interface IVectorStore
    {
        Task UpsertAsync(DocumentChunk chunk);
        Task<List<DocumentChunk>> SearchAsync(float[] vector, int limit = 3);
        Task CreateCollectionIfNotExistsAsync();
    }
}
