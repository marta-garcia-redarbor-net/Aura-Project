namespace Aura.Domain.SemanticIndex.ValueObjects;

/// <summary>
/// Filterable tag for semantic chunks. Value object with structural equality.
/// </summary>
public sealed record DomainTag
{
    public string Key { get; }
    public string Value { get; }

    public DomainTag(string key, string value)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        if (string.IsNullOrEmpty(value))
            throw new ArgumentException("Value must not be null or empty.", nameof(value));

        Key = key;
        Value = value;
    }

    public override string ToString() => $"{Key}:{Value}";
}
