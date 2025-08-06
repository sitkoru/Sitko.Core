using FluentValidation;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Storage;

public abstract class StorageOptions : BaseModuleOptions
{
    protected StorageOptions() => Name = GetType().Name;

    [PublicAPI] public Uri? PublicUri { get; set; }

    [PublicAPI] public string? Prefix { get; set; }
    [PublicAPI] public string Name { get; set; }
    [PublicAPI] public bool IsDefault { get; set; }

    [PublicAPI] public bool PreserveOriginalFileName { get; set; }
}

public abstract class StorageOptionsValidator<TStorageOptions> : AbstractValidator<TStorageOptions>
    where TStorageOptions : StorageOptions
{
    public StorageOptionsValidator() => RuleFor(o => o.Name).NotEmpty()
        .WithMessage($"Storage {typeof(TStorageOptions)} name is empty");
}
