using System.Windows;
using System.Windows.Controls;

namespace Bitwarden.AutoType.Desktop.Views
{
    /// <summary>
    /// Interaction logic for SettingsControl.xaml
    /// </summary>
    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            AccessMethodComboBox_SelectionChanged(null, null);
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        private void AccessMethodComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ClientIDTextBlock == null || ClientIDTextBox == null ||
                ClientSecretTextBlock == null || ClientSecretTextBox == null ||
                TOTPTextBlock == null || TOTPTextBox == null)
            {
                return;
            }

            if (AccessMethodComboBox.SelectedIndex == 0)
            {
                ClientIDTextBlock.Visibility = Visibility.Visible;
                ClientIDTextBox.Visibility = Visibility.Visible;
                ClientSecretTextBlock.Visibility = Visibility.Visible;
                ClientSecretTextBox.Visibility = Visibility.Visible;
                TOTPTextBlock.Visibility = Visibility.Collapsed;
                TOTPTextBox.Visibility = Visibility.Collapsed;
            }
            else
            {
                ClientIDTextBlock.Visibility = Visibility.Collapsed;
                ClientIDTextBox.Visibility = Visibility.Collapsed;
                ClientSecretTextBlock.Visibility = Visibility.Collapsed;
                ClientSecretTextBox.Visibility = Visibility.Collapsed;
                TOTPTextBlock.Visibility = Visibility.Visible;
                TOTPTextBox.Visibility = Visibility.Visible;
            }
        }
    }
}