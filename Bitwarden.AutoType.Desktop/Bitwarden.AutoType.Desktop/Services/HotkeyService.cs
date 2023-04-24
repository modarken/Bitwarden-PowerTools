using System;
using System.Collections.Generic;
using System.Windows;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Services;

public class HotkeyService : IDisposable
{
    private WindowsHotKey _hotKeyNew;
    private readonly List<Action<WindowsHotKey>> _hotKeyActions;

    public HotkeyService()
    {
        _hotKeyActions = new List<Action<WindowsHotKey>>();
        _hotKeyNew = new WindowsHotKey(VirtualKeys.A, RegisterHotKeyModifiers.Ctrl | RegisterHotKeyModifiers.Alt, ExecuteOnHotKey);
        var success = _hotKeyNew.RegisterHotKey();
    }

    public void RegisterOnHotKey(Action<WindowsHotKey> onHotKey)
    {
        if (onHotKey != null)
        {
            _hotKeyActions.Add(onHotKey);
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