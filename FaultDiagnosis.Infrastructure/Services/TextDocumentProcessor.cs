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
        private const int MaxChunkSize = 1000;
        private readonly string[] _separators = new[] { "\r\n\r\n", "\n\n", "\r\n", "\n", ". ", " ", "" };

        public async Task<List<DocumentChunk>> ProcessFileAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var text = await File.ReadAllTextAsync(filePath);
            if (string.IsNullOrWhiteSpace(text))
            {
                return new List<DocumentChunk>();
            }

            var textChunks = SplitText(text, MaxChunkSize);

            var chunks = new List<DocumentChunk>();
            foreach (var content in textChunks)
            {
                var chunk = new DocumentChunk
                {
                    Content = content.Trim(),
                    SourceFile = Path.GetFileName(filePath)
                };

                // Metadata Extraction
                // 1. Title (First line of the chunk, simplified)
                var firstLine = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(firstLine))
                {
                    chunk.Metadata["title"] = firstLine!.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
                }

                // 2. Error Codes (Regex for standard DTCs like P0123, U1000, B1234, C0123)
                var errorCodes = System.Text.RegularExpressions.Regex.Matches(content, @"\b[PBUC][0-9]{4}\b");
                if (errorCodes.Count > 0)
                {
                    chunk.Metadata["error_codes"] = string.Join(",", errorCodes.Select(m => m.Value).Distinct());
                }

                chunks.Add(chunk);
            }

            return chunks;
        }

        private List<string> SplitText(string text, int maxLength)
        {
            var finalChunks = new List<string>();
            SplitRecursive(text, maxLength, 0, finalChunks);
            return finalChunks;
        }

        private void SplitRecursive(string text, int maxLength, int separatorIndex, List<string> chunks)
        {
            if (text.Length <= maxLength)
            {
                chunks.Add(text);
                return;
            }

            if (separatorIndex >= _separators.Length)
            {
                // Fallback: Fixed size split
                for (int i = 0; i < text.Length; i += maxLength)
                {
                    chunks.Add(text.Substring(i, Math.Min(maxLength, text.Length - i)));
                }
                return;
            }

            var separator = _separators[separatorIndex];
            var parts = text.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            var currentChunk = "";

            foreach (var part in parts)
            {
                var nextChunk = string.IsNullOrEmpty(currentChunk) ? part : currentChunk + separator + part;

                if (nextChunk.Length > maxLength)
                {
                    if (!string.IsNullOrEmpty(currentChunk))
                    {
                        chunks.Add(currentChunk);
                        currentChunk = "";
                    }
                    
                    // If the part itself is too big, go deeper
                    if (part.Length > maxLength)
                    {
                        SplitRecursive(part, maxLength, separatorIndex + 1, chunks);
                    }
                    else
                    {
                        currentChunk = part;
                    }
                }
                else
                {
                    currentChunk = nextChunk;
                }
            }

            if (!string.IsNullOrEmpty(currentChunk))
            {
                chunks.Add(currentChunk);
            }
        }
    }
}
