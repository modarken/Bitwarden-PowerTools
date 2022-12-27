using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Windows;

/// <summary>
/// Types the specified keystrokes.
///
/// abc                         // key press characters abc
/// {1000}                      // delay for 1 second
/// {DELAY=X}                   // delay for x milliseconds for all subsequent keypresses
/// {VKEY=X}                    // key press virtual key of value x
/// {VKEY=X:1500}               // key press virtual key of value x for 1.5 seconds
/// {[X]}                       // key press specified character
/// {[X]:1500}                  // key press specified character for 1.5 seconds
/// {SPECIALKEY}                // key press special character
/// {SPECIALKEY:1500}           // hold special down key for 1.5 seconds
/// {SPECIALKEY:DOWN}           // special key press down
/// {SPECIALKEY:UP}             // special key press up
/// {APPACTIVATE:WindowTitleRegEx}   // App activate Tiyle
/// </summary>
/// <param name="keystrokes">The keystrokes.</param>
///

public enum EmulatedKeystrokeTypes
{
    Press,
    Down,
    Up
}

public class EmulatedKeystroke
{
    public EmulatedKeystrokeTypes DirectionType { get; set; }
    public byte? VirtualKey { get; set; }
    public byte? KeyModifierFlags { get; set; }
    public TimeSpan? Delay { get; set; }

    #region Helpers

    public bool IsShiftVirtualKey { get => (VirtualKey is not null) && ((VirtualKeys)VirtualKey == VirtualKeys.Shift || (VirtualKeys)VirtualKey == VirtualKeys.LeftShift || (VirtualKeys)VirtualKey == VirtualKeys.RightShift); }
    public bool IsShiftModifier { get => (KeyModifierFlags & 0x01) == 1; }
    public bool IsCtrlModifier { get => (KeyModifierFlags & 0x02) == 2; }
    public bool IsAltModifier { get => (KeyModifierFlags & 0x04) == 4; }
    public bool IsHankakuModifier { get => (KeyModifierFlags & 0x08) == 8; }

    #endregion Helpers
}

public interface IKeystrokeProvider
{
    IKeystrokeConfiguration Configuration { get; }

    IEnumerable<EmulatedKeystroke> Provide();
}

public class KeywordKeystrokeSequence : KeystrokeSequence
{
    private static readonly Regex _specialKeyRegEx = new(@"{.*?}", RegexOptions.Compiled);

    private static readonly Dictionary<string, EmulatedKeystroke> _keywords = new Dictionary<string, EmulatedKeystroke>
    {
        {"shift", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Shift } },

    };

    //public enum SpecialKeys
    //{
    //    LEFTCURLYBRACE,
    //    RIGHTCURLYBRACE,
    //    SHIFT,
    //    ALT,
    //    TAB,
    //    ENTER,
    //    SPACE,
    //    BACKSPACE,
    //    UP,
    //    DOWN,
    //    LEFT,
    //    RIGHT,
    //    INSERT,
    //    DELETE,
    //    HOME,
    //    END,
    //    PGUP,
    //    PGDOWN,
    //    CAPSLOCK,
    //    ESCAPE,
    //    NUMLOCK,
    //    PRINTSCREEN,
    //    SCROLLLOCK,
    //    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    //    WIN,
    //}

    //public enum KeyDirections
    //{
    //    Down,
    //    Up
    //}

    public KeywordKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration) : base(sequence, configuration)
    {
    }

    /// <summary>
    /// Processes the keyword sequence.
    ///
    /// Keyword
    /// Keyword:KeyDirections
    /// Keyword:int             time for keypress in milliseconds
    ///
    /// </summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns></returns>
    private IEnumerable<EmulatedKeystroke> ProcessKeywordSequence(string sequence)
    {
        var s = sequence.Substring(1,sequence.Length-2);



        string keyword = "";
        EmulatedKeystrokeTypes? keystrokeType= null;
        TimeSpan? timeSpan= null;
        EmulatedKeystroke? emulatedKeystroke= null;
        if (s.Contains(":"))
        {
            var split = s.Split(':');
            keyword = split[0];
            var unknown = split[1].ToLower();

            if (Int32.TryParse(unknown, out int result))
            {
                timeSpan = TimeSpan.FromMilliseconds(result);
            }

            if (Enum.IsDefined(typeof(EmulatedKeystrokeTypes), unknown))
            {
                keystrokeType = (EmulatedKeystrokeTypes ?)Enum.Parse(typeof(EmulatedKeystrokeTypes), unknown);
            }
        }
        else
        {
            keyword = s;
        }



        if (_keywords.ContainsKey(keyword))
        {
            emulatedKeystroke = _keywords[keyword];
        }









        return new EmulatedKeystroke[] {  };
    }

    protected override IEnumerable<EmulatedKeystroke> Process(string keystrokes)
    {

        var matches = _specialKeyRegEx.Matches(_sequence).ToArray();
        var splits = _specialKeyRegEx.Split(_sequence);
        var chunks = new List<string>();

        int matchIndex = 0;
        foreach (var item in splits)
        {
            chunks.Add(item);

            if (matchIndex < matches.Length)
            {
                chunks.Add(matches[matchIndex].Value);
            }

            matchIndex++;
        }

        var processedChunks = new List<IEnumerable<EmulatedKeystroke>>();

        foreach (var item in chunks)
        {
            if (string.IsNullOrEmpty(item)) continue;

            if (_specialKeyRegEx.IsMatch(item))
            {
                processedChunks.Add(ProcessKeywordSequence(item));
            }
            else
            {

                processedChunks.Add(base.Process(item));
            }
        }

        var xx = processedChunks.SelectMany(i => i).ToArray();
        var len = xx.Length;
        return xx;


    }
}

public interface IKeystrokeConfiguration
{
    TimeSpan DelayBetweenKeystrokes { get; set; }
    TimeSpan PressKeyTime { get; set; }
}

public class DefaultKeystrokeConfiguration : IKeystrokeConfiguration
{
    private static readonly int _delayBetweenKeyStrokes = 15;
    private static readonly int _pressKeyTime = 15;
    public TimeSpan DelayBetweenKeystrokes { get; set; } = TimeSpan.FromMilliseconds(_delayBetweenKeyStrokes);
    public TimeSpan PressKeyTime { get; set; } = TimeSpan.FromMilliseconds(_pressKeyTime);
}

public class KeystrokeSequence : IKeystrokeProvider
{
    public IKeystrokeConfiguration Configuration { get; }
    protected string _sequence;
    protected IEnumerable<EmulatedKeystroke>? _emulatedKeyStrokes;

    public KeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration = null)
    {
        _sequence = sequence;
        Configuration = configuration ?? new DefaultKeystrokeConfiguration();
        Provide();
    }

    protected virtual IEnumerable<EmulatedKeystroke> Process(string keystrokes)
    {
        return keystrokes.Select(c => WindowsDLLs.VkKeyScan(c))
            .Select(c => new { a = (byte)((c >> 8) & 0x00FF), b = (byte)((c >> 0) & 0x00FF) })
            .Select(i => new EmulatedKeystroke { DirectionType = EmulatedKeystrokeTypes.Press, VirtualKey = i.b, KeyModifierFlags = i.a });
    }


    public virtual IEnumerable<EmulatedKeystroke> Provide()
    {
        _emulatedKeyStrokes ??= Process(_sequence).ToArray();

        return _emulatedKeyStrokes;
    }
}

public static class WindowsKeyboard
{
    public static async Task SendKeystrokes(IKeystrokeProvider keystrokeProvider)
    {
        var isShiftVirtualKeyDown = false;

        foreach (var item in keystrokeProvider.Provide())
        {
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
                    await SendKeyPress((VirtualKeys)item.VirtualKey, keystrokeProvider.Configuration.PressKeyTime).ConfigureAwait(false);
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

                await Task.Delay(keystrokeProvider.Configuration.DelayBetweenKeystrokes).ConfigureAwait(false);

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