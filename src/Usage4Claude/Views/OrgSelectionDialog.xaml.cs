using System.Windows;
using System.Windows.Controls;
using Usage4Claude.Models;

namespace Usage4Claude.Views;

/// <summary>
/// Dialog for selecting an organization when multiple are available.
/// </summary>
public partial class OrgSelectionDialog : Window
{
    /// <summary>
    /// The organization selected by the user, or null if cancelled.
    /// </summary>
    public Organization? SelectedOrganization { get; private set; }

    public OrgSelectionDialog(List<Organization> organizations)
    {
        InitializeComponent();
        OrgListBox.ItemsSource = organizations;
    }

    private void OrgListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        OkButton.IsEnabled = OrgListBox.SelectedItem != null;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedOrganization = OrgListBox.SelectedItem as Organization;
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
