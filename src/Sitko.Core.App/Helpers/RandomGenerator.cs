using System;
using System.Text;

namespace Sitko.Core.App.Helpers
{
    public static class RandomGenerator
    {
#if !NET6_0
        private static readonly UniformRandom Rnd = new();
#else
        private static readonly Random Rnd = Random.Shared;
#endif
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
#if !NET6_0
    internal class UniformRandom
    {
        private static readonly System.Security.Cryptography.RNGCryptoServiceProvider Global = new();

        private readonly Random rnd;

        public UniformRandom()
        {
            var buffer = new byte[4];
            Global.GetBytes(buffer);
            rnd = new Random(BitConverter.ToInt32(buffer, 0));
        }

        public int Next() => rnd.Next();

        public int Next(int maxValue) => rnd.Next(maxValue);

        public int Next(int minValue, int maxValue) => rnd.Next(minValue, maxValue);

        public double NextDouble() => rnd.NextDouble();

        public double NextDouble(double minValue, double maxValue)
        {
            var r = rnd.NextDouble() * (maxValue - minValue);
            return minValue + r;
        }
    }
#endif
}
