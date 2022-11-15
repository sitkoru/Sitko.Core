using System.Text;

namespace Sitko.Core.App.Helpers
{
    public static class RandomGenerator
    {
        private static readonly Random Rnd = Random.Shared;
        private const string ValidSymbols = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public static string GetRandomString(int length)
        {
            var res = new StringBuilder();

            while (length-- > 0)
            {
                var num = Rnd.Next(0, ValidSymbols.Length);
                res.Append(ValidSymbols[(int)(num % (uint)ValidSymbols.Length)]);
            }

            return res.ToString();
        }
    }
}
