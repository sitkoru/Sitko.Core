namespace Sitko.Core.Auth.Basic
{
    public class BasicAuthOptions : AuthOptions
    {
        public BasicAuthOptions(string realm, string username, string password)
        {
            Realm = realm;
            Username = username;
            Password = password;
        }

        public string Realm { get; }
        public string Username { get; }
        public string Password { get; }
    }
}
