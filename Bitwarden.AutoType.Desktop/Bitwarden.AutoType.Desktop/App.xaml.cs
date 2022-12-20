using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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

        _host = Host.CreateDefaultBuilder()
        .ConfigureUserLocalAppDataJsonFile<Settings>(DesktopConstants.DefaultDataFolderName, "settings.json",
            out Settings? settings, out Action<Settings>? onSave, true)
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton(settings!.BitwardenClientConfiguration!);
            services.AddSingleton(new Action<BitwardenClientConfiguration>((c) => { onSave!.Invoke(settings!); }));
        })
        .ConfigureServices((hostContext, services) =>
        {
            services.AddSingleton<AutoTypeService>();
            services.AddSingleton<BitwardenService>();
            services.AddSingleton<HotkeyService>();
            services.AddHostedService<TestService>();
            services.AddSingleton<AutoTypeViewModel>();
            services.AddSingleton<MainWindow>();
        }).Build();
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
        using var host = _host;
        try
        {
            await host.StopAsync();
        }
        catch
        {
        }
        finally
        {
        }
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        throw (Exception)e.ExceptionObject;
    }
}