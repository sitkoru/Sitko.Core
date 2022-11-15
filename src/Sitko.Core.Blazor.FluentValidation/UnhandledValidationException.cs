namespace Sitko.Core.Blazor.FluentValidation;

/// <summary>
///     Describes an unhandled exception which occurs during validation.
/// </summary>
internal sealed class UnhandledValidationException : Exception
{
    /// <summary>
    ///     Constructs an instance of <see cref="UnhandledValidationException" />
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public UnhandledValidationException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

