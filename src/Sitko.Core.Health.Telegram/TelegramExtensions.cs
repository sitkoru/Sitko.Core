namespace Sitko.Core.Health.Telegram
{
    public static class TelegramExtensions
    {
        public static string TelegramRaw(string text)
        {
            return text
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
    }
}
