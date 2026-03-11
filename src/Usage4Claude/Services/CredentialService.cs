using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Serilog;
using Usage4Claude.Models;

namespace Usage4Claude.Services;

/// <summary>
/// Manages secure storage of account credentials using Windows DPAPI.
/// Credentials are encrypted per-user and stored in %APPDATA%\Usage4Claude\credentials.dat
/// </summary>
public class CredentialService
{
    private static readonly string AppDataPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Usage4Claude");

    private static readonly string CredentialsFilePath = Path.Combine(AppDataPath, "credentials.dat");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Save accounts securely. Encrypts account data with DPAPI (CurrentUser scope).
    /// </summary>
    public bool SaveAccounts(List<Account> accounts)
    {
        try
        {
            Directory.CreateDirectory(AppDataPath);

            var json = JsonSerializer.Serialize(accounts, JsonOptions);
            var plainBytes = Encoding.UTF8.GetBytes(json);
            var encryptedBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);

            File.WriteAllBytes(CredentialsFilePath, encryptedBytes);
            return true;
        }
        catch (CryptographicException ex)
        {
            Log.Error(ex, "[CredentialService] Encryption failed");
            return false;
        }
        catch (IOException ex)
        {
            Log.Error(ex, "[CredentialService] File write failed");
            return false;
        }
    }

    /// <summary>
    /// Load accounts from secure storage. Decrypts with DPAPI.
    /// </summary>
    public List<Account>? LoadAccounts()
    {
        try
        {
            if (!File.Exists(CredentialsFilePath))
                return null;

            var encryptedBytes = File.ReadAllBytes(CredentialsFilePath);
            var plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(plainBytes);

            return JsonSerializer.Deserialize<List<Account>>(json, JsonOptions);
        }
        catch (CryptographicException ex)
        {
            Log.Error(ex, "[CredentialService] Decryption failed");
            return null;
        }
        catch (IOException ex)
        {
            Log.Error(ex, "[CredentialService] File read failed");
            return null;
        }
        catch (JsonException)
        {
            Log.Error("[CredentialService] Credential deserialization failed (data corrupted or format changed)");
            return null;
        }
    }

    /// <summary>
    /// Delete all stored credentials.
    /// </summary>
    public bool DeleteAll()
    {
        try
        {
            if (File.Exists(CredentialsFilePath))
            {
                File.Delete(CredentialsFilePath);
            }
            return true;
        }
        catch (IOException ex)
        {
            Log.Error(ex, "[CredentialService] Delete failed");
            return false;
        }
    }

    /// <summary>
    /// Check if credentials file exists.
    /// </summary>
    public bool HasStoredCredentials() => File.Exists(CredentialsFilePath);
}
