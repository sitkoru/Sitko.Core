using Serilog.Core;

namespace Sitko.Core.App.Logging
{
    public class LogLevelSwitcher
    {
        public readonly LoggingLevelSwitch Switch = new LoggingLevelSwitch();
    }
}
