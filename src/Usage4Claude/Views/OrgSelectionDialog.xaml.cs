using System.Windows;
using System.Windows.Controls;
using Usage4Claude.Models;

namespace Usage4Claude.Views;

/// <summary>
/// Dialog for selecting one or more organizations from the list.
/// Supports multi-select with checkboxes and a Select All option.
/// </summary>
public partial class OrgSelectionDialog : Window
{
    private readonly List<Organization> _organizations;
    private bool _updatingSelectAll;

    /// <summary>
    /// The organizations selected by the user, empty if cancelled.
    /// </summary>
    public List<Organization> SelectedOrganizations { get; private set; } = new();

    public OrgSelectionDialog(List<Organization> organizations)
    {
        InitializeComponent();
        _organizations = organizations;
        OrgListBox.ItemsSource = organizations;
    }

    private void OrgListBoxItem_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateOkButtonState();
        UpdateSelectAllCheckBoxState();
    }

    private void UpdateOkButtonState()
    {
        OkButton.IsEnabled = OrgListBox.SelectedItems.Count > 0;
    }

    private void UpdateSelectAllCheckBoxState()
    {
        if (_updatingSelectAll) return;

        _updatingSelectAll = true;
        try
        {
            var selectedCount = OrgListBox.SelectedItems.Count;
            var totalCount = _organizations.Count;

            if (selectedCount == totalCount)
                SelectAllCheckBox.IsChecked = true;
            else if (selectedCount == 0)
                SelectAllCheckBox.IsChecked = false;
            else
                SelectAllCheckBox.IsChecked = null; // Indeterminate
        }
        finally
        {
            _updatingSelectAll = false;
        }
    }

    private void SelectAllCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_updatingSelectAll) return;

        _updatingSelectAll = true;
        try
        {
            if (SelectAllCheckBox.IsChecked == true)
            {
                OrgListBox.SelectAll();
            }
            else
            {
                OrgListBox.UnselectAll();
            }
            UpdateOkButtonState();
        }
        finally
        {
            _updatingSelectAll = false;
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedOrganizations = OrgListBox.SelectedItems
            .Cast<Organization>()
            .ToList();
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
