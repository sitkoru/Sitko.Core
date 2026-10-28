using System.Security.Claims;
using System.Net;
using System.Diagnostics;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Redis;
using Sitko.Core.Auth.IdentityServer;
using Xunit;
using StackExchange.Redis;

namespace Sitko.Core.Auth.Tests
{
    public sealed class RedisAuthTests
    {
        [Fact]
        public async Task RedisModuleWorksWithoutAuthAndDisposesConnection()
        {
            using var observer = ConnectSentinel();
            var primary = await observer.GetServer("127.0.0.1", 26379).SentinelGetMasterAddressByNameAsync("sitko-test");
            var primaryPort = Assert.IsType<IPEndPoint>(primary).Port;
            var builder = Host.CreateApplicationBuilder();
            _ = builder.AddSitkoCore().AddRedis(options =>
            {
                options.Host = "127.0.0.1";
                options.Port = primaryPort;
                options.Password = "default-test";
                options.Db = 3;
            });
            IConnectionMultiplexer connection;
            using (var app = builder.Build())
            {
                var database = app.Services.GetRequiredService<IDatabase>();
                connection = database.Multiplexer;
                var key = $"redis-module-{Guid.NewGuid():N}";
                Assert.True(await database.StringSetAsync(key, "standalone"));
                Assert.Equal("standalone", (await database.StringGetAsync(key)).ToString());
                Assert.Equal(3, database.Database);
                Assert.True(await database.KeyDeleteAsync(key));
            }

            Assert.False(connection.IsConnected);
        }

        [Fact]
        public Task DirectConnectionPreservesDataProtectionAndTokens() => VerifyConnection(false);

        [Fact]
        public Task SentinelRecoversAfterPrimaryFailureAndPreservesDataProtectionAndTokens() => VerifyConnection(true);

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DisposingApplicationClosesRedisAndSentinelConnections(bool sentinel)
        {
            using var observer = ConnectSentinel();
            var server = observer.GetServer("127.0.0.1", 26379);
            var primary = await server.SentinelGetMasterAddressByNameAsync("sitko-test");
            var primaryPort = Assert.IsType<IPEndPoint>(primary).Port;
            var before = (await server.ClientListAsync()).Select(static client => client.Id).ToHashSet();
            var app = CreateApplication($"dispose-{Guid.NewGuid():N}", sentinel, primaryPort);
            IConnectionMultiplexer connection;
            HashSet<long> sentinelClients;
            using (app)
            {
                var database = app.Services.GetRequiredKeyedService<IDatabase>(typeof(OidcIdentityServerModuleOptions));
                connection = database.Multiplexer;
                Assert.True(connection.IsConnected);
                sentinelClients = [.. (await server.ClientListAsync())
                    .Where(client => !before.Contains(client.Id) && client.Name?.StartsWith("sentinel-", StringComparison.Ordinal) != true)
                    .Select(static client => client.Id)];
                if (sentinel)
                {
                    Assert.NotEmpty(sentinelClients);
                }
            }

            Assert.False(connection.IsConnected);
            await WaitUntilAsync(async () => (await server.ClientListAsync())
                .All(client => !sentinelClients.Contains(client.Id)));
        }

        private static async Task VerifyConnection(bool sentinel)
        {
            var name = $"sentinel-tests-{Guid.NewGuid():N}";
            using var observer = ConnectSentinel();
            var primary = await observer.GetServer("127.0.0.1", 26379).SentinelGetMasterAddressByNameAsync("sitko-test");
            Assert.NotNull(primary);
            var primaryPort = Assert.IsType<IPEndPoint>(primary).Port;
            using var admin = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { primary },
                Password = "default-test",
                AllowAdmin = true
            });
            using var legacy = new ServiceCollection().AddLogging().AddDataProtection()
                .PersistKeysToStackExchangeRedis(() => admin.GetDatabase(3), $"{name}-DP")
                .SetApplicationName(name).Services.BuildServiceProvider();
            var legacyProtector = legacy.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
            var legacyProtectedText = legacyProtector.Protect("legacy-payload");
            using var first = CreateApplication(name, sentinel, primaryPort);
            var protector = first.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
            Assert.Equal("legacy-payload", protector.Unprotect(legacyProtectedText));
            var protectedText = protector.Protect("before-failover");
            Assert.Equal("before-failover", legacyProtector.Unprotect(protectedText));
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "alice")]));
            var store = first.Services.GetRequiredService<IUserTokenStore>();
            await store.StoreTokenAsync(user, new UserToken { AccessToken = "access-before", RefreshToken = "refresh-before" });

            if (sentinel)
            {
                // PING Sentinel ещё не означает, что обнаружены реплики и участники кворума.
                foreach (var port in new[] { 26379, 26380, 26381 })
                {
                    var server = observer.GetServer("127.0.0.1", port);
                    await WaitUntilAsync(async () =>
                        (await server.SentinelGetReplicaAddressesAsync("sitko-test")).Length == 2 &&
                        (await server.SentinelGetSentinelAddressesAsync("sitko-test")).Length == 2);
                }

                // A barrier on this connection covers preceding DP and token writes on the primary.
                Assert.True(await admin.GetDatabase(3).StringSetAsync($"{name}-barrier", "replicated"));
                Assert.Equal(2, (int)await admin.GetDatabase(3).ExecuteAsync("WAIT", 2, 10000));
                admin.GetServer(primary).Shutdown(ShutdownMode.Never, CommandFlags.FireAndForget);
                await WaitUntilAsync(async () =>
                {
                    var next = await observer.GetServer("127.0.0.1", 26379)
                        .SentinelGetMasterAddressByNameAsync("sitko-test");
                    return next is not null && !next.Equals(primary);
                });
                await WaitUntilAsync(async () => (await store.GetTokenAsync(user)).AccessToken == "access-before");
                Assert.Equal("before-failover", protector.Unprotect(protectedText));
                Assert.Equal("legacy-payload", protector.Unprotect(legacyProtectedText));
                _ = first.Services.GetRequiredService<IKeyManager>()
                    .CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
            }

            using var second = CreateApplication(name, sentinel, primaryPort);
            Assert.Equal("before-failover", second.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test").Unprotect(protectedText));
            Assert.Equal("legacy-payload", second.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test").Unprotect(legacyProtectedText));
            var restored = await second.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user);
            Assert.Equal("access-before", restored.AccessToken);
            Assert.Equal("refresh-before", restored.RefreshToken);
            var secondStore = second.Services.GetRequiredService<IUserTokenStore>();
            if (sentinel)
            {
                await WaitUntilAsync(async () =>
                {
                    await store.StoreTokenAsync(user, new UserToken { AccessToken = "access-after", RefreshToken = "refresh-after" });
                    return true;
                });
                await WaitUntilAsync(async () =>
                {
                    var updated = await secondStore.GetTokenAsync(user);
                    return updated.AccessToken == "access-after" && updated.RefreshToken == "refresh-after";
                });
            }
            else
            {
                await store.StoreTokenAsync(user, new UserToken { AccessToken = "access-after", RefreshToken = "refresh-after" });
            }

            var after = await secondStore.GetTokenAsync(user);
            Assert.Equal("access-after", after.AccessToken);
            Assert.Equal("refresh-after", after.RefreshToken);
            var newProtectedText = second.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test").Protect("after-failover");
            using var third = CreateApplication(name, sentinel, primaryPort);
            Assert.Equal("after-failover", third.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test").Unprotect(newProtectedText));
            var thirdStore = third.Services.GetRequiredService<IUserTokenStore>();
            if (sentinel)
            {
                await WaitUntilAsync(async () =>
                {
                    await store.ClearTokenAsync(user);
                    return true;
                });
                await WaitUntilAsync(async () => (await thirdStore.GetTokenAsync(user)).Error == "not found");
            }
            else
            {
                await store.ClearTokenAsync(user);
            }

            Assert.Equal("not found", (await thirdStore.GetTokenAsync(user)).Error);
        }

        // Sentinel needs a PING handshake; the observer also needs CLIENT LIST.
        private static ConnectionMultiplexer ConnectSentinel() => ConnectionMultiplexer.Connect(
            new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:26379", "127.0.0.1:26380", "127.0.0.1:26381" },
                User = "sentinel-client",
                Password = "sentinel-test",
                AllowAdmin = true,
                CommandMap = CommandMap.Create(
                [
                    "AUTH", "HELLO", "PING", "INFO", "ROLE", "SENTINEL", "SUBSCRIBE",
                    "PSUBSCRIBE", "UNSUBSCRIBE", "PUNSUBSCRIBE", "CLIENT"
                ], available: true)
            });

        private static async Task WaitUntilAsync(Func<Task<bool>> condition)
        {
            // Ждём полный штатный интервал проверки ролей с запасом на команды и планирование CI.
            var timeout = TimeSpan.FromSeconds(new ConfigurationOptions().ConfigCheckSeconds + 30);
            var timer = Stopwatch.StartNew();
            Exception? lastError = null;
            while (timer.Elapsed < timeout)
            {
                try
                {
                    if (await condition())
                    {
                        return;
                    }
                }
                catch (RedisException exception)
                {
                    lastError = exception;
                }
                catch (RedisTimeoutException exception)
                {
                    lastError = exception;
                }
                catch (RedisCommandException exception) when (
                    exception.Message.StartsWith("Command cannot be issued to a replica:", StringComparison.Ordinal))
                {
                    lastError = exception;
                }

                await Task.Delay(200);
            }

            throw new TimeoutException($"Sentinel did not restore the expected operation within {timeout.TotalSeconds} seconds", lastError);
        }

        private static IHost CreateApplication(string name, bool sentinel, int directPort)
        {
            var builder = Host.CreateApplicationBuilder();
            var settings = new Dictionary<string, string?>
            {
                ["Application:Name"] = name,
                ["Application:Environment"] = "Test",
                ["Auth:IdentityServer:Oidc:OidcClientId"] = "test",
                ["Auth:IdentityServer:Oidc:OidcClientSecret"] = "test",
                ["Auth:IdentityServer:Oidc:EnableHealthChecks"] = "false",
                ["Auth:IdentityServer:Oidc:EnableRedisDataProtection"] = "true",
                ["Auth:IdentityServer:Oidc:TokenStoreType"] = "Redis",
                ["Auth:IdentityServer:Oidc:RedisDb"] = "3",
                ["Auth:IdentityServer:Oidc:RedisPassword"] = sentinel ? "valkey-test" : "default-test"
            };
            if (sentinel)
            {
                settings["Auth:IdentityServer:Oidc:RedisUser"] = "app";
                settings["Auth:IdentityServer:Oidc:RedisServiceName"] = "sitko-test";
                settings["Auth:IdentityServer:Oidc:RedisSentinelEndpoints:0"] = "127.0.0.1:26378";
                settings["Auth:IdentityServer:Oidc:RedisSentinelEndpoints:1"] = "127.0.0.1:26379";
                settings["Auth:IdentityServer:Oidc:RedisSentinelEndpoints:2"] = "127.0.0.1:26380";
                settings["Auth:IdentityServer:Oidc:RedisSentinelEndpoints:3"] = "127.0.0.1:26381";
                settings["Auth:IdentityServer:Oidc:RedisSentinelUser"] = "sentinel-client";
                settings["Auth:IdentityServer:Oidc:RedisSentinelPassword"] = "sentinel-test";
            }
            else
            {
                settings["Auth:IdentityServer:Oidc:RedisHost"] = "127.0.0.1";
                settings["Auth:IdentityServer:Oidc:RedisPort"] = directPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            _ = builder.Configuration.AddInMemoryCollection(settings);
            _ = builder.AddSitkoCore().AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>();
            return builder.Build();
        }
    }
}
