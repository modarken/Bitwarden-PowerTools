using System.Windows;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Views;

/// <summary>
/// Dialog for entering a backup password.
/// </summary>
public partial class BackupPasswordDialog : MetroWindow
{
    public string? Password { get; private set; }

    public BackupPasswordDialog()
    {
        InitializeComponent();
        PasswordInput.Focus();
    }

    private void BackupButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(PasswordInput.Password))
        {
            MessageBox.Show("Please enter a password.", "Password Required", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (PasswordInput.Password.Length < 4)
        {
            MessageBox.Show("Password must be at least 4 characters.", "Password Too Short", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        Password = PasswordInput.Password;
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
