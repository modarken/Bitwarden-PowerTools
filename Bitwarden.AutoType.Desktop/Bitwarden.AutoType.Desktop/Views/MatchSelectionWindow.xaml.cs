using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using Bitwarden.Core.Models;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Views
{
    //public class ToStringConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return value?.ToString()!;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}

    public class ToAutoTypeCustomFieldConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            if (value is KeyValuePair<AutoTypeCustomField, Cipher> valueKeyValuePair)
            {
                return $"{ valueKeyValuePair.Key.Name }   {valueKeyValuePair.Key.UserName}   { valueKeyValuePair.Key.Target }   {valueKeyValuePair.Key.Sequence }";
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
    }
}