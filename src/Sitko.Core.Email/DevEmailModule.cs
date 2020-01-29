using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Email
{
    public class DevEmailModule : FluentEmailModule<DevEmailModuleConfig>
    {
        protected override void ConfigureBuilder(FluentEmailServicesBuilder builder)
        {
            builder.Services.TryAdd(ServiceDescriptor.Scoped<ISender, DevEmailSender>());
        }
    }

    public class DevEmailModuleConfig : FluentEmailModuleConfig
    {
        public DevEmailModuleConfig() : base("dev@localhost", "localhost", "http")
        {
        }
    }

    public class DevEmailSender : ISender
    {
        private readonly ILogger<DevEmailSender> _logger;

        public DevEmailSender(ILogger<DevEmailSender> logger)
        {
            _logger = logger;
        }

        public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
        {
            _logger.LogInformation("Send email {subject} to {recipients}", email.Data.Subject,
                string.Join(", ", email.Data.ToAddresses));
            return new SendResponse {MessageId = Guid.NewGuid().ToString(), ErrorMessages = new List<string>()};
        }

        public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
        {
            _logger.LogInformation("Send email {subject} to {recipients}", email.Data.Subject,
                string.Join(", ", email.Data.ToAddresses));
            return Task.FromResult(new SendResponse
            {
                MessageId = Guid.NewGuid().ToString(), ErrorMessages = new List<string>()
            });
        }
    }
}
