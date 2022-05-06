using System;

namespace Sitko.Core.Auth.IdentityServer.Tokens;

public class AutomaticTokenManagementOptions
{
    public string? Scheme { get; set; }
    public TimeSpan RefreshBeforeExpiration { get; set; } = TimeSpan.FromMinutes(1);
    public bool RevokeRefreshTokenOnSignout { get; set; } = true;
}
