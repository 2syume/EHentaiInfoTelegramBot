using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using EHentaiInfoTelegramBot.Model;
using FluentCache;
using Microsoft.Extensions.Logging;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace EHentaiInfoTelegramBot
{
    public class NHentaiInfo : IHentaiInfo
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EHentaiInfo> _logger;
        private readonly ITagTranslationInfo _tagTranslationInfo;

        public NHentaiInfo(IConfiguration configuration, ILogger<EHentaiInfo> logger, ICache cache,
            ITagTranslationInfo tagTranslationInfo)
        {
            _configuration = configuration;
            _logger = logger;
            _tagTranslationInfo = tagTranslationInfo;

            HttpClient = new HttpClient();
            CachedHttpClient = new Cache<HttpClient>(HttpClient, cache);
            TitleRegex = new Regex(@"(?<=<h2>).*?(?=</h2>)", RegexOptions.Compiled);
            UrlRegex = new Regex(@"https?://nhentai.net/g/(?<id>\d+)", RegexOptions.Compiled);
            CoverRegex = new Regex(@"(?<=<img.*data-src="").*?cover.*?(?="")", RegexOptions.Compiled);
        }

        private HttpClient HttpClient { get; }
        private Cache<HttpClient> CachedHttpClient { get; }
        private Regex TitleRegex { get; }
        private Regex CoverRegex { get; }
        public Regex UrlRegex { get; }

        public async Task<IHentaiInfoModel> GetInfoAsync(string url)
        {
            var html = await CachedHttpClient.Method(t => t.GetStringAsync(url))
                .ExpireAfter(TimeSpan.FromDays(1))
                .GetValueAsync();
            var htmlParser = BrowsingContext.New();
            var document = await htmlParser.OpenAsync(req => req.Content(html));

            var model = new NHentaiInfoModel
            {
                Title = string.Join("", document.QuerySelector("h2.title").Children.Select(t => t.Text())),
                Id = UrlRegex.Match(url).Groups["id"].Value,
                Cover = await GetCoverAsync(html)
            };

            foreach (var pair in await ExtractTagsAsync(html)) model.Tags.Add(pair);

            return model;
        }

        private async Task<IDictionary<string, IList<string>>> ExtractTagsAsync(string html)
        {
            var tagRowCleanupRegex = new Regex(@".*?(?=:\n)", RegexOptions.Compiled);
            var htmlParser = BrowsingContext.New();
            var document = await htmlParser.OpenAsync(req => req.Content(html));
            var tags = (await Task.WhenAll(document.QuerySelectorAll<IElement>("#tags div:not(.hidden)")
                    .Where(t=>!t.Text().Contains("Pages:") && !t.Text().Contains("Uploaded:"))
                    .Select(async t => new
                    {
                        key = await GetTranslationAsync(tagRowCleanupRegex.Match(t.Text()).Value.Trim()),
                        value = (await Task.WhenAll((await htmlParser.OpenAsync(req => req.Content(t.OuterHtml)))
                                .QuerySelectorAll<IElement>("a span.name")
                                .Select(async d => await GetTranslationAsync(
                                    tagRowCleanupRegex.Match(t.Text()).Value.Trim(),
                                    d.Text().Trim()))))
                            .ToList() as IList<string>
                    })))
                .ToDictionary(t => t.key, t => t.value);

            return tags;
        }

        private async Task<string> GetTranslationAsync(string row, string tag = null)
        {
            if (tag == null)
                switch (row)
                {
                    case "Tags":
                        return "标签";
                    case "Artists":
                        return "艺术家";
                    case "Parodies":
                        return "原作";
                    case "Characters":
                        return "角色";
                    case "Groups":
                        return "团队";
                    case "Languages":
                        return "语言";
                    case "Categories":
                        return "分类";
                }
            else
                switch (row)
                {
                    case "Tags":
                        var (success, translatedTag) = await _tagTranslationInfo.GetTranslationAsync("female", tag);
                        if (success)
                            return translatedTag;
                        else
                            (success, translatedTag) = await _tagTranslationInfo.GetTranslationAsync("male", tag);
                        if (success)
                            return translatedTag;
                        else
                            (_, translatedTag) = await _tagTranslationInfo.GetTranslationAsync("misc", tag);
                        return translatedTag;
                    case "Artists":
                        return (await _tagTranslationInfo.GetTranslationAsync("artist", tag)).Item2;
                    case "Parodies":
                        return (await _tagTranslationInfo.GetTranslationAsync("parody", tag)).Item2;
                    case "Characters":
                        return (await _tagTranslationInfo.GetTranslationAsync("character", tag)).Item2;
                    case "Groups":
                        return (await _tagTranslationInfo.GetTranslationAsync("group", tag)).Item2;
                    case "Languages":
                        return (await _tagTranslationInfo.GetTranslationAsync("language", tag)).Item2;
                }

            return tag ?? row;
        }

        private async Task<MemoryStream> GetCoverAsync(string html)
        {
            var coverUrl = CoverRegex.Match(html).Value;
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