using System.Net;
using System.Net.Sockets;

namespace Sitko.Core.App.Helpers;

public static class DockerHelper
{
    public static bool IsRunningInDocker() =>
        bool.TryParse(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), out var inContainer) &&
        inContainer;

    public static string? GetContainerAddress()
    {
        if (IsRunningInDocker())
        {
            var name = Dns.GetHostName(); // get container id
            return Dns.GetHostEntry(name).AddressList
                .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?.ToString();
        }

        return null;
    }
}

