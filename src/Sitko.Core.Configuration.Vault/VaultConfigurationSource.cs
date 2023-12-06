using Microsoft.Extensions.Configuration;

namespace Sitko.Core.Configuration.Vault;

public sealed class VaultConfigurationSource : IConfigurationSource
{
    /// <summary>
    /// Initializes a new instance of the <see cref="VaultConfigurationSource"/> class.
    /// </summary>
    /// <param name="options">Vault options.</param>
    /// <param name="basePath">Base path.</param>
    public VaultConfigurationSource(VaultConfigurationModuleOptions options, string basePath)
    {
        Options = options;
        BasePath = basePath;
    }

    /// <summary>
    /// Gets Vault connection options.
    /// </summary>
    public VaultConfigurationModuleOptions Options { get; }

    /// <summary>
    /// Gets base path for vault URLs.
    /// </summary>
    public string BasePath { get; }

    /// <summary>
    /// Build configuration provider.
    /// </summary>
    /// <param name="builder">Configuration builder.</param>
    /// <returns>Instance of <see cref="IConfigurationProvider"/>.</returns>
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new VaultConfigurationProvider(this);
}
