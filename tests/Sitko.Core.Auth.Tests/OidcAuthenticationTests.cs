using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Sitko.Core.App.Web;
using Sitko.Core.Auth.OIDC;
using Sitko.Core.Auth.OIDC.Tokens;
using Xunit;

namespace Sitko.Core.Auth.Tests
{
    public sealed class OidcAuthenticationTests
    {
        [Theory]
        [InlineData(TokenStoreType.None)]
        [InlineData(TokenStoreType.InMemory)]
        public async Task CodePkceLoginCreatesCookieAndExposesAccessToken(TokenStoreType storage)
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: storage);
            using var login = await fixture.LoginAsync();
            Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
            Assert.Contains(login.Headers.GetValues("Set-Cookie"), static cookie => cookie.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
            fixture.UseCookies(login);
            Assert.Equal("login-access", await fixture.Client.GetStringAsync("/token", TestContext.Current.CancellationToken));
            Assert.Equal("authorization_code", fixture.Provider.LastGrantType);
            Assert.True(fixture.Provider.PkceVerified);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UserInfoMustMatchValidatedSubjectBeforeTokensAreStored(bool mismatchedSubject)
        {
            await using var fixture = await OidcFixture.CreateAsync(
                moduleConfigure: static options => options.GetClaimsFromUserInfoEndpoint = true);
            fixture.Provider.UserInfoSubject = mismatchedSubject ? "another-user" : "alice";
            using var login = await fixture.LoginAsync();
            Assert.Equal(mismatchedSubject ? HttpStatusCode.Forbidden : HttpStatusCode.Redirect, login.StatusCode);
            var store = fixture.App.Services.GetRequiredService<Duende.AccessTokenManagement.OpenIdConnect.IUserTokenStore>();
            var result = await store.GetTokenAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "alice")], "test")), ct: TestContext.Current.CancellationToken);
            Assert.Equal(!mismatchedSubject, result.Succeeded);
        }

        [Fact]
        public async Task CustomCallbackAndClaimMappingPreserveManagedTokens()
        {
            await using var fixture = await OidcFixture.CreateAsync(static options =>
            {
                options.CallbackPath = "/custom-callback";
                options.MapInboundClaims = false;
            }, moduleConfigure: static options => options.UserIdClaimName = "sub");
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            Assert.Equal("login-access", await fixture.Client.GetStringAsync("/token", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task TokenValidationRejectionDoesNotCreateCookieOrStoreTokens()
        {
            await using var fixture = await OidcFixture.CreateAsync(static options =>
                options.Events.OnTokenValidated = static context =>
                {
                    context.Fail("Denied by application");
                    return Task.CompletedTask;
                });
            using var denied = await fixture.LoginAsync();
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            var store = fixture.App.Services.GetRequiredService<Duende.AccessTokenManagement.OpenIdConnect.IUserTokenStore>();
            var result = await store.GetTokenAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "alice")], "test")), ct: TestContext.Current.CancellationToken);
            Assert.False(result.Succeeded);
        }

        [Fact]
        public async Task ProviderLogoutUsesEndSessionEndpoint()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None);
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var logout = await fixture.Client.PostAsync("/provider-logout", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, logout.StatusCode);
            Assert.Equal("https://issuer.example/logout", logout.Headers.Location!.GetLeftPart(UriPartial.Path));
            var query = QueryHelpers.ParseQuery(logout.Headers.Location.Query);
            Assert.False(string.IsNullOrEmpty(query["id_token_hint"].ToString()));
            Assert.Equal("https://app.example/signout-callback-oidc", query["post_logout_redirect_uri"].ToString());
        }

        [Fact]
        public async Task RefreshFailureDoesNotReturnStaleCookieToken()
        {
            await using var fixture = await OidcFixture.CreateAsync();
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            Assert.Equal("missing", await fixture.Client.GetStringAsync("/token", TestContext.Current.CancellationToken));
            Assert.Equal("login-refresh", fixture.Provider.UsedRefreshToken);
        }

        [Fact]
        public async Task ExpiredTokenRefreshesAndCookieLogoutRevokesAndClearsTokens()
        {
            await using var fixture = await OidcFixture.CreateAsync();
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            Assert.Equal("refreshed-access", await fixture.Client.GetStringAsync("/token", TestContext.Current.CancellationToken));
            Assert.Equal("login-refresh", fixture.Provider.UsedRefreshToken);
            using var logout = await fixture.Client.PostAsync("/logout", null, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, logout.StatusCode);
            Assert.Equal("rotated-refresh", fixture.Provider.RevokedToken);
            var store = fixture.App.Services.GetRequiredService<Duende.AccessTokenManagement.OpenIdConnect.IUserTokenStore>();
            var result = await store.GetTokenAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "alice")], "test")), ct: TestContext.Current.CancellationToken);
            Assert.False(result.Succeeded);
            Assert.Contains(logout.Headers.GetValues("Set-Cookie"), static cookie => cookie.StartsWith(".AspNetCore.Cookies=;", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task InvalidStateOrIdTokenDoesNotCreateSession(bool invalidState)
        {
            await using var fixture = await OidcFixture.CreateAsync();
            if (!invalidState)
            {
                fixture.Provider.IdTokenAudience = "another-client";
            }
            using var response = await fixture.LoginAsync(invalidState);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
            Assert.DoesNotContain(response.Headers.TryGetValues("Set-Cookie", out var cookies) ? cookies : [], static cookie => cookie.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal));
        }

        [Fact]
        public async Task RejectedTicketDoesNotStoreLoginTokens()
        {
            await using var fixture = await OidcFixture.CreateAsync(static options =>
                options.Events.OnTicketReceived = static context =>
                {
                    context.Response.StatusCode = 403;
                    context.HandleResponse();
                    return Task.CompletedTask;
                });
            using var denied = await fixture.LoginAsync();
            Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
            var store = fixture.App.Services.GetRequiredService<Duende.AccessTokenManagement.OpenIdConnect.IUserTokenStore>();
            var token = await store.GetTokenAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "alice")], "test")), ct: TestContext.Current.CancellationToken);
            Assert.Equal("not found", token.FailedResult!.Error);
        }
    }

    internal sealed class OidcFixture(WebApplication app, HttpClient client, TestIdentityProvider provider) : IAsyncDisposable
    {
        public WebApplication App { get; } = app;
        public HttpClient Client { get; } = client;
        public TestIdentityProvider Provider { get; } = provider;

        public static async Task<OidcFixture> CreateAsync(Action<OpenIdConnectOptions>? configure = null, TokenStoreType storage = TokenStoreType.InMemory, bool withJwt = false, Action<OidcAuthModuleOptions>? moduleConfigure = null)
        {
            var provider = new TestIdentityProvider();
            var builder = WebApplication.CreateBuilder();
            _ = builder.WebHost.UseTestServer();
            _ = builder.AddSitkoCoreWeb().AddOidcAuth(options =>
            {
                options.OidcClientId = "test";
                options.OidcClientSecret = "secret";
                options.OidcServerUrl = "https://issuer.example";
                options.EnableHealthChecks = false;
                options.EnableRedisDataProtection = false;
                options.GetClaimsFromUserInfoEndpoint = false;
                options.TokenStoreType = storage;
                options.ConfigureOpenIdConnectOptions = configure;
                moduleConfigure?.Invoke(options);
            });
            if (withJwt)
            {
                _ = builder.AddJwtAuth(options =>
                {
                    options.JwtAudience = "api";
                    options.OidcServerUrl = "https://issuer.example";
                    options.EnableHealthChecks = false;
                    options.EnableRedisDataProtection = false;
                    options.ConfigureJwtBearerOptions = jwt =>
                    {
                        jwt.Configuration = provider.Configuration;
                        jwt.TokenValidationParameters.ClockSkew = TimeSpan.Zero;
                    };
                });
            }
            _ = builder.Services.Configure<OpenIdConnectOptions>("oidc", options =>
            {
                options.Configuration = provider.Configuration;
                options.Backchannel = new HttpClient(provider);
                options.ResponseMode = "query";
                options.Events.OnRemoteFailure = static context =>
                {
                    context.Response.StatusCode = 403;
                    context.HandleResponse();
                    return Task.CompletedTask;
                };
            });
            var app = builder.Build();
            _ = app.UseAuthentication();
            _ = app.UseAuthorization();
            _ = app.MapGet("/api", static () => "authorized").RequireAuthorization(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Bearer")
                    .RequireAuthenticatedUser().RequireClaim("sub", "alice").Build());
            _ = app.MapGet("/login", static async context => await context.ChallengeAsync("oidc"));
            _ = app.MapPost("/logout", static async context => await context.SignOutAsync("Cookies"));
            _ = app.MapPost("/provider-logout", static async context => await context.SignOutAsync("oidc"));
            _ = app.MapGet("/token", static async context =>
            {
                var value = await context.RequestServices.GetRequiredService<IUserTokenProvider>()
                    .GetUserTokenAsync(context, context.RequestAborted);
                await context.Response.WriteAsync(value ?? "missing", context.RequestAborted);
            }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = "Cookies" });
            await app.StartAsync(TestContext.Current.CancellationToken);
            var client = app.GetTestClient();
            client.BaseAddress = new Uri("https://app.example");
            return new OidcFixture(app, client, provider);
        }

        public async Task<HttpResponseMessage> LoginAsync(bool invalidState = false)
        {
            using var challenge = await Client.GetAsync("/login");
            var query = QueryHelpers.ParseQuery(challenge.Headers.Location!.Query);
            Provider.Nonce = query["nonce"].ToString();
            Assert.Equal("code", query["response_type"].ToString());
            Assert.Equal("S256", query["code_challenge_method"].ToString());
            Provider.CodeChallenge = query["code_challenge"].ToString();
            using var callback = new HttpRequestMessage(HttpMethod.Get,
                new Uri(query["redirect_uri"].ToString()).AbsolutePath + "?code=code&state=" + Uri.EscapeDataString(invalidState ? "tampered" : query["state"].ToString()));
            _ = callback.Headers.TryAddWithoutValidation("Cookie", string.Join("; ",
                challenge.Headers.GetValues("Set-Cookie").Select(static cookie => cookie.Split(';')[0])));
            return await Client.SendAsync(callback);
        }

        public void UseCookies(HttpResponseMessage response)
        {
            _ = Client.DefaultRequestHeaders.Remove("Cookie");
            _ = Client.DefaultRequestHeaders.TryAddWithoutValidation("Cookie", string.Join("; ",
                response.Headers.GetValues("Set-Cookie").Select(static cookie => cookie.Split(';')[0])));
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await App.DisposeAsync();
            Provider.Dispose();
        }
    }

    internal sealed class TestIdentityProvider : HttpMessageHandler
    {
        private readonly RSA rsa = RSA.Create(2048);
        public string Nonce { get; set; } = "";
        public string IdTokenAudience { get; set; } = "test";
        public string UserInfoSubject { get; set; } = "alice";
        public string CodeChallenge { get; set; } = "";
        public string LastGrantType { get; private set; } = "";
        public bool PkceVerified { get; private set; }
        public int LoginExpiresIn { get; set; } = 300;
        public bool RejectRefresh { get; set; }
        public string UsedRefreshToken { get; private set; } = "";
        public string RevokedToken { get; private set; } = "";
        public OpenIdConnectConfiguration Configuration { get; }
        public TestIdentityProvider()
        {
            Configuration = new OpenIdConnectConfiguration
            {
                Issuer = "https://issuer.example",
                AuthorizationEndpoint = "https://issuer.example/authorize",
                TokenEndpoint = "https://issuer.example/token",
                EndSessionEndpoint = "https://issuer.example/logout",
                RevocationEndpoint = "https://issuer.example/revoke",
                UserInfoEndpoint = "https://issuer.example/userinfo"
            };
            Configuration.SigningKeys.Add(new RsaSecurityKey(rsa) { KeyId = "test" });
        }

        public string CreateApiToken(string invalidPart)
        {
            var token = new JwtSecurityToken(
                invalidPart == "issuer" ? "https://other.example" : "https://issuer.example",
                invalidPart == "audience" ? "other-api" : "api", [new Claim("sub", "alice")],
                DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(invalidPart == "expired" ? -1 : 5),
                invalidPart == "signature"
                    ? new SigningCredentials(new SymmetricSecurityKey(new byte[32]), SecurityAlgorithms.HmacSha256)
                    : new SigningCredentials(Configuration.SigningKeys.First(), SecurityAlgorithms.RsaSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.RequestUri!.AbsolutePath == "/userinfo")
            {
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { sub = UserInfoSubject, name = "Alice" }) };
            }
            var form = QueryHelpers.ParseQuery(await request.Content!.ReadAsStringAsync(cancellationToken));
            if (request.RequestUri!.AbsolutePath == "/revoke")
            {
                RevokedToken = form["token"].ToString();
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(new { }) };
            }
            LastGrantType = form["grant_type"].ToString();
            if (LastGrantType == "refresh_token")
            {
                UsedRefreshToken = form["refresh_token"].ToString();
                if (RejectRefresh)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = JsonContent.Create(new { error = "invalid_grant" }) };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(new { access_token = "refreshed-access", refresh_token = "rotated-refresh", token_type = "Bearer", expires_in = 300 })
                };
            }
            PkceVerified = WebEncoders.Base64UrlEncode(SHA256.HashData(System.Text.Encoding.ASCII.GetBytes(form["code_verifier"].ToString()))) == CodeChallenge;
            var token = new JwtSecurityToken("https://issuer.example", IdTokenAudience,
                [new Claim("sub", "alice"), new Claim("nonce", Nonce), new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture), ClaimValueTypes.Integer64)],
                DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(5),
                new SigningCredentials(Configuration.SigningKeys.First(), SecurityAlgorithms.RsaSha256));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new
                {
                    access_token = "login-access",
                    refresh_token = "login-refresh",
                    token_type = "Bearer",
                    expires_in = LoginExpiresIn,
                    scope = "openid profile offline_access",
                    id_token = new JwtSecurityTokenHandler().WriteToken(token)
                })
            };
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                rsa.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
