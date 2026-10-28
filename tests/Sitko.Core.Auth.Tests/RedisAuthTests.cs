using System.Security.Claims;
using System.Net;
using System.Diagnostics;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using Duende.IdentityModel;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Redis;
using Sitko.Core.Auth.OIDC;
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
                var database = app.Services.GetRequiredKeyedService<IDatabase>(typeof(OidcAuthModuleOptions));
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
            var context = first.Services.GetRequiredService<IApplicationContext>();
            var legacyTokenKey = $"DOT_{context.Name}_{context.Environment}_alice".ToSha256();
            var legacyTokenProtector = legacy.GetRequiredService<IDataProtectionProvider>().CreateProtector("RedisTokenStore");
            _ = await admin.GetDatabase(3).StringSetAsync(legacyTokenKey, legacyTokenProtector.Protect(
                """{"AccessToken":"legacy-access","RefreshToken":"legacy-refresh","Expiration":"2030-01-01T00:00:00+00:00","Scope":"openid"}"""));
            var legacyToken = await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.True(legacyToken.Succeeded);
            Assert.Equal("legacy-access", legacyToken.Token.TokenForSpecifiedParameters!.AccessToken.ToString());
            Assert.Equal("legacy-refresh", legacyToken.Token.RefreshToken!.RefreshToken.ToString());
            await store.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-before"), RefreshToken = RefreshToken.Parse("refresh-before"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);

            if (sentinel)
            {
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
                await WaitUntilAsync(async () => (await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).Token!.TokenForSpecifiedParameters!.AccessToken.ToString() == "access-before");
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
            var restored = await second.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.Equal("access-before", restored.Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
            Assert.Equal("refresh-before", restored.Token!.TokenForSpecifiedParameters!.RefreshToken.ToString());
            var secondStore = second.Services.GetRequiredService<IUserTokenStore>();
            if (sentinel)
            {
                await WaitUntilAsync(async () =>
                {
                    await store.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-after"), RefreshToken = RefreshToken.Parse("refresh-after"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
                    return true;
                });
                await WaitUntilAsync(async () =>
                {
                    var updated = await secondStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
                    return updated.Token!.TokenForSpecifiedParameters!.AccessToken.ToString() == "access-after" && updated.Token!.TokenForSpecifiedParameters!.RefreshToken.ToString() == "refresh-after";
                });
            }
            else
            {
                await store.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-after"), RefreshToken = RefreshToken.Parse("refresh-after"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
            }

            var after = await secondStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.Equal("access-after", after.Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
            Assert.Equal("refresh-after", after.Token!.TokenForSpecifiedParameters!.RefreshToken.ToString());
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
                    await store.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
                    return true;
                });
                await WaitUntilAsync(async () => (await thirdStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).FailedResult!.Error == "not found");
            }
            else
            {
                await store.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
            }

            Assert.Equal("not found", (await thirdStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).FailedResult!.Error);
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
                ["Auth:OIDC:Client:OidcClientId"] = "test",
                ["Auth:OIDC:Client:OidcClientSecret"] = "test",
                ["Auth:OIDC:Client:EnableHealthChecks"] = "false",
                ["Auth:OIDC:Client:EnableRedisDataProtection"] = "true",
                ["Auth:OIDC:Client:TokenStoreType"] = "Redis",
                ["Auth:OIDC:Client:RedisDb"] = "3",
                ["Auth:OIDC:Client:RedisPassword"] = sentinel ? "valkey-test" : "default-test"
            };
            if (sentinel)
            {
                settings["Auth:OIDC:Client:RedisUser"] = "app";
                settings["Auth:OIDC:Client:RedisServiceName"] = "sitko-test";
                settings["Auth:OIDC:Client:RedisSentinelEndpoints:0"] = "127.0.0.1:26378";
                settings["Auth:OIDC:Client:RedisSentinelEndpoints:1"] = "127.0.0.1:26379";
                settings["Auth:OIDC:Client:RedisSentinelEndpoints:2"] = "127.0.0.1:26380";
                settings["Auth:OIDC:Client:RedisSentinelEndpoints:3"] = "127.0.0.1:26381";
                settings["Auth:OIDC:Client:RedisSentinelUser"] = "sentinel-client";
                settings["Auth:OIDC:Client:RedisSentinelPassword"] = "sentinel-test";
            }
            else
            {
                settings["Auth:OIDC:Client:RedisHost"] = "127.0.0.1";
                settings["Auth:OIDC:Client:RedisPort"] = directPort.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            _ = builder.Configuration.AddInMemoryCollection(settings);
            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            return builder.Build();
        }
    }
}
