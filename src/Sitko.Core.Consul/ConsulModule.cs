using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sitko.Core.App;

namespace Sitko.Core.Consul
{
    public interface IConsulModule
    {
    }

    public class ConsulModule<TConfig> : BaseApplicationModule<TConfig>, IConsulModule
        where TConfig : ConsulModuleOptions, new()
    {
        public override string OptionsKey => "Consul";

        public override void ConfigureServices(ApplicationContext context, IServiceCollection services,
            TConfig startupOptions)
        {
            base.ConfigureServices(context, services, startupOptions);
            services.AddSingleton<IConsulClientProvider, ConsulClientProvider>();
            services.AddSingleton(provider => provider.GetRequiredService<IConsulClientProvider>().Client);
        }
    }

    public class ConsulModuleOptions : BaseModuleOptions
    {
        public string ConsulUri { get; set; } = "http://localhost:8500";
    }

    public class ConsulModuleOptionsValidator : AbstractValidator<ConsulModuleOptions>
    {
        public ConsulModuleOptionsValidator() =>
            RuleFor(options => options.ConsulUri).NotEmpty().WithMessage("Consul uri can't be empty");
    }
}
