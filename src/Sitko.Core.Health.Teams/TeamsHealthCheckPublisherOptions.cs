namespace Sitko.Core.Health.Teams
{
    public class TeamsHealthCheckPublisherOptions
    {
        public string WebHookUrl { get; set; } = string.Empty;
        public string UnHealthyColor { get; set; } = "#c74f4f";
        public string HealthyColor { get; set; } = "#91c337";
        public string DegradedColor { get; set; } = "#ffc107";
    }
}
