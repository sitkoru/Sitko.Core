using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace Sitko.Core.Auth.Google
{
    public class GoogleAuthModuleOptions : AuthOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public List<string> Users { get; set; } = new();
        public TimeSpan CookieExpire { get; set; } = TimeSpan.FromDays(30);
        public string SignInScheme { get; set; } = "Cookies";
        public string ChallengeScheme { get; set; } = "Google";
        public Action<CookieBuilder>? ConfigureCookie { get; set; }

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(ClientId))
                {
                    return (false, new[] {"ClientId can't be empty"});
                }

                if (string.IsNullOrEmpty(ClientSecret))
                {
                    return (false, new[] {"ClientSecret can't be empty"});
                }

                if (string.IsNullOrEmpty(SignInScheme))
                {
                    return (false, new[] {"SignInScheme can't be empty"});
                }

                if (string.IsNullOrEmpty(ChallengeScheme))
                {
                    return (false, new[] {"ChallengeScheme can't be empty"});
                }
            }

            return result;
        }
    }
}
