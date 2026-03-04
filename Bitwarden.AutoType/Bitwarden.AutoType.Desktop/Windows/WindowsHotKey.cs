namespace Bitwarden.AutoType.Desktop.Windows;

using System;
using System.Text.Json.Serialization;
using System.Windows.Interop;
using Bitwarden.AutoType.Desktop.Windows.Native;

[Serializable]
public sealed class WindowsHotKey : IDisposable
{
    private ThreadMessageEventHandler? _hotKeyFilter;

    public int ID { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public VirtualKeys Key { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public RegisterHotKeyModifiers KeyModifiers { get; set; }

    [JsonIgnore]
    public Action<WindowsHotKey>? Action { get; set; }

    public WindowsHotKey(VirtualKeys key, RegisterHotKeyModifiers keyModifiers)
    {
        Key = key;
        KeyModifiers = keyModifiers;
        ID = ((int)Key << 0xFF) + (int)KeyModifiers;
    }

    public void SetAction(Action<WindowsHotKey> action)
    {
        Action = action;
    }

    public bool RegisterHotKey()
    {
        bool result = false;

        App.Current.Dispatcher.Invoke(() =>
        {
            result = WindowsDLLs.RegisterHotKey(IntPtr.Zero, ID, (uint)KeyModifiers, (uint)Key);
            if (result)
            {
                _hotKeyFilter = new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
                ComponentDispatcher.ThreadFilterMessage += _hotKeyFilter;
            }
        });
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
        App.Current.Dispatcher.Invoke(() =>
        {
            WindowsDLLs.UnregisterHotKey(IntPtr.Zero, ID);
            if (_hotKeyFilter != null)
            {
                ComponentDispatcher.ThreadFilterMessage -= _hotKeyFilter;
            }
        });
    }
}