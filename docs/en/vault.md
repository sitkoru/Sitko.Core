# Configuration from Vault

[Русский](../ru/vault.md) · [All topics](README.md)

`Sitko.Core.Configuration.Vault` reads KV v2 through VaultSharp and adds values
to .NET configuration. You need a reachable server, an existing KV v2 mount and
permission to read secrets and list paths in the selected tree. After
`AddSitkoCore()`, call `AddVaultConfiguration()` from the package namespace.

The `Vault` section:

| Setting | Purpose and default |
| --- | --- |
| `Uri` | Required Vault address. |
| `MountPoint` | KV v2 mount, defaults to `secret`. |
| `Secrets` | Secret/tree paths; an empty list uses the application name. |
| `AuthType` | Defaults to `Token`; alternative: `RoleApp`. |
| `Token` | Required for `Token`. |
| `VaultRoleId`, `VaultSecret` | Required for `RoleApp` (AppRole). |
| `ReloadOnChange`, `ReloadCheckIntervalSeconds` | Reload polling enabled, every 60 seconds. |
| `RenewToken`, `TokenRenewIntervalMinutes` | Token renewal enabled, every 60 minutes. |
| `ThrowOnEmptySecrets` | `true`: startup checks reject empty sources. |
| `OmitVaultKeyName` | `false`: retains the key name in the configuration path. |

For an isolated environment, supply `Vault__Uri`, `Vault__MountPoint`,
`Vault__Secrets__0` and `Vault__Token` through environment configuration, then check
an expected value through `IConfiguration` without logging the secret. The provider
flattens nested JSON objects and arrays into .NET paths; `/` in relative paths becomes
`:`, with additional substitutions available through `AdditionalCharactersForConfigurationPath`.

Reloading configuration does not mean every consumer rebuilds its connections.
HTTP/Vault load errors may leave previously loaded values in place; an access denial
after successful authentication resets the token. An initial authentication denial
propagates. This is not a guarantee of secret freshness or an available fallback source;
verify secret rotation and failure scenarios separately. `ThrowOnEmptySecrets` checks
emptiness, not the completeness of your application schema.

Sources: [module and settings](../../src/Sitko.Core.Configuration.Vault/VaultConfigurationModule.cs),
[loading](../../src/Sitko.Core.Configuration.Vault/VaultConfigurationProvider.cs).
