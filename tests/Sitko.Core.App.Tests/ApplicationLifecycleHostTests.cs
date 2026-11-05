using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Sitko.Core.App.Tests
{

    public class ApplicationLifecycleHostTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task ModuleLifecycleFollowsHostStartAndStopWhenEnabled(bool enabled)
        {
            var events = new List<string>();
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Services.AddSingleton(events);
            _ = builder.AddSitkoCore().AddModule<LifecycleModule, LifecycleModuleOptions>(options => options.Enabled = enabled);

            using var host = builder.Build();
            await host.StartAsync(TestContext.Current.CancellationToken);
            await host.StopAsync(TestContext.Current.CancellationToken);

            string[] expected = enabled ? ["init", "started", "stopping", "stopped"] : [];
            Assert.Equal(expected, events);
        }

        [Fact]
        public async Task HostStartRejectsMissingRequiredModuleBeforeInitialization()
        {
            var events = new List<string>();
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Services.AddSingleton(events);
            _ = builder.AddSitkoCore().AddModule<RequiresMissingModule, LifecycleModuleOptions>();

            using var host = builder.Build();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                host.StartAsync(TestContext.Current.CancellationToken));

            Assert.Contains(nameof(MissingModule), exception.Message);
            Assert.Empty(events);
        }
    }

    public class LifecycleModuleOptions : BaseModuleOptions;

    public class LifecycleModule : BaseApplicationModule<LifecycleModuleOptions>
    {
        public override string OptionsKey => "LifecycleTest";

        public override Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            serviceProvider.GetRequiredService<List<string>>().Add("init");
            return Task.CompletedTask;
        }

        public override Task ApplicationStarted(IApplicationContext applicationContext, IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            serviceProvider.GetRequiredService<List<string>>().Add("started");
            return Task.CompletedTask;
        }

        public override Task ApplicationStopping(IApplicationContext applicationContext, IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            serviceProvider.GetRequiredService<List<string>>().Add("stopping");
            return Task.CompletedTask;
        }

        public override Task ApplicationStopped(IApplicationContext applicationContext, IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            serviceProvider.GetRequiredService<List<string>>().Add("stopped");
            return Task.CompletedTask;
        }
    }

    public class RequiresMissingModule : BaseApplicationModule<LifecycleModuleOptions>
    {
        public override string OptionsKey => "RequiredModuleTest";

        public override IEnumerable<Type> GetRequiredModules(IApplicationContext applicationContext,
            LifecycleModuleOptions options) => [typeof(MissingModule)];

        public override Task InitAsync(IApplicationContext applicationContext, IServiceProvider serviceProvider,
            CancellationToken cancellationToken = default)
        {
            serviceProvider.GetRequiredService<List<string>>().Add("init");
            return Task.CompletedTask;
        }
    }

    public class MissingModule : BaseApplicationModule<LifecycleModuleOptions>
    {
        public override string OptionsKey => "MissingModuleTest";
    }
}
