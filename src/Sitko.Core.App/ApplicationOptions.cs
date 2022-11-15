namespace Sitko.Core.App;

public class ApplicationOptions
{
    public const string BaseConsoleLogFormat =
        "[{Timestamp:HH:mm:ss} {Level:u3} {SourceContext}]{NewLine}\t{Message:lj}{NewLine}{Exception}";

    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Environment { get; set; } = "";

    public string ConsoleLogFormat { get; set; } = BaseConsoleLogFormat;
    public bool? EnableConsoleLogging { get; set; }
}
