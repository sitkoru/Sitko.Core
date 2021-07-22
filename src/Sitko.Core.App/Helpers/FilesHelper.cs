using System;
using System.Globalization;

namespace Sitko.Core.App.Helpers
{
    public static class FilesHelper
    {
        private static readonly string[] Units = {"bytes", "KB", "MB", "GB", "TB", "PB"};

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

            var sizeStr = Math.Round(size, 2).ToString("N2", CultureInfo.InvariantCulture);
            return $"{sizeStr} {Units[unit]}";
        }
    }
}
