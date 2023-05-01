using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.AutoType.Desktop.Windows.Native;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Services;

public class HotkeyService : IDisposable
{
    private readonly List<Action<WindowsHotKey>> _hotKeyActions;
    private WindowsHotKey? _hotKeyNew;

    public HotkeyService()
    {
        _hotKeyActions = new List<Action<WindowsHotKey>>();
    }

    public void Enable()
    {
        _hotKeyNew = new WindowsHotKey(VirtualKeys.A, RegisterHotKeyModifiers.Ctrl | RegisterHotKeyModifiers.Alt, ExecuteOnHotKey);
        var success = _hotKeyNew.RegisterHotKey();
    }

    public void Disable()
    {
        _hotKeyNew?.Dispose();
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