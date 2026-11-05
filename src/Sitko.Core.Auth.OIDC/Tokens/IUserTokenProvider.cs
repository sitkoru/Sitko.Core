using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Auth.OIDC.Tokens;

public interface IUserTokenProvider
{
    Task<string?> GetUserTokenAsync(HttpContext httpContext, CancellationToken cancellationToken = default);
}
