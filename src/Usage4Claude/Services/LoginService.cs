using System.Diagnostics;
using System.IO;
using System.Windows;
using Usage4Claude.Models;
using Usage4Claude.Views;

namespace Usage4Claude.Services;

/// <summary>
/// Orchestrates the browser-based login flow:
/// 1. Opens the WebView2 login window
/// 2. Extracts the session key from cookies
/// 3. Fetches organizations
/// 4. Handles org selection (auto or multi-select)
/// 5. Adds the account(s) via AccountManager
/// 6. Cleans up WebView2 user data
/// </summary>
public class LoginService
{
    private readonly ClaudeApiService _claudeApiService;
    private readonly AccountManager _accountManager;

    public LoginService(ClaudeApiService claudeApiService, AccountManager accountManager)
    {
        _claudeApiService = claudeApiService;
        _accountManager = accountManager;
    }

    /// <summary>
    /// Open the browser login window and handle the complete login flow.
    /// Returns true if at least one account was successfully added.
    /// </summary>
    public async Task<bool> StartLoginFlowAsync(Window owner)
    {
        try
        {
            // 1. Show the LoginWindow as a dialog
            var loginWindow = new LoginWindow { Owner = owner };
            var result = loginWindow.ShowDialog();

            if (result != true || string.IsNullOrEmpty(loginWindow.ExtractedSessionKey))
                return false;

            var sessionKey = loginWindow.ExtractedSessionKey;

            // 2. Fetch organizations using the session key
            List<Organization> orgs;
            try
            {
                orgs = await _claudeApiService.FetchOrganizationsAsync(sessionKey);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LoginService] Failed to fetch organizations: {ex}");
                MessageBox.Show(
                    $"Failed to fetch organizations: {ex.Message}",
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (orgs.Count == 0)
            {
                MessageBox.Show(
                    "No organizations found for this account.",
                    "Login Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            // 3. Single org: auto-add without showing the dialog
            if (orgs.Count == 1)
            {
                return _accountManager.AddAccount(sessionKey, orgs[0].Uuid, orgs[0].Name);
            }

            // 4. Multiple orgs: let user select multiple
            var selectDialog = new OrgSelectionDialog(orgs) { Owner = owner };
            if (selectDialog.ShowDialog() != true || selectDialog.SelectedOrganizations.Count == 0)
                return false;

            bool anyAdded = false;
            foreach (var org in selectDialog.SelectedOrganizations)
            {
                if (_accountManager.AddAccount(sessionKey, org.Uuid, org.Name))
                    anyAdded = true;
            }

            return anyAdded;
        }
        finally
        {
            // Always clean up WebView2 data after login flow completes
            CleanupWebView2Data();
        }
    }

    /// <summary>
    /// Removes the WebView2 user data folder to clean up any cached login data.
    /// This ensures no stale session data remains on disk after the login flow.
    /// </summary>
    private static void CleanupWebView2Data()
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Usage4Claude",
                "WebView2");

            if (Directory.Exists(userDataFolder))
                Directory.Delete(userDataFolder, recursive: true);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[LoginService] WebView2 cleanup failed: {ex.Message}");
        }
    }
}
