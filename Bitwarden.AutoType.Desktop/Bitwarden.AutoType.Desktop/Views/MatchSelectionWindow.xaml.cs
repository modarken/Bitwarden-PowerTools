using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Bitwarden.Core.Models;
using CommunityToolkit.Mvvm.Input;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Views;

public class ItemIndexConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ListBoxItem listBoxItem)
        {
            var listBox = ItemsControl.ItemsControlFromItemContainer(listBoxItem);
            var index = listBox.ItemContainerGenerator.IndexFromContainer(listBoxItem);
            return index + 1;
        }

#pragma warning disable CS8603 // Possible null reference return.
        return null;
#pragma warning restore CS8603 // Possible null reference return.
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public class ToAutoTypeCustomFieldConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is KeyValuePair<AutoTypeCustomField, Cipher> valueKeyValuePair)
        {
            return $"{valueKeyValuePair.Key.Name}   {valueKeyValuePair.Key.UserName}   {valueKeyValuePair.Key.Sequence}   {valueKeyValuePair.Key.Target}";
        }

        return value?.ToString()!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Interaction logic for MatchSelectionWindow.xaml
/// </summary>
public partial class MatchSelectionWindow : MetroWindow
{
    public KeyValuePair<AutoTypeCustomField, Cipher>? SelectedMatch { get; private set; }

    public MatchSelectionWindow(IEnumerable<KeyValuePair<AutoTypeCustomField, Cipher>> matches)
    {
        DataContext = this;
        InitializeComponent();
        MatchListBox.ItemsSource = matches;
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (MatchListBox.SelectedItem is KeyValuePair<AutoTypeCustomField, Cipher> selectedMatch)
        {
            SelectedMatch = selectedMatch;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select a match.", "No Match Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    [RelayCommand]
    private void SelectedMatchDoubleClicked()
    {
        if (MatchListBox.SelectedItem is KeyValuePair<AutoTypeCustomField, Cipher> selectedMatch)
        {
            SelectedMatch = selectedMatch;
            DialogResult = true;
        }
    }

    [RelayCommand]
    private void NumberClicked(int key)
    {
        if (key >= 1 && key <= MatchListBox.Items.Count)
        {
            if (MatchListBox.Items[key - 1] is KeyValuePair<AutoTypeCustomField, Cipher> selectedMatch)
            {
                SelectedMatch = selectedMatch;
                DialogResult = true;
            }
        }
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) { return; }
        var matchItem = button.CommandParameter;

        if (matchItem is KeyValuePair<AutoTypeCustomField, Cipher> selectedMatch)
        {
            SelectedMatch = selectedMatch;
            DialogResult = true;
        }
    }
}