using System;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace Bitwarden.AutoType.Desktop;

// Example Template for WPFBackgroundService
//
//      public class TestService : WPFBackgroundService
//      {
//          protected async override Task ExecuteAsync(CancellationToken stoppingToken)
//          {
//              await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
//          }

//          public override void Dispose()
//          {
//              base.Dispose();
//          }
//      }
//
//      services.AddHostedService<TestService>();
//      var testService = _host.Services.GetRequiredService<TestService>();

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IHost Host => _host;
    private readonly IHost _host;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        SystemEvents.SessionEnding += OnSessionEnding;
#pragma warning disable CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).
        Console.CancelKeyPress += Console_CancelKeyPress; // Add this line
#pragma warning restore CS8622 // Nullability of reference types in type of parameter doesn't match the target delegate (possibly because of nullability attributes).

        _host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
        .ConfigureUserLocalAppDataJsonFile(BitwardenConstants.DefaultDataFolderName, "settings.json",
            out Settings? appSettings, out Action<Settings>? saveSettingsToFile, true)
        .ConfigureUserLocalAppDataJsonFile(BitwardenConstants.DefaultDataFolderName, "autotype.settings.json",
            out AutoTypeSettings? autoTypeSettings, out Action<AutoTypeSettings>? autoTypeSaveSettingsToFile, true)
        .ConfigureServices((hostContext, services) => // configuration
        {
            services.AddSingleton(appSettings!.BitwardenClientConfiguration!);
            services.AddSingleton(new Action<BitwardenClientConfiguration>((c) => { saveSettingsToFile!.Invoke(appSettings!); }));
            services.AddSingleton(autoTypeSettings!);
            services.AddSingleton(new Action<AutoTypeService>((c) => { autoTypeSaveSettingsToFile!.Invoke(autoTypeSettings!); }));
            RegisterRegularServices(services);
            RegisterHostedServices(services);
        })

        .Build();
    }

    private void RegisterRegularServices(IServiceCollection services)
    {
        services.AddSingleton<AutoTypeService>();
        services.AddSingleton<HotkeyService>();
        services.AddSingleton<AutoTypeViewModel>();
        services.AddSingleton<SettingsControlViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<System.Windows.Forms.NotifyIcon>();
        services.AddSingleton<NotifyIconService>();
        services.AddSingleton(this);
    }

    private void RegisterHostedServices(IServiceCollection services)
    {
        var logger = services.BuildServiceProvider().GetService<ILogger<BitwardenService>>()!;
        var config = services.BuildServiceProvider().GetService<BitwardenClientConfiguration>()!;
        var save = services.BuildServiceProvider().GetService<Action<BitwardenClientConfiguration>>()!;
        var bitwardenService = new BitwardenService(logger, config, save);
        services.AddSingleton<BitwardenService>(bitwardenService);
        services.AddHostedService((sp) => bitwardenService);
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();
        var window = _host.Services.GetRequiredService<MainWindow>();

        //window.PositionWindow();
        //window.Show();

        var iconService = _host.Services.GetRequiredService<NotifyIconService>();
        iconService.Configure(window);
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        await CleanupAsync();
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var logger = _host.Services.GetRequiredService<ILogger<App>>();
        logger.LogError((Exception)e.ExceptionObject, "Unhandled exception occurred");
    }

    private async void OnSessionEnding(object sender, SessionEndingEventArgs e)
    {
        await CleanupAsync();
    }

    private async void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
        e.Cancel = true; // Prevent the process from being terminated immediately

        await App.Current.Dispatcher.InvokeAsync(() =>
        {
            App.Current.Shutdown();
        });
    }

    private async Task CleanupAsync()
    {
        using var host = _host;

        try
        {
            await host.StopAsync();
        }
        catch
        {
            // Handle exceptions, if necessary.
        }
        finally
        {
            // Perform any additional cleanup operations here.
        }
    }
}