using Amazon;
using Amazon.Auth.AccessControlPolicy;
using Amazon.Runtime;
using Amazon.S3;
using FluentValidation;
using HealthChecks.Aws.S3;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;
using Sitko.Core.App.Health;

namespace Sitko.Core.Storage.S3;

public class S3StorageModule<TS3StorageOptions> : StorageModule<S3Storage<TS3StorageOptions>, TS3StorageOptions>
    where TS3StorageOptions : S3StorageOptions, new()
{
    public override string OptionsKey => $"Storage:S3:{typeof(TS3StorageOptions).Name}";
    public override string[] OptionKeys => new[] { "Storage:S3:Default", OptionsKey };

    public override void ConfigureServices(IApplicationContext applicationContext, IServiceCollection services,
        TS3StorageOptions startupOptions)
    {
        base.ConfigureServices(applicationContext, services, startupOptions);
        services.AddSingleton<S3ClientProvider<TS3StorageOptions>>();
        services.AddHealthChecks().Add(new HealthCheckRegistration(GetType().Name,
            serviceProvider =>
            {
                var config = GetOptions(serviceProvider);
                var options = new S3BucketOptions
                {
                    BucketName = config.Bucket,
                    S3Config = new AmazonS3Config
                    {
                        RegionEndpoint = config.Region,
                        ServiceURL = config.Server?.ToString(),
                        ForcePathStyle = true
                    },
                    Credentials = new BasicAWSCredentials(config.AccessKey, config.SecretKey)
                };
                return new S3HealthCheck(options);
            }, null, tags: HealthCheckStages.GetSkipTags(HealthCheckStages.Liveness, HealthCheckStages.Readiness),
            null));
    }
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
