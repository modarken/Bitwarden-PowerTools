using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Bitwarden.AutoType.Desktop.Services;
using Bitwarden.Core.Models;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Bitwarden.AutoType.Desktop.Tests;

public class AccessTokenUtilitiesTests
{
    [Fact]
    public void ShouldRefreshReturnsTrueWhenTokenIsMissing()
    {
        Assert.True(AccessTokenUtilities.ShouldRefresh(null, DateTimeOffset.UtcNow));
        Assert.True(AccessTokenUtilities.ShouldRefresh(new TokenResponse(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ShouldRefreshReturnsFalseForValidFutureToken()
    {
        var now = DateTimeOffset.UtcNow;
        var token = new TokenResponse { access_token = CreateJwt(now.AddMinutes(10)) };

        Assert.False(AccessTokenUtilities.ShouldRefresh(token, now));
    }

    [Fact]
    public void ShouldRefreshReturnsTrueForExpiredOrMalformedToken()
    {
        var now = DateTimeOffset.UtcNow;
        var expiredToken = new TokenResponse { access_token = CreateJwt(now.AddMinutes(-1)) };
        var malformedToken = new TokenResponse { access_token = "not-a-jwt" };

        Assert.True(AccessTokenUtilities.ShouldRefresh(expiredToken, now));
        Assert.True(AccessTokenUtilities.ShouldRefresh(malformedToken, now));
    }

    private static string CreateJwt(DateTimeOffset expiresAt)
    {
        var token = new JwtSecurityToken(
            claims: [new Claim("exp", expiresAt.ToUnixTimeSeconds().ToString())],
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}