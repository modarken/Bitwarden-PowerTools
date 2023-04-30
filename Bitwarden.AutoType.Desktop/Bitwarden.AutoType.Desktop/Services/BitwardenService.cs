using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.Core.API;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;

namespace Bitwarden.AutoType.Desktop.Services;

public class BitwardenService : WPFBackgroundService
{
    //private readonly ILogger<BitwardenService> _logger;
    private readonly BitwardenClientConfiguration _bitwardenClientConfiguration;

    private readonly Action<BitwardenClientConfiguration> _save;
    private TokenResponse? _accessToken;
    private SyncResponse? _syncResponse;
    private DateTimeOffset? _revisionDate;
    private readonly List<Action<SyncResponse>> _syncResponseActions;
    private readonly object _syncLock = new();

    public BitwardenService(BitwardenClientConfiguration bitwardenClientConfiguration, Action<BitwardenClientConfiguration> save)
    {
        //_logger = logger;
        _bitwardenClientConfiguration = bitwardenClientConfiguration;
        _save = save;
        _syncResponseActions = new List<Action<SyncResponse>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(false);
        while (true)
        {
            try
            {
                //_logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Refreshing Database.");
                lock (_syncLock)
                {
                    RefreshLocalDatabase();
                }
                //_logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Database Refreshed.");

                //_logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waiting TimeSpan.FromMinutes(15).");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken).ConfigureAwait(false);
                //logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waited TimeSpan.FromMinutes(15).");
            }
            //catch (TaskCanceledException e)
            catch (TaskCanceledException)
            {
                //_logger.Log(LogLevel.Warning, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Exception:'{e.Message}'");
            }
            //catch (Exception e)
            catch (Exception)
            {
                //_logger.Log(LogLevel.Error, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Exception:'{e.Message}'");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
            }
        }
    }

    private void RefreshAccessTokenIfNeeded()
    {
        // check if access token is valid, if not , make it null so it will be refreshed
        if (_accessToken != null)
        {
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwt = jwtHandler.ReadJwtToken(_accessToken.access_token);
            var exp = jwt.Claims.FirstOrDefault(c => c.Type == "exp")?.Value;
            if (exp != null)
            {
                var expDate = DateTimeOffset.FromUnixTimeSeconds(long.Parse(exp!));
                if (expDate < DateTimeOffset.UtcNow)
                {
                    _accessToken = null;
                }
            }
        }

        // if access token is null, get a new one
        if (_accessToken == null)
        {
            TokenResponse? accessToken = default;
            if (_bitwardenClientConfiguration.client_id != null && _bitwardenClientConfiguration.client_secret != null)
            {
                accessToken = BitwardenProtocol.PostAccessTokenFromAPIKey(
                    _bitwardenClientConfiguration.base_address!,
                    _bitwardenClientConfiguration.client_id!,
                    _bitwardenClientConfiguration.client_secret!,
                    _bitwardenClientConfiguration.device_name!,
                    _bitwardenClientConfiguration.device_identifier!)
                    .GetAwaiter().GetResult();
            }

            if (_bitwardenClientConfiguration.refresh_token != null)
            {
                accessToken = BitwardenProtocol.PostAccessTokenFromRefreshToken(
                    _bitwardenClientConfiguration.base_address!,
                    _bitwardenClientConfiguration.refresh_token!,
                    _bitwardenClientConfiguration.device_name!,
                    _bitwardenClientConfiguration.device_identifier!)
                    .GetAwaiter().GetResult();
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            _accessToken = accessToken;
        }
    }

    public void RefreshLocalDatabase()
    {
        RefreshAccessTokenIfNeeded();

        string? revisonDate = BitwardenProtocol.GetRevisionDate(
            _bitwardenClientConfiguration!.base_address!,
            _accessToken!.access_token!).GetAwaiter().GetResult();

        if (revisonDate == null)
        {
            throw new ArgumentNullException(nameof(revisonDate));
        }

        // If I decide to sync the revsion date, I need to do it here

        //if (revisonDate != null)
        //{
        //    _bitwardenClientConfiguration.revision_date = revisonDate;
        //    _save(_bitwardenClientConfiguration);
        //}

        var revDate = Converters.ConvertEpochStringToDateTimeOffset(revisonDate);

        if (_revisionDate is null || revDate > _revisionDate)
        {
            _revisionDate = revDate;
            // since we dont have a revion date or the rev date is stale
            // lets clear out the data so we get a new fresh copy
            _syncResponse = null;
        }

        if (_syncResponse is null)
        {
            SyncResponse? syncResponse = BitwardenProtocol.GetSync(
                _bitwardenClientConfiguration!.base_address!,
                _accessToken!.access_token!).GetAwaiter().GetResult();

            if (syncResponse is null)
            {
                throw new ArgumentNullException(nameof(syncResponse));
            }

            _syncResponse = syncResponse;

            if (_syncResponseActions is not null)
            {
                foreach (var action in _syncResponseActions)
                {
                    action?.Invoke(_syncResponse);
                }
            }
        }
    }

    public SyncResponse GetDatabase()
    {
        lock (_syncLock)
        {
            if (_syncResponse is null)
            {
                RefreshLocalDatabase();
            }

            return _syncResponse ?? throw new ArgumentNullException(nameof(_syncResponse));
        }
    }

    public void RegisterOnDatabaseUpdated(Action<SyncResponse> onDatabaseUpdated)
    {
        if (_syncResponseActions != null && onDatabaseUpdated != null)
        {
            _syncResponseActions.Add(onDatabaseUpdated);
        }
    }

    public string? GetEncryptionKey(string masterPassword, out TokenResponse? tokenResponse)
    {
        tokenResponse = default;
        if (_bitwardenClientConfiguration.base_address is null || _bitwardenClientConfiguration.email is null)
        {
            return null;
        }

        var preLogin = BitwardenProtocol.PostPreLogin(_bitwardenClientConfiguration.base_address, _bitwardenClientConfiguration.email).GetAwaiter().GetResult();

        if (preLogin is null)
        {
            return null;
        }

        var masterKey = BitwardenCrypto.DerriveMasterKey(masterPassword, _bitwardenClientConfiguration.email, preLogin.KdfIterations);

        var masterPasswordHash = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

        tokenResponse = BitwardenProtocol.PostAccessTokenFromAPIKey(
            _bitwardenClientConfiguration.base_address,
            _bitwardenClientConfiguration.client_id,
            _bitwardenClientConfiguration.client_secret,
            _bitwardenClientConfiguration.device_identifier,
            _bitwardenClientConfiguration.device_name).GetAwaiter().GetResult();

        if (tokenResponse is null)
        {
            return null;
        }

        var protectedKey = tokenResponse.Key;

        var encryptionKeyBytes = BitwardenCrypto.DecryptEncryptionKey(protectedKey, masterKey);
        if (encryptionKeyBytes is null)
        {
            return null;
        }

        var encryptionKey = Convert.ToBase64String(encryptionKeyBytes);

        return encryptionKey;
    }

    public string? GetEncryptionKey(string masterPassword, string? twoFactorToken, out TokenResponse? tokenResponse)
    {
        tokenResponse = default;
        if (_bitwardenClientConfiguration.base_address is null || _bitwardenClientConfiguration.email is null)
        {
            return null;
        }

        var preLogin = BitwardenProtocol.PostPreLogin(_bitwardenClientConfiguration.base_address, _bitwardenClientConfiguration.email).GetAwaiter().GetResult();

        if (preLogin is null)
        {
            return null;
        }

        var masterKey = BitwardenCrypto.DerriveMasterKey(masterPassword, _bitwardenClientConfiguration.email, preLogin.KdfIterations);
        var masterPasswordHash = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

        if (twoFactorToken is null)
        {
            tokenResponse = BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(
                _bitwardenClientConfiguration.base_address,
                _bitwardenClientConfiguration.email,
                masterPasswordHash,
                _bitwardenClientConfiguration.device_identifier,
                _bitwardenClientConfiguration.device_name).GetAwaiter().GetResult();
        }
        else
        {
            tokenResponse = BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(
                _bitwardenClientConfiguration.base_address,
                _bitwardenClientConfiguration.email,
                masterPasswordHash,
                twoFactorToken,
                _bitwardenClientConfiguration.device_identifier,
                _bitwardenClientConfiguration.device_name).GetAwaiter().GetResult();
        }

        if (tokenResponse is null)
        {
            return null;
        }

        var protectedKey = tokenResponse.Key;

        var encryptionKeyBytes = BitwardenCrypto.DecryptEncryptionKey(protectedKey, masterKey);
        if (encryptionKeyBytes is null)
        {
            return null;
        }

        var encryptionKey = Convert.ToBase64String(encryptionKeyBytes);

        return encryptionKey;
    }

    public byte[]? GetEncryptionKey()
    {
        if (_bitwardenClientConfiguration?.encryption_key is null) return null;

        var encryption_key = Convert.FromBase64String(_bitwardenClientConfiguration.encryption_key!);

        return encryption_key;
    }

    public override void Dispose()
    {
        base.Dispose();
    }
}