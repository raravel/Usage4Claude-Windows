using System.Windows;
using System.Windows.Controls;

namespace Usage4Claude.Views.Tabs;

/// <summary>
/// Auth settings tab for managing Claude accounts.
/// Provides code-behind for PasswordBox interactions (WPF PasswordBox cannot be directly bound).
/// Binds to SettingsViewModel which is inherited from the parent SettingsWindow DataContext.
/// </summary>
public partial class AuthSettingsTab : UserControl
{
    private bool _isEditSessionKeyVisible;
    private bool _suppressPasswordSync;

    public AuthSettingsTab()
    {
        InitializeComponent();

        // When the selected account changes, update the edit PasswordBox
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.NewValue is ViewModels.SettingsViewModel vm)
        {
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(vm.EditSessionKey) && !_suppressPasswordSync)
                {
                    _suppressPasswordSync = true;
                    EditSessionKeyBox.Password = vm.EditSessionKey;
                    _suppressPasswordSync = false;
                }
            };
        }
    }

    /// <summary>
    /// Sync new session key PasswordBox value to ViewModel.
    /// </summary>
    private void NewSessionKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is ViewModels.SettingsViewModel vm)
        {
            vm.NewSessionKey = NewSessionKeyBox.Password;
        }
    }

    /// <summary>
    /// Sync edit session key PasswordBox value to ViewModel.
    /// </summary>
    private void EditSessionKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_suppressPasswordSync) return;

        if (DataContext is ViewModels.SettingsViewModel vm)
        {
            _suppressPasswordSync = true;
            vm.EditSessionKey = EditSessionKeyBox.Password;
            _suppressPasswordSync = false;
        }
    }

    /// <summary>
    /// Toggle between PasswordBox (masked) and TextBox (visible) for the edit session key.
    /// </summary>
    private void ToggleEditSessionKeyVisibility_Click(object sender, RoutedEventArgs e)
    {
        _isEditSessionKeyVisible = !_isEditSessionKeyVisible;

        if (_isEditSessionKeyVisible)
        {
            // Show plain text
            EditSessionKeyTextBox.Visibility = Visibility.Visible;
            EditSessionKeyBox.Visibility = Visibility.Collapsed;
            if (sender is Button btn) btn.Content = "Hide";
        }
        else
        {
            // Show masked
            EditSessionKeyBox.Visibility = Visibility.Visible;
            EditSessionKeyTextBox.Visibility = Visibility.Collapsed;
            if (sender is Button btn) btn.Content = "Show";
        }
    }
}
