using System;

namespace Sitko.Core.SonyFlake
{
    public class SonyFlakeModuleConfig
    {
        public Uri SonyflakeUri { get; }

        public SonyFlakeModuleConfig(Uri sonyflakeUri)
        {
            SonyflakeUri = sonyflakeUri;
        }
    }
}
