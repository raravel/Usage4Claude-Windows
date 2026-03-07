using System.Diagnostics;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

/// <summary>
/// Manages user accounts, coordinating between CredentialService (secure storage)
/// and SettingsService (current account selection).
/// </summary>
public class AccountManager
{
    private readonly CredentialService _credentialService;
    private readonly SettingsService _settingsService;

    private List<Account> _accounts = new();

    /// <summary>
    /// All registered accounts.
    /// </summary>
    public IReadOnlyList<Account> Accounts => _accounts.AsReadOnly();

    /// <summary>
    /// The currently active account, or null if none selected.
    /// </summary>
    public Account? CurrentAccount
    {
        get
        {
            var currentId = _settingsService.Settings.CurrentAccountId;
            if (currentId == null) return _accounts.FirstOrDefault();
            if (Guid.TryParse(currentId, out var guid))
            {
                return _accounts.FirstOrDefault(a => a.Id == guid) ?? _accounts.FirstOrDefault();
            }
            return _accounts.FirstOrDefault();
        }
    }

    /// <summary>
    /// Whether any accounts are configured.
    /// </summary>
    public bool HasAccounts => _accounts.Count > 0;

    /// <summary>
    /// Fired when the current account changes (switch, add first, or remove current).
    /// </summary>
    public event EventHandler<Account?>? CurrentAccountChanged;

    /// <summary>
    /// Fired when accounts are added or removed (for UI elements like tray menu that need rebuilding).
    /// </summary>
    public event EventHandler? AccountListChanged;

    public AccountManager(CredentialService credentialService, SettingsService settingsService)
    {
        _credentialService = credentialService;
        _settingsService = settingsService;
        LoadAccounts();
    }

    /// <summary>
    /// Load accounts from secure storage.
    /// </summary>
    public void LoadAccounts()
    {
        var accounts = _credentialService.LoadAccounts();
        _accounts = accounts ?? new List<Account>();
    }

    /// <summary>
    /// Add a new account and save to secure storage.
    /// If an account with the same OrganizationId already exists, its session key is updated.
    /// </summary>
    public bool AddAccount(string sessionKey, string organizationId, string organizationName, string? alias = null)
    {
        var account = new Account
        {
            SessionKey = sessionKey,
            OrganizationId = organizationId,
            OrganizationName = organizationName,
            Alias = alias
        };

        // Check for duplicate organization
        var existing = _accounts.FindIndex(a => a.OrganizationId == organizationId);
        if (existing >= 0)
        {
            // Update existing account's session key
            _accounts[existing].SessionKey = sessionKey;
            _accounts[existing].OrganizationName = organizationName;
            if (alias != null) _accounts[existing].Alias = alias;
        }
        else
        {
            _accounts.Add(account);
        }

        // Auto-select if first account
        if (_accounts.Count == 1)
        {
            _settingsService.Settings.CurrentAccountId = _accounts[0].Id.ToString();
            _settingsService.Save();
            CurrentAccountChanged?.Invoke(this, _accounts[0]);
        }

        var saved = _credentialService.SaveAccounts(_accounts);
        if (saved)
        {
            AccountListChanged?.Invoke(this, EventArgs.Empty);
        }
        return saved;
    }

    /// <summary>
    /// Remove an account by ID and save.
    /// </summary>
    public bool RemoveAccount(Guid accountId)
    {
        var removed = _accounts.RemoveAll(a => a.Id == accountId);
        if (removed == 0) return false;

        // If removed the current account, switch to first available
        if (_settingsService.Settings.CurrentAccountId == accountId.ToString())
        {
            _settingsService.Settings.CurrentAccountId = _accounts.FirstOrDefault()?.Id.ToString();
            _settingsService.Save();
            CurrentAccountChanged?.Invoke(this, CurrentAccount);
        }

        var saved = _credentialService.SaveAccounts(_accounts);
        if (saved)
        {
            AccountListChanged?.Invoke(this, EventArgs.Empty);
        }
        return saved;
    }

    /// <summary>
    /// Switch to a different account by ID.
    /// </summary>
    public bool SwitchAccount(Guid accountId)
    {
        if (!_accounts.Any(a => a.Id == accountId)) return false;

        _settingsService.Settings.CurrentAccountId = accountId.ToString();
        _settingsService.Save();
        CurrentAccountChanged?.Invoke(this, CurrentAccount);
        return true;
    }

    /// <summary>
    /// Update an existing account's alias.
    /// </summary>
    public bool UpdateAlias(Guid accountId, string? alias)
    {
        var account = _accounts.FirstOrDefault(a => a.Id == accountId);
        if (account == null) return false;

        account.Alias = alias;
        return _credentialService.SaveAccounts(_accounts);
    }

    /// <summary>
    /// Delete all accounts and credentials.
    /// </summary>
    public bool DeleteAll()
    {
        _accounts.Clear();
        _settingsService.Settings.CurrentAccountId = null;
        _settingsService.Save();
        return _credentialService.DeleteAll();
    }
}
