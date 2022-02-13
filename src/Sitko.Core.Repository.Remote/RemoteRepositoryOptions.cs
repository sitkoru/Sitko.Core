using FluentValidation;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

public class RemoteRepositoryOptions : RepositoriesModuleOptions<IRemoteRepository>
{
    public Func<HttpClient>? HttpClientFactory { get; set; }
    [PublicAPI] public Uri RepositoryControllerApiRoute { get; set; }
    [PublicAPI] public string Name { get; set; } = string.Empty;
}
public abstract class RepositoryOptionsValidator<TRepositoryOptions> : AbstractValidator<TRepositoryOptions>
    where TRepositoryOptions : RemoteRepositoryOptions
{
    public RepositoryOptionsValidator() => RuleFor(o => o.RepositoryControllerApiRoute).NotEmpty()
        .WithMessage($"Repository {typeof(TRepositoryOptions)} api route is empty");
}
