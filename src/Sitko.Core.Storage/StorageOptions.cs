using System;
using FluentValidation;
using Sitko.Core.App;

namespace Sitko.Core.Storage
{
    using JetBrains.Annotations;

    public abstract class StorageOptions : BaseModuleOptions
    {
        protected StorageOptions() => Name = GetType().Name;

        [PublicAPI] public Uri? PublicUri { get; set; }

        [PublicAPI] public string? Prefix { get; set; }
        [PublicAPI] public string Name { get; set; }
        [PublicAPI] public bool IsDefault { get; set; }
    }

    public abstract class StorageOptionsValidator<TStorageOptions> : AbstractValidator<TStorageOptions>
        where TStorageOptions : StorageOptions
    {
        public StorageOptionsValidator() => RuleFor(o => o.Name).NotEmpty()
            .WithMessage($"Storage {typeof(TStorageOptions)} name is empty");
    }
}
