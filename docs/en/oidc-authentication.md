# OIDC and JWT authentication

[Русский](../ru/oidc-authentication.md) · [Version 13 migration](migration-v13.md)

`Sitko.Core.Auth.OIDC` registers standard ASP.NET Core handlers for OpenID Connect
login and incoming JWT validation in a server-side .NET 10 application. Keycloak
and other compatible providers can be used. User admission and API permissions
are configured separately from authentication.

## Web application registration

This example uses client `admin-web` in realm `example`. Replace these names and
URLs for your application. Supply the secret through a secret store or
`Auth__OIDC__Client__OidcClientSecret`; do not commit it.

```csharp
using Sitko.Core.App.Web;
using Sitko.Core.Auth.OIDC;

var builder = WebApplication.CreateBuilder(args);
builder.AddSitkoCoreWeb().AddOidcAuth();
var app = builder.Build();
app.MapSitkoCore();
// Register the application's protected pages and login/logout actions here.
app.Run();
```

```json
{
  "Auth": {
    "OIDC": {
      "Client": {
        "OidcServerUrl": "https://sso.example.com/realms/example",
        "OidcClientId": "admin-web",
        "RequireHttps": true,
        "OidcScopes": ["email"],
        "TokenStoreType": "None"
      }
    }
  }
}
```

Defaults are schemes `Cookies` and `oidc`, Code Flow with PKCE, `SaveTokens = true`
and `GetClaimsFromUserInfoEndpoint = true`. `offline_access` is added to the standard
scopes. Allow the required scopes at the provider, or remove `offline_access` through
the callback below if offline refresh is unnecessary. `RequireHttps` defaults to
`false` for existing configurations; set it to `true` in deployed environments.
Cookies always use `Secure`, so use HTTPS for browser testing too. An HTTP Keycloak
endpoint in a local environment does not remove the application's HTTPS requirement.

Configure standard OIDC options through the callback:

```csharp
builder.AddOidcAuth(options =>
{
    options.ConfigureOpenIdConnectOptions = oidc =>
    {
        oidc.CallbackPath = "/signin-oidc";
        oidc.Scope.Remove("offline_access");
        oidc.MapInboundClaims = false;
        oidc.TokenValidationParameters.NameClaimType = "name";
    };
    options.UserIdClaimName = "sub";
});
```

This is an alternative registration after `AddSitkoCoreWeb()`, not a second call
in addition to the first example. Match `UserIdClaimName` to the resulting principal
when changing claim mapping; its default is `ClaimTypes.NameIdentifier`. Both builder
forms support a callback receiving `IApplicationContext` and an `optionsKey` argument
for a different configuration section. Multiple providers under the fixed `oidc`
scheme are outside this API's supported registration model.

Set custom callbacks through `oidc.Events`. With managed storage, `EventsType` is
rejected when options are created so it cannot silently disable internal handling.
Use `context.Fail(...)` to reject `OnTokenValidated`. For `OnTicketReceived`, complete
the response with `HandleResponse()` or pass processing on with `SkipHandler()` as
specified by ASP.NET Core; `Fail()` alone at this late event does not finish the
response. Tokens are stored after successful protocol validation, UserInfo and the
custom `OnTicketReceived` callback.

## JWT and policies

Register `AddJwtAuth()` for an API using `Auth:OIDC:Jwt`:

```json
{
  "Auth": {
    "OIDC": {
      "Jwt": {
        "OidcServerUrl": "https://sso.example.com/realms/example",
        "JwtAudience": "example-api",
        "RequireHttps": true
      }
    }
  }
}
```

The provider must include `example-api` in the API token's `aud`. The `Bearer`
handler validates signature, issuer, audience and lifetime. `ValidIssuers` and
`ConfigureJwtBearerOptions` retain application-specific configuration. JWT uses
original claim names (`MapInboundClaims = false`); the callback can explicitly
change this setting.

When OIDC and JWT are both registered, select schemes explicitly in policies instead
of relying on module order or the default scheme:

```csharp
builder.AddOidcAuth();
builder.AddJwtAuth(options => options.Policies["api"] =
    new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder("Bearer")
        .RequireAuthenticatedUser().Build());
// After Build(): app.MapGet("/api/example", ...).RequireAuthorization("api");
```

Page policies similarly select `Cookies`. The Keycloak client role that admits login
does not replace API authorization. `resource_access` is nested JSON and is not
automatically flattened into role claims. To use `RequireRole`, configure a protocol
mapper and `RoleClaimType` consistently and inspect a real token. Do not disable
issuer/audience validation to make authentication succeed.

## Tokens and logout

`IUserTokenProvider.GetUserTokenAsync(HttpContext, CancellationToken)` returns the
current request's access token, or `null` when it cannot obtain one.

| `TokenStoreType` | Behavior |
| --- | --- |
| `None` (`0`, default) | Reads the authentication handler's saved token; no automatic refresh. With `SaveTokens = false`, there is no saved token. |
| `Redis` (`1`) | Duende refreshes tokens and stores them in Redis. See [Redis configuration](redis-sentinel.md) for connections and Data Protection. |
| `InMemory` (`2`) | Duende refreshes tokens; storage is lost on process shutdown and is not shared by replicas. |

A failed managed refresh does not fall back to the stale cookie token. Handle `null`
and initiate a new login if appropriate. `ConfigureUserTokenManagement` receives
Duende 4 options.

The application owns login/logout actions. Use `ChallengeAsync("oidc")` for login.
`SignOutAsync("Cookies")` ends the local session; with managed tokens it also requests
refresh token revocation and clears storage. Additionally call
`SignOutAsync("oidc", properties)` to end the provider session. Use CSRF-protected
application actions for logout and a trusted local `RedirectUri`. The module does
not automatically expose public login/logout routes.

Cookie lifetime defaults to 30 days with sliding expiration (`CookieExpireInMinutes`).
Removing a Keycloak role or session does not delete an existing application cookie.
Immediate revocation needs a separate application session check/revocation mechanism;
this integration does not promise it.

Redis retains SHA-256 keys derived from `DOT_{Application.Name}_{Environment}_{userId}`,
Data Protection purpose `RedisTokenStore`, and string-valued JSON fields. Preserve
application name, environment, user claim, Redis DB and Data Protection keys to read
existing records. Storage remains per user/application, not per browser session:
logging out one session can clear tokens used by another session of that user.

## Restricting a Keycloak client

A role alone does not prevent login: Keycloak authentication flows must enforce it.
This example is for the separate consumer migration. Live Keycloak configuration
has not been validated by this library change.

Example: client `admin-web`, client role `access`, technical client scope
`admin-web-access-check`. The scope selects the client to check; the role grants access.

1. Create a server OIDC client with `Client authentication` and `Standard flow`.
   Set exact redirect URI `https://admin.example.com/signin-oidc`, post-logout URI
   `https://admin.example.com/signout-callback-oidc` and PKCE `S256`. Disable unused
   `Direct access grants`, `Implicit flow` and `Service accounts`.
2. Create client role `access` on `admin-web` and assign it to selected users or a
   group. Do not make it a default role for every realm user.
3. Assign `admin-web-access-check` as a **default client scope only on this client**;
   an optional scope would make this mandatory check avoidable.
4. Copy the Browser flow and select it in the client's `Authentication flow overrides`
   → `Browser Flow`. A REQUIRED authentication subflow contains the Cookie, forms
   and external-IdP alternatives. A following CONDITIONAL subflow denies users without
   the role. Cookie must not be a top-level alternative to this gate, or SSO can
   complete before the gate runs.
5. In the gate, use REQUIRED `Condition - client scope` (`admin-web-access-check`,
   not negated), REQUIRED `Condition - User Role` (`admin-web.access`, `Negate output`
   enabled), then REQUIRED `Deny Access`.
6. Add the same gate to each external IdP's `Post login flow`, retaining existing
   steps. Browser processing does not resume after successful external-IdP login.
   The default-client-scope condition limits this shared post-login flow to the
   intended client and preserves other applications' access.
7. Review other enabled entry paths: registration, reset credentials, device/CIBA
   and token exchange do not automatically inherit the Browser override. Keep
   unverified paths unavailable for this client, or apply and independently verify
   the same restriction there.

Sources: [conditions and denial](https://www.keycloak.org/docs/latest/server_admin/index.html#_authentication-flows),
[external IdP and Post login flow](https://www.keycloak.org/docs/26.7.5/server_admin/#_identity_broker),
[client-scope condition](https://www.keycloak.org/docs/26.5.7/server_admin/index.html#_authentication-flows).
Screen labels vary by installed version; consult its documentation before applying
changes. Client `Authorization` settings and putting a role in a token do not by
themselves deny login.

Complete acceptance before removing the old restriction:

| Scenario | Expected result |
| --- | --- |
| Fresh login with the role | Application cookie issued; intended pages accessible. |
| Fresh login without the role | Keycloak denies access; no application cookie. |
| Existing SSO from another client, no role | `admin-web` login denied. |
| Google/another IdP, including repeat login | Admission follows the client role, not external account existence. |
| Role removed, application cookie cleared, SSO retained | New login denied. |
| Another realm client | Its previous admission behavior unchanged. |
| Role removed while application cookie remains valid | Accepted remaining-access duration recorded; no immediate-revocation claim. |

## Consumer migration

| Version 12 | Version 13 |
| --- | --- |
| Package/namespace `Sitko.Core.Auth.IdentityServer` | `Sitko.Core.Auth.OIDC` |
| `AddOidcIdentityServer` / `AddJwtIdentityServer` | `AddOidcAuth` / `AddJwtAuth` |
| `OidcIdentityServerModule` / `OidcIdentityServerModuleOptions` | `OidcAuthModule` / `OidcAuthModuleOptions` |
| `JwtIdentityServerModule` / `JwtIdentityServerModuleOptions` | `JwtAuthModule` / `JwtAuthModuleOptions` |
| `IdentityServerModule<T>` / `IdentityServerAuthOptions` | `OidcProviderModule<T>` / `OidcProviderAuthOptions` |
| `IdentityServerAuthOptionsValidator<T>` | `OidcProviderAuthOptionsValidator<T>` |
| `Auth:IdentityServer:Oidc` / `Auth:IdentityServer:Jwt` | `Auth:OIDC:Client` / `Auth:OIDC:Jwt` |
| Namespace `.Auth.IdentityServer.Tokens` | `.Auth.OIDC.Tokens` |

`OidcAuthOptionsValidator`, `JwtAuthOptionsValidator`, `IUserTokenProvider`,
`OidcEvents` and `TokenStoreType` retain their short names in the new namespace.
No automatic package/type/section aliases are provided. Preserve a custom
`optionsKey` explicitly. Direct Duende consumers must follow the
[Duende 4 upgrade guide](https://docs.duendesoftware.com/accesstokenmanagement/upgrading/atm-v3-to-v4/):
use `IUserTokenManager`, typed tokens and `TokenResult`; custom `IUserTokenStore`
implementations require adaptation.

For Google migration, configure the Keycloak role and all admission flows first,
then replace the package, registration and configuration and run the acceptance
matrix. A nonempty former `Users` list enforced exact email matching; removing it
without a replacement broadens access. Assign roles to verified accounts rather than
copying email strings into arbitrary claims without checking account linking. The
new provider's `sub` can differ; migrate application user associations separately.
An existing Google cookie does not prove Keycloak admission: end old sessions or
change the cookie name and require new login when switching providers.

A Blazor Server circuit is not an HTTP request. Use `IUserTokenProvider` where an
up-to-date `HttpContext` exists. The module does not add circuit/authentication-state
refresh or browser token storage. Verify full-page reload after login/logout,
reconnect and session expiration in the actual application. Blazor WebAssembly needs
its own client-side OIDC integration; never send the server client secret to it.

Automated checks establish ASP.NET handler, PKCE, callback/UserInfo, JWT,
refresh/revoke and storage behavior within their test environments. They do not
establish current Keycloak configuration or consumer readiness. `Sitko.Core.Identity`
retains its separate contract and is outside this migration.
