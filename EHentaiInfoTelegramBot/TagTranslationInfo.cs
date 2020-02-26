using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentCache;
using Microsoft.Extensions.Logging;

namespace EHentaiInfoTelegramBot
{
    public interface ITagTranslationInfo
    {
        Task<(bool, string)> GetTranslationAsync(string row, string tag = null);
    }

    public class TagTranslationInfo : ITagTranslationInfo
    {
        private const string DatabaseUrl = "https://raw.githubusercontent.com/EhTagTranslation/Database/master/database/";
        private readonly ILogger<TagTranslationInfo> _logger;

        public TagTranslationInfo(ICache cache, ILogger<TagTranslationInfo> logger)
        {
            _logger = logger;

            HttpClient = new HttpClient();
            CachedHttpClient = new Cache<HttpClient>(HttpClient, cache);
            MarkdownPicReplaceRegex = new Regex(@"!\[.+?\]\(.+?\)", RegexOptions.Compiled);
        }

        private HttpClient HttpClient { get; }
        private Cache<HttpClient> CachedHttpClient { get; }
        private Regex MarkdownPicReplaceRegex { get; }

        public async Task<(bool, string)> GetTranslationAsync(string row, string tag = null)
        {
            string url, target;
            if (tag == null)
            {
                url = $"{DatabaseUrl}rows.md";
                target = row;
            }
            else
            {
                url = $"{DatabaseUrl}{row}.md";
                target = tag;
            }

            string markdown;
            try
            {
                markdown = await CachedHttpClient.Method(t => t.GetStringAsync(url))
                    .ExpireAfter(TimeSpan.FromDays(1))
                    .GetValueAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Get translation failed");
                return (false, target.Replace("_", " "));
            }

            var regex = new Regex($@"\| {target.Replace("_", " ")} \| (?<translation>.+?) \|");
            var match = regex.Match(markdown);
            return (match.Success, match.Success
                ? MarkdownPicReplaceRegex.Replace(match.Groups["translation"].Value, "").Replace(@"\|", "|")
                : target.Replace("_", " "));
        }
    }
}