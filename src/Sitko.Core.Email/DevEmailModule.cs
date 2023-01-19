using FluentEmail.Core;
using FluentEmail.Core.Interfaces;
using FluentEmail.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sitko.Core.Email;

public class DevEmailModule : FluentEmailModule<DevEmailModuleOptions>
{
    public override string OptionsKey => "Email:Dev";

    protected override void ConfigureBuilder(FluentEmailServicesBuilder builder,
        DevEmailModuleOptions moduleOptions) =>
        builder.Services.TryAdd(ServiceDescriptor.Scoped<ISender, DevEmailSender>());
}

public class DevEmailModuleOptions : FluentEmailModuleOptions
{
}

public class DevEmailSender : ISender
{
    private readonly ILogger<DevEmailSender> logger;

    public DevEmailSender(ILogger<DevEmailSender> logger) => this.logger = logger;

    public SendResponse Send(IFluentEmail email, CancellationToken? token = null)
    {
        logger.LogInformation("Send email {Subject} to {Recipients}", email.Data.Subject,
            string.Join(", ", email.Data.ToAddresses));
        return new SendResponse { MessageId = Guid.NewGuid().ToString(), ErrorMessages = new List<string>() };
    }

    public Task<SendResponse> SendAsync(IFluentEmail email, CancellationToken? token = null)
    {
        logger.LogInformation("Send email {Subject} to {Recipients}", email.Data.Subject,
            string.Join(", ", email.Data.ToAddresses));
        return Task.FromResult(new SendResponse
        {
            MessageId = Guid.NewGuid().ToString(), ErrorMessages = new List<string>()
        });
    }
}

