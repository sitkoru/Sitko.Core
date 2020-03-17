namespace Sitko.Core.Auth.Basic
{
    public class BasicAuthOptions : AuthOptions
    {
        public string Realm { get; } = "Basic Auth";
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
