using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components.Endpoints;
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
        public async Task BearerTokenRemainsAvailableWithRecovery(bool withCookie)
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true, saveJwtToken: true, storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            if (withCookie)
            {
                using var login = await fixture.LoginAsync();
                fixture.UseCookies(login);
            }
            var token = fixture.Provider.CreateApiToken("", "bob");
            using var request = new HttpRequestMessage(HttpMethod.Get, "/bearer-page");
            request.Headers.Accept.ParseAdd("text/html");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(token, await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ReplacedPrincipalWithoutSchemeEvidenceDoesNotRecoverCookie()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var response = await fixture.Client.GetAsync("/replaced-principal", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("missing", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task MissingCookieTokenDoesNotUseOtherSchemeToken()
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true, saveJwtToken: true, storage: TokenStoreType.None,
                moduleConfigure: static options => { options.EnableSessionRecovery = true; options.SaveTokens = false; });
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/mixed-page");
            request.Headers.Accept.ParseAdd("text/html");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", fixture.Provider.CreateApiToken("", "bob"));
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://issuer.example/authorize", response.Headers.Location!.GetLeftPart(UriPartial.Path));
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

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task BearerPageDoesNotUseAnotherCookieSession(bool withCookie)
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            if (withCookie)
            {
                fixture.Provider.LoginExpiresIn = -60;
                using var login = await fixture.LoginAsync();
                fixture.UseCookies(login);
            }
            using var request = new HttpRequestMessage(HttpMethod.Get, "/bearer-page");
            request.Headers.Accept.ParseAdd("text/html");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", fixture.Provider.CreateApiToken("", "bob"));
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("missing", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task EffectivePolicyProtectsHtmlPage(bool fallback)
        {
            await using var fixture = await OidcFixture.CreateAsync(fallbackPolicy: fallback,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, fallback ? "/fallback-page" : "/policy-page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://issuer.example/authorize", response.Headers.Location!.GetLeftPart(UriPartial.Path));
            Assert.Equal("refresh_token", fixture.Provider.LastGrantType);
        }

        [Fact]
        public async Task ClonedCookiePrincipalStillRecoversSession()
        {
            await using var fixture = await OidcFixture.CreateAsync(cloneClaims: true,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("refresh_token", fixture.Provider.LastGrantType);
        }

        [Fact]
        public async Task MixedPolicyRefreshesCookiePrincipalNotBearerPrincipal()
        {
            await using var fixture = await OidcFixture.CreateAsync(withJwt: true,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/mixed-page");
            request.Headers.Accept.ParseAdd("text/html");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", fixture.Provider.CreateApiToken("", "bob"));
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://issuer.example/authorize", response.Headers.Location!.GetLeftPart(UriPartial.Path));
            Assert.Equal("refresh_token", fixture.Provider.LastGrantType);
        }

        [Fact]
        public async Task StandardPipelineEnablesRecoveryForProtectedPage()
        {
            await using var fixture = await OidcFixture.CreateAsync(standardPipeline: true,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://issuer.example/authorize", response.Headers.Location!.GetLeftPart(UriPartial.Path));
        }

        [Fact]
        public async Task MissingManagedTokenOnHtmlPageStartsSignIn()
        {
            await using var fixture = await OidcFixture.CreateAsync(moduleConfigure: static options => options.EnableSessionRecovery = true);
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            var store = fixture.App.Services.GetRequiredService<Duende.AccessTokenManagement.OpenIdConnect.IUserTokenStore>();
            await store.ClearTokenAsync(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "alice")], "test")), ct: TestContext.Current.CancellationToken);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task SuccessfulRefreshOpensHtmlPageWithoutSignIn()
        {
            await using var fixture = await OidcFixture.CreateAsync(moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("refresh_token", fixture.Provider.LastGrantType);
            Assert.Empty(fixture.Provider.RevokedToken);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TemporaryRefreshFailureDoesNotStartSignInOrRevoke(bool networkFailure)
        {
            await using var fixture = await OidcFixture.CreateAsync(moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RefreshUnavailable = true;
            fixture.Provider.NetworkFailure = networkFailure;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.NotEqual(HttpStatusCode.Redirect, response.StatusCode);
            Assert.False(response.Headers.Contains("Set-Cookie"));
            Assert.Empty(fixture.Provider.RevokedToken);
        }

        [Fact]
        public async Task HealthyReplacementCookieOpensPageWithoutSignIn()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var oldLogin = await fixture.LoginAsync();
            fixture.UseCookies(oldLogin);
            fixture.Provider.LoginExpiresIn = 300;
            using var newLogin = await fixture.LoginAsync();
            fixture.UseCookies(newLogin);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("page", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ApiWithInvalidUserTokenReturnsUnauthorizedWithoutRedirect()
        {
            await using var fixture = await OidcFixture.CreateAsync(moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var response = await fixture.Client.GetAsync("/token", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
            Assert.DoesNotContain("text/html", response.Content.Headers.ContentType?.ToString() ?? "");
        }

        [Fact]
        public async Task ApiRequestIsNotRedirectedBySessionRecovery()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/machine");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
        }

        [Fact]
        public async Task InvalidSessionAfterAutomaticSignInDoesNotLoop()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var challenge = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            using var callback = await fixture.CompleteLoginAsync(challenge);
            fixture.UseCookies(callback);
            using var retry = new HttpRequestMessage(HttpMethod.Get, "/page");
            retry.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(retry, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
            Assert.Null(response.Headers.Location);
            var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
            Assert.Contains("__sitko_relogin=1", body);
            using var manual = new HttpRequestMessage(HttpMethod.Get, "/page?__SITKO_RELOGIN=1");
            manual.Headers.Accept.ParseAdd("text/html");
            using var manualChallenge = await fixture.Client.SendAsync(manual, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, manualChallenge.StatusCode);
            using var manualCallback = await fixture.CompleteLoginAsync(manualChallenge);
            Assert.Equal("/page", manualCallback.Headers.Location!.OriginalString);
        }

        [Fact]
        public async Task ExpiredCookieTokenOnHtmlPageStartsSignIn()
        {
            await using var fixture = await OidcFixture.CreateAsync(storage: TokenStoreType.None,
                moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }

        [Fact]
        public async Task InvalidRefreshOnHtmlPageStartsSignIn()
        {
            await using var fixture = await OidcFixture.CreateAsync(moduleConfigure: static options => options.EnableSessionRecovery = true);
            fixture.Provider.LoginExpiresIn = -60;
            fixture.Provider.RejectRefresh = true;
            using var login = await fixture.LoginAsync();
            fixture.UseCookies(login);
            using var request = new HttpRequestMessage(HttpMethod.Get, "/page?tab=details");
            request.Headers.Accept.ParseAdd("text/html");
            using var response = await fixture.Client.SendAsync(request, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("https://issuer.example/authorize", response.Headers.Location!.GetLeftPart(UriPartial.Path));
            fixture.Provider.LoginExpiresIn = 300;
            fixture.Provider.RejectRefresh = false;
            using var callback = await fixture.CompleteLoginAsync(response);
            Assert.Equal("/page?tab=details", callback.Headers.Location!.OriginalString);
            fixture.UseCookies(callback);
            using var retry = new HttpRequestMessage(HttpMethod.Get, "/page?tab=details");
            retry.Headers.Accept.ParseAdd("text/html");
            using var page = await fixture.Client.SendAsync(retry, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, page.StatusCode);
            Assert.Empty(fixture.Provider.RevokedToken);
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

        public static async Task<OidcFixture> CreateAsync(Action<OpenIdConnectOptions>? configure = null, TokenStoreType storage = TokenStoreType.InMemory, bool withJwt = false, Action<OidcAuthModuleOptions>? moduleConfigure = null, bool standardPipeline = false, bool fallbackPolicy = false, bool saveJwtToken = false, bool cloneClaims = false)
        {
            var provider = new TestIdentityProvider();
            var builder = WebApplication.CreateBuilder();
            _ = builder.WebHost.UseTestServer();
            if (cloneClaims)
            {
                _ = builder.Services.AddTransient<IClaimsTransformation, CloneClaimsTransformation>();
            }
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
                        jwt.SaveToken = saveJwtToken;
                    };
                });
            }
            if (fallbackPolicy)
            {
                _ = builder.Services.PostConfigure<Microsoft.AspNetCore.Authorization.AuthorizationOptions>(options =>
                    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Cookies").RequireAuthenticatedUser().Build());
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
            if (standardPipeline)
            {
                _ = app.MapSitkoCore();
            }
            else
            {
                _ = app.UseAuthentication();
                _ = app.UseAuthorization();
                _ = app.UseOidcSessionRecovery();
            }
            _ = app.MapGet("/api", static () => "authorized").RequireAuthorization(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Bearer")
                    .RequireAuthenticatedUser().RequireClaim("sub", "alice").Build());
            _ = app.MapGet("/login", static async context => await context.ChallengeAsync("oidc")).AllowAnonymous();
            _ = app.MapGet("/page", static () => "page").WithMetadata(new ComponentTypeMetadata(typeof(OidcAuthenticationTests))).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = "Cookies" });
            _ = app.MapGet("/fallback-page", static () => "page").WithMetadata(new ComponentTypeMetadata(typeof(OidcAuthenticationTests)));
            _ = app.MapGet("/policy-page", static () => "page").WithMetadata(new ComponentTypeMetadata(typeof(OidcAuthenticationTests))).WithMetadata(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Cookies").RequireAuthenticatedUser().Build());
            _ = app.MapGet("/mixed-page", static () => "page").WithMetadata(new ComponentTypeMetadata(typeof(OidcAuthenticationTests))).WithMetadata(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Cookies", "Bearer").RequireAuthenticatedUser().Build());
            _ = app.MapGet("/machine", static () => Results.Unauthorized()).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = "Cookies" });
            _ = app.MapGet("/replaced-principal", static async context =>
            {
                context.User = new ClaimsPrincipal(context.User.Identities.Select(static identity => identity.Clone()));
                var value = await context.RequestServices.GetRequiredService<IUserTokenProvider>().GetUserTokenAsync(context, context.RequestAborted);
                await context.Response.WriteAsync(value ?? "missing", context.RequestAborted);
            }).RequireAuthorization(new Microsoft.AspNetCore.Authorization.AuthorizeAttribute { AuthenticationSchemes = "Cookies" });
            _ = app.MapGet("/bearer-page", static async context =>
            {
                var value = await context.RequestServices.GetRequiredService<IUserTokenProvider>().GetUserTokenAsync(context, context.RequestAborted);
                await context.Response.WriteAsync(value ?? "missing", context.RequestAborted);
            }).WithMetadata(new ComponentTypeMetadata(typeof(OidcAuthenticationTests))).RequireAuthorization(
                new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Bearer").RequireAuthenticatedUser().Build());
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
            return await CompleteLoginAsync(challenge, invalidState);
        }

        public async Task<HttpResponseMessage> CompleteLoginAsync(HttpResponseMessage challenge, bool invalidState = false)
        {
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

    internal sealed class CloneClaimsTransformation : IClaimsTransformation
    {
        public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) =>
            Task.FromResult(new ClaimsPrincipal(principal.Identities.Select(static identity => identity.Clone())));
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
        public bool RefreshUnavailable { get; set; }
        public bool NetworkFailure { get; set; }
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

        public string CreateApiToken(string invalidPart, string subject = "alice")
        {
            var token = new JwtSecurityToken(
                invalidPart == "issuer" ? "https://other.example" : "https://issuer.example",
                invalidPart == "audience" ? "other-api" : "api", [new Claim("sub", subject)],
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
                if (RefreshUnavailable)
                {
                    if (NetworkFailure)
                    {
                        throw new HttpRequestException("simulated network failure");
                    }
                    return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
                }
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
