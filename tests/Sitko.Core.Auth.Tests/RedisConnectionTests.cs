using Sitko.Core.Auth.OIDC;
using Sitko.Core.Auth.OIDC.Tokens;
using Microsoft.AspNetCore.DataProtection;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Sitko.Core.App;
using Sitko.Core.Redis;
using StackExchange.Redis;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    [Collection("Redis standalone")]
    public sealed class RedisConnectionTests
    {
        [Fact]
        public async Task NewSectionSelectsConnectionAndIgnoresLegacySettings()
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Host"] = "old.invalid",
                ["Redis:Port"] = "1",
                ["Redis:Db"] = "9",
                ["Redis:ConnectionString"] = "old.invalid:1",
                ["Redis:Connection:Configuration"] = GetDirectConfiguration()
            });
            _ = builder.AddSitkoCore().AddRedis();
            using var app = builder.Build();
            var database = app.Services.GetRequiredService<IDatabase>();
            Assert.Equal(3, database.Database);
            var key = $"shared-redis-{Guid.NewGuid():N}";
            Assert.True(await database.StringSetAsync(key, "new-section"));
            Assert.Equal("new-section", (await database.StringGetAsync(key)).ToString());
            Assert.True(await database.KeyDeleteAsync(key));
        }
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task AuthAndRedisShareConnection(bool authFirst)
        {
            var builder = CreateAuthBuilder(true, TokenStoreType.Redis);
            var sitko = builder.AddSitkoCore();
            if (authFirst)
            {
                _ = sitko.AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            }

            _ = sitko.AddRedis();
            if (!authFirst)
            {
                _ = sitko.AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            }

            using var app = builder.Build();
            var owner = Assert.Single(app.Services.GetServices<RedisConnection>());
            var database = Assert.Single(app.Services.GetServices<IDatabase>());
            Assert.Same(owner.Connection, database.Multiplexer);
            Assert.Equal(3, database.Database);
            Assert.Null(app.Services.GetKeyedService<IDatabase>(typeof(OidcAuthModuleOptions)));
            var protector = app.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("shared-test");
            Assert.Equal("protected-value", protector.Unprotect(protector.Protect("protected-value")));
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "shared-user")]));
            var store = app.Services.GetRequiredService<IUserTokenStore>();
            await store.StoreTokenAsync(user, new UserToken
            {
                AccessToken = AccessToken.Parse("shared-access"),
                AccessTokenType = AccessTokenType.Parse("Bearer"),
                ClientId = ClientId.Parse("test"),
                Expiration = DateTimeOffset.MaxValue
            }, ct: TestContext.Current.CancellationToken);
            var token = await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.Equal("shared-access", token.Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
            await store.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
        }

        [Theory]
        [InlineData(false, TokenStoreType.None, false)]
        [InlineData(false, TokenStoreType.InMemory, false)]
        [InlineData(true, TokenStoreType.None, true)]
        [InlineData(false, TokenStoreType.Redis, true)]
        [InlineData(true, TokenStoreType.Redis, true)]
        public void AuthAutomaticallyRegistersOnlyRequiredRedis(bool dataProtection, TokenStoreType tokenStore, bool expected)
        {
            var builder = CreateAuthBuilder(dataProtection, tokenStore);
            if (!expected)
            {
                _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Redis:Connection:Configuration"] = null
                });
            }

            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            using var app = builder.Build();
            var connection = app.Services.GetService<RedisConnection>();
            if (expected)
            {
                Assert.NotNull(connection);
                Assert.Equal(3, connection.Database.Database);
                Assert.Same(connection.Connection, app.Services.GetRequiredService<IDatabase>().Multiplexer);
            }
            else
            {
                Assert.Null(connection);
                Assert.Null(app.Services.GetService<IDatabase>());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void MissingNewSectionRejectsLegacySettings(bool auth)
        {
            var builder = CreateAuthBuilder(true, TokenStoreType.Redis);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Connection:Configuration"] = null
            });
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Host"] = "127.0.0.1",
                ["Redis:Password"] = "default-test"
            });
            var sitko = builder.AddSitkoCore();
            if (auth)
            {
                _ = sitko.AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            }
            else
            {
                _ = sitko.AddRedis();
            }

            var exception = Assert.Throws<OptionsValidationException>(builder.Build);
            Assert.Contains("Redis:Connection:Configuration", exception.Message, StringComparison.Ordinal);
        }

        [Theory]
        [InlineData("Direct", "")]
        [InlineData("Direct", " ")]
        [InlineData("Direct", "password=test-secret")]
        [InlineData("Direct", "127.0.0.1:16379,password=test-secret,unknown=test-secret")]
        [InlineData("Direct", "127.0.0.1:16379,password=test-secret,serviceName=group")]
        [InlineData("Sentinel", "127.0.0.1:16379,password=test-secret")]
        [InlineData("99", "127.0.0.1:16379,password=test-secret")]
        public void InvalidConnectionFailsWithoutLeakingConfiguration(string mode, string configuration)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Connection:Mode"] = mode,
                ["Redis:Connection:Configuration"] = configuration
            });
            _ = builder.AddSitkoCore().AddRedis();
            var exception = Assert.Throws<OptionsValidationException>(builder.Build);
            Assert.DoesNotContain("test-secret", exception.ToString(), StringComparison.Ordinal);
        }

        [Fact]
        public void ConnectionIsFixedPerHostAndOwnedByHost()
        {
            var builder = CreateAuthBuilder(true, TokenStoreType.None);
            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            using var first = builder.Build();
            ((IConfigurationRoot)builder.Configuration).Providers.OfType<MemoryConfigurationProvider>().Last()
                .Set("Redis:Connection:Configuration", "old.invalid:1");
            Assert.Equal("old.invalid:1", builder.Configuration["Redis:Connection:Configuration"]);
            var connection = first.Services.GetRequiredService<RedisConnection>();
            Assert.Equal(3, connection.Database.Database);
            var secondBuilder = CreateAuthBuilder(true, TokenStoreType.None);
            _ = secondBuilder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            using var second = secondBuilder.Build();
            var secondConnection = second.Services.GetRequiredService<RedisConnection>();
            Assert.NotSame(connection.Connection, secondConnection.Connection);
            first.Dispose();
            Assert.False(connection.Connection.IsConnected);
            Assert.True(secondConnection.Connection.IsConnected);
        }

        [Fact]
        public void DirectModeIgnoresInheritedSentinelConfiguration()
        {
            var builder = CreateAuthBuilder(true, TokenStoreType.None);
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Connection:SentinelConfiguration"] = "invalid=unused-secret"
            });
            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            using var app = builder.Build();
            Assert.True(app.Services.GetRequiredService<RedisConnection>().Connection.IsConnected);
        }

        [Fact]
        public void LegacyBindingIgnoresNewSection()
        {
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Redis:Host"] = "old.example",
                ["Redis:Port"] = "6379",
                ["Redis:Db"] = "3",
                ["Redis:Connection:Mode"] = "Direct",
                ["Redis:Connection:Configuration"] = "new.example:6379,defaultDatabase=0"
            }).Build();
            var legacy = new LegacyRedisOptions();
            configuration.GetSection("Redis").Bind(legacy);
            Assert.Equal("old.example", legacy.Host);
            Assert.Equal(6379, legacy.Port);
            Assert.Equal(3, legacy.Db);
        }

        private sealed class LegacyRedisOptions
        {
            public string? Host { get; set; }
            public int Port { get; set; }
            public int Db { get; set; }
        }

        private static string GetDirectConfiguration()
        {
            using var sentinel = ConnectionMultiplexer.SentinelConnect(new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:26379" },
                User = "sentinel-client",
                Password = "sentinel-test"
            });
            var primary = sentinel.GetServer("127.0.0.1", 26379).SentinelGetMasterAddressByName("sitko-test");
            Assert.NotNull(primary);
            return $"{primary},password=default-test,defaultDatabase=3";
        }

        private static HostApplicationBuilder CreateAuthBuilder(bool dataProtection, TokenStoreType tokenStore)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = $"shared-{Guid.NewGuid():N}",
                ["Auth:OIDC:Client:OidcClientId"] = "test",
                ["Auth:OIDC:Client:OidcClientSecret"] = "test",
                ["Auth:OIDC:Client:EnableHealthChecks"] = "false",
                ["Auth:OIDC:Client:EnableRedisDataProtection"] = dataProtection.ToString(),
                ["Auth:OIDC:Client:TokenStoreType"] = tokenStore.ToString(),
                ["Auth:OIDC:Client:RedisHost"] = "old.invalid",
                ["Auth:OIDC:Client:RedisDb"] = "9",
                ["Redis:Connection:Configuration"] = dataProtection || tokenStore == TokenStoreType.Redis
                    ? GetDirectConfiguration()
                    : null
            });
            return builder;
        }
    }
}
