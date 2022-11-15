namespace Sitko.Core.Storage.Internal;

public static class Helpers
{
    public static string? PreparePath(string? path)
    {
        if (path?.StartsWith('/') == true)
        {
            path = path.Substring(1);
        }

        return path?.Replace("\\", "/").Replace("//", "/");
    }

    public static string GetPathWithoutPrefix(string? prefix, string filePath)
    {
        if (!string.IsNullOrEmpty(prefix))
        {
            if (filePath.StartsWith("/", StringComparison.InvariantCulture))
            {
                prefix = "/" + prefix;
            }

            filePath = PreparePath(Path.GetRelativePath(prefix, filePath))!;
        }

        return filePath;
    }
}

