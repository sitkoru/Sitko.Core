using Serilog.Core;

namespace Sitko.Core.Logging
{
    public class LogLevelSwitcher
    {
        public readonly LoggingLevelSwitch Switch = new LoggingLevelSwitch();
    }
}
