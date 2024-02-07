using Microsoft.Extensions.Logging;

namespace Sitko.Core.App.Logging;

internal interface IBootLogger<out T> : ILogger<T>
{
}
