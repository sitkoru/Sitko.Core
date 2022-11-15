using FluentValidation;
using JetBrains.Annotations;
using Sitko.Core.App;

namespace Sitko.Core.Repository.Remote;

//add interface
public class HttpRepositoryTransportOptions : BaseModuleOptions
{
    public Func<IHttpClientFactory, HttpClient>? HttpClientFactory { get; set; }
    [PublicAPI] public Uri RepositoryControllerApiRoute { get; set; } = null!;
}

public class RepositoryOptionsValidator : AbstractValidator<HttpRepositoryTransportOptions>
{
    public RepositoryOptionsValidator() => RuleFor(o => o.RepositoryControllerApiRoute).NotEmpty()
        .WithMessage($"Repository {typeof(HttpRepositoryTransportOptions)} api route is empty");
}

