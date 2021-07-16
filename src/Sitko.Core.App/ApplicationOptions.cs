namespace Sitko.Core.App
{
    public class ApplicationOptions
    {
        public const string BaseConsoleLogFormat =
            "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine}\t{Message:lj}{NewLine}{Exception}";

        public string Name { get; set; } = "App";
        public string Version { get; set; } = "Dev";

        public string ConsoleLogFormat { get; set; } = BaseConsoleLogFormat;
    }
}
