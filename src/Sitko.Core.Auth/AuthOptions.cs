using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;

namespace Sitko.Core.Auth
{
    public abstract class AuthOptions
    {
        public readonly Dictionary<string, AuthorizationPolicy>
            Policies = new Dictionary<string, AuthorizationPolicy>();

        public string ForcePolicy { get; set; }
        public readonly List<string> IgnoreUrls = new List<string> {"/health", "/metrics"};
    }
}
