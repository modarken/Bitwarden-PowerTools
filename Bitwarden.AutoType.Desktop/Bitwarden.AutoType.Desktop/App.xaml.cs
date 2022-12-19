using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Bitwarden.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Bitwarden.AutoType.Desktop;

[JsonSerializable(typeof(Settings))]
public class Settings
{
    public BitwardenClientConfiguration? BitwardenClientConfiguration { get; set; }
}

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

public static class DesktopConstants
{
    public static readonly string ExecutionBinaryPathKey = "ExecutionBinaryPath";
    public static readonly string DataFolderPathKey = "DataPath";
    public static readonly string DefaultDataFolderName = "BitwardentAutoType";
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
        .ConfigureHostConfiguration(config =>
        {
            var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DesktopConstants.DefaultDataFolderName);

            Directory.CreateDirectory(dataPath); // create folder if not exists

            var settingsPath = Path.Combine(dataPath, "settings.json");
            var syncPath = Path.Combine(dataPath, "sync.json");

            if (!File.Exists(settingsPath))
            {
                var options = new JsonSerializerOptions() { WriteIndented = true };
                var settingsString = JsonSerializer.Serialize(new Settings() { BitwardenClientConfiguration = new BitwardenClientConfiguration() }, options);
                File.WriteAllText(settingsPath, settingsString);
            }

            if (!File.Exists(syncPath))
            {
            }

            config.AddEnvironmentVariables();
            config.AddJsonFile(settingsPath, optional: false, reloadOnChange: false);
            config.AddJsonFile(syncPath, optional: true, reloadOnChange: false);
            config.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
        })
        .ConfigureAppConfiguration((hostingContext, configuration) =>
        {
            var dataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DesktopConstants.DefaultDataFolderName);
            hostingContext.Configuration[DesktopConstants.DataFolderPathKey] = dataPath;
            var executionbinaryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            hostingContext.Configuration[DesktopConstants.ExecutionBinaryPathKey] = executionbinaryPath;
        })

    .ConfigureServices((hostContext, services) =>
    {
        var settings = hostContext.Configuration.GetSection(nameof(BitwardenClientConfiguration)).Get<BitwardenClientConfiguration>();
        services.AddSingleton(settings!);
    })

       .ConfigureServices((hostContext, services) =>
       {
           services.AddHostedService<TestService>();
           services.AddSingleton<AutoTypeViewModel>();
           services.AddSingleton<MainWindow>();
       }).Build();
    }

    private void UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
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
}