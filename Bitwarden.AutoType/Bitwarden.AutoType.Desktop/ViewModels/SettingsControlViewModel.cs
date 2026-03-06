using System;
using System.Diagnostics;
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
    private string? _savedBaseAddress;
    private string? _savedEmail;

    [ObservableProperty]
    private BitwardenClientConfiguration? _bitwardenClientConfiguration;

    [ObservableProperty]
    private string? _totp;

    [ObservableProperty]
    private string? _masterPassword;

    [ObservableProperty]
    private string? _authorizationClientId;

    [ObservableProperty]
    private string? _authorizationClientSecret;

    [ObservableProperty]
    private AuthorizationInputMode _authorizationInputMode;

    [ObservableProperty]
    private bool _isAuthorizationInputVisible;

    [ObservableProperty]
    private bool _isApiKeyInputVisible;

    [ObservableProperty]
    private bool _isPasswordInputVisible;

    [ObservableProperty]
    private bool _canStartAuthorization;

    [ObservableProperty]
    private bool _canClearStoredAuthorization;

    [ObservableProperty]
    private string _authorizationStatusText = "Status: Not authorized";

    [ObservableProperty]
    private string _authorizationMethodText = "Authorization path: Not set";

    [ObservableProperty]
    private string _authorizationDetailText = "Save your account settings before authorizing this device.";

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
        AuthorizationInputMode = AuthorizationInputMode.None;
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
        CaptureSavedSettingsBaseline(bitwardenClientConfiguration);
        RefreshAuthorizationState();
    }

    partial void OnAuthorizationInputModeChanged(AuthorizationInputMode value)
    {
        IsApiKeyInputVisible = value == AuthorizationInputMode.ApiKey;
        IsPasswordInputVisible = value == AuthorizationInputMode.Password;
        IsAuthorizationInputVisible = value != AuthorizationInputMode.None;
    }

    private void CaptureSavedSettingsBaseline(BitwardenClientConfiguration bitwardenClientConfiguration)
    {
        _savedBaseAddress = bitwardenClientConfiguration.base_address;
        _savedEmail = bitwardenClientConfiguration.email;
    }

    private void RefreshAuthorizationState()
    {
        var snapshot = AuthorizationStateHelper.GetSnapshot(BitwardenClientConfiguration);
        AuthorizationStatusText = snapshot.StatusText;
        AuthorizationMethodText = snapshot.MethodText;
        AuthorizationDetailText = snapshot.DetailText;
        CanStartAuthorization = BitwardenClientConfiguration?.HasSavedSettings() == true;
        CanClearStoredAuthorization = BitwardenClientConfiguration is not null
            && (!BitwardenClientConfiguration.encryption_key.IsNullOrEmpty()
                || BitwardenClientConfiguration.HasStoredAuthorizationMaterial()
                || !string.IsNullOrWhiteSpace(BitwardenClientConfiguration.authorization_method)
                || BitwardenClientConfiguration.authorization_invalidated);
    }

    private void ClearEphemeralAuthorizationInputs()
    {
        MasterPassword = null;
        Totp = null;
        AuthorizationClientId = null;
        AuthorizationClientSecret = null;
    }

    private void UpdateConfiguredState()
    {
        _state.SetState(BitwardenClientConfiguration is not null && BitwardenClientConfiguration.Validate()
            ? AutoTypeConfigurationStates.Configured
            : AutoTypeConfigurationStates.NotConfigured);
    }

    [RelayCommand]
    public void OpenStartupAppsSettings()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "ms-settings:startupapps",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Unable to open Windows Startup Apps settings.");
        }
    }

    [RelayCommand]
    public void SaveSettings()
    {
        if (BitwardenClientConfiguration == null)
        {
            return;
        }

        var accountChanged = !string.Equals(_savedBaseAddress, BitwardenClientConfiguration.base_address, StringComparison.Ordinal)
            || !string.Equals(_savedEmail, BitwardenClientConfiguration.email, StringComparison.OrdinalIgnoreCase);

        if (accountChanged)
        {
            _bitwardenService?.InvalidateStoredAuthorization("Account settings changed. Re-authorize this device to continue.");
        }

        if (!accountChanged)
        {
            UpdateConfiguredState();
            _save?.Invoke(BitwardenClientConfiguration);
        }

        CaptureSavedSettingsBaseline(BitwardenClientConfiguration);
        RefreshAuthorizationState();
    }

    [RelayCommand]
    public void BeginApiKeyAuthorization()
    {
        AuthorizationClientId = BitwardenClientConfiguration?.client_id;
        AuthorizationClientSecret = BitwardenClientConfiguration?.client_secret;
        AuthorizationInputMode = AuthorizationInputMode.ApiKey;
    }

    [RelayCommand]
    public void BeginPasswordAuthorization()
    {
        AuthorizationInputMode = AuthorizationInputMode.Password;
    }

    [RelayCommand]
    public void CancelAuthorizationInput()
    {
        AuthorizationInputMode = AuthorizationInputMode.None;
        ClearEphemeralAuthorizationInputs();
    }

    [RelayCommand]
    public async Task CompleteAuthorization()
    {
        if (BitwardenClientConfiguration == null || _bitwardenService == null)
        {
            return;
        }

        var success = AuthorizationInputMode switch
        {
            AuthorizationInputMode.ApiKey => await _bitwardenService.AuthorizeWithApiKeyAsync(
                MasterPassword ?? string.Empty,
                AuthorizationClientId ?? string.Empty,
                AuthorizationClientSecret ?? string.Empty),
            AuthorizationInputMode.Password => await _bitwardenService.AuthorizeWithPasswordAsync(MasterPassword ?? string.Empty, Totp),
            _ => false,
        };

        ClearEphemeralAuthorizationInputs();
        CaptureSavedSettingsBaseline(BitwardenClientConfiguration);
        RefreshAuthorizationState();

        if (success)
        {
            AuthorizationInputMode = AuthorizationInputMode.None;
            return;
        }

        _logger?.LogWarning("Authorization attempt failed for the current settings workflow.");
    }

    [RelayCommand]
    public void ClearStoredAuthorization()
    {
        if (BitwardenClientConfiguration == null)
        {
            return;
        }

        if (_bitwardenService != null)
        {
            _bitwardenService.ClearStoredAuthorization(clearApiCredentials: true);
        }
        else
        {
            AuthorizationStateHelper.ClearStoredAuthorization(BitwardenClientConfiguration, clearApiCredentials: true);
            UpdateConfiguredState();
            _save?.Invoke(BitwardenClientConfiguration);
        }

        AuthorizationInputMode = AuthorizationInputMode.None;
        ClearEphemeralAuthorizationInputs();
        RefreshAuthorizationState();
    }
}