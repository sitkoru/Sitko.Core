using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Auth.OIDC;
using Sitko.Core.Redis;
using StackExchange.Redis;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class RedisClusterTests
    {
        [Fact]
        public async Task RedisModuleUsesNonzeroClusterDatabase()
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.AddSitkoCore().AddRedis(static options =>
                options.Configuration = "127.0.0.1:17379,password=cluster-test,defaultDatabase=3");
            using var app = builder.Build();
            var database = app.Services.GetRequiredService<IDatabase>();
            var key = $"cluster-db-{Guid.NewGuid():N}";
            Assert.True(await database.StringSetAsync(key, "database-three"));
            Assert.Equal("database-three", (await database.StringGetAsync(key)).ToString());
            Assert.Equal(3, database.Database);
            Assert.True(await database.KeyDeleteAsync(key));
        }

        [Fact]
        public async Task ClusterDatabaseDeleteRecoversAfterPrimarySwitch()
        {
            using var app = CreateApplication($"cluster-delete-{Guid.NewGuid():N}", 3);
            var database = app.Services.GetRequiredService<IDatabase>();
            var key = $"cluster-delete-value-{Guid.NewGuid():N}";
            Assert.True(await database.StringSetAsync(key, "before-delete"));
            using var observer = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:17379" },
                Password = "cluster-test",
                AllowAdmin = true
            });
            var topology = await observer.GetServer("127.0.0.1", 17379).ClusterNodesAsync();
            Assert.NotNull(topology);
            var primary = topology.GetBySlot(key);
            Assert.NotNull(primary);
            var replica = Assert.Single(primary.Children);
            Assert.NotNull(replica.EndPoint);
            Assert.NotNull(primary.EndPoint);
            var promoted = observer.GetServer(replica.EndPoint);
            _ = await promoted.ExecuteAsync("CLUSTER", "FAILOVER");
            await WaitUntilAsync(async () =>
            {
                var current = await promoted.ClusterNodesAsync();
                return current?[replica.EndPoint] is { IsReplica: false } &&
                       current[primary.EndPoint] is { IsReplica: true };
            });
            Assert.Equal("before-delete", (await database.StringGetAsync(key)).ToString());
            await WaitUntilAsync(async () =>
            {
                _ = await database.KeyDeleteAsync(key);
                return true;
            });
            await WaitUntilAsync(async () => !await database.KeyExistsAsync(key));
        }

        [Fact]
        public async Task ClusterDatabasesKeepValuesTokensAndDataProtectionIsolatedAfterManagedPrimarySwitches()
        {
            var name = $"cluster-auth-{Guid.NewGuid():N}";
            var key = $"cluster-value-{Guid.NewGuid():N}";
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "alice")]));
            using var zero = CreateApplication(name, 0);
            using var three = CreateApplication(name, 3);
            var zeroDatabase = zero.Services.GetRequiredService<IDatabase>();
            var threeDatabase = three.Services.GetRequiredService<IDatabase>();
            Assert.Equal(0, zeroDatabase.Database);
            Assert.Equal(3, threeDatabase.Database);
            Assert.True(await zeroDatabase.StringSetAsync(key, "value-zero"));
            Assert.True(await threeDatabase.StringSetAsync(key, "value-three"));
            var zeroStore = zero.Services.GetRequiredService<IUserTokenStore>();
            var threeStore = three.Services.GetRequiredService<IUserTokenStore>();
            await zeroStore.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-zero"), RefreshToken = RefreshToken.Parse("refresh-zero"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
            await threeStore.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-three"), RefreshToken = RefreshToken.Parse("refresh-three"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
            var protectedZero = zero.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test")
                .Protect("payload-zero");
            var protectedThree = three.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test")
                .Protect("payload-three");
            await VerifyBothDatabases();

            using var observer = ConnectionMultiplexer.Connect(new ConfigurationOptions
            {
                EndPoints = { "127.0.0.1:17379" },
                Password = "cluster-test",
                AllowAdmin = true
            });
            var topology = await observer.GetServer("127.0.0.1", 17379).ClusterNodesAsync();
            Assert.NotNull(topology);
            var primaries = topology.Nodes.Where(static node => !node.IsReplica).ToArray();
            Assert.Equal(3, primaries.Length);
            foreach (var primary in primaries)
            {
                var replica = Assert.Single(primary.Children);
                Assert.NotNull(replica.EndPoint);
                Assert.NotNull(primary.EndPoint);
                var promoted = observer.GetServer(replica.EndPoint);
                _ = await promoted.ExecuteAsync("CLUSTER", "FAILOVER");
                await WaitUntilAsync(async () =>
                {
                    var current = await promoted.ClusterNodesAsync();
                    return current?[replica.EndPoint] is { IsReplica: false } &&
                           current[primary.EndPoint] is { IsReplica: true };
                });
                await VerifyBothDatabases();
                await WaitUntilAsync(async () =>
                {
                    await zeroStore.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-zero"), RefreshToken = RefreshToken.Parse("refresh-zero"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
                    await threeStore.StoreTokenAsync(user, new UserToken { AccessToken = AccessToken.Parse("access-three"), RefreshToken = RefreshToken.Parse("refresh-three"), ClientId = ClientId.Parse("test"), AccessTokenType = AccessTokenType.Parse("Bearer"), Expiration = DateTimeOffset.MaxValue }, ct: TestContext.Current.CancellationToken);
                    return (await zeroStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).Token!.TokenForSpecifiedParameters!.AccessToken.ToString() == "access-zero" &&
                           (await threeStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).Token!.TokenForSpecifiedParameters!.AccessToken.ToString() == "access-three";
                });
                await VerifyBothDatabases();
            }

            await WaitUntilAsync(async () =>
            {
                await zeroStore.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
                return true;
            });
            await WaitUntilAsync(async () => (await zeroStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).FailedResult!.Error == "not found");
            Assert.Equal("access-three", (await threeStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
            await WaitUntilAsync(async () =>
            {
                await threeStore.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
                return true;
            });
            await WaitUntilAsync(async () => (await threeStore.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).FailedResult!.Error == "not found");
            await WaitUntilAsync(async () =>
            {
                _ = await zeroDatabase.KeyDeleteAsync(key);
                return true;
            });
            await WaitUntilAsync(async () => !await zeroDatabase.KeyExistsAsync(key));
            Assert.Equal("value-three", (await threeDatabase.StringGetAsync(key)).ToString());
            await WaitUntilAsync(async () =>
            {
                _ = await threeDatabase.KeyDeleteAsync(key);
                return true;
            });
            await WaitUntilAsync(async () => !await threeDatabase.KeyExistsAsync(key));

            async Task VerifyBothDatabases()
            {
                Assert.Equal("value-zero", (await zeroDatabase.StringGetAsync(key)).ToString());
                Assert.Equal("value-three", (await threeDatabase.StringGetAsync(key)).ToString());
                using var restoredZero = CreateApplication(name, 0);
                using var restoredThree = CreateApplication(name, 3);
                var tokenZero = await restoredZero.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
                var tokenThree = await restoredThree.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
                Assert.Equal("access-zero", tokenZero.Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
                Assert.Equal("refresh-zero", tokenZero.Token!.TokenForSpecifiedParameters!.RefreshToken.ToString());
                Assert.Equal("access-three", tokenThree.Token!.TokenForSpecifiedParameters!.AccessToken.ToString());
                Assert.Equal("refresh-three", tokenThree.Token!.TokenForSpecifiedParameters!.RefreshToken.ToString());
                var protectorZero = restoredZero.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
                var protectorThree = restoredThree.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test");
                Assert.Equal("payload-zero", protectorZero.Unprotect(protectedZero));
                Assert.Equal("payload-three", protectorThree.Unprotect(protectedThree));
                _ = Assert.Throws<CryptographicException>(() => protectorZero.Unprotect(protectedThree));
                _ = Assert.Throws<CryptographicException>(() => protectorThree.Unprotect(protectedZero));
            }
        }

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

            throw new TimeoutException($"Cluster did not restore the expected operation within {timeout.TotalSeconds} seconds", lastError);
        }

        private static IHost CreateApplication(string name, int database)
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Application:Name"] = name,
                ["Application:Environment"] = "Test",
                ["Auth:OIDC:Client:OidcClientId"] = "test",
                ["Auth:OIDC:Client:OidcClientSecret"] = "test",
                ["Auth:OIDC:Client:EnableHealthChecks"] = "false",
                ["Auth:OIDC:Client:EnableRedisDataProtection"] = "true",
                ["Auth:OIDC:Client:TokenStoreType"] = "Redis",
                ["Redis:Connection:Configuration"] =
                    $"127.0.0.1:17379,password=cluster-test,defaultDatabase={database}"
            });
            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>();
            return builder.Build();
        }
    }
}
