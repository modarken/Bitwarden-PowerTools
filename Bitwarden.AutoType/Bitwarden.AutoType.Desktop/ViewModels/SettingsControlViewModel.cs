using System;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Models;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace Bitwarden.AutoType.Desktop;

[INotifyPropertyChanged]
public partial class SettingsControlViewModel
{
    private readonly ILogger<SettingsControlViewModel>? _logger;

    [ObservableProperty]
    private BitwardenClientConfiguration? _bitwardenClientConfiguration;

    [ObservableProperty]
    private int? _accessMethod;

    [ObservableProperty]
    private string? _totp;

    [ObservableProperty]
    private string? _masterPassword;

    private readonly Action<BitwardenClientConfiguration>? _save;
    private readonly BitwardenService? _bitwardenService;
    private readonly StateController<AutoTypeConfigurationStates> _state;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsControlViewModel"/> class.
    ///
    /// This is only for WPF
    ///
    /// </summary>
    public SettingsControlViewModel()
    {
        _logger = null;
        _bitwardenClientConfiguration = default;
        _save = default;
        _bitwardenService = default;
        _state = new StateController<AutoTypeConfigurationStates>();
        AccessMethod = 0;
    }

    public SettingsControlViewModel(ILogger<SettingsControlViewModel> logger,
        BitwardenClientConfiguration bitwardenClientConfiguration,
        Action<BitwardenClientConfiguration> save,
        BitwardenService bitwardenService,
        StateController<AutoTypeConfigurationStates> state)
    {
        _logger = logger;
        _bitwardenClientConfiguration = bitwardenClientConfiguration;
        _save = save;
        _bitwardenService = bitwardenService;
        _state = state;
        ConfigureMode(bitwardenClientConfiguration);
    }

    private void ConfigureMode(BitwardenClientConfiguration bitwardenClientConfiguration)
    {
        AccessMethod = string.IsNullOrEmpty(bitwardenClientConfiguration.refresh_token) ? 0 : 1;
    }

    [RelayCommand]
    public async Task SaveConfig()
    {
        if (BitwardenClientConfiguration == null)
        {
            return;
        }

        // If no master password provided, just save the current settings (e.g., SSL toggle changes)
        if (string.IsNullOrEmpty(MasterPassword))
        {
            if (_save != null && BitwardenClientConfiguration != null)
            {
                _save(BitwardenClientConfiguration);
            }
            return;
        }

        if (_bitwardenService == null)
        {
            return;
        }

        if (string.IsNullOrEmpty(BitwardenClientConfiguration.device_identifier))
        {
            BitwardenClientConfiguration.device_identifier = Guid.NewGuid().ToString();
        }

        if (string.IsNullOrEmpty(BitwardenClientConfiguration.device_name))
        {
            BitwardenClientConfiguration.device_name = Constants.BitwardenClientConfigurationDeviceName;
        }

        if (AccessMethod == 0)
        {
            var (encryptionKey, tokenResponse) = await _bitwardenService.GetEncryptionKey(MasterPassword!);
            MasterPassword = null;
            Totp = null;

            if (encryptionKey is null)
            {
                return;
            }

            BitwardenClientConfiguration.encryption_key = encryptionKey;
            BitwardenClientConfiguration.refresh_token = null;
        }
        else
        {
            var (encryptionKey, tokenResponse) = await _bitwardenService.GetEncryptionKey(MasterPassword!, Totp);
            MasterPassword = null;
            Totp = null;

            if (encryptionKey is null)
            {
                return;
            }

            BitwardenClientConfiguration.encryption_key = encryptionKey;
            BitwardenClientConfiguration.refresh_token = tokenResponse!.refresh_token;
            BitwardenClientConfiguration.client_id = null;
            BitwardenClientConfiguration.client_secret = null;
        }

        _state.SetState(BitwardenClientConfiguration.Validate() ? AutoTypeConfigurationStates.Configured : AutoTypeConfigurationStates.NotConfigured);

        if (_save != null && BitwardenClientConfiguration != null)
        {
            _save(BitwardenClientConfiguration);
        }

        await _bitwardenService.RefreshLocalDatabaseAsync();
    }
}