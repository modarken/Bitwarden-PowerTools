using System.Text.Json;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Services;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class BackupSettingsSerializationTests
{
    [Fact]
    public void ScheduledBackupPasswordIsWrittenProtectedAndRoundTrips()
    {
        var settings = new BackupSettings
        {
            ScheduledBackupPassword = "test-password",
            ScheduledBackupEnabled = true,
        };

        var json = JsonSerializer.Serialize(settings, HostBuilderExtensions.SerializerOptions);

        Assert.Contains("<Protected>", json);
        Assert.DoesNotContain("\"test-password\"", json);

        var roundTrip = JsonSerializer.Deserialize<BackupSettings>(json, HostBuilderExtensions.SerializerOptions);

        Assert.NotNull(roundTrip);
        Assert.Equal("test-password", roundTrip!.ScheduledBackupPassword);
    }
}