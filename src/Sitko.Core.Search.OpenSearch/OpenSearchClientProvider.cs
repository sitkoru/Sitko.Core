using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;

namespace Sitko.Core.Search.OpenSearch;

public sealed class OpenSearchClientProvider
{
    private readonly IOptionsMonitor<OpenSearchModuleOptions> optionsMonitor;
    private readonly ILogger logger;
    private readonly object sync = new();
    private readonly Func<Uri, ConnectionSettings> createSettings;
    private (string Url, string Login, string Password, bool DisableCertificatesValidation)? currentSettings;
    private OpenSearchClient? client;

    public OpenSearchClientProvider(IOptionsMonitor<OpenSearchModuleOptions> optionsMonitor,
        ILogger<OpenSearchClientProvider> logger) : this(optionsMonitor, (ILogger)logger) { }

    internal OpenSearchClientProvider(IOptionsMonitor<OpenSearchModuleOptions> optionsMonitor, ILogger logger,
        Func<Uri, ConnectionSettings>? createSettings = null)
    {
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
        this.createSettings = createSettings ?? (uri => new ConnectionSettings(uri));
    }

    internal OpenSearchClient GetClient()
    {
        lock (sync)
        {
            var options = optionsMonitor.CurrentValue;
            var connection = (options.Url, options.Login, options.Password, options.DisableCertificatesValidation);
            if (client != null && currentSettings == connection)
            {
                return client;
            }

            logger.LogDebug("Create OpenSearch client");
            var settings = createSettings(new Uri(connection.Url)).DisableDirectStreaming()
                .OnRequestCompleted(details =>
                {
                    if (!optionsMonitor.CurrentValue.EnableClientLogging)
                    {
                        return;
                    }

                    logger.LogDebug("### OpenSearch REQUEST ###");
                    if (details.RequestBodyInBytes != null)
                    {
                        logger.LogDebug("{Request}", Encoding.UTF8.GetString(details.RequestBodyInBytes));
                    }

                    logger.LogDebug("### OpenSearch RESPONSE ###");
                    if (details.ResponseBodyInBytes != null)
                    {
                        logger.LogDebug("{Response}", Encoding.UTF8.GetString(details.ResponseBodyInBytes));
                    }
                })
                .PrettyJson();
            if (!string.IsNullOrEmpty(connection.Login))
            {
                settings.BasicAuthentication(connection.Login, connection.Password);
            }

            if (connection.DisableCertificatesValidation)
            {
                settings.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                    .ServerCertificateValidationCallback((_, _, _, _) => true);
            }

            client = new OpenSearchClient(settings);
            currentSettings = connection;
            return client;
        }
    }
}
