using System;

namespace Sitko.Core.Email
{
    public abstract class FluentEmailModuleConfig : EmailModuleConfig
    {
        public string From { get; }

        protected FluentEmailModuleConfig(string from, string host, string scheme) : base(host, scheme)
        {
            if (string.IsNullOrEmpty(from))
            {
                throw new ArgumentException("Provide value for from address", nameof(from));
            }

            From = from;
        }
    }
}
