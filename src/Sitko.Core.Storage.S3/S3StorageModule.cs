using System;
using Amazon;
using Amazon.S3;
using FluentValidation;
using HealthChecks.Aws.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;

namespace Sitko.Core.Storage.S3
{
    using JetBrains.Annotations;

    public class S3StorageModule<TS3StorageOptions> : StorageModule<S3Storage<TS3StorageOptions>, TS3StorageOptions>
        where TS3StorageOptions : S3StorageOptions, new()
    {
        public override string OptionsKey => $"Storage:S3:{typeof(TS3StorageOptions).Name}";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TS3StorageOptions startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<S3ClientProvider<TS3StorageOptions>>();
            services.AddHealthChecks().Add(new HealthCheckRegistration(GetType().Name,
                serviceProvider =>
                {
                    var config = GetOptions(serviceProvider);
                    var options = new S3BucketOptions
                    {
                        AccessKey = config.AccessKey,
                        BucketName = config.Bucket,
                        SecretKey = config.SecretKey,
                        S3Config = new AmazonS3Config
                        {
                            RegionEndpoint = config.Region,
                            ServiceURL = config.Server?.ToString(),
                            ForcePathStyle = true
                        }
                    };
                    return new S3HealthCheck(options);
                }, null, null, null));
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
}
