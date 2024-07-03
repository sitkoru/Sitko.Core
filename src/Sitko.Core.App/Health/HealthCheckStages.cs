namespace Sitko.Core.App.Health;

public class HealthCheckStages
{
    private const string SkipPrefix = "skip";
    public const string Startup = "startup";
    public const string Liveness = "liveness";
    public const string Readiness = "ready";
    private const string All = "all";
    public static string[] GetSkipTags(params string[] stages) => stages.Select(GetSkipTag).ToArray();
    public static string GetSkipTag(string stage) => $"{SkipPrefix}{stage}";

    public static string GetSkipAllTag() => GetSkipTag(All);
    public static string[] GetSkipAllTags() => GetSkipTags(All);
}
