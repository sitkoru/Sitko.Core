using idunno.Authentication.Basic;

namespace Sitko.Core.Auth.Basic
{
    public class BasicAuthModuleOptions : AuthOptions
    {
        public string Realm { get; set; } = "Basic Auth";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public override bool RequiresCookie => false;
        public override string SignInScheme => BasicAuthenticationDefaults.AuthenticationScheme;
        public override string ChallengeScheme => BasicAuthenticationDefaults.AuthenticationScheme;
    }
}
