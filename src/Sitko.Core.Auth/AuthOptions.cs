using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Sitko.Core.App;

namespace Sitko.Core.Auth
{
    public abstract class AuthOptions : BaseModuleConfig
    {
        public readonly Dictionary<string, AuthorizationPolicy>
            Policies = new Dictionary<string, AuthorizationPolicy>();

        public string? ForcePolicy { get; set; }
        public readonly List<string> IgnoreUrls = new List<string> {"/health", "/metrics"};
    }
}
