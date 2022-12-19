using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

[JsonSerializable(typeof(TokenResponse))]
public class TokenResponse
{
    public int Kdf { get; set; }
    public int KdfIterations { get; set; }
    public string? Key { get; set; }
    public string? PrivateKey { get; set; }
    public bool ResetMasterPassword { get; set; }
    public string? access_token { get; set; }
    public int expires_in { get; set; }
    public string? scope { get; set; }
    public string? token_type { get; set; }
    public bool unofficialServer { get; set; }

    #region TwofactorRequred

    public int[]? TwoFactorProviders { get; set; }
    public Twofactorproviders2? TwoFactorProviders2 { get; set; }
    public string? error { get; set; }
    public string? error_description { get; set; }

    #endregion TwofactorRequred

    #region Password + TwoFactor Response

    public string? TwoFactorToken { get; set; }

    public string? refresh_token { get; set; }

    #endregion Password + TwoFactor Response

    #region Invalid TOTP Code

    public Errormodel? ErrorModel { get; set; }
    public object? ExceptionMessage { get; set; }
    public object? ExceptionStackTrace { get; set; }
    public object? InnerExceptionMessage { get; set; }
    public string? Message { get; set; }
    public string? Object { get; set; }
    public Validationerrors? ValidationErrors { get; set; }

    #endregion Invalid TOTP Code

    public class Twofactorproviders2
    {
        public string? _0 { get; set; }
    }

    public class Errormodel
    {
        public string? Message { get; set; }
        public string? Object { get; set; }
    }

    public class Validationerrors
    {
        public string[]? _ { get; set; }
    }
}