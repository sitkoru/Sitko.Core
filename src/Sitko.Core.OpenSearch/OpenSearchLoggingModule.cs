using System.Globalization;
using OpenSearch.Net;
using Serilog;
using Serilog.Sinks.OpenSearch;
using Sitko.Core.App;

namespace Sitko.Core.OpenSearch;

public class OpenSearchLoggingModule : BaseApplicationModule<OpenSearchLoggingModuleOptions>, ILoggingModule<OpenSearchLoggingModuleOptions>
{
    public override string OptionsKey => "OpenSearch:Logging";

    public LoggerConfiguration ConfigureLogging(IApplicationContext context, OpenSearchLoggingModuleOptions options,
        LoggerConfiguration loggerConfiguration)
    {
        if (options.LoggingEnabled)
        {
            var sinkOptions = new OpenSearchSinkOptions(new Uri(options.Url))
            {
                AutoRegisterTemplate = true,
                AutoRegisterTemplateVersion = options.LoggingTemplateVersion ?? AutoRegisterTemplateVersion.OSv1,
                NumberOfReplicas = options.LoggingNumberOfReplicas,
                IndexFormat =
                    options.LoggingIndexFormat ??
                    $"dotnet-logs-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{context.Name.ToLower(CultureInfo.InvariantCulture).Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}",
                EmitEventFailure = options.EmitEventFailure,
                FailureCallback = options.FailureCallback,
                FailureSink = options.FailureSink,
                TypeName = options.LogIndexTypeName,
                ModifyConnectionSettings = x =>
                {
                    if (!string.IsNullOrEmpty(options.Login) && !string.IsNullOrEmpty(options.Password))
                    {
                        x = x.BasicAuthentication(options.Login, options.Password);
                    }

                    if (options.DisableCertificatesValidation)
                    {
                        return x.ServerCertificateValidationCallback(CertificateValidations.AllowAll)
                            .ServerCertificateValidationCallback((_, _, _, _) => true);
                    }

                    return x;
                },
            };
            if (!string.IsNullOrEmpty(options.LoggingLifeCycleName))
            {
                sinkOptions.TemplateCustomSettings = new Dictionary<string, string>
                {
                    { "lifecycle.name", options.LoggingLifeCycleName }
                };
            }

            if (!string.IsNullOrEmpty(options.LoggingRolloverAlias))
            {
                sinkOptions.TemplateName = options.LoggingRolloverAlias;
                sinkOptions.IndexAliases = [options.LoggingRolloverAlias];
                sinkOptions.TemplateCustomSettings["lifecycle.rollover_alias"] = options.LoggingRolloverAlias;
            }

            loggerConfiguration = loggerConfiguration
                .WriteTo.OpenSearch(sinkOptions)
                .Enrich.WithProperty("ApplicationId", context.Id)
                .Enrich.WithProperty("ApplicationName", context.Name)
                .Enrich.WithProperty("ApplicationVersion", context.Version);
        }

        return loggerConfiguration;
    }
}
