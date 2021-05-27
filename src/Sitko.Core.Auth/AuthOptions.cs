using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Sitko.Core.App;

namespace Sitko.Core.Auth
{
    public abstract class AuthOptions : BaseModuleOptions
    {
        public readonly Dictionary<string, AuthorizationPolicy> Policies = new();

        public string? ForcePolicy { get; set; }
        public readonly List<string> IgnoreUrls = new() {"/health", "/metrics"};
    }

    public abstract class AuthOptionsValidator<TOptions> : AbstractValidator<TOptions> where TOptions : AuthOptions
    {
    }
}
