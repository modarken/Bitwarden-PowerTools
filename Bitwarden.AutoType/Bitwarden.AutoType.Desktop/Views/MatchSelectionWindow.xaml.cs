using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
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
        if (value is CachedAutoTypeEntry entry)
        {
            return $"{entry.Field.Name}   {entry.Field.UserName}   {entry.Field.Sequence}   {entry.Field.Target}";
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
public partial class MatchSelectionWindow : MetroWindow, IDisposable
{
    // private bool _properlyDeactivated = false;
    public CachedAutoTypeEntry? SelectedMatch { get; private set; }

    public MatchSelectionWindow(IEnumerable<CachedAutoTypeEntry> matches)
    {
        DataContext = this;
        InitializeComponent();
        MatchListBox.ItemsSource = matches;

        this.Loaded += (sender, e) =>
        {
            this.Activate(); // this was found to be necessary to bring the window to the front in certain situations (e.g. when the user has clicked on a different window after this window was already displayed)
        };
        //this.Deactivated += MatchSelectionWindow_Deactivated;
    }

    //private void MatchSelectionWindow_Deactivated(object? sender, EventArgs e)
    //{
    //    if (_properlyDeactivated == false)
    //    {
    //        DialogResult = false;
    //    }
    //}

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (MatchListBox.SelectedItem is CachedAutoTypeEntry selectedMatch)
        {
            SelectedMatch = selectedMatch;
            //_properlyDeactivated = true;
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please select a match.", "No Match Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        //_properlyDeactivated = true;
        DialogResult = false;
    }

    [RelayCommand]
    private void SelectedMatchDoubleClicked()
    {
        if (MatchListBox.SelectedItem is CachedAutoTypeEntry selectedMatch)
        {
            SelectedMatch = selectedMatch;
            //_properlyDeactivated = true;
            DialogResult = true;
        }
    }

    [RelayCommand]
    private void NumberClicked(int key)
    {
        if (key >= 1 && key <= MatchListBox.Items.Count)
        {
            if (MatchListBox.Items[key - 1] is CachedAutoTypeEntry selectedMatch)
            {
                SelectedMatch = selectedMatch;
                //_properlyDeactivated = true;
                DialogResult = true;
            }
        }
    }

    private void ExecuteButton_Click(object sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        if (button == null) { return; }
        var matchItem = button.CommandParameter;

        if (matchItem is CachedAutoTypeEntry selectedMatch)
        {
            SelectedMatch = selectedMatch;
            //_propertyDeactivated = true;
            DialogResult = true;
        }
    }

    public void Dispose()
    {
        this.Close();
    }
}