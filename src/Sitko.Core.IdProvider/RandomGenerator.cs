using System.Security.Cryptography;
using System.Text;

namespace Sitko.Core.IdProvider;

public static class RandomGenerator
{
    private const string ValidSymbols = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
    private static readonly UniformRandom Rnd = new();

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

internal sealed class UniformRandom
{
    private readonly Random rnd;

    public UniformRandom()
    {
        var buffer = RandomNumberGenerator.GetBytes(4);
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

