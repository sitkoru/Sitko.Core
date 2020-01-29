using System;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Email
{
    public abstract class EmailModuleConfig
    {
        protected EmailModuleConfig(string host, string scheme)
        {
            if (string.IsNullOrEmpty(host))
            {
                throw new ArgumentException("Provide value for host uri to generate absolute urls", nameof(host));
            }

            Host = new HostString(host);

            if (string.IsNullOrEmpty(scheme))
            {
                throw new ArgumentException("Provide value for uri scheme to generate absolute urls", nameof(scheme));
            }

            Scheme = scheme;
        }

        public HostString Host { get; }
        public string Scheme { get; }
    }
}
