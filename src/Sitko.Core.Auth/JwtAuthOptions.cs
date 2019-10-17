namespace Sitko.Core.Auth
{
    public class JwtAuthOptions : AuthOptions
    {
        public string JwtAudience { get; }

        public JwtAuthOptions(string oidcServerUrl, string jwtAudience) : base(oidcServerUrl)
        {
            JwtAudience = jwtAudience;
        }
    }
}
