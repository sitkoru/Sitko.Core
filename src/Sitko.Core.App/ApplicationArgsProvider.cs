namespace Sitko.Core.App;

internal class ApplicationArgsProvider : IApplicationArgsProvider
{
    public ApplicationArgsProvider(string[] args) => Args = args;

    public string[] Args { get; }
}
