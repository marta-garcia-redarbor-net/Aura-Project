namespace Aura.Application.Ports;

/// <summary>
/// Store for dynamically managed alert rules (VIP senders and keywords).
/// Used by interruption rules to look up configuration at evaluation time.
/// </summary>
public interface IAlertRuleStore
{
    /// <summary>Returns the list of VIP sender email addresses.</summary>
    Task<IReadOnlyList<string>> GetVipSendersAsync(CancellationToken ct);

    /// <summary>Returns the list of alert trigger keywords.</summary>
    Task<IReadOnlyList<string>> GetKeywordsAsync(CancellationToken ct);

    /// <summary>Adds a VIP sender email address.</summary>
    Task AddVipSenderAsync(string email, string addedBy, CancellationToken ct);

    /// <summary>Removes a VIP sender email address.</summary>
    Task RemoveVipSenderAsync(string email, CancellationToken ct);

    /// <summary>Adds an alert trigger keyword.</summary>
    Task AddKeywordAsync(string keyword, string addedBy, CancellationToken ct);

    /// <summary>Removes an alert trigger keyword.</summary>
    Task RemoveKeywordAsync(string keyword, CancellationToken ct);
}
