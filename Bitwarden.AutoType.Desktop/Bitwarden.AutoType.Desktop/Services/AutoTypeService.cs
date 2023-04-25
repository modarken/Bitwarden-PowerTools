using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Windows;
using Bitwarden.Core.Models;

namespace Bitwarden.AutoType.Desktop.Services;

public class AutoTypeService
{
    private DefaultKeystrokeConfiguration _config;

    public AutoTypeService()
    {
        _config = new DefaultKeystrokeConfiguration
        {
            DelayBetweenKeystrokes = TimeSpan.FromMilliseconds(25),
            PressKeyTime = TimeSpan.FromMilliseconds(15)
        };
    }

    public Task TypeSequenceAsync(KeyValuePair<AutoTypeCustomField, Cipher> match, Func<string, string?> decryptor, CancellationToken token = default)
    {
        var sequence = match.Key.Sequence;
        BitwardenKeystrokeSequence bitwardenKeystrokeSequence = new(sequence!, _config, match.Value, decryptor);
        return WindowsKeyboard.SendKeystrokesAsync(bitwardenKeystrokeSequence, token);
    }
}