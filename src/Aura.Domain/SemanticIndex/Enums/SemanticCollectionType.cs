namespace Aura.Domain.SemanticIndex.Enums;

/// <summary>
/// Segregates semantic data by volatility and purpose.
/// </summary>
public enum SemanticCollectionType
{
    /// <summary>Stable evidence: architectural decisions, project knowledge.</summary>
    ProjectKnowledge,

    /// <summary>Fast-moving context: PR updates, triage events.</summary>
    ActivityMemory
}
