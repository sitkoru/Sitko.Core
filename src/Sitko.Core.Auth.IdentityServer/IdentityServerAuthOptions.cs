namespace Sitko.Core.Auth.IdentityServer
{
    public abstract class IdentityServerAuthOptions : AuthOptions
    {
        public string OidcServerUrl { get; set; } = "https://localhost";
        public bool RequireHttps { get; set; }
    }
}
