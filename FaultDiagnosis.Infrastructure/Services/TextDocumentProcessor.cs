using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;

namespace FaultDiagnosis.Infrastructure.Services
{
    public class TextDocumentProcessor : IDocumentProcessor
    {
        private const int ChunkSize = 1000;
        private const int Overlap = 100;

        public async Task<List<DocumentChunk>> ProcessFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var text = await File.ReadAllTextAsync(filePath);
            var chunks = new List<DocumentChunk>();
            
            for (int i = 0; i < text.Length; i += (ChunkSize - Overlap))
            {
                var length = Math.Min(ChunkSize, text.Length - i);
                var content = text.Substring(i, length);

                chunks.Add(new DocumentChunk
                {
                    Content = content,
                    SourceFile = Path.GetFileName(filePath)
                });

                if (i + length >= text.Length) break;
            }

            return chunks;
        }
    }
}
