using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using VaultSharp;
using VaultSharp.Core;
using VaultSharp.V1.AuthMethods;
using VaultSharp.V1.AuthMethods.AppRole;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

namespace Sitko.Core.Configuration.Vault;

public class VaultConfigurationProvider : ConfigurationProvider
{
    private readonly Dictionary<string, int> versionsCache;
    private IVaultClient? currentVaultClient;
    private bool hasSuccessAuth;

    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationProvider"/> class.
    /// </summary>
    /// <param name="source">Vault configuration source.</param>
    public VaultConfigurationProvider(VaultConfigurationSource source)
    {
        ConfigurationSource = source ?? throw new ArgumentNullException(nameof(source));
        versionsCache = new Dictionary<string, int>();
    }

    /// <summary>
    /// Gets <see cref="VaultConfigurationSource"/>.
    /// </summary>
    internal VaultConfigurationSource ConfigurationSource { get; private set; }

    /// <inheritdoc/>
    public override void Load() => LoadAsync().GetAwaiter().GetResult();

    private async Task<bool> LoadVaultDataAsync(IVaultClient vaultClient)
    {
        var hasChanges = false;
        await foreach (var secretData in ReadKeysAsync(vaultClient, ConfigurationSource.BasePath))
        {
            //logger?.LogDebug($"VaultConfigurationProvider: got Vault data with key `{secretData.Key}`");

            var key = secretData.Key;
            key = key.TrimStart('/')[ConfigurationSource.BasePath.TrimStart('/').Length..].TrimStart('/')
                .Replace('/', ':');
            key = ReplaceTheAdditionalCharactersForConfigurationPath(key);
            var data = secretData.SecretData.Data;

            var shouldSetValue = true;
            if (versionsCache.TryGetValue(key, out var currentVersion))
            {
                shouldSetValue = secretData.SecretData.Metadata.Version > currentVersion;
                //    logger?.LogDebug($"VaultConfigurationProvider: Data for key `{secretData.Key}` {keyMsg}");
            }

            if (shouldSetValue)
            {
                SetData(data, ConfigurationSource.Options.OmitVaultKeyName ? string.Empty : key);
                hasChanges = true;
                versionsCache[key] = secretData.SecretData.Metadata.Version;
            }
        }

        return hasChanges;
    }

    private void SetData<TValue>(IEnumerable<KeyValuePair<string, TValue>> data, string? key)
    {
        foreach (var pair in data)
        {
            var nestedKey = string.IsNullOrEmpty(key) ? pair.Key : $"{key}:{pair.Key}";
            nestedKey = ReplaceTheAdditionalCharactersForConfigurationPath(nestedKey);

            var nestedValue = (JsonElement)(object)pair.Value!;
            SetItemData(nestedKey, nestedValue);
        }
    }

    private void SetItemData(string nestedKey, JsonElement nestedValue)
    {
        switch (nestedValue.ValueKind)
        {
            case JsonValueKind.Object:
                var jObject = nestedValue.EnumerateObject().ToDictionary(x => x.Name, x => x.Value).ToList();
                SetData(jObject, nestedKey);

                break;
            case JsonValueKind.Array:
                var array = nestedValue.EnumerateArray();
                for (var i = 0; i < array.Count(); i++)
                {
                    var arrElement = array.ElementAt(i);

                    if (arrElement.ValueKind == JsonValueKind.Array)
                    {
                        SetData(new[] { new KeyValuePair<string, JsonElement?>($"{nestedKey}:{i}", arrElement) }, null);
                    }
                    else if (arrElement.ValueKind == JsonValueKind.Object)
                    {
                        SetData(new[] { new KeyValuePair<string, JsonElement?>($"{nestedKey}:{i}", arrElement) }, null);
                    }
                    else
                    {
                        SetItemData($"{nestedKey}:{i}", arrElement);
                    }
                }

                break;
            case JsonValueKind.String:
                Set(nestedKey, nestedValue.GetString());
                break;
            case JsonValueKind.Number:
                Set(nestedKey, nestedValue.GetDecimal().ToString(CultureInfo.InvariantCulture));
                break;
            case JsonValueKind.True:
                Set(nestedKey, true.ToString());
                break;
            case JsonValueKind.False:
                Set(nestedKey, false.ToString());
                break;
            case JsonValueKind.Null:
                Set(nestedKey, null);
                break;
        }
    }

    private async IAsyncEnumerable<KeyedSecretData> ReadKeysAsync(IVaultClient vaultClient, string path)
    {
        Secret<ListInfo>? keys = null;
        var folderPath = path;

        if (folderPath.EndsWith('/') == false)
        {
            folderPath += "/";
        }

        if (folderPath.EndsWith('/'))
        {
            try
            {
                keys = await vaultClient.V1.Secrets.KeyValue.V2
                    .ReadSecretPathsAsync(folderPath, ConfigurationSource.Options.MountPoint).ConfigureAwait(false);
            }
            catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // this is key, not a folder
            }
        }

        if (keys != null)
        {
            foreach (var key in keys.Data.Keys)
            {
                var keyData = ReadKeysAsync(vaultClient, folderPath + key);
                await foreach (var secretData in keyData)
                {
                    yield return secretData;
                }
            }
        }

        var valuePath = path;
        if (valuePath.EndsWith('/'))
        {
            valuePath = valuePath.TrimEnd('/');
        }

        KeyedSecretData? keyedSecretData = null;
        try
        {
            var secretData = await vaultClient.V1.Secrets.KeyValue.V2
                .ReadSecretAsync(valuePath, null, ConfigurationSource.Options.MountPoint)
                .ConfigureAwait(false);
            keyedSecretData = new KeyedSecretData(valuePath, secretData.Data);
        }
        catch (VaultApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
        {
            // this is folder, not a key
        }

        if (keyedSecretData != null)
        {
            yield return keyedSecretData;
        }
    }

    private string ReplaceTheAdditionalCharactersForConfigurationPath(string inputKey)
    {
        if (ConfigurationSource.Options.AdditionalCharactersForConfigurationPath?.Any() != true)
        {
            return inputKey;
        }

        var outputKey = new StringBuilder(inputKey);

        foreach (var c in ConfigurationSource.Options.AdditionalCharactersForConfigurationPath)
        {
            outputKey.Replace(c, ':');
        }

        return outputKey.ToString();
    }

    public async Task LoadAsync()
    {
        try
        {
            if (currentVaultClient == null)
            {
                IAuthMethodInfo authMethod = ConfigurationSource.Options.AuthType switch
                {
                    VaultAuthType.Token => new TokenAuthMethodInfo(ConfigurationSource.Options.Token),
                    VaultAuthType.RoleApp => new AppRoleAuthMethodInfo(ConfigurationSource.Options.VaultRoleId,
                        ConfigurationSource.Options.VaultSecret),
                    _ => throw new ArgumentOutOfRangeException()
                };

                var vaultClientSettings = new VaultClientSettings(ConfigurationSource.Options.Uri, authMethod)
                {
                    UseVaultTokenHeaderInsteadOfAuthorizationHeader = true
                };
                currentVaultClient = new VaultClient(vaultClientSettings);
            }

            var hasChanges = await LoadVaultDataAsync(currentVaultClient);

            if (hasChanges)
            {
                OnReload();
            }

            hasSuccessAuth = true;
        }
        catch (VaultApiException e) when (e is
                                          {
                                              StatusCode: (int)HttpStatusCode.Forbidden
                                              or (int)HttpStatusCode.Unauthorized
                                          })
        {
            if (hasSuccessAuth)
            {
                currentVaultClient?.V1.Auth.ResetVaultToken();
            }
            else
            {
                throw;
            }
        }
        catch (Exception e) when (e is VaultApiException || e is HttpRequestException)
        {
            //logger?.Log(LogLevel.Error, e, "Cannot load configuration from Vault");
        }
    }

    private class KeyedSecretData
    {
        public KeyedSecretData(string key, SecretData secretData)
        {
            Key = key;
            SecretData = secretData;
        }

        public string Key { get; }

        public SecretData SecretData { get; }
    }
}
