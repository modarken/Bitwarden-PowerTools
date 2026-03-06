using System;
using System.Net.Http;
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
}