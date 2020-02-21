namespace Sitko.Core.Auth.IdentityServer
{
    public abstract class IdentityServerAuthOptions : AuthOptions
    {
        protected IdentityServerAuthOptions(string oidcServerUrl)
        {
            OidcServerUrl = oidcServerUrl;
        }

        public string OidcServerUrl { get; set; }
        public bool RequireHttps { get; set; }
    }
}
