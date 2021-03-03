using System;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;

namespace Sitko.Core.Storage.S3
{
    public class S3StorageModule<T> : StorageModule<S3Storage<T>, T> where T : StorageOptions, IS3StorageOptions, new()
    {
        public S3StorageModule(T config, Application application) : base(config, application)
        {
            if (config.ConfigureMetadata is null)
            {
                config.UseMetadata<S3StorageMetadataProvider<T>, S3StorageMetadataProviderOptions>();
            }
        }

        public override void ConfigureServices(IServiceCollection services, IConfiguration configuration,
            IHostEnvironment environment)
        {
            base.ConfigureServices(services, configuration, environment);
            services.AddSingleton(_ => new S3ClientProvider<T>(Config));
            if (Config.Server != null)
            {
                services.AddHealthChecks().AddS3(options =>
                {
                    options.AccessKey = Config.AccessKey;
                    options.BucketName = Config.Bucket;
                    options.SecretKey = Config.SecretKey;
                    options.S3Config = new AmazonS3Config
                    {
                        RegionEndpoint = Config.Region, ServiceURL = Config.Server.ToString(), ForcePathStyle = true
                    };
                });
            }
        }

        public override void CheckConfig()
        {
            base.CheckConfig();
            if (Config.Server is null)
            {
                throw new ArgumentException("S3 server url is empty");
            }

            if (string.IsNullOrEmpty(Config.Bucket))
            {
                throw new ArgumentException("S3 bucketName is empty");
            }

            if (string.IsNullOrEmpty(Config.AccessKey))
            {
                throw new ArgumentException("S3 access key is empty");
            }

            if (string.IsNullOrEmpty(Config.SecretKey))
            {
                throw new ArgumentException("S3 secret key is empty");
            }
        }
    }
}
