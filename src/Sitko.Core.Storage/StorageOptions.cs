using System;
using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Storage
{
    public abstract class StorageOptions : BaseModuleOptions
    {
        public StorageOptions() => Name = GetType().Name;

        public Uri? PublicUri { get; set; }

        public string? Prefix { get; set; }

        public string Name { get; set; }
        public bool IsDefault { get; set; }
    }

    public abstract class StorageOptionsValidator<TStorageOptions> : AbstractValidator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        public StorageOptionsValidator() => RuleFor(o => o.Name).NotEmpty()
            .WithMessage($"Storage {typeof(TStorageOptions)} name is empty");
    }
}
