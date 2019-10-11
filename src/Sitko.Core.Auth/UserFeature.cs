namespace Sitko.Core.Auth
{
    public class UserFeature
    {
        public User User { get; }

        public UserFeature(User user)
        {
            User = user;
        }
    }
}