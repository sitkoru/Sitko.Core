using System.Diagnostics;

namespace Sitko.Core.Tasks.Tracing;

public static class TracingHelper
{
    public static readonly ActivitySource ActivitySource = new("Sitko.Core.Tasks");
}
