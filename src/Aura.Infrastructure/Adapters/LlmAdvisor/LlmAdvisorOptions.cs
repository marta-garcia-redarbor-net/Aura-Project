namespace Aura.Infrastructure.Adapters.LlmAdvisor;

/// <summary>
/// Configuration for LLM decision advisory.
/// </summary>
public sealed class LlmAdvisorOptions
{
    public const string SectionName = "LlmAdvisor";

    public bool Enabled { get; set; }

    public int TimeoutSeconds { get; set; } = 10;

    public double ConfidenceThreshold { get; set; } = 0.7;

    /// <summary>
    /// Chat provider used by the advisor runtime.
    /// Currently supported: Ollama.
    /// </summary>
    public string Provider { get; set; } = "Ollama";

    /// <summary>
    /// Optional chat endpoint override for advisor runtime.
    /// Falls back to EmbeddingProvider:Endpoint when omitted.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Explicit chat model id for advisor runtime.
    /// Must point to a chat/instruct model and MUST NOT reuse embedding deployment names.
    /// </summary>
    public string? ModelId { get; set; }
}
