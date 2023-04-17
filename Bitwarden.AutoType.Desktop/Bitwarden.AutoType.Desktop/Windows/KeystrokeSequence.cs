using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bitwarden.AutoType.Desktop.Windows.Native;

namespace Bitwarden.AutoType.Desktop.Windows;

public enum EmulatedKeystrokeTypes
{
    Press,
    Down,
    Up
}

public class EmulatedKeystroke : ICloneable
{
    public EmulatedKeystrokeTypes DirectionType { get; set; }
    public byte? VirtualKey { get; set; }
    public byte? KeyModifierFlags { get; set; }
    public TimeSpan? PressTime { get; set; }
    public TimeSpan? Delay { get; set; }

    #region Helpers

    public bool IsShiftVirtualKey { get => (VirtualKey is not null) && ((VirtualKeys)VirtualKey == VirtualKeys.Shift || (VirtualKeys)VirtualKey == VirtualKeys.LeftShift || (VirtualKeys)VirtualKey == VirtualKeys.RightShift); }
    public bool IsShiftModifier { get => (KeyModifierFlags & 0x01) == 1; }
    public bool IsCtrlModifier { get => (KeyModifierFlags & 0x02) == 2; }
    public bool IsAltModifier { get => (KeyModifierFlags & 0x04) == 4; }
    public bool IsHankakuModifier { get => (KeyModifierFlags & 0x08) == 8; }

    public object Clone()
    {
        return this.MemberwiseClone();
    }

    #endregion Helpers
}

public interface IKeystrokeProvider
{
    IKeystrokeConfiguration Configuration { get; }

    IEnumerable<EmulatedKeystroke> Provide();
}

public class SpecialKeystrokeSequence : DelayKeystrokeSequence
{
    private readonly Regex _keyRegEx = new(@"{.*?}", RegexOptions.Compiled);

    private static readonly Dictionary<string, EmulatedKeystroke> _specialKeywords = new()
    {
        {"leftcurlybrace", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        {"rightcurlybrace", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM6, KeyModifierFlags = 0x01 } },

        {"shift", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Shift } },
        {"rightshift", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.LeftShift } },
        {"leftshift", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.RightShift } },
        {"alt", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Menu } },
        {"leftalt", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.LeftMenu } },
        {"rightalt", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.RightMenu } },
        {"control", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Control } },
        {"leftcontrol", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.LeftControl } },
        {"rightcontrol", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.RightControl } },
        {"tab", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Tab } },

        {"leftwindows", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.LeftWindows } },
        {"rightwindows", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.RightWindows } },

        {"enter", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Return } },
        {"back", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Back } },
        {"space", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Space } },

        {"left", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Left } },
        {"down", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Down } },
        {"right", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Right } },
        {"up", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Up } },

        {"insert", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Insert } },
        {"delete", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Delete } },
        {"home", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Home } },
        {"end", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.End } },
        {"pgup", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Prior } },
        {"pgdown", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Next } },
        {"capslock", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.CapsLock } },
        {"escape", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Escape } },
        {"numlock", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.NumLock } },
        {"printscreen", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.Print } },
        {"scrolllock", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.ScrollLock } },

        {"f1", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F1 } },
        {"f2", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F2 } },
        {"f3", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F3 } },
        {"f4", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F4 } },
        {"f5", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F5 } },
        {"f6", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F6 } },
        {"f7", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F7 } },
        {"f8", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F8 } },
        {"f9", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F9 } },
        {"f10", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F10 } },
        {"f11", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F11 } },
        {"f12", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.F12 } },
    };

    public SpecialKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration) : base(sequence, configuration)
    {
    }

    /// <summary>
    /// Processes the keyword sequence.
    ///
    /// Keyword
    /// Keyword:KeyDirections   up, down, or press
    /// Keyword:int             time for keypress in milliseconds
    /// VK54                    Would print the virtual key "6"
    ///
    /// </summary>
    /// <param name="sequence">The sequence.</param>
    /// <returns></returns>
    private IEnumerable<EmulatedKeystroke>? ProcessRegExSequence(string sequence)
    {
        var innerSequence = sequence[1..^1].ToLower();

        string keyword = "";
        EmulatedKeystrokeTypes? keystrokeType = null;
        TimeSpan? timeSpan = null;
        EmulatedKeystroke? emulatedKeystroke = null;

        if (innerSequence.Contains(':'))
        {
            var split = innerSequence.Split(':');
            keyword = split[0];
            var unknown = split[1];

            if (Int32.TryParse(unknown, out int result))
            {
                timeSpan = TimeSpan.FromMilliseconds(result);
            }
            else if (Enum.TryParse(typeof(EmulatedKeystrokeTypes), unknown, true, out object? parsed))
            {
                keystrokeType = (EmulatedKeystrokeTypes?)parsed;
            }
        }
        else
        {
            keyword = innerSequence;
        }

        if (_specialKeywords.ContainsKey(keyword))
        {
            emulatedKeystroke = (EmulatedKeystroke?)_specialKeywords[keyword].Clone();
        }
        else if (keyword.StartsWith("vk") && keyword.Length > 2)
        {
            var tryGetByte = keyword[2..];

            if (Byte.TryParse(tryGetByte, out byte outByte))
            {
                emulatedKeystroke = new EmulatedKeystroke { VirtualKey = outByte };
            }
        }

        if (emulatedKeystroke is EmulatedKeystroke)
        {
            if (keystrokeType is EmulatedKeystrokeTypes ks)
            {
                emulatedKeystroke!.DirectionType = ks;
            }
            if (timeSpan is TimeSpan ts)
            {
                emulatedKeystroke!.PressTime = ts;
            }
            return new EmulatedKeystroke[] { emulatedKeystroke! };
        }

        return null;
    }

    protected override IEnumerable<EmulatedKeystroke> Process(string keystrokes, string sequence)
    {
        var matches = _keyRegEx.Matches(sequence).ToArray();
        var splits = _keyRegEx.Split(sequence);
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

            if (_keyRegEx.IsMatch(item))
            {
                var processed = ProcessRegExSequence(item);
                if (processed != null)
                {
                    processedChunks.Add(processed);
                }
                else
                {
                    processedChunks.Add(base.Process(item, item));
                }
            }
            else
            {
                processedChunks.Add(base.Process(item, item));
            }
        }

        return processedChunks.SelectMany(i => i).ToArray();
    }
}

public class DelayKeystrokeSequence : KeystrokeSequence
{
    private readonly Regex _keyRegEx = new(@"{.*?}", RegexOptions.Compiled);

    public DelayKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration) : base(sequence, configuration)
    {
    }

    private IEnumerable<EmulatedKeystroke>? ProcessRegExSequence(string sequence)
    {
        var innerSequence = sequence[1..^1].ToLower();

        TimeSpan? timeSpan = null;
        if (Int32.TryParse(innerSequence, out int result))
        {
            timeSpan = TimeSpan.FromMilliseconds(result);

            return new EmulatedKeystroke[] { new EmulatedKeystroke { Delay = timeSpan } };
        }

        return null;
    }

    protected override IEnumerable<EmulatedKeystroke> Process(string keystrokes, string sequence)
    {
        var matches = _keyRegEx.Matches(sequence).ToArray();
        var splits = _keyRegEx.Split(sequence);
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

            if (_keyRegEx.IsMatch(item))
            {
                var processed = ProcessRegExSequence(item);
                if (processed != null)
                {
                    processedChunks.Add(processed);
                }
                else
                {
                    processedChunks.Add(base.Process(item, item));
                }
            }
            else
            {
                processedChunks.Add(base.Process(item, item));
            }
        }

        return processedChunks.SelectMany(i => i).ToArray();
    }
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

    protected virtual IEnumerable<EmulatedKeystroke> Process(string keystrokes, string sequence)
    {
        return keystrokes.Select(c => WindowsDLLs.VkKeyScan(c))
            .Select(c => new { KeyModifierFlags = (byte)((c >> 8) & 0x00FF), VirtualKey = (byte)((c >> 0) & 0x00FF) })
            .Select(i => new EmulatedKeystroke
            {
                DirectionType = EmulatedKeystrokeTypes.Press,
                VirtualKey = i.VirtualKey,
                KeyModifierFlags = i.KeyModifierFlags
            });
    }

    public virtual IEnumerable<EmulatedKeystroke> Provide()
    {
        _emulatedKeyStrokes ??= Process(_sequence, _sequence).ToArray();
        return _emulatedKeyStrokes;
    }
}