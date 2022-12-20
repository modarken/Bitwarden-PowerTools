using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Helpers;

namespace Bitwarden.AutoType.Desktop.Services;

public class BitwardenService
{
    private readonly BitwardenClientConfiguration _bitwardenClientConfiguration;
    private readonly Action<BitwardenClientConfiguration> _save;

    public BitwardenService(BitwardenClientConfiguration bitwardenClientConfiguration, Action<BitwardenClientConfiguration> save)
    {
        _bitwardenClientConfiguration = bitwardenClientConfiguration;
        _save = save;
    }
}
