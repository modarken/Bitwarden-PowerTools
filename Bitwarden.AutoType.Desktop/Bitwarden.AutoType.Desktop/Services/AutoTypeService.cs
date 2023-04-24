using System;
using System.Collections.Generic;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.Core.Models;

namespace Bitwarden.AutoType.Desktop.Services;

public class AutoTypeService
{
    private DefaultKeystrokeConfiguration _config;

    public AutoTypeService()
    {
        //var task = Task.Run(async () =>
        //{
        //    WindowsDLLs.keybd_event((byte)49, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)49, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)50, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)50, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)51, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)51, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)52, 0, WindowsConstants.KEYEVENTF_KEYDOWN, 0);
        //    await Task.Delay(200);
        //    WindowsDLLs.keybd_event((byte)52, 0, WindowsConstants.KEYEVENTF_KEYUP, 0);
        //});

        //task.GetAwaiter().GetResult();
        _config = new DefaultKeystrokeConfiguration
        {
            DelayBetweenKeystrokes = TimeSpan.FromMilliseconds(25),
            PressKeyTime = TimeSpan.FromMilliseconds(15)
        };
        //var y = new KeystrokeSequence("Hello My name is Baby, hello my name is Bobby.{}", c);
        //var z = new KeystrokeSequence("Hello 1234567890 !@#$%^&*()", c);
        //var z = new SpecialKeystrokeSequence("{vk54}{1500}{tab}{leftcurlybrace}{1500}{rightcurlybrace}123{Shift:Down}456{Shift:Up}789{vk57}", c);
        //var z = new DelayKeystrokeSequence("{vk54}{1500}{leftcurlybrace}{1500}123{Shift:Down}456{Shift:Up}78{Shift:3000}9{vk57}", c);
        //var z = new SpecialKeystrokeSequence("{Shift}{Shift}", c);

        //System.Threading.Thread.Sleep(4000);
        // WindowsKeyboard.SendKeystrokes(y).GetAwaiter().GetResult();
        // WindowsKeyboard.SendKeystrokes(z).GetAwaiter().GetResult();
        //System.Threading.Thread.Sleep(2000);

        //var x = new WindowsKeyboard();

        //IntPtr calcWindow = WindowsDLLs.FindWindow(null, "Calculator");

        //if (WindowsDLLs.SetForegroundWindow(calcWindow))
        //{
        //    _ = WindowsKeyboard.SendKeyPress(VirtualKeys.N7, TimeSpan.FromMilliseconds(230));
        //    //WindowsDLLs.SendKeys.Send("10{+}10=");
        //}
    }

    public void TypeSequence(KeyValuePair<AutoTypeCustomField, Cipher> match, Func<string, string?> decryptor)
    {
        var sequence = match.Key.Sequence;
        BitwardenKeystrokeSequence bitwardenKeystrokeSequence = new(sequence!, _config, match.Value, decryptor);
        WindowsKeyboard.SendKeystrokes(bitwardenKeystrokeSequence).GetAwaiter().GetResult();
    }
}