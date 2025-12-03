using System;
using NuGet.Versioning;
using Velopack;

namespace Bitwarden.AutoType.Desktop;

/// <summary>
/// Application entry point with Velopack integration.
/// </summary>
public static class Program
{
    /// <summary>
    /// Application entry point.
    /// </summary>
    /// <remarks>
    /// Velopack MUST be initialized before any other code runs.
    /// This handles install, uninstall, and update hooks.
    /// </remarks>
    [STAThread]
    public static void Main(string[] args)
    {
        // Velopack initialization - MUST be first!
        // This handles:
        // - First-run after install (creating shortcuts, etc.)
        // - Uninstall cleanup
        // - Update application (applying updates)
        // If running in a Velopack context, this may exit the app early
        VelopackApp.Build()
            .WithFirstRun(v => OnFirstRun(v))
            .Run();

        // Normal WPF startup
        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    /// <summary>
    /// Called on first run after installation.
    /// </summary>
    private static void OnFirstRun(SemanticVersion version)
    {
        // Optional: Show welcome message, open documentation, etc.
        // For now, we just let the app start normally
        System.Diagnostics.Debug.WriteLine($"First run after install: v{version}");
    }
}
