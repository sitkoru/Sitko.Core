using System;

namespace Sitko.Core.App.Helpers
{
    public static class FilesHelper
    {
        private static readonly string[] s_units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

        public static string HumanSize(long fileSize)
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

            return $"{Math.Round(size, 2):N}{s_units[unit]}";
        }
    }
}
