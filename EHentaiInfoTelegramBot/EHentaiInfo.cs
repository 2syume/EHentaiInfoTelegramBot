using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EHentaiInfoTelegramBot.Model;
using FluentCache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EHentaiInfoTelegramBot
{
    public class EHentaiInfo : IHentaiInfo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EHentaiInfo> _logger;
        private readonly ITagTranslationInfo _tagTranslationInfo;

        public EHentaiInfo(IConfiguration configuration, ILogger<EHentaiInfo> logger, ICache cache,
            ITagTranslationInfo tagTranslationInfo)
        {
            _configuration = configuration;
            _logger = logger;
            _tagTranslationInfo = tagTranslationInfo;

            if (!CheckCookie()) return;
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri("https://exhentai.org"),
                new Cookie("ipb_member_id", _configuration["ipb_member_id"], "/", "exhentai.org"));
            cookieContainer.Add(new Uri("https://exhentai.org"),
                new Cookie("ipb_pass_hash", _configuration["ipb_pass_hash"], "/", "exhentai.org"));
            cookieContainer.Add(new Uri("https://e-hentai.org"),
                new Cookie("ipb_member_id", _configuration["ipb_member_id"], "/", "e-hentai.org"));
            cookieContainer.Add(new Uri("https://e-hentai.org"),
                new Cookie("ipb_pass_hash", _configuration["ipb_pass_hash"], "/", "e-hentai.org"));
            var handler = new HttpClientHandler {CookieContainer = cookieContainer};
            HttpClient = new HttpClient(handler);
            CachedHttpClient = new Cache<HttpClient>(HttpClient, cache);
            TitleRegex = new Regex(@"(?<=<h1 id=\""gj\"">).*?(?=</h1>)", RegexOptions.Compiled);
            TagRowRegex = new Regex(@"<tr><td class=""tc"">(?<name>.+?):<\/td>(?<content>.+?)<\/tr>",
                RegexOptions.Compiled);
            TagRegex = new Regex(@"<div id=""td_([\w\d]+:)?(?<tag>.+?)""", RegexOptions.Compiled);
            CoverRegex = new Regex(@"<div id=""gd1"">.*?url\((?<cover>.+?)\)", RegexOptions.Compiled);
            UrlRegex = new Regex(@"https?://e[x\-]hentai.org/g/(?<id>[\d\w]+?/[\d\w]+)", RegexOptions.Compiled);
        }

        private HttpClient HttpClient { get; }
        private Cache<HttpClient> CachedHttpClient { get; }
        private Regex TitleRegex { get; }
        private Regex TagRowRegex { get; }
        private Regex TagRegex { get; }
        private Regex CoverRegex { get; }
        public Regex UrlRegex { get; }

        public async Task<IHentaiInfoModel> GetInfoAsync(string url)
        {
            var html = await CachedHttpClient.Method(t => t.GetStringAsync(url))
                .ExpireAfter(TimeSpan.FromDays(1))
                .GetValueAsync();

            var model = new EHentaiInfoModel
            {
                Title = TitleRegex.Match(html).Value,
                Id = UrlRegex.Match(url).Groups["id"].Value,
                Cover = await GetCoverAsync(html)
            };
            foreach (var pair in await ExtractTagsAsync(html)) model.Tags.Add(pair);

            return model;
        }

        private bool CheckCookie()
        {
            if (string.IsNullOrEmpty(_configuration["ipb_member_id"]) ||
                string.IsNullOrEmpty(_configuration["ipb_pass_hash"]))
            {
                _logger.LogCritical(
                    "Cannot find the ehentai cookies. Please put your ehentai cookies in the json file.");
                return false;
            }

            return true;
        }

        private async Task<IDictionary<string, IList<string>>> ExtractTagsAsync(string html)
        {
            var tags = new Dictionary<string, IList<string>>();
            foreach (Match match in TagRowRegex.Matches(html))
            {
                var name = match.Groups["name"].Value;
                var tag = (await Task.WhenAll(TagRegex.Matches(match.Groups["content"].Value)
                        .Select(async t => await _tagTranslationInfo.GetTranslationAsync(name, t.Groups["tag"].Value))))
                    .Select(t => t.Item2).ToList();
                tags.Add((await _tagTranslationInfo.GetTranslationAsync(name)).Item2, tag);
            }

            return tags;
        }

        private async Task<MemoryStream> GetCoverAsync(string html)
        {
            var coverUrl = CoverRegex.Match(html).Groups["cover"].Value;
            var ms = new MemoryStream();
            using (var webStream = await HttpClient.GetStreamAsync(coverUrl))
            {
                await webStream.CopyToAsync(ms);
            }

            ms.Seek(0, SeekOrigin.Begin);

            return ms;
        }
    }
}