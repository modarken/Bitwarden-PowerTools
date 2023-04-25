using System;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Bitwarden.AutoType.Desktop;

[INotifyPropertyChanged]
public partial class SettingsControlViewModel
{
    [ObservableProperty]
    private BitwardenClientConfiguration? _bitwardenClientConfiguration;

    private readonly Action<BitwardenClientConfiguration>? _save;
    private readonly BitwardenService? _bitwardenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsControlViewModel"/> class.
    ///
    /// This is only for WPF
    ///
    /// </summary>
    public SettingsControlViewModel()
    {
        _bitwardenClientConfiguration = default;
        _save = default;
        _bitwardenService = default;
    }

    public SettingsControlViewModel(BitwardenClientConfiguration bitwardenClientConfiguration,
        Action<BitwardenClientConfiguration> save,
        BitwardenService bitwardenService)
    {
        _bitwardenClientConfiguration = bitwardenClientConfiguration;
        _save = save;
        _bitwardenService = bitwardenService;
    }

    [RelayCommand]
    public void SaveConfig()
    {
        if (_save != null && BitwardenClientConfiguration != null)
        {
            _save(BitwardenClientConfiguration);
        }
    }
}