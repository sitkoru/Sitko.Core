using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Sitko.Core.App;

namespace Sitko.Core.Auth
{
    using Newtonsoft.Json;

    public abstract class AuthOptions : BaseModuleOptions
    {
        [JsonIgnore] public Dictionary<string, AuthorizationPolicy> Policies { get; } = new();

        public string? ForcePolicy { get; set; }
        public List<string> IgnoreUrls { get; set; } = new() {"/health", "/metrics"};
    }

    public abstract class AuthOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : AuthOptions
    {
    }
}
