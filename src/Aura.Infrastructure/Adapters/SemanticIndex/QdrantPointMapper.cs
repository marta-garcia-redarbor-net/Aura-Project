using Aura.Application.Models;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;
using Google.Protobuf.Collections;
using Qdrant.Client.Grpc;

namespace Aura.Infrastructure.Adapters.Ingestion.SemanticIndex;

/// <summary>
/// Pure mapping functions between domain types and Qdrant SDK types.
/// Kept static and side-effect-free for direct unit testing.
/// </summary>
internal static class QdrantPointMapper
{
    public static PointStruct ToPointStruct(SemanticChunk chunk, ReadOnlyMemory<float> embedding)
    {
        var vector = new Qdrant.Client.Grpc.Vector();
        vector.Data.AddRange(embedding.ToArray());

        var point = new PointStruct
        {
            Id = new PointId { Uuid = chunk.Id.ToString() },
            Vectors = new Vectors { Vector = vector },
        };

        point.Payload["canonical_source_id"] = chunk.CanonicalSourceId;
        point.Payload["content"] = chunk.Content;
        point.Payload["collection"] = chunk.Collection.ToString();
        point.Payload["created_at"] = chunk.CreatedAt.ToString("O");

        var tagsList = new ListValue();
        foreach (var tag in chunk.Tags)
        {
            tagsList.Values.Add(new Value { StringValue = tag.ToString() });
        }
        point.Payload["tags"] = new Value { ListValue = tagsList };

        return point;
    }

    public static ScoredSemanticChunk ToScoredSemanticChunk(
        PointId pointId,
        MapField<string, Value> payload,
        float score)
    {
        var id = Guid.Parse(pointId.Uuid);
        var canonicalSourceId = payload["canonical_source_id"].StringValue;
        var content = payload["content"].StringValue;
        var collectionStr = payload["collection"].StringValue;
        var collection = Enum.Parse<SemanticCollectionType>(collectionStr);
        var createdAt = DateTimeOffset.Parse(payload["created_at"].StringValue, System.Globalization.CultureInfo.InvariantCulture);

        var tags = new List<DomainTag>();
        if (payload.TryGetValue("tags", out var tagsValue))
        {
            foreach (var tagValue in tagsValue.ListValue.Values)
            {
                var parts = tagValue.StringValue.Split(':', 2);
                if (parts.Length == 2)
                {
                    tags.Add(new DomainTag(parts[0], parts[1]));
                }
            }
        }

        var chunk = new SemanticChunk(id, canonicalSourceId, content, collection, tags, createdAt);

        return new ScoredSemanticChunk
        {
            Chunk = chunk,
            Score = score
        };
    }
}
