# SSL Certificate Validation Configuration

## Overview

Added configurable SSL certificate validation to address security concerns with self-hosted Bitwarden deployments while giving users control.

## Problem Solved

**Original Issue:**  
- SSL certificates were always bypassed (hardcoded `ServerCertificateCustomValidationCallback = true`)
- Users had no choice whether to validate certificates
- Settings overwrote on updates, losing user configuration

**Solutions Implemented:**
1. **Configurable SSL Validation**: Users can now enable/disable certificate validation via settings UI
2. **Settings Preservation**: Fixed `alwaysWriteFileOnLoad` to prevent existing settings from being overwritten during updates

---

## Changes Made

### 1. Configuration Property Added

**File:** `Libraries/Bitwardent.Utilities/BitwardenClientConfiguration.cs`

```csharp
/// <summary>
/// Gets or sets whether to allow invalid/self-signed SSL certificates.
/// Set to true for self-hosted Bitwarden instances with self-signed certificates.
/// WARNING: This disables SSL certificate validation. Only use with servers you trust.
/// </summary>
public bool AllowInvalidCertificates { get; set; } = true; // Default: true for backward compatibility
```

- Defaults to `true` for backward compatibility with existing installations
- Persisted to `%LocalAppData%\Bitwarden-PowerTools\client.settings.json`

---

### 2. API Factory Logic Updated

**File:** `Libraries/Bitwarden.Core/API/BitwardenApiFactory.cs`

**Before:**
```csharp
private static IBitwardenApi CreateApi(string baseAddress)
{
    var handler = new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual,
        ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true
    };
    // ...
}
```

**After:**
```csharp
private static IBitwardenApi CreateApi(string baseAddress, bool allowInvalidCertificates)
{
    var handler = new HttpClientHandler
    {
        ClientCertificateOptions = ClientCertificateOption.Manual
    };

    if (allowInvalidCertificates)
    {
        // SECURITY NOTE: Certificate validation is bypassed...
        handler.ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, certChain, policyErrors) => true;
    }
    // ...
}
```

**Signature Change:**
```csharp
public static IBitwardenApi GetApi(string baseAddress, bool allowInvalidCertificates = true)
```

---

### 3. Protocol Methods Updated

**File:** `Libraries/Bitwarden.Core/API/BitwardenProtocol.cs`

All 8 public methods now accept `bool allowInvalidCertificates = true` parameter:

- `PostPreLogin()`
- `PostAccessTokenFromAPIKey()`
- `PostAccessTokenFromRefreshToken()`
- `PostAccessTokenFromMasterPasswordHash()` (2 overloads)
- `GetRevisionDate()`
- `GetProfile()`
- `GetSync()`

Each method passes the parameter through to `BitwardenApiFactory.GetApi()`.

---

### 4. Service Layer Updated

**File:** `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Services/BitwardenService.cs`

All calls to `BitwardenProtocol` methods now pass `_bitwardenClientConfiguration.AllowInvalidCertificates`:

```csharp
// Example
var accessToken = await BitwardenProtocol.PostAccessTokenFromAPIKey(
    _bitwardenClientConfiguration.base_address!,
    _bitwardenClientConfiguration.client_id!,
    _bitwardenClientConfiguration.client_secret!,
    _bitwardenClientConfiguration.device_name!,
    _bitwardenClientConfiguration.device_identifier!,
    _bitwardenClientConfiguration.AllowInvalidCertificates) // ← Added
    .ConfigureAwait(false);
```

**Total Updates:** 9 method calls updated

---

### 5. Settings UI Enhanced

**File:** `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Views/SettingsControl.xaml`

Added checkbox with security icon and clear labeling:

```xaml
<CheckBox Grid.Row="2" Grid.Column="2" Margin="0 0 0 10"
          IsChecked="{Binding Path=BitwardenClientConfiguration.AllowInvalidCertificates, Mode=TwoWay}">
    <StackPanel Orientation="Horizontal">
        <iconPacks:PackIconMaterial Kind="ShieldAlert" Foreground="Orange" Width="16" Height="16" />
        <TextBlock Text="Allow Invalid SSL Certificates (for self-hosted with self-signed certs)"/>
    </StackPanel>
</CheckBox>
```

**Location:** Displayed between "Email" and "Access Method" fields

**Visual:** Orange shield alert icon to indicate security setting

---

### 6. Settings Migration Fixed

**File:** `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/App.xaml.cs`

**Before:**
```csharp
.ConfigureUserLocalAppDataJsonFile(BitwardenConstants.DefaultDataFolderName, "client.settings.json",
    out BitwardenClientConfiguration? clientSettings, out Action<BitwardenClientConfiguration>? saveClientSettingsToFile, true) // ← Always overwrites
```

**After:**
```csharp
.ConfigureUserLocalAppDataJsonFile(BitwardenConstants.DefaultDataFolderName, "client.settings.json",
    out BitwardenClientConfiguration? clientSettings, out Action<BitwardenClientConfiguration>? saveClientSettingsToFile, false) // ← Preserves existing settings
```

**Impact:**  
- Changed `alwaysWriteFileOnLoad` from `true` to `false` for all 3 settings files:
  - `client.settings.json`
  - `autotype.settings.json`
  - `backup.settings.json`
- Existing user settings are now preserved during updates
- New properties get default values when missing from JSON

---

## User Experience

### For New Users

1. Install application
2. Open Settings
3. **Default:** "Allow Invalid SSL Certificates" is **checked** (backward compatible)
4. For trusted self-hosted servers with self-signed certs: Leave checked
5. For cloud Bitwarden or properly configured servers: **Uncheck** for security

### For Existing Users (Upgrading)

1. Update to new version
2. **Existing settings preserved** (including any manual edits)
3. New `AllowInvalidCertificates` property added with default value `true`
4. No behavior change unless user explicitly changes the setting

### Settings File Example

**File:** `%LocalAppData%\Bitwarden-PowerTools\client.settings.json`

```json
{
  "base_address": "PROTECTED_DATA_HERE",
  "email": "PROTECTED_DATA_HERE",
  "encryption_key": "PROTECTED_DATA_HERE",
  "client_id": "PROTECTED_DATA_HERE",
  "client_secret": "PROTECTED_DATA_HERE",
  "refresh_token": null,
  "device_name": "PROTECTED_DATA_HERE",
  "device_identifier": "PROTECTED_DATA_HERE",
  "AllowInvalidCertificates": true
}
```

---

## Security Implications

### When ENABLED (AllowInvalidCertificates = true)

⚠️ **WARNING:**
- SSL/TLS certificate validation is **completely bypassed**
- Application will trust **any** certificate, including:
  - Self-signed certificates
  - Expired certificates
  - Certificates with mismatched hostnames
  - Certificates from untrusted CAs
- Vulnerable to Man-in-the-Middle (MITM) attacks

**Use Cases:**
- Self-hosted Bitwarden with self-signed certificates
- Internal corporate deployments with private CAs
- Development/testing environments

**Recommendation:** Only use with servers you completely trust and control

### When DISABLED (AllowInvalidCertificates = false)

✅ **SECURE:**
- Full SSL/TLS certificate validation enforced
- Only trusted certificates accepted
- Protection against MITM attacks

**Use Cases:**
- Cloud Bitwarden (vault.bitwarden.com)
- Self-hosted Bitwarden with properly configured Let's Encrypt or commercial certificates
- Production environments

**Recommendation:** Use this setting whenever possible

---

## Testing

### Manual Testing Checklist

- [ ] Fresh install creates settings with `AllowInvalidCertificates: true`
- [ ] Upgrade preserves existing settings without overwriting
- [ ] Checkbox appears in Settings UI with shield icon
- [ ] Checking/unchecking checkbox updates the setting
- [ ] Setting persists after application restart
- [ ] Connection to cloud Bitwarden works with setting disabled
- [ ] Connection to self-hosted with self-signed cert works with setting enabled
- [ ] Connection to self-hosted with self-signed cert fails with setting disabled (expected)

### Edge Cases

1. **Missing property in existing JSON:**  
   → Default value (`true`) used automatically
   
2. **Manual JSON edit:**  
   → Value correctly read and applied
   
3. **API cache invalidation:**  
   → Changing the setting requires app restart or API cache clear

---

## Future Enhancements

### Potential Improvements

1. **Per-Server Settings:**
   - Allow different SSL validation settings for different Bitwarden servers
   - Store as dictionary keyed by base address

2. **Certificate Pinning:**
   - Allow users to explicitly trust specific certificates
   - Store certificate fingerprints

3. **Trusted CA Import:**
   - Allow importing custom CA certificates
   - Validate against user-trusted CAs only

4. **Live Setting Application:**
   - Clear API cache when setting changes
   - No restart required

5. **Warning UI:**
   - Show warning dialog when enabling invalid certificates
   - Require explicit acknowledgment

---

## Backward Compatibility

✅ **Fully Backward Compatible**

| Scenario | v1.3.1 (old) | v1.3.2 (new) | Impact |
|----------|--------------|--------------|--------|
| New install | Always bypasses SSL | Defaults to bypass (changeable) | None |
| Upgrade | Always bypasses SSL | Defaults to bypass (changeable) | None |
| Existing settings | Overwritten on load | Preserved | **Fixed bug** |
| API signature | `GetApi(baseAddress)` | `GetApi(baseAddress, allowInvalidCertificates = true)` | Backward compatible (optional param) |

---

## Files Modified

1. `Libraries/Bitwardent.Utilities/BitwardenClientConfiguration.cs` - Added property
2. `Libraries/Bitwarden.Core/API/BitwardenApiFactory.cs` - Updated factory logic
3. `Libraries/Bitwarden.Core/API/BitwardenProtocol.cs` - Updated all protocol methods
4. `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Services/BitwardenService.cs` - Updated service calls
5. `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/Views/Settings Control.xaml` - Added UI checkbox
6. `Bitwarden.AutoType/Bitwarden.AutoType.Desktop/App.xaml.cs` - Fixed settings overwrite issue

**Total Lines Changed:** ~150 lines across 6 files
