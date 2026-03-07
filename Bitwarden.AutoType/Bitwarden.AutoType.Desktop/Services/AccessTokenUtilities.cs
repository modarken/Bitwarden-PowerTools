using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Bitwarden.Core.Models;

namespace Bitwarden.AutoType.Desktop.Services;

public static class AccessTokenUtilities
{
    public static bool ShouldRefresh(TokenResponse? accessToken, DateTimeOffset utcNow)
    {
        if (accessToken is null || string.IsNullOrWhiteSpace(accessToken.access_token))
        {
            return true;
        }

        try
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(accessToken.access_token);
            var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;

            if (string.IsNullOrWhiteSpace(exp) || !long.TryParse(exp, out var expUnixSeconds))
            {
                return true;
            }

            var expDate = DateTimeOffset.FromUnixTimeSeconds(expUnixSeconds);
            return expDate <= utcNow;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }
}