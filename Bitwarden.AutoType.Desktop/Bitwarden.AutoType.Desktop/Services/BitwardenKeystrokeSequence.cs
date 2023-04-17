using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Windows.Native;
using Bitwarden.AutoType.Desktop.Windows;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Types the specified keystrokes.
///
/// abc                         // key press characters abc
/// {1000}                      // delay for 1 second
/// {DELAY=X}                   // delay for x milliseconds for all subsequent keypresses
/// {VKEY=X}                    // key press virtual key of value x
/// {VKEY=X:1500}               // key press virtual key of value x for 1.5 seconds
/// {[X]}                       // key press specified character
/// {[X]:1500}                  /}123$%^/ key press specified character for 1.5 seconds
/// {SPECIALKEY}                // key press special character
/// {SPECIALKEY:1500}           // hold special down key for 1.5 seconds
/// {SPECIALKEY:DOWN}           // special key press down
/// {SPECIALKEY:UP}             // special key press up
/// {APPACTIVATE:WindowTitleRegEx}   // App activate Tiyle
///
///
/// {bw:title}   // App activate Tiyle
/// {bw:username}   // App activate Tiyle
/// {bw:password}   // App activate Tiyle
/// {bw:url}   // App activate Tiyle
/// {bw:notes}   // App activate Tiyle
///
///
///
/// </summary>
/// <param name="keystrokes">The keystrokes.</param>
///
public class BitwardenKeystrokeSequence : SpecialKeystrokeSequence
{
    private readonly Regex _keyRegEx = new(@"{.*?}", RegexOptions.Compiled);

    private static readonly Dictionary<string, EmulatedKeystroke> _specialKeywords = new()
    {
        {"name", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        {"username", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        {"password", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        {"totp", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        //{"uri1", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },
        // field:uri1
        // field:key2
        {"notes", new EmulatedKeystroke { VirtualKey = (byte)VirtualKeys.OEM4, KeyModifierFlags = 0x01 } },

    };

    public BitwardenKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration) : base(sequence, configuration)
    {
    }

    private IEnumerable<EmulatedKeystroke>? ProcessRegExSequence(string sequence)
    {
        throw new NotImplementedException();
        //var innerSequence = sequence[1..^1].ToLower();

        //string keyword = "";
        //EmulatedKeystrokeTypes? keystrokeType = null;
        //TimeSpan? timeSpan = null;
        //EmulatedKeystroke? emulatedKeystroke = null;

        //if (innerSequence.Contains(':'))
        //{
        //    var split = innerSequence.Split(':');
        //    keyword = split[0];
        //    var unknown = split[1];

        //    if (Int32.TryParse(unknown, out int result))
        //    {
        //        timeSpan = TimeSpan.FromMilliseconds(result);
        //    }
        //    else if (Enum.TryParse(typeof(EmulatedKeystrokeTypes), unknown, true, out object? parsed))
        //    {
        //        keystrokeType = (EmulatedKeystrokeTypes?)parsed;
        //    }
        //}
        //else
        //{
        //    keyword = innerSequence;
        //}

        //if (_specialKeywords.ContainsKey(keyword))
        //{
        //    emulatedKeystroke = (EmulatedKeystroke?)_specialKeywords[keyword].Clone();
        //}
        //else if (keyword.StartsWith("vk") && keyword.Length > 2)
        //{
        //    var tryGetByte = keyword[2..];

        //    if (Byte.TryParse(tryGetByte, out byte outByte))
        //    {
        //        emulatedKeystroke = new EmulatedKeystroke { VirtualKey = outByte };
        //    }
        //}

        //if (emulatedKeystroke is EmulatedKeystroke)
        //{
        //    if (keystrokeType is EmulatedKeystrokeTypes ks)
        //    {
        //        emulatedKeystroke!.DirectionType = ks;
        //    }
        //    if (timeSpan is TimeSpan ts)
        //    {
        //        emulatedKeystroke!.PressTime = ts;
        //    }
        //    return new EmulatedKeystroke[] { emulatedKeystroke! };
        //}

        //return null;
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
