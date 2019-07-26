namespace EHentaiInfoTelegramBot
{
    public static class Utils
    {
        public static string MarkdownEscape(string str)
        {
            return str.Replace("[", @"\[");
        }
    }
}