namespace Sitko.Core.Auth.IdentityServer
{
    public class JwtAuthOptions : IdentityServerAuthOptions
    {
        public string JwtAudience { get; }

        public JwtAuthOptions(string oidcServerUrl, string jwtAudience) : base(oidcServerUrl)
        {
            JwtAudience = jwtAudience;
        }
    }
}
