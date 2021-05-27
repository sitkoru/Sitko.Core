using System;
using System.Collections.Generic;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sitko.Core.App;
using Sitko.Core.Storage.Cache;
using Sitko.Core.Storage.Metadata;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions : BaseModuleOptions
    {
        public Uri? PublicUri { get; set; }

        public string? Prefix { get; set; }

        public abstract string Name { get; set; }
    }

    public abstract class StorageOptionsValidator<TStorageOptions> : AbstractValidator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        public StorageOptionsValidator()
        {
            RuleFor(o => o.Name).NotEmpty().WithMessage($"Storage {typeof(TStorageOptions)} name is empty");
        }
    }
}
