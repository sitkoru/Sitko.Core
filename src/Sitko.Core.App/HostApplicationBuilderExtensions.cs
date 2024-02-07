using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App;

public static class HostApplicationBuilderExtensions
{
    public static ISitkoCoreApplicationBuilder
        GetSitkoCore(this IHostApplicationBuilder builder) => GetSitkoCore<ISitkoCoreApplicationBuilder>(builder);

    public static TSitkoCoreApplicationBuilder
        GetSitkoCore<TSitkoCoreApplicationBuilder>(this IHostApplicationBuilder builder)
        where TSitkoCoreApplicationBuilder : ISitkoCoreApplicationBuilder =>
        ApplicationBuilderFactory.GetApplicationBuilder<IHostApplicationBuilder, TSitkoCoreApplicationBuilder>(builder);

    public static ISitkoCoreServerApplicationBuilder AddSitkoCore(this HostApplicationBuilder builder) =>
        builder.AddSitkoCore(Array.Empty<string>());

    public static ISitkoCoreServerApplicationBuilder AddSitkoCore(this HostApplicationBuilder builder, string[] args) =>
        ApplicationBuilderFactory.GetOrCreateApplicationBuilder(builder,
            applicationBuilder => new SitkoCoreServerApplicationBuilder(applicationBuilder, args));

    public static IHostBuilder ToHostBuilder(this IHostApplicationBuilder hostApplicationBuilder) =>
        new ProxyHostBuilder(hostApplicationBuilder);

    private class ProxyHostBuilder : IHostBuilder
    {
        private readonly IHostApplicationBuilder hostApplicationBuilder;
        private readonly HostBuilderContext Context;

        public ProxyHostBuilder(IHostApplicationBuilder hostApplicationBuilder)
        {
            this.hostApplicationBuilder = hostApplicationBuilder;
            Context = new HostBuilderContext(Properties)
            {
                Configuration = hostApplicationBuilder.Configuration,
                HostingEnvironment = hostApplicationBuilder.Environment
            };
        }

        public IHost Build() => throw new InvalidOperationException("Do not create app from this proxy");

        public IHostBuilder ConfigureAppConfiguration(
            Action<HostBuilderContext, IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(Context, hostApplicationBuilder.Configuration);
            return this;
        }

        public IHostBuilder ConfigureContainer<TContainerBuilder>(
            Action<HostBuilderContext, TContainerBuilder> configureDelegate) =>
            throw new InvalidOperationException("Can't configure container through this proxy");

        public IHostBuilder ConfigureHostConfiguration(Action<IConfigurationBuilder> configureDelegate)
        {
            configureDelegate(hostApplicationBuilder.Configuration);
            return this;
        }

        public IHostBuilder ConfigureServices(Action<HostBuilderContext, IServiceCollection> configureDelegate)
        {
            configureDelegate(Context, hostApplicationBuilder.Services);
            return this;
        }

        public IHostBuilder
            UseServiceProviderFactory<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory)
            where TContainerBuilder : notnull =>
            throw new InvalidOperationException("Can't configure container through this proxy");

        public IHostBuilder UseServiceProviderFactory<TContainerBuilder>(
            Func<HostBuilderContext, IServiceProviderFactory<TContainerBuilder>> factory)
            where TContainerBuilder : notnull =>
            throw new InvalidOperationException("Can't configure container through this proxy");

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();
    }
}
