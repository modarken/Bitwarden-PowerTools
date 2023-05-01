using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.Core.Models;

namespace Bitwarden.AutoType.Desktop.Services;

public class BitwardenKeystrokeSequence : SpecialKeystrokeSequence
{
    private readonly Regex _keyRegEx = new(@"{.*?}", RegexOptions.Compiled);

    public enum BitwardenPlaceholders
    {
        NAME,
        USERNAME,
        PASSWORD,
        URL,
        NOTES
    }

    private readonly Cipher _cipher;
    private readonly Func<string, string?> _decryptor;

    public BitwardenKeystrokeSequence(string sequence, IKeystrokeConfiguration? configuration, Cipher cipher, Func<string, string?> decryptor) : base(sequence, configuration)
    {
        _cipher = cipher;
        _decryptor = decryptor;
    }
    // Each sequence is a string like "{NAME}", "{USERNAME}", "{PASSWORD}", "{URL}", "{NOTES}", {FIELD:ping}

    // {TAB}, {ENTER}, {UP}, {DOWN}, {LEFT}, {RIGHT}, {SHIFT}, {CTRL}, {ALT}, {DELETE}, {BACKSPACE}, {HOME}, {END}, {PGUP}, {PGDN}, {ESC}, {INSERT}, {F1}, {F2}, {F3}, {F4}, {F5}, {F6}, {F7}, {F8}, {F9}, {F10}, {F11}, {F12}, {WIN}, {CAPSLOCK}, {SCROLLLOCK}, {NUMLOCK}, {PRTSC}, {BREAK}, {HELP}, {CLEAR}, {SLEEP}, {NUMPAD0}, {NUMPAD1}, {NUMPAD2}, {NUMPAD3}, {NUMPAD4}, {NUMPAD5}, {NUMPAD6}, {NUMPAD7}, {NUMPAD8}, {NUMPAD9}, {MULTIPLY}, {ADD}, {SEPARATOR}, {SUBTRACT}, {DECIMAL}, {DIVIDE}, {OEM_1}, {OEM_PLUS}, {OEM_COMMA}, {OEM_MINUS}, {OEM_PERIOD}, {OEM_2}, {OEM_3}, {OEM_4}, {OEM_5}, {OEM_6}, {OEM_7}, {OEM_8}, {OEM_102}, {PROCESSKEY}, {ATTN}, {CRSEL}, {EXSEL}, {EREOF}, {PLAY}, {ZOOM}, {NONAME}, {PA1}, {OEM_CLEAR}}
    private IEnumerable<EmulatedKeystroke>? ProcessRegExSequence(string sequence)
    {
        var keyword = sequence[1..^1].ToLower();

        if (keyword.StartsWith("FIELD:", StringComparison.InvariantCultureIgnoreCase))
        {
            var fieldName = keyword[6..];

            var field = _cipher
                .Fields?
                .Where(f => f.Name is not null)
                .Select(f => new { Name = _decryptor(f.Name!), f.Value })
                .FirstOrDefault(a => a.Name!.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

            if (field is null || field.Value is null) return null;
            var plainText = _decryptor(field.Value);
            if (plainText is string)
            {
                var plainTextSequence = new KeystrokeSequence(plainText, Configuration);
                return plainTextSequence.Provide();
            }
        }
        else if (Enum.TryParse(keyword, true, out BitwardenPlaceholders placeHolder))
        {
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