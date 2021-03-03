using System;
using System.IO;

namespace Sitko.Core.Storage
{
    internal static class Helpers
    {
        private static readonly string[] _units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

        internal static string HumanSize(long fileSize)
        {
            if (fileSize < 1)
            {
                return "-";
            }

            var unit = 0;

            double size = fileSize;
            while (size >= 1024)
            {
                size /= 1024;
                unit++;
            }

            return $"{Math.Round(size, 2):N}{_units[unit]}";
        }

        internal static string? PreparePath(string? path)
        {
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

                filePath = Helpers.PreparePath(Path.GetRelativePath(prefix, filePath))!;
            }

            return filePath;
        }
    }
}
