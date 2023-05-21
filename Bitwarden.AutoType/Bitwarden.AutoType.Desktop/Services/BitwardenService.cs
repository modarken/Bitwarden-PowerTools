using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Bitwarden.AutoType.Desktop.Helpers;
using Bitwarden.AutoType.Desktop.Models;
using Bitwarden.Core.API;
using Bitwarden.Core.Crypto;
using Bitwarden.Core.Models;
using Bitwarden.Utilities;
using Microsoft.Extensions.Logging;

namespace Bitwarden.AutoType.Desktop.Services;

public class BitwardenService : WPFBackgroundService
{
    private readonly ILogger<BitwardenService> _logger;
    private readonly BitwardenClientConfiguration _bitwardenClientConfiguration;

    private readonly Action<BitwardenClientConfiguration> _save;
    private readonly StateController<AutoTypeConfigurationStates> _state;
    private readonly List<Action<SyncResponse>> _syncResponseActions;
    private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
    private TokenResponse? _accessToken;
    private SyncResponse? _syncResponse;
    private DateTimeOffset? _revisionDate;

    public BitwardenService(ILogger<BitwardenService> logger,
        BitwardenClientConfiguration bitwardenClientConfiguration,
        Action<BitwardenClientConfiguration> save,
        StateController<AutoTypeConfigurationStates> state)
    {
        _logger = logger;
        _bitwardenClientConfiguration = bitwardenClientConfiguration;
        _save = save;
        _state = state;
        _syncResponseActions = new List<Action<SyncResponse>>();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var errorCount = 0;
        while (true)
        {
            try
            {
                _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Refreshing Database.");

                while(_state.GetState() == AutoTypeConfigurationStates.NotConfigured)
                {
                    _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waiting TimeSpan.FromSeconds(30).");
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
                    _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waited TimeSpan.FromSeconds(30).");
                }

                await _syncLock.WaitAsync(); // Acquire the lock asynchronously
                try
                {
                    await RefreshLocalDatabaseAsync()
                       .ConfigureAwait(false);
                }
                catch
                {
                    throw;
                }
                finally
                {
                    _syncLock.Release(); // Release the lock
                }

                errorCount = 0;

                _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Database Refreshed.");

                _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waiting TimeSpan.FromMinutes(15).");
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken).ConfigureAwait(false);
                _logger.Log(LogLevel.Trace, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Waited TimeSpan.FromMinutes(15).");
            }
            catch (TaskCanceledException e)
            {
                _logger.Log(LogLevel.Warning, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Exception:'{e.Message}'");
                break;
            }
            catch (Exception e)
            {
                errorCount++;
                _logger.Log(LogLevel.Error, $"{nameof(BitwardenService)}.{nameof(ExecuteAsync)}() Exception:'{e.Message}'");
                if (errorCount > 10)
                {
                    errorCount = 11;
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken).ConfigureAwait(false);
                }
                else
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }

    //private bool ValidateConfiguration()
    //{
    //    if ( string.IsNullOrWhiteSpace(_bitwardenClientConfiguration.base_address))
    //    {
    //        return false;
    //    }
    //    if (string.IsNullOrWhiteSpace(_bitwardenClientConfiguration.email))
    //    {
    //        return false;
    //    }
    //    //if (string.IsNullOrWhiteSpace(_bitwardenClientConfiguration.encryption_key))
    //    //{
    //    //    return false;
    //    //}
    //    if (string.IsNullOrWhiteSpace(_bitwardenClientConfiguration.device_name))
    //    {
    //        return false;
    //    }
    //    if (string.IsNullOrWhiteSpace(_bitwardenClientConfiguration.device_identifier))
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    private async Task RefreshAccessTokenIfNeededAsync()
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
                accessToken = await BitwardenProtocol.PostAccessTokenFromAPIKey(
                    _bitwardenClientConfiguration.base_address!,
                    _bitwardenClientConfiguration.client_id!,
                    _bitwardenClientConfiguration.client_secret!,
                    _bitwardenClientConfiguration.device_name!,
                    _bitwardenClientConfiguration.device_identifier!)
                    .ConfigureAwait(false);
            }

            if (_bitwardenClientConfiguration.refresh_token != null)
            {
                accessToken = await BitwardenProtocol.PostAccessTokenFromRefreshToken(
                    _bitwardenClientConfiguration.base_address!,
                    _bitwardenClientConfiguration.refresh_token!,
                    _bitwardenClientConfiguration.device_name!,
                    _bitwardenClientConfiguration.device_identifier!)
                    .ConfigureAwait(false);
            }

            if (accessToken == null)
            {
                throw new ArgumentNullException(nameof(accessToken));
            }

            _accessToken = accessToken;
        }
    }

    public async Task RefreshLocalDatabaseAsync()
    {
        if (_state.GetState() == AutoTypeConfigurationStates.NotConfigured)
        {
            throw new Exception("Not configured to refresh local database.");
        }

        await RefreshAccessTokenIfNeededAsync();

        string? revisonDate = await BitwardenProtocol.GetRevisionDate(
            _bitwardenClientConfiguration!.base_address!,
            _accessToken!.access_token!)
            .ConfigureAwait(false);

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
            SyncResponse? syncResponse = await BitwardenProtocol.GetSync(
                _bitwardenClientConfiguration!.base_address!,
                _accessToken!.access_token!)
                .ConfigureAwait(false);

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

    public async Task<SyncResponse> GetDatabase()
    {
        await _syncLock.WaitAsync(); // Acquire the lock asynchronously
        try
        {
            if (_syncResponse is null)
            {
                await RefreshLocalDatabaseAsync();
            }
            return _syncResponse ?? throw new ArgumentNullException(nameof(_syncResponse));
        }
        finally
        {
            _syncLock.Release(); // Release the lock
        }
    }

    public void RegisterOnDatabaseUpdated(Action<SyncResponse> onDatabaseUpdated)
    {
        _syncResponseActions.Add(onDatabaseUpdated);
    }

    public async Task<(string? EncryptionKey, TokenResponse? TokenResponse)> GetEncryptionKey(string masterPassword)
    {
        if (_bitwardenClientConfiguration.base_address is null || _bitwardenClientConfiguration.email is null)
        {
            return (null, null);
        }

        var preLogin = await BitwardenProtocol.PostPreLogin(_bitwardenClientConfiguration.base_address, _bitwardenClientConfiguration.email)
            .ConfigureAwait(false);

        if (preLogin is null)
        {
            return (null, null);
        }

        var masterKey = BitwardenCrypto.DerriveMasterKey(masterPassword, _bitwardenClientConfiguration.email, preLogin.KdfIterations);

        var masterPasswordHash = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

        var tokenResponse = await BitwardenProtocol.PostAccessTokenFromAPIKey(
            _bitwardenClientConfiguration.base_address,
            _bitwardenClientConfiguration.client_id!,
            _bitwardenClientConfiguration.client_secret!,
            _bitwardenClientConfiguration.device_identifier!,
            _bitwardenClientConfiguration.device_name!)
            .ConfigureAwait(false);

        if (tokenResponse is null)
        {
            return (null, null);
        }

        var protectedKey = tokenResponse.Key;

        var encryptionKeyBytes = BitwardenCrypto.DecryptEncryptionKey(protectedKey!, masterKey);
        if (encryptionKeyBytes is null)
        {
            return (null, null);
        }

        var encryptionKey = Convert.ToBase64String(encryptionKeyBytes);

        return (encryptionKey, tokenResponse);
    }

    public async Task<(string? EncryptionKey, TokenResponse? TokenResponse)> GetEncryptionKey(string masterPassword, string? twoFactorToken)
    {
        if (_bitwardenClientConfiguration.base_address is null || _bitwardenClientConfiguration.email is null)
        {
            return (null, null);
        }

        var preLogin = await BitwardenProtocol.PostPreLogin(_bitwardenClientConfiguration.base_address, _bitwardenClientConfiguration.email)
            .ConfigureAwait(false);

        if (preLogin is null)
        {
            return (null, null);
        }

        var masterKey = BitwardenCrypto.DerriveMasterKey(masterPassword, _bitwardenClientConfiguration.email, preLogin.KdfIterations);
        var masterPasswordHash = BitwardenCrypto.DerriveMasterPasswordHashFromMasterKey(masterKey, Encoding.ASCII.GetBytes(masterPassword));

        TokenResponse? tokenResponse = null;
        if (twoFactorToken is null)
        {
            tokenResponse = await BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(
                _bitwardenClientConfiguration.base_address,
                _bitwardenClientConfiguration.email,
                masterPasswordHash,
                _bitwardenClientConfiguration.device_identifier!,
                _bitwardenClientConfiguration.device_name!)
                .ConfigureAwait(false);
        }
        else
        {
            tokenResponse = await BitwardenProtocol.PostAccessTokenFromMasterPasswordHash(
                _bitwardenClientConfiguration.base_address,
                _bitwardenClientConfiguration.email,
                masterPasswordHash,
                twoFactorToken,
                _bitwardenClientConfiguration.device_identifier!,
                _bitwardenClientConfiguration.device_name!)
                .ConfigureAwait(false);
        }

        if (tokenResponse is null)
        {
            return (null, null);
        }

        var protectedKey = tokenResponse.Key;

        var encryptionKeyBytes = BitwardenCrypto.DecryptEncryptionKey(protectedKey!, masterKey);
        if (encryptionKeyBytes is null)
        {
            return (null, null);
        }

        var encryptionKey = Convert.ToBase64String(encryptionKeyBytes);

        return (encryptionKey, tokenResponse);
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