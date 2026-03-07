using System.Diagnostics;
using System.Windows;
using Usage4Claude.Models;
using Usage4Claude.Views;

namespace Usage4Claude.Services;

/// <summary>
/// Orchestrates the browser-based login flow:
/// 1. Opens the WebView2 login window
/// 2. Extracts the session key from cookies
/// 3. Fetches organizations
/// 4. Handles org selection (auto or manual)
/// 5. Adds the account via AccountManager
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
    /// Returns true if an account was successfully added.
    /// </summary>
    public async Task<bool> StartLoginFlowAsync(Window owner)
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

        // 3. If single org, auto-add. If multiple, let user choose.
        Organization selectedOrg;
        if (orgs.Count == 1)
        {
            selectedOrg = orgs[0];
        }
        else
        {
            // Show organization selection dialog
            var selectDialog = new OrgSelectionDialog(orgs) { Owner = owner };
            if (selectDialog.ShowDialog() != true || selectDialog.SelectedOrganization == null)
                return false;
            selectedOrg = selectDialog.SelectedOrganization;
        }

        // 4. Add account
        var added = _accountManager.AddAccount(sessionKey, selectedOrg.Uuid, selectedOrg.Name);
        return added;
    }
}
