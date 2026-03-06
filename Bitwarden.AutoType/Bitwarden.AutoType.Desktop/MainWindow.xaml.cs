using MahApps.Metro.Controls;
using System.Windows;

namespace Bitwarden.AutoType.Desktop;

public partial class MainWindow : MetroWindow
{
    public const int ItemsTabIndex = 0;
    public const int SettingsTabIndex = 3;

    public MainWindow(AutoTypeViewModel viewModel)
    {
        DataContext = viewModel;
        InitializeComponent();
    }

    public void PositionWindow()
    {
        var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
        Left = desktopWorkingArea.Right - Width - 5;
        Top = desktopWorkingArea.Bottom - Height - 5;
    }

    public void ShowTab(int tabIndex)
    {
        MainTabControl.SelectedIndex = tabIndex;
        Visibility = Visibility.Visible;
        Activate();
    }
}