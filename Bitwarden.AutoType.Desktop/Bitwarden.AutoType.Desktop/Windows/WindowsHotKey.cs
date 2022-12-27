using System;
using System.Windows.Interop;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Windows;

public sealed class WindowsHotKey : IDisposable
{
    private ThreadMessageEventHandler? _hotKeyFilter;
    public int ID { get; set; }
    public VirtualKeys Key { get; }
    public RegisterHotKeyModifiers KeyModifiers { get; }
    public Action<WindowsHotKey> Action { get; }

    public WindowsHotKey(VirtualKeys key, RegisterHotKeyModifiers keyModifiers, Action<WindowsHotKey> action)
    {
        Key = key;
        KeyModifiers = keyModifiers;
        Action = action;
        ID = ((int)Key << 0xFF) + (int)KeyModifiers;
    }

    public bool RegisterHotKey()
    {
        bool result = WindowsDLLs.RegisterHotKey(IntPtr.Zero, ID, (uint)KeyModifiers, (uint)Key);
        if (!result) return false;
        _hotKeyFilter = new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
        ComponentDispatcher.ThreadFilterMessage += _hotKeyFilter;
        return result;
    }

    private void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
    {
        if (!handled && msg.message == WindowsConstants.WM_HOTKEY && msg.wParam == ID)
        {
            Action?.Invoke(this);
            handled = true;
        }
    }

    public void Dispose()
    {
        WindowsDLLs.UnregisterHotKey(IntPtr.Zero, ID);
        if (_hotKeyFilter != null)
        {
            ComponentDispatcher.ThreadFilterMessage -= _hotKeyFilter;
        }
    }
}