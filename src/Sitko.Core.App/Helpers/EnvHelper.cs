﻿using Microsoft.Extensions.Hosting;

namespace Sitko.Core.App.Helpers;

public static class EnvHelper
{
    private static string DotNetEnvironmentVariable { get; } = $"DOTNET_{HostDefaults.EnvironmentKey}";
    private static string AspNetEnvironmentVariable { get; } = $"ASPNETCORE_{HostDefaults.EnvironmentKey}";

    public static string GetEnvironmentName()
    {
        var envVariables = Environment.GetEnvironmentVariables();
        string? envKey = null;
        foreach (var envVariablesKey in envVariables.Keys)
        {
            var envVariablesKeyStr = envVariablesKey.ToString();
            if (string.Equals(DotNetEnvironmentVariable, envVariablesKeyStr,
                    StringComparison.OrdinalIgnoreCase))
            {
                envKey = envVariablesKeyStr;
                break;
            }

            if (string.Equals(AspNetEnvironmentVariable, envVariablesKeyStr,
                    StringComparison.OrdinalIgnoreCase))
            {
                envKey = envVariablesKeyStr;
                break;
            }
        }

        if (envKey is null)
        {
            return Environments.Production;
        }

        var envName = (string?)envVariables[envKey] ?? "";
        return envName.ToLowerInvariant() switch
        {
            "development" => Environments.Development,
            "production" => Environments.Production,
            "staging" => Environments.Staging,
            _ => envName
        };
    }
}

