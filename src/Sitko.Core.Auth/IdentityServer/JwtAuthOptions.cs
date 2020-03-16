namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtAuthOptions : IdentityServerAuthOptions
    {
        public string JwtAudience { get; set; } = string.Empty;
    }
}
