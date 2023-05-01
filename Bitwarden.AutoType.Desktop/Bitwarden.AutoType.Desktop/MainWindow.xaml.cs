using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop;

public partial class MainWindow : MetroWindow
{
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
}