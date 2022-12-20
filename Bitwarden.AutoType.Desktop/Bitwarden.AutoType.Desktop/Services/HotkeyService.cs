using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Windows;
using MahApps.Metro.Controls;

namespace Bitwarden.AutoType.Desktop.Services;



public class HotkeyService : WPFBackgroundService
{
    private GlobalKeyboardHook _globalKeyboardHook;
    private Windows.HotKey _hotKey;

    private WindowsHotKey _hotKeyNew;
    public HotkeyService()
    {
        //_globalKeyboardHook = new GlobalKeyboardHook();
        //_globalKeyboardHook.KeyboardPressed += OnKeyPressed;
        //_hotKey = new UnManaged.HotKey(Key.F9, KeyModifier.Shift | KeyModifier.Win, OnHotKeyHandler);
        //_hotKey = new UnManaged.HotKey(Key.A, KeyModifier.Ctrl | KeyModifier.Alt, OnHotKeyHandler);
        //_hotKey = new Windows.HotKey(Key.A, KeyModifier.Ctrl | KeyModifier.Alt, OnHotKeyHandler);
        //_hotKey = new Windows.HotKey(VirtualKeys.A, KeyModifier.Ctrl | KeyModifier.Alt, OnHotKeyHandler);

        _hotKeyNew = new WindowsHotKey(VirtualKeys.A, KeyModifier.Ctrl | KeyModifier.Alt, TakAction);
    }

    private void TakAction(WindowsHotKey hotKey)
    {
        MessageBox.Show("OH YEAH");
    }

    private void OnHotKeyHandler(Windows.HotKey hotKey)
    {
        MessageBox.Show("UAA");
    }

    private void OnKeyPressed(object? sender, GlobalKeyboardHookEventArgs e)
       {
        if (e.KeyboardData.VirtualCode != GlobalKeyboardHook.VkSnapshot)
            return;


        // seems, not needed in the life.
        if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.SysKeyDown &&
            e.KeyboardData.Flags == GlobalKeyboardHook.LlkhfAltdown &&
            e.KeyboardData.Flags == GlobalKeyboardHook.VK_CONTROL)
        {
            MessageBox.Show("Alt + Print Screen");
            e.Handled = true;
        }
        //else

        //if (e.KeyboardState == GlobalKeyboardHook.KeyboardState.KeyDown)
        //{
        //    MessageBox.Show("Print Screen");
        //    e.Handled = true;
        //}
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {





        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        base.Dispose();

        _globalKeyboardHook?.Dispose();
        _hotKey?.Dispose();
        _hotKeyNew?.Dispose();
    }
}
