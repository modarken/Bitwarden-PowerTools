using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Services;

public class HotkeyService : IDisposable
{
    private readonly List<Action<WindowsHotKey>> _hotKeyActions;
    private readonly AutoTypeSettings _autoTypeSettings;
    private readonly Action<AutoTypeSettings> _save;
    private WindowsHotKey? _hotKeyNew;
    private bool _isEnabled = false;
    private bool _isActive = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public event EventHandler<bool>? OnActiveChanged;

    public bool IsActive => _isActive;
    public bool IsEnabled => _isEnabled;

    public HotkeyService()
    {
        _hotKeyActions = new List<Action<WindowsHotKey>>();
        _autoTypeSettings = new AutoTypeSettings();
        _save = new Action<AutoTypeSettings>((a) => { });
    }

    public HotkeyService(AutoTypeSettings autoTypeSettings, Action<AutoTypeSettings> save)
    {
        _hotKeyActions = new List<Action<WindowsHotKey>>();
        _autoTypeSettings = autoTypeSettings;
        _save = save;
        StartCheckingHotkey();
    }

    public void StartCheckingHotkey()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(async () =>
        {
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                if (_isEnabled && !_isActive)
                {
                    CheckHotkey();
                }
                await Task.Delay(5000); // Wait for 1 second before checking again
            }
        }, _cancellationTokenSource.Token);
    }

    public void StopCheckingHotkey()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void CheckHotkey()
    {
        if (IsEnabled)
        {
            Disable();
            Enable();
        }
    }

    public WindowsHotKey? GetHotkey()
    {
        return _autoTypeSettings?.WindowsHotKey;
    }

    public void SetHotKey(WindowsHotKey hotKey)
    {
        _autoTypeSettings.WindowsHotKey = hotKey;
        _save(_autoTypeSettings);
    }

    public void Enable()
    {
        if (_autoTypeSettings.WindowsHotKey is null)
        {
            _hotKeyNew = new WindowsHotKey(VirtualKeys.A, RegisterHotKeyModifiers.Ctrl | RegisterHotKeyModifiers.Alt);
            _autoTypeSettings.WindowsHotKey = _hotKeyNew;
            _save(_autoTypeSettings);
        }
        else
        {
            // load hoktey from settings
            _hotKeyNew = _autoTypeSettings.WindowsHotKey;
        }

        _hotKeyNew.SetAction(ExecuteOnHotKey);
        var success = _hotKeyNew.RegisterHotKey();

        _isActive = success;

        OnActiveChanged?.Invoke(this, success);

        _isEnabled = true;
    }

    public void Disable()
    {
        _hotKeyNew?.Dispose();
        _isActive = false;
        _isEnabled = false;
        OnActiveChanged?.Invoke(this, false);
    }

    public void RegisterOnHotKeyAction(Action<WindowsHotKey> onHotkey)
    {
        if (onHotkey != null)
        {
            _hotKeyActions.Add(onHotkey);
        }
    }

    private void ExecuteOnHotKey(WindowsHotKey hotKey)
    {
        foreach (var action in _hotKeyActions)
        {
            action.Invoke(hotKey);
        }
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        _hotKeyNew?.Dispose();
    }
}