namespace Sitko.Core.Email
{
    public abstract class FluentEmailModuleConfig : EmailModuleConfig
    {
        public string From { get; set; } = "admin@localhost";
    }
}
