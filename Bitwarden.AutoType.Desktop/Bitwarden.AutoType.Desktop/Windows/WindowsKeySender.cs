using System;
using System.Linq;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Windows;

public static class WindowsKeyboard
{
    public static async Task SendKeystrokes(IKeystrokeProvider keystrokeProvider)
    {
        var isShiftVirtualKeyDown = false;
        var onLastItem = false;
        var items = keystrokeProvider.Provide().ToArray();
        foreach (var item in items)
        {
            if (item == items[^1]) onLastItem = true;
            if (item.VirtualKey is byte)
            {
                if (item.IsShiftVirtualKey && item.DirectionType == EmulatedKeystrokeTypes.Down)
                {
                    isShiftVirtualKeyDown = true;
                }
                else if (item.IsShiftVirtualKey && item.DirectionType == EmulatedKeystrokeTypes.Up)
                {
                    isShiftVirtualKeyDown = false;
                }

                if (!isShiftVirtualKeyDown && item.IsShiftModifier)
                {
                    SendKeyDown(VirtualKeys.Shift);
                }

                if (item.DirectionType == EmulatedKeystrokeTypes.Press)
                {
                    await SendKeyPress((VirtualKeys)item.VirtualKey, item.PressTime ?? keystrokeProvider.Configuration.PressKeyTime).ConfigureAwait(false);
                }
                else
                {
                    if (item.DirectionType == EmulatedKeystrokeTypes.Down)
                    {
                        SendKey((VirtualKeys)item.VirtualKey, WindowsConstants.KEYEVENTF_KEYDOWN);
                    }
                    else if (item.DirectionType == EmulatedKeystrokeTypes.Up)
                    {
                        SendKey((VirtualKeys)item.VirtualKey, WindowsConstants.KEYEVENTF_KEYUP);
                    }
                }

                if (!onLastItem) await Task.Delay(keystrokeProvider.Configuration.DelayBetweenKeystrokes).ConfigureAwait(false);

                if (!isShiftVirtualKeyDown && item.IsShiftModifier)
                {
                    SendKeyUp(VirtualKeys.Shift);
                }
            }
            else if (item.Delay is TimeSpan delayTime)
            {
                await Task.Delay(delayTime).ConfigureAwait(false);
            }
        }
    }

    public static void SendKeyPress(VirtualKeys key)
    {
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
    }

    public static async Task SendKeyPress(VirtualKeys key, TimeSpan pressKeyTime)
    {
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        await Task.Delay(pressKeyTime).ConfigureAwait(false);
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
    }

    public static void SendKeyDown(VirtualKeys key)
    {
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
    }

    public static void SendKeyUp(VirtualKeys key)
    {
        WindowsDLLs.keybd_event((byte)key, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
    }

    public static void SendKey(VirtualKeys key, int dwFlags)
    {
        WindowsDLLs.keybd_event((byte)key, 0, dwFlags, 0);
    }
}