using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FaultDiagnosis.Core.Entities;
using FaultDiagnosis.Core.Interfaces;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace FaultDiagnosis.Infrastructure.Qdrant
{
    public class QdrantVectorStore : IVectorStore
    {
        private readonly QdrantClient _client;
        private const string CollectionName = "fault_diagnosis";
        private const ulong VectorSize = 768; // nomic-embed-text dimension

        public QdrantVectorStore(QdrantClient client)
        {
            _client = client;
        }

        public async Task CreateCollectionIfNotExistsAsync()
        {
            var collections = await _client.ListCollectionsAsync();
            if (!collections.Contains(CollectionName))
            {
                await _client.CreateCollectionAsync(CollectionName, new VectorParams { Size = VectorSize, Distance = Distance.Cosine });
            }
        }

        public async Task UpsertAsync(DocumentChunk chunk)
        {
            if (chunk.Embedding == null) throw new ArgumentException("Embedding cannot be null");

            var point = new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = chunk.Embedding,
                Payload = 
                {
                    ["content"] = chunk.Content,
                    ["source"] = chunk.SourceFile
                }
            };

            foreach (var kvp in chunk.Metadata)
            {
                point.Payload.Add(kvp.Key, kvp.Value);
            }

            await _client.UpsertAsync(CollectionName, new[] { point });
        }

        public async Task<List<DocumentChunk>> SearchAsync(float[] vector, int limit = 3)
        {
            var results = await _client.SearchAsync(CollectionName, vector, limit: (ulong)limit);

            return results.Select(s => new DocumentChunk
            {
                Id = Guid.Parse(s.Id.Uuid),
                Content = s.Payload.TryGetValue("content", out var c) ? c.StringValue : string.Empty,
                SourceFile = s.Payload.TryGetValue("source", out var src) ? src.StringValue : string.Empty,
                // Note: We don't necessarily need to return the embedding itself for RAG
            }).ToList();
        }
    }
}
