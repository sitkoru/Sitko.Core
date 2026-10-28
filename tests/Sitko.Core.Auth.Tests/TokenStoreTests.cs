using System.Security.Claims;
using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.AccessTokenManagement;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Auth.OIDC;
using Sitko.Core.Auth.OIDC.Tokens;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class TokenStoreTests
    {
        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task StoresReadsAndClearsTokens(bool hasRefresh)
        {
            using var host = CreateHost();
            var store = host.Services.GetRequiredService<IUserTokenStore>();
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "alice")], "test"));
            var token = new UserToken
            {
                AccessToken = AccessToken.Parse("access"),
                RefreshToken = hasRefresh ? RefreshToken.Parse("refresh") : null,
                ClientId = ClientId.Parse("test"),
                AccessTokenType = AccessTokenType.Parse("Bearer"),
                Expiration = DateTimeOffset.MaxValue
            };
            await store.StoreTokenAsync(user, token, ct: TestContext.Current.CancellationToken);
            var result = await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.True(result.Succeeded);
            Assert.Equal(token, result.Token.TokenForSpecifiedParameters);
            Assert.Equal(hasRefresh, result.Token.RefreshToken is not null);
            await store.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.False((await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken)).Succeeded);
        }

        [Fact]
        public async Task MissingUserIdentifierCannotReadWriteOrClearSharedStorage()
        {
            using var host = CreateHost();
            var store = host.Services.GetRequiredService<IUserTokenStore>();
            var user = new ClaimsPrincipal(new ClaimsIdentity());
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => store.ClearTokenAsync(user, ct: TestContext.Current.CancellationToken));
            _ = await Assert.ThrowsAsync<InvalidOperationException>(() => store.StoreTokenAsync(user, new UserToken
            {
                AccessToken = AccessToken.Parse("access"),
                ClientId = ClientId.Parse("test"),
                AccessTokenType = AccessTokenType.Parse("Bearer"),
                Expiration = DateTimeOffset.MaxValue
            }, ct: TestContext.Current.CancellationToken));
        }

        private static IHost CreateHost()
        {
            var builder = Host.CreateApplicationBuilder();
            _ = builder.AddSitkoCore().AddModule<OidcAuthModule, OidcAuthModuleOptions>(static options =>
            {
                options.OidcClientId = "test";
                options.OidcClientSecret = "secret";
                options.EnableHealthChecks = false;
                options.EnableRedisDataProtection = false;
                options.TokenStoreType = TokenStoreType.InMemory;
            });
            _ = builder.Services.AddMemoryCache();
            return builder.Build();
        }

        [Fact]
        public async Task ReadsLegacyStringTokenPayload()
        {
            using var host = CreateHost();
            _ = host.Services.GetRequiredService<IMemoryCache>().Set("alice",
                """{"AccessToken":"legacy-access","RefreshToken":"legacy-refresh","Expiration":"2030-01-01T00:00:00+00:00","Scope":"openid profile"}""");
            var store = host.Services.GetRequiredService<IUserTokenStore>();
            var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "alice")], "test"));
            var result = await store.GetTokenAsync(user, ct: TestContext.Current.CancellationToken);
            Assert.True(result.Succeeded);
            var token = result.Token.TokenForSpecifiedParameters!;
            Assert.Equal("legacy-access", token.AccessToken.ToString());
            Assert.Equal("legacy-refresh", token.RefreshToken.ToString());
            Assert.Equal("openid profile", token.Scope.ToString());
            Assert.Equal(DateTimeOffset.Parse("2030-01-01T00:00:00+00:00", System.Globalization.CultureInfo.InvariantCulture), token.Expiration);
        }
    }
}
