namespace Sitko.Core.Auth
{
    public class User
    {
        public User(int id, string[] userFlags)
        {
            Id = id;
            UserFlags = userFlags;
        }

        public int Id { get; }
        public string[] UserFlags { get; }
    }
}
