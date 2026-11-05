# Local accounts with ASP.NET Core Identity

[Русский](../ru/identity.md) · [All topics](README.md)

`Sitko.Core.Identity` connects ASP.NET Core Identity with EF stores for users
and roles. It is an independent feature for local accounts.
For an external sign-in provider, use [OIDC/JWT](oidc-authentication.md).
`Sitko.Core.Auth` contains shared cookie, policy and Data Protection settings
used by Identity and OIDC; it is not a complete standalone sign-in provider.

After [registering the web application](application.md), call
`AddIdentity<TUser, TRole, TPk, TDbContext>(...)` from `Sitko.Core.Identity`.
Supply your `IdentityUser<TPk>`, `IdentityRole<TPk>` and
`IdentityDbContext<TUser,TRole,TPk>`. The application separately registers its
database context, migrations and connection, for example through the
[PostgreSQL module](data.md).

The `Identity` section:

| Setting | Default / purpose |
| --- | --- |
| `RequireConfirmedAccount` | `true`: sign-in requires a confirmed account. |
| `AddDefaultUi` | `false`; `true` registers the standard Identity UI and Razor Pages. |
| `LoginPath` | Options field defaulting to `/Identity/Account/Login`; see the application limitation below. |
| `LogoutPath` | Options field defaulting to `/Identity/Account/Logout`; see the application limitation below. |
| `Policies`, `ForcePolicy` | Named policies and the selected fallback policy. |
| `EnableRedisDataProtection` | Store keys in Redis; see [Redis](redis-sentinel.md) for connection and durability requirements. |

The `Identity:LoginPath` and `Identity:LogoutPath` fields currently do not override
Identity cookies: the module returns `RequiresAuthentication=false`, so the shared
`AuthModule.ConfigureCookieOptions` branch is not called. Configure paths through
standard ASP.NET Core Identity cookie options in your application when needed.

After Build, call `MapSitkoCore()`: with `AddDefaultUi=true`, the Identity module
maps Razor Pages. Without the UI, the application implements sign-in, sign-out,
confirmation and password recovery pages/actions. Registering the module does not
create users or confirm their addresses.

Verify account creation in an isolated database, rejection of an unconfirmed
account, successful sign-in after confirmation, sign-out and policy-based access.
A cookie does not replace role and application permission checks.
Confirmation email delivery and access configuration are application responsibilities.

Sources: [registration](../../src/Sitko.Core.Identity/ApplicationExtensions.cs),
[module and options](../../src/Sitko.Core.Identity/IdentityModule.cs),
[shared Auth base](../../src/Sitko.Core.Auth/AuthModule.cs).
