using System;
using System.Collections.Generic;
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    public HotkeyService()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
    }

    public bool IsEnabled => _isEnabled;

    public HotkeyService(AutoTypeSettings autoTypeSettings, Action<AutoTypeSettings> save)
    {
        _hotKeyActions = new List<Action<WindowsHotKey>>();
        _autoTypeSettings = autoTypeSettings;
        _save = save;
    }

    public WindowsHotKey? GetHotkey()
    {
        return _autoTypeSettings.WindowsHotKey;
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
        _isEnabled = true;
    }

    public void Disable()
    {
        _hotKeyNew?.Dispose();
        _isEnabled = false;
    }

    public void RegisterOnHotKey(Action<WindowsHotKey> onHotkey)
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
        _hotKeyNew?.Dispose();
    }
}