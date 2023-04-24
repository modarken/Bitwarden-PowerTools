using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.Core.Models;

namespace Bitwarden.AutoType.Desktop.Services;

/// <summary>
/// Types the specified keystrokes.
///
///
/// TODO
///
///
///
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

    public enum BitwardenPlaceholders
    {
        NAME,
        USERNAME,
        PASSWORD,
        URL,
        NOTES,
    }

    private readonly Cipher _cipher;
    private readonly Func<string, string?> _decryptor;

    public BitwardenKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration, Cipher cipher, Func<string, string?> decryptor) : base(sequence, configuration)
    {
        _cipher = cipher;
        _decryptor = decryptor;
    }

    private IEnumerable<EmulatedKeystroke>? ProcessRegExSequence(string sequence)
    {
        var keyword = sequence[1..^1].ToLower();

        if (Enum.TryParse(keyword, true, out BitwardenPlaceholders placeHolder))
        {
            // Console.WriteLine($"The input '{input}' matches the enum case: {color}");
            var cipherText = placeHolder switch
            {
                BitwardenPlaceholders.NAME => _cipher.Name,
                BitwardenPlaceholders.USERNAME => _cipher.Login?.Username,
                BitwardenPlaceholders.PASSWORD => _cipher.Login?.Password,
                BitwardenPlaceholders.URL => _cipher.Login?.Uri,
                BitwardenPlaceholders.NOTES => _cipher.Notes,
                _ => throw new ArgumentOutOfRangeException(nameof(placeHolder), placeHolder, null),
            };

            if (cipherText is string)
            {
                var plainText = _decryptor(cipherText);
                if (plainText is string)
                {
                    var plainTextSequence = new KeystrokeSequence(plainText, Configuration);

                    return plainTextSequence.Provide();
                }
            }
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