using System;
using System.IO;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using Bitwarden.AutoType.Desktop.Services;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class OperationFailureFormatterTests
{
    [Fact]
    public void FormatMapsNetworkFailuresToFriendlyMessage()
    {
        var failure = OperationFailureFormatter.Format("Sync failed", new HttpRequestException("Connection refused"));

        Assert.Equal("Sync failed", failure.Summary);
        Assert.Contains("could not be reached", failure.Detail);
    }

    [Fact]
    public void FormatMapsAuthorizationInvalidationToReauthorizationRequired()
    {
        var failure = OperationFailureFormatter.Format(
            "Sync failed",
            new InvalidOperationException("Stored authorization was invalidated after the server KDF settings changed."));

        Assert.Equal("Re-authorization required", failure.Summary);
        Assert.Contains("Re-authorize this device", failure.Detail);
    }

    [Fact]
    public void FormatMapsCryptographicFailuresToPasswordGuidance()
    {
        var failure = OperationFailureFormatter.Format("Decryption failed", new CryptographicException("bad password"));

        Assert.Equal("Decryption failed", failure.Summary);
        Assert.Contains("password is incorrect", failure.Detail);
    }

    [Fact]
    public void FormatMapsCanceledOperationsToFriendlyMessage()
    {
        var failure = OperationFailureFormatter.Format("Backup failed", new OperationCanceledException("cancelled"));

        Assert.Equal("Backup failed", failure.Summary);
        Assert.Contains("canceled", failure.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(typeof(IOException))]
    [InlineData(typeof(UnauthorizedAccessException))]
    [InlineData(typeof(SecurityException))]
    public void FormatMapsFileAccessFailuresToFriendlyMessage(Type exceptionType)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "disk problem")!;

        var failure = OperationFailureFormatter.Format("Backup failed", exception);

        Assert.Equal("Backup failed", failure.Summary);
        Assert.Contains("file", failure.Detail, StringComparison.OrdinalIgnoreCase);
    }
}