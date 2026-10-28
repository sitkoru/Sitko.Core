using System.Diagnostics;
using System.Security.Claims;
using System.Security.Cryptography;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Auth.IdentityServer;
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
            {
                options.Host = "127.0.0.1";
                options.Port = 17379;
                options.Password = "cluster-test";
                options.Db = 3;
            });
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
            var database = app.Services.GetRequiredKeyedService<IDatabase>(typeof(OidcIdentityServerModuleOptions));
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
            var zeroDatabase = zero.Services.GetRequiredKeyedService<IDatabase>(typeof(OidcIdentityServerModuleOptions));
            var threeDatabase = three.Services.GetRequiredKeyedService<IDatabase>(typeof(OidcIdentityServerModuleOptions));
            Assert.Equal(0, zeroDatabase.Database);
            Assert.Equal(3, threeDatabase.Database);
            Assert.True(await zeroDatabase.StringSetAsync(key, "value-zero"));
            Assert.True(await threeDatabase.StringSetAsync(key, "value-three"));
            var zeroStore = zero.Services.GetRequiredService<IUserTokenStore>();
            var threeStore = three.Services.GetRequiredService<IUserTokenStore>();
            await zeroStore.StoreTokenAsync(user, new UserToken { AccessToken = "access-zero", RefreshToken = "refresh-zero" });
            await threeStore.StoreTokenAsync(user, new UserToken { AccessToken = "access-three", RefreshToken = "refresh-three" });
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
                    await zeroStore.StoreTokenAsync(user, new UserToken { AccessToken = "access-zero", RefreshToken = "refresh-zero" });
                    await threeStore.StoreTokenAsync(user, new UserToken { AccessToken = "access-three", RefreshToken = "refresh-three" });
                    return (await zeroStore.GetTokenAsync(user)).AccessToken == "access-zero" &&
                           (await threeStore.GetTokenAsync(user)).AccessToken == "access-three";
                });
                await VerifyBothDatabases();
            }

            await WaitUntilAsync(async () =>
            {
                await zeroStore.ClearTokenAsync(user);
                return true;
            });
            await WaitUntilAsync(async () => (await zeroStore.GetTokenAsync(user)).Error == "not found");
            Assert.Equal("access-three", (await threeStore.GetTokenAsync(user)).AccessToken);
            await WaitUntilAsync(async () =>
            {
                await threeStore.ClearTokenAsync(user);
                return true;
            });
            await WaitUntilAsync(async () => (await threeStore.GetTokenAsync(user)).Error == "not found");
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
                var tokenZero = await restoredZero.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user);
                var tokenThree = await restoredThree.Services.GetRequiredService<IUserTokenStore>().GetTokenAsync(user);
                Assert.Equal("access-zero", tokenZero.AccessToken);
                Assert.Equal("refresh-zero", tokenZero.RefreshToken);
                Assert.Equal("access-three", tokenThree.AccessToken);
                Assert.Equal("refresh-three", tokenThree.RefreshToken);
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
                ["Auth:IdentityServer:Oidc:OidcClientId"] = "test",
                ["Auth:IdentityServer:Oidc:OidcClientSecret"] = "test",
                ["Auth:IdentityServer:Oidc:EnableHealthChecks"] = "false",
                ["Auth:IdentityServer:Oidc:EnableRedisDataProtection"] = "true",
                ["Auth:IdentityServer:Oidc:TokenStoreType"] = "Redis",
                ["Auth:IdentityServer:Oidc:RedisHost"] = "127.0.0.1",
                ["Auth:IdentityServer:Oidc:RedisPort"] = "17379",
                ["Auth:IdentityServer:Oidc:RedisPassword"] = "cluster-test",
                ["Auth:IdentityServer:Oidc:RedisDb"] = database.ToString(System.Globalization.CultureInfo.InvariantCulture)
            });
            _ = builder.AddSitkoCore().AddModule<OidcIdentityServerModule, OidcIdentityServerModuleOptions>();
            return builder.Build();
        }
    }
}
