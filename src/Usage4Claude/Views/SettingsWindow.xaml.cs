using System.Windows;

namespace Usage4Claude.Views;

/// <summary>
/// Settings window with tabbed navigation (General, Auth, About).
/// Uses custom RadioButton-based tab switching for a toolbar-style appearance.
/// </summary>
public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Navigate to a specific tab by index.
    /// 0 = General, 1 = Auth, 2 = About.
    /// </summary>
    public void NavigateToTab(int tabIndex)
    {
        switch (tabIndex)
        {
            case 1:
                AuthTab.IsChecked = true;
                break;
            case 2:
                AboutTab.IsChecked = true;
                break;
            default:
                GeneralTab.IsChecked = true;
                break;
        }
    }
}
