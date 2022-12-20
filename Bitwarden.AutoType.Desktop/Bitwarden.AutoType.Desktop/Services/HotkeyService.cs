using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Windows;

namespace Bitwarden.AutoType.Desktop.Services;

public class HotkeyService : WPFBackgroundService
{
    private WindowsHotKey _hotKeyNew;

    public HotkeyService()
    {
        _hotKeyNew = new WindowsHotKey(VirtualKeys.A, KeyModifier.Ctrl | KeyModifier.Alt, TakAction);
        var success = _hotKeyNew.RegisterHotKey();
    }

    private void TakAction(WindowsHotKey hotKey)
    {
        MessageBox.Show("OH YEAH");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        base.Dispose();
        _hotKeyNew?.Dispose();
    }
}