using System.Text.Json.Serialization;

namespace Bitwarden.Core.Models;

public class SyncResponse
{
    public Cipher[]? Ciphers { get; set; }
    public Collection[]? Collections { get; set; }
    public Domains? Domains { get; set; }
    public Folder[]? Folders { get; set; }
    public string? Object { get; set; }
    public Policy[]? Policies { get; set; }
    public Profile? Profile { get; set; }
    public object[]? Sends { get; set; }
    public bool unofficialServer { get; set; }
}

public class Domains
{
    public object[]? EquivalentDomains { get; set; }
    public Globalequivalentdomain[]? GlobalEquivalentDomains { get; set; }
    public string? Object { get; set; }
}

public class Globalequivalentdomain
{
    public string[]? Domains { get; set; }
    public bool Excluded { get; set; }
    public int Type { get; set; }
}

public class Profile
{
    public string? Culture { get; set; }
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public bool ForcePasswordReset { get; set; }
    public string? Id { get; set; }
    public string? Key { get; set; }
    public object? MasterPasswordHint { get; set; }
    public string? Name { get; set; }
    public string? Object { get; set; }
    public Organization[]? Organizations { get; set; }
    public bool Premium { get; set; }
    public string? PrivateKey { get; set; }
    public object[]? ProviderOrganizations { get; set; }
    public object[]? Providers { get; set; }
    public string? SecurityStamp { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public int _Status { get; set; }
}

public class Organization
{
    public bool AccessSecretsManager { get; set; }
    public bool AllowAdminAccessToAllCollectionItems { get; set; }
    public bool Enabled { get; set; }
    public bool FamilySponsorshipAvailable { get; set; }
    public string? FamilySponsorshipFriendlyName { get; set; }
    public DateTime? FamilySponsorshipLastSyncDate { get; set; }
    public DateTime? FamilySponsorshipValidUntil { get; set; }
    public bool FlexibleCollections { get; set; }
    public bool HasPublicAndPrivateKeys { get; set; }
    public string? Id { get; set; }
    public string? Identifier { get; set; }
    public string? Key { get; set; }
    public bool KeyConnectorEnabled { get; set; }
    public string? KeyConnectorUrl { get; set; }
    public bool LimitCollectionCreationDeletion { get; set; }
    public int? MaxCollections { get; set; }
    public int MaxStorageGb { get; set; }
    public string? Name { get; set; }
    public string? Object { get; set; }
    public string? OrganizationUserId { get; set; }
    public Permissions? Permissions { get; set; }
    public int PlanProductType { get; set; }
    public int ProductTierType { get; set; }
    public string? ProviderId { get; set; }
    public string? ProviderName { get; set; }
    public string? ProviderType { get; set; }
    public bool ResetPasswordEnrolled { get; set; }
    public int? Seats { get; set; }
    public bool SelfHost { get; set; }
    public bool SsoBound { get; set; }
    public int Status { get; set; }
    public int Type { get; set; }
    public bool Use2fa { get; set; }
    public bool UseActivateAutofillPolicy { get; set; }
    public bool UseApi { get; set; }
    public bool UseCustomPermissions { get; set; }
    public bool UseDirectory { get; set; }
    public bool UseEvents { get; set; }
    public bool UseGroups { get; set; }
    public bool UseKeyConnector { get; set; }
    public bool UsePasswordManager { get; set; }
    public bool UsePolicies { get; set; }
    public bool UseResetPassword { get; set; }
    public bool UseScim { get; set; }
    public bool UseSecretsManager { get; set; }
    public bool UseSso { get; set; }
    public bool UseTotp { get; set; }
    public string? UserId { get; set; }
    public bool UsersGetPremium { get; set; }
}
//public class Organization
//{
//    public bool Enabled { get; set; }
//    public bool HasPublicAndPrivateKeys { get; set; }
//    public string? Id { get; set; }
//    public object? Identifier { get; set; }
//    public string? Key { get; set; }
//    public int MaxCollections { get; set; }
//    public int MaxStorageGb { get; set; }
//    public string? Name { get; set; }
//    public string? Object { get; set; }
//    public object? ProviderId { get; set; }
//    public object? ProviderName { get; set; }
//    public bool ResetPasswordEnrolled { get; set; }
//    public int Seats { get; set; }
//    public bool SelfHost { get; set; }
//    public bool SsoBound { get; set; }
//    public int Status { get; set; }
//    public int Type { get; set; }
//    public bool Use2fa { get; set; }
//    public bool UseApi { get; set; }
//    public bool UseDirectory { get; set; }
//    public bool UseEvents { get; set; }
//    public bool UseGroups { get; set; }
//    public bool UsePolicies { get; set; }
//    public bool UseSso { get; set; }
//    public bool UseTotp { get; set; }
//    public string? UserId { get; set; }
//    public bool UsersGetPremium { get; set; }
//}

public class Permissions
{
    public bool AccessEventLogs { get; set; }
    public bool AccessImportExport { get; set; }
    public bool AccessReports { get; set; }
    public bool CreateNewCollections { get; set; }
    public bool DeleteAnyCollection { get; set; }
    public bool DeleteAssignedCollections { get; set; }
    public bool EditAnyCollection { get; set; }
    public bool EditAssignedCollections { get; set; }
    public bool ManageGroups { get; set; }
    public bool ManagePolicies { get; set; }
    public bool ManageResetPassword { get; set; }
    public bool ManageScim { get; set; }
    public bool ManageSso { get; set; }
    public bool ManageUsers { get; set; }
}

public class Cipher
{
    public object? Attachments { get; set; }
    public object? Card { get; set; }
    public string[]? CollectionIds { get; set; }
    public DateTime CreationDate { get; set; }
    public Data? Data { get; set; }
    public DateTime? DeletedDate { get; set; }
    public bool Edit { get; set; }
    public bool Favorite { get; set; }
    public Field1[]? Fields { get; set; }
    public string? FolderId { get; set; }
    public string? Id { get; set; }
    public object? Identity { get; set; }
    public Login? Login { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public string? Object { get; set; }
    public string? OrganizationId { get; set; }
    public bool OrganizationUseTotp { get; set; }
    public Passwordhistory1[]? PasswordHistory { get; set; }
    public int Reprompt { get; set; }
    public DateTime RevisionDate { get; set; }
    public Securenote? SecureNote { get; set; }
    public int Type { get; set; }
    public bool ViewPassword { get; set; }
}

public class Data
{
    public object? AutofillOnPageLoad { get; set; }
    public Field[]? Fields { get; set; }
    public string? Name { get; set; }
    public string? Notes { get; set; }
    public string? Password { get; set; }
    public Passwordhistory[]? PasswordHistory { get; set; }
    public DateTime? PasswordRevisionDate { get; set; }
    public string? Totp { get; set; }
    public string? Uri { get; set; }
    public Uri[]? Uris { get; set; }
    public string? Username { get; set; }
    public int Type { get; set; }
}

public class Field
{
    public object? LinkedId { get; set; }
    public string? Name { get; set; }
    public int Type { get; set; }
    public string? Value { get; set; }
}

public class Passwordhistory
{
    public DateTime LastUsedDate { get; set; }
    public string? Password { get; set; }
}

public class Uri
{
    public int? Match { get; set; }
    public string? uri { get; set; }
}

public class Login
{
    public object? AutofillOnPageLoad { get; set; }
    public string? Password { get; set; }
    public DateTime? PasswordRevisionDate { get; set; }
    public string? Totp { get; set; }
    public string? Uri { get; set; }
    public Uri1[]? Uris { get; set; }
    public string? Username { get; set; }
}

public class Uri1
{
    public int? Match { get; set; }
    public string? Uri { get; set; }
}

public class Securenote
{
    public int Type { get; set; }
}

public class Field1
{
    public object? LinkedId { get; set; }
    public string? Name { get; set; }
    public int Type { get; set; }
    public string? Value { get; set; }
}

public class Passwordhistory1
{
    public DateTime LastUsedDate { get; set; }
    public string? Password { get; set; }
}

public class Collection
{
    public object? ExternalId { get; set; }
    public bool HidePasswords { get; set; }
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Object { get; set; }
    public string? OrganizationId { get; set; }
    public bool ReadOnly { get; set; }
}

public class Folder
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Object { get; set; }
    public DateTime RevisionDate { get; set; }
}

public class Policy
{
    public Data1? Data { get; set; }
    public bool Enabled { get; set; }
    public string? Id { get; set; }
    public string? Object { get; set; }
    public string? OrganizationId { get; set; }
    public int Type { get; set; }
}

public class Data1
{
    public bool disableHideEmail { get; set; }
}