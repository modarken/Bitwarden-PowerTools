using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;

namespace Bitwarden.AutoType.Desktop;

public class TestService : WPFBackgroundService
{
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly IHost _host;

    public App()
    {
        AppDomain.CurrentDomain.UnhandledException += UnhandledException;
        SystemEvents.SessionEnding += OnSessionEnding;

        _host = Host.CreateDefaultBuilder()
        .ConfigureUserLocalAppDataJsonFile<Settings>(DesktopConstants.DefaultDataFolderName, "appSettings.json",
            out Settings? appSettings, out Action<Settings>? saveSettingsToFile, true)
        .ConfigureServices((hostContext, services) => // configuration
        {
            services.AddSingleton(appSettings!.BitwardenClientConfiguration!);
            services.AddSingleton(new Action<BitwardenClientConfiguration>((c) => { saveSettingsToFile!.Invoke(appSettings!); }));
        })
        .ConfigureServices((hostContext, services) => // regular services
        {
            services.AddSingleton<AutoTypeService>();
            services.AddSingleton<HotkeyService>();
            services.AddSingleton<AutoTypeViewModel>();
            services.AddSingleton<MainWindow>();
        })
        .ConfigureServices((hostContext, services) => // hosted services
        {
            var config = services.BuildServiceProvider().GetService<BitwardenClientConfiguration>()!;
            var save = services.BuildServiceProvider().GetService<Action<BitwardenClientConfiguration>>()!;
            var bitwardenService = new BitwardenService(config, save);
            services.AddSingleton<BitwardenService>(bitwardenService);
            services.AddHostedService((sp) => bitwardenService);
            services.AddHostedService<TestService>();
        })

        .Build();
    }

    private async void Application_Startup(object sender, StartupEventArgs e)
    {
        await _host.StartAsync();
        var window = _host.Services.GetRequiredService<MainWindow>();
        //var window2 = _host.Services.GetRequiredService<TestService>();
        window.PositionWindow();
        window.Show();
    }

    private async void Application_Exit(object sender, ExitEventArgs e)
    {
        await CleanupAsync();
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        throw (Exception)e.ExceptionObject;
    }

    private async void OnSessionEnding(object sender, SessionEndingEventArgs e)
    {
        await CleanupAsync();
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