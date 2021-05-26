using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions : BaseModuleConfig
    {
        public Uri? PublicUri { get; set; }

        public string? Prefix { get; set; }

        public abstract string Name { get; set; }

        public override (bool isSuccess, IEnumerable<string> errors) CheckConfig()
        {
            var result = base.CheckConfig();
            if (result.isSuccess)
            {
                if (string.IsNullOrEmpty(Name))
                {
                    return (false, new[] {"Storage name is empty"});
                }
            }

            return result;
        }
    }
}
