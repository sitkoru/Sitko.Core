using Serilog.Core;

namespace Sitko.Core.Infrastructure.Logging
{
    public class LogLevelSwitcher
    {
        public readonly LoggingLevelSwitch Switch = new LoggingLevelSwitch();
    }
}
