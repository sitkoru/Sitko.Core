using System.Net;
using System.Net.Http.Headers;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class JwtAuthenticationTests
    {
        [Theory]
        [InlineData("valid", HttpStatusCode.OK)]
        [InlineData("issuer", HttpStatusCode.Unauthorized)]
        [InlineData("audience", HttpStatusCode.Unauthorized)]
        [InlineData("signature", HttpStatusCode.Unauthorized)]
        [InlineData("expired", HttpStatusCode.Unauthorized)]
        public async Task JwtValidatesIssuerAudienceSignatureAndLifetime(string invalidPart, HttpStatusCode expected)
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", fixture.Provider.CreateApiToken(invalidPart));
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(expected, response.StatusCode);
            if (expected == HttpStatusCode.OK)
            {
                Assert.Equal("authorized", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            }
        }

        [Fact]
        public async Task OidcAndJwtUseExplicitIndependentPolicies()
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true);
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            Assert.Equal("login-access", await fixture.Client.GetStringAsync("/token", TestContext.Current.CancellationToken));
            using var api = await fixture.Client.GetAsync("/api", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, api.StatusCode);
        }
    }
}
