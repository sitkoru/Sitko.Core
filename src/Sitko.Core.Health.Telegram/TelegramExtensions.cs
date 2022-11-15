namespace Sitko.Core.Health.Telegram;

public static class TelegramExtensions
{
    public static string TelegramRaw(string text) =>
        text
            .Replace("+", "\\+")
            .Replace("=", "\\=")
            .Replace("-", "\\-")
            .Replace("#", "\\#")
            .Replace(".", "\\.")
            .Replace("!", "\\!")
            .Replace(")", "\\)")
            .Replace(">", "\\>")
            .Replace("<", "\\<")
            .Replace("(", "\\(");
}

