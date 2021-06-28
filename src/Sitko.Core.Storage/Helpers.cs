using System.IO;

namespace Sitko.Core.Storage
{
    internal static class Helpers
    {
        internal static string? PreparePath(string? path)
        {
            if (path?.StartsWith('/') == true)
            {
                path = path.Substring(1);
            }

            return path?.Replace("\\", "/").Replace("//", "/");
        }

        internal static string GetPathWithoutPrefix(string? prefix, string filePath)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                if (filePath.StartsWith("/"))
                {
                    prefix = "/" + prefix;
                }

                filePath = PreparePath(Path.GetRelativePath(prefix, filePath))!;
            }

            return filePath;
        }
    }
}
