using System;
using System.Collections.Generic;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;

namespace Sitko.Core.Auth.Google
{
    public class GoogleAuthModuleOptions : AuthOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();

        public int CookieExpireInMinutes { get; set; } = 30 * 24 * 60;
        public string SignInScheme { get; set; } = "Cookies";
        public string ChallengeScheme { get; set; } = "Google";
        [JsonIgnore] public Action<CookieBuilder>? ConfigureCookie { get; set; }
    }

    public class GoogleAuthModuleOptionsValidator : AbstractValidator<GoogleAuthModuleOptions>
    {
        public GoogleAuthModuleOptionsValidator()
        {
            RuleFor(o => o.ClientId).NotEmpty().WithMessage("ClientId can't be empty");
            RuleFor(o => o.ClientSecret).NotEmpty().WithMessage("ClientSecret can't be empty");
            RuleFor(o => o.SignInScheme).NotEmpty().WithMessage("SignInScheme can't be empty");
            RuleFor(o => o.ChallengeScheme).NotEmpty().WithMessage("ChallengeScheme can't be empty");
        }
    }
}
