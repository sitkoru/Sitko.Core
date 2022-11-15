﻿using System.Text.Json.Serialization;

namespace Sitko.Core.App.Localization;

public class JsonLocalizationModule : LocalizationModule<JsonLocalizationModuleOptions, JsonStringLocalizerFactory>
{
    public override string OptionsKey => "Localization:Json";
}

public class JsonLocalizationModuleOptions : LocalizationModuleOptions
{
    private readonly HashSet<Type> defaultResources = new();

    [JsonIgnore] public Type[] DefaultResources => defaultResources.ToArray();

    public int CacheTimeInMinutes { get; set; } = 60;

    public JsonLocalizationModuleOptions AddDefaultResource<T>() => AddDefaultResource(typeof(T));

    public JsonLocalizationModuleOptions AddDefaultResource(Type resourceType)
    {
        defaultResources.Add(resourceType);
        return this;
    }
}

