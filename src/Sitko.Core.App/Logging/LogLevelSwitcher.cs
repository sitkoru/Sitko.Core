using Serilog.Core;

namespace Sitko.Core.App.Logging
{
    public class LogLevelSwitcher
    {
        public LoggingLevelSwitch Switch { get; } = new();
    }
}
