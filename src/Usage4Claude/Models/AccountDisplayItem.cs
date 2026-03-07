namespace Usage4Claude.Models;

/// <summary>
/// Display wrapper around Account for use in ListBox binding.
/// Provides computed properties like IsCurrent and TruncatedOrgId.
/// </summary>
public class AccountDisplayItem
{
    private readonly Account _account;

    public AccountDisplayItem(Account account, bool isCurrent)
    {
        _account = account;
        IsCurrent = isCurrent;
    }

    /// <summary>The underlying account.</summary>
    public Account Account => _account;

    /// <summary>Account unique ID.</summary>
    public Guid Id => _account.Id;

    /// <summary>Display name (alias or organization name).</summary>
    public string DisplayName => _account.DisplayName;

    /// <summary>Full organization ID.</summary>
    public string OrganizationId => _account.OrganizationId;

    /// <summary>Session key (for editing).</summary>
    public string SessionKey => _account.SessionKey;

    /// <summary>Alias (for editing).</summary>
    public string? Alias => _account.Alias;

    /// <summary>Whether this is the currently active account.</summary>
    public bool IsCurrent { get; }

    /// <summary>Truncated org ID for display (first 8 chars + "...").</summary>
    public string TruncatedOrgId
    {
        get
        {
            if (string.IsNullOrEmpty(_account.OrganizationId)) return "(no org ID)";
            if (_account.OrganizationId.Length <= 12) return _account.OrganizationId;
            return _account.OrganizationId[..12] + "...";
        }
    }
}
