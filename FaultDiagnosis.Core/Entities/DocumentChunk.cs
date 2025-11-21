using System;
using System.Collections.Generic;

namespace FaultDiagnosis.Core.Entities
{
    public class DocumentChunk
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Content { get; set; } = string.Empty;
        public string SourceFile { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }
}
