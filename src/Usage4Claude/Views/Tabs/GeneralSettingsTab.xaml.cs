using System.Windows.Controls;

namespace Usage4Claude.Views.Tabs;

/// <summary>
/// General settings tab containing display, refresh, appearance, and system settings.
/// Binds to SettingsViewModel which is inherited from the parent SettingsWindow DataContext.
/// </summary>
public partial class GeneralSettingsTab : UserControl
{
    public GeneralSettingsTab()
    {
        InitializeComponent();
    }
}
