using FluentValidation;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryOptions : BaseModuleOptions
{
    protected RemoteRepositoryOptions() => Name = GetType().Name;

    [PublicAPI] public string RepositoryControllerApiRoute { get; set; }
    [PublicAPI] public string Name { get; set; }
}
public abstract class RepositoryOptionsValidator<TRepositoryOptions> : AbstractValidator<TRepositoryOptions>
    where TRepositoryOptions : RemoteRepositoryOptions
{
    public RepositoryOptionsValidator() => RuleFor(o => o.Name).NotEmpty()
        .WithMessage($"Repository {typeof(TRepositoryOptions)} name is empty");
}
