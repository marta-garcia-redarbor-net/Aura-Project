using System.Text.RegularExpressions;
using Aura.Application.Ports;
using Aura.Domain.SemanticIndex.Enums;
using Aura.Domain.SemanticIndex.ValueObjects;

namespace Aura.Application.Services;

/// <summary>
/// Minimal V1 implementation of <see cref="ISemanticChunkExtractor"/>.
/// Performs paragraph-based chunk splitting, regex PII stripping, and basic domain tagging.
/// <para>
/// This is a minimal implementation suitable for bootstrapping the semantic index pipeline.
/// Future hardening: configurable chunk sizes, NLP-aware splitting, ML-based PII detection,
/// richer tag inference, and overlap/sliding-window strategies.
/// </para>
/// </summary>
public sealed class BasicSemanticChunkExtractor : ISemanticChunkExtractor
{
    /// <summary>Maximum characters per chunk before splitting.</summary>
    internal const int MaxChunkSize = 1000;

    // PII patterns — minimal regex set for V1
    private static readonly Regex EmailPattern = new(
        @"[a-zA-Z0-9._%+\-]+@[a-zA-Z0-9.\-]+\.[a-zA-Z]{2,}",
        RegexOptions.Compiled);

    private static readonly Regex SsnPattern = new(
        @"\b\d{3}-\d{2}-\d{4}\b",
        RegexOptions.Compiled);

    private static readonly Regex PhonePattern = new(
        @"(\+?1[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b",
        RegexOptions.Compiled);

    private static readonly string[] ParagraphSeparators = ["\n\n", "\r\n\r\n"];

    /// <inheritdoc />
    public Task<IReadOnlyList<SemanticChunk>> ExtractAsync(
        string canonicalSourceId,
        string content,
        SemanticCollectionType target,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(canonicalSourceId))
            throw new ArgumentException("Canonical source ID must not be null or empty.", nameof(canonicalSourceId));

        if (string.IsNullOrEmpty(content))
            return Task.FromResult<IReadOnlyList<SemanticChunk>>(Array.Empty<SemanticChunk>());

        // 1. Strip PII before any chunking
        var sanitized = StripPii(content);

        // 2. Split into chunks
        var segments = SplitIntoChunks(sanitized);

        // 3. Build SemanticChunk for each segment with basic tags
        var tags = new List<DomainTag> { new("collection", target.ToString()) };
        var chunks = new List<SemanticChunk>(segments.Count);

        foreach (var segment in segments)
        {
            if (string.IsNullOrWhiteSpace(segment))
                continue;

            chunks.Add(new SemanticChunk(
                Guid.NewGuid(),
                canonicalSourceId,
                segment.Trim(),
                target,
                tags,
                DateTimeOffset.UtcNow));
        }

        return Task.FromResult<IReadOnlyList<SemanticChunk>>(chunks);
    }

    /// <summary>Strips known PII patterns and replaces with [REDACTED].</summary>
    internal static string StripPii(string text)
    {
        text = EmailPattern.Replace(text, "[REDACTED]");
        text = SsnPattern.Replace(text, "[REDACTED]");
        text = PhonePattern.Replace(text, "[REDACTED]");
        return text;
    }

    /// <summary>
    /// Splits content into segments by paragraph boundaries first, then by size limit.
    /// </summary>
    internal static List<string> SplitIntoChunks(string content)
    {
        // Split by double newline (paragraph boundary)
        var paragraphs = content.Split(ParagraphSeparators, StringSplitOptions.RemoveEmptyEntries);

        var chunks = new List<string>();

        foreach (var paragraph in paragraphs)
        {
            if (paragraph.Length <= MaxChunkSize)
            {
                chunks.Add(paragraph);
            }
            else
            {
                // Split oversized paragraphs by MaxChunkSize
                for (var i = 0; i < paragraph.Length; i += MaxChunkSize)
                {
                    var length = Math.Min(MaxChunkSize, paragraph.Length - i);
                    chunks.Add(paragraph.Substring(i, length));
                }
            }
        }

        return chunks;
    }
}
