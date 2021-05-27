using System;
using System.Collections.Generic;
using Amazon;
using Amazon.S3;
using HealthChecks.Aws.S3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Sitko.Core.App;

namespace Sitko.Core.Storage.S3
{
    public class S3StorageModule<TS3StorageOptions> : StorageModule<S3Storage<TS3StorageOptions>, TS3StorageOptions>
        where TS3StorageOptions : S3StorageOptions, new()
    {
        public override string GetConfigKey()
        {
            return $"Storage:S3:{typeof(TS3StorageOptions).Name}";
        }

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TS3StorageOptions startupConfig)
        {
            base.ConfigureServices(context, services, startupConfig);
            services.AddSingleton<S3ClientProvider<TS3StorageOptions>>();
            services.AddHealthChecks().Add(new HealthCheckRegistration(GetType().Name,
                serviceProvider =>
                {
                    var config = GetConfig(serviceProvider);
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

    public class S3StorageOptions : StorageOptions
    {
        public Uri? Server { get; set; }
        public string Bucket { get; set; } = string.Empty;
        public string AccessKey { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public RegionEndpoint Region { get; set; } = RegionEndpoint.USEast1;
        public override string Name { get; set; } = string.Empty;

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (Server is null)
                {
                    return (false, new[] {"S3 server url is empty"});
                }

                if (string.IsNullOrEmpty(Bucket))
                {
                    return (false, new[] {"S3 bucketName is empty"});
                }

                if (string.IsNullOrEmpty(AccessKey))
                {
                    return (false, new[] {"S3 access key is empty"});
                }

                if (string.IsNullOrEmpty(SecretKey))
                {
                    return (false, new[] {"S3 secret key is empty"});
                }
            }

            return result;
        }
    }
}
