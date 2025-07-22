using Amazon.Runtime;

namespace Sitko.Core.Storage.S3;

internal class S3HttpClientFactory<TS3StorageOptions>(IHttpClientFactory httpClientFactory) : HttpClientFactory
{
    public override HttpClient CreateHttpClient(IClientConfig clientConfig) =>
        httpClientFactory.CreateClient(nameof(TS3StorageOptions));

    public override bool UseSDKHttpClientCaching(IClientConfig clientConfig) =>
        // return false to indicate that the SDK should not cache clients internally
        false;

    public override bool DisposeHttpClientsAfterUse(IClientConfig clientConfig) =>
        // return false to indicate that the SDK shouldn't dispose httpClients because they're cached in your pool
        false;

    public override string? GetConfigUniqueString(IClientConfig clientConfig) =>
        // has no effect because UseSDKHttpClientCaching returns false
        null;
}
