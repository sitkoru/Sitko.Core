using System.Runtime.CompilerServices;
using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.S3;
using FluentValidation;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Trace;
using Sitko.Core.App;
using Sitko.Core.App.OpenTelemetry;

[assembly: InternalsVisibleTo("Sitko.Core.Storage.S3.Tests")]

namespace Sitko.Core.Storage.S3;

public class S3StorageModule<TS3StorageOptions> : StorageModule<S3Storage<TS3StorageOptions>, TS3StorageOptions>,
    IOpenTelemetryModule<TS3StorageOptions>
    where TS3StorageOptions : S3StorageOptions, new()
{
    public override string OptionsKey => $"Storage:S3:{typeof(TS3StorageOptions).Name}";
    public override string[] OptionKeys => ["Storage:S3:Default", OptionsKey];

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TS3StorageOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddHttpClient(nameof(TS3StorageOptions), client =>
        {
            startupOptions.ConfigureHttpClient?.Invoke(client);
        });
        services.AddSingleton(typeof(S3HttpClientFactory<>));
        services.AddSingleton<S3ClientProvider>();
        services.AddKeyedSingleton<IAmazonS3, AmazonS3Client>(typeof(TS3StorageOptions).Name, (provider, _) =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<TS3StorageOptions>>();
            return new AmazonS3Client(options.CurrentValue.AccessKey,
                options.CurrentValue.SecretKey,
                options.CurrentValue.GetAmazonS3Config(provider
                    .GetRequiredService<S3HttpClientFactory<TS3StorageOptions>>()));
        });
        if (!startupOptions.DisableHealthCheck)
        {
            string[]
                skipTags = /*HealthCheckStages.GetSkipTags(HealthCheckStages.Liveness, HealthCheckStages.Readiness)*/
                    []; // don't skip for now

            services.AddSingleton<S3HealthCheck<TS3StorageOptions>>();
            services.AddHealthChecks().Add(new HealthCheckRegistration(
                $"S3 Storage ({typeof(TS3StorageOptions).Name}) Objects",
                serviceProvider => serviceProvider.GetRequiredService<S3HealthCheck<TS3StorageOptions>>(),
                HealthStatus.Unhealthy,
                skipTags,
                startupOptions.HealthCheckTimeout));

            services.AddSingleton<S3BucketHealthCheck<TS3StorageOptions>>();
            services.AddHealthChecks().Add(new HealthCheckRegistration(
                $"S3 Storage ({typeof(TS3StorageOptions).Name}) Bucket",
                serviceProvider => serviceProvider.GetRequiredService<S3BucketHealthCheck<TS3StorageOptions>>(),
                HealthStatus.Unhealthy,
                skipTags,
                startupOptions.HealthCheckTimeout));
        }
    }


    public OpenTelemetryBuilder ConfigureOpenTelemetry(IApplicationContext context, TS3StorageOptions options,
        OpenTelemetryBuilder builder) =>
        builder.WithTracing(providerBuilder => providerBuilder.AddAWSInstrumentation());
}

[PublicAPI]
public class S3StorageOptions : StorageOptions, IModuleOptionsWithValidation
{
    public Uri? Server { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;

    public bool GeneratePreSignedUrls { get; set; }
    public int PreSignedUrlsExpirationInHours { get; set; } = 1;
    public Policy? BucketPolicy { get; set; }
    public bool DeleteBucketOnCleanup { get; set; }

    public bool DisableHealthCheck { get; set; }
    public TimeSpan? HealthCheckTimeout { get; set; } = TimeSpan.FromSeconds(10);

    public Policy AnonymousReadPolicy => new()
    {
        Statements = new List<Statement>
        {
            new(Statement.StatementEffect.Allow)
            {
                Principals = new List<Principal> { new("*") },
                Actions = new List<ActionIdentifier> { new("s3:GetObject"), new("s3:GetObjectVersion") },
                Resources = new List<Resource> { new($"arn:aws:s3:::{Bucket}/*") },
                Id = "PublicRead"
            }
        }
    };

    public Action<HttpClient>? ConfigureHttpClient { get; set; }

    public Type GetValidatorType() => typeof(S3StorageOptionsValidator);
}

public class S3StorageOptionsValidator : StorageOptionsValidator<S3StorageOptions>
{
    public S3StorageOptionsValidator()
    {
        RuleFor(o => o.Server).NotEmpty().WithMessage("S3 server url is empty");
        RuleFor(o => o.Bucket).NotEmpty().WithMessage("S3 bucketName is empty");
        RuleFor(o => o.AccessKey).NotEmpty().WithMessage("S3 access key is empty");
        RuleFor(o => o.SecretKey).NotEmpty().WithMessage("S3 secret key is empty");
    }
}
