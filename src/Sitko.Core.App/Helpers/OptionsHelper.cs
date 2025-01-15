using System.Reflection;
using FluentValidation;
using IL.FluentValidation.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Sitko.Core.App.Helpers;

public static class OptionsHelper
{
    public static void AddOptions<TOptions, TOptionsValidator>(IApplicationContext applicationContext,
        IServiceCollection services,
        string optionKey, Action<IApplicationContext, TOptions>? configureOptions = null)
        where TOptions : class, new()
        where TOptionsValidator : IValidator<TOptions>, new() => AddOptions(applicationContext, services, [optionKey],
        configureOptions, typeof(TOptionsValidator));

    public static void AddOptions<TOptions>(IApplicationContext applicationContext, IServiceCollection services,
        string[] optionKeys, Action<IApplicationContext, TOptions>? configureOptions = null, Type? validatorType = null)
        where TOptions : class, new()
    {
        var builder = services.AddOptions<TOptions>();
        foreach (var optionsKey in optionKeys)
        {
            builder = builder.Bind(applicationContext.Configuration.GetSection(optionsKey));
        }

        builder = builder.PostConfigure(
            options =>
            {
                if (options is BaseModuleOptions moduleOptions)
                {
                    moduleOptions.Configure(applicationContext);
                }

                configureOptions?.Invoke(applicationContext, options);
            });

        if (validatorType is not null)
        {
            builder.Services.AddTransient(validatorType);
            builder.FluentValidate()
                .With(provider => (IValidator<TOptions>)provider.GetRequiredService(validatorType));
        }
    }

    public static TOptions GetOptions<TOptions, TOptionsValidator>(IApplicationContext applicationContext,
        string optionKey,
        Action<IApplicationContext, TOptions>? configureOptions = null, bool validateOptions = false)
        where TOptions : class, new() where TOptionsValidator : IValidator<TOptions>, new() => GetOptions(
        applicationContext, [optionKey], configureOptions,
        typeof(TOptionsValidator), validateOptions);

    public static TOptions GetOptions<TOptions>(IApplicationContext applicationContext,
        string[] optionKeys,
        Action<IApplicationContext, TOptions>? configureOptions = null, Type? validatorType = null,
        bool validateOptions = false)
        where TOptions : class, new()
    {
        var options = Activator.CreateInstance<TOptions>();
        foreach (var optionsKey in optionKeys)
        {
            applicationContext.Configuration.Bind(optionsKey, options);
        }

        if (options is BaseModuleOptions moduleOptions)
        {
            moduleOptions.Configure(applicationContext);
        }

        configureOptions?.Invoke(applicationContext, options);

        if (validatorType is not null && validateOptions)
        {
            ValidateOptions(applicationContext, options, validatorType);
        }

        return options;
    }

    public static void ValidateOptions<TOptions>(IApplicationContext applicationContext, TOptions options,
        Type validatorType) where TOptions : notnull
    {
        try
        {
            if (Activator.CreateInstance(validatorType) is IValidator<TOptions> validator)
            {
                var result = validator.Validate(options);
                if (!result.IsValid)
                {
                    throw new OptionsValidationException(options.GetType().Name, options.GetType(),
                        result.Errors.Select(e => $"{options.GetType().Name}: {e}"));
                }
            }
        }
        catch (TargetInvocationException exception)
        {
            applicationContext.Logger.LogDebug(exception, "Can't create validator {ValidatorType}: {ErrorText}",
                validatorType, exception.ToString());
        }
    }
}
