using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EHentaiInfoTelegramBot.Model
{
    public class NHentaiInfoModel : IHentaiInfoModel
    {
        public NHentaiInfoModel()
        {
            Tags = new Dictionary<string, IList<string>>();
        }

        public string Id { get; set; }
        public string Url => string.IsNullOrEmpty(Id) ? null : $"https://nhentai.net/g/{Id}";
        public string Title { get; set; }
        public IDictionary<string, IList<string>> Tags { get; }
        public MemoryStream Cover { get; set; }
        public IDictionary<string, string> Urls => new Dictionary<string, string> {{"Url", Url}};

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(Title ?? "");

            if (Tags != null)
                foreach (var tag in Tags)
                    if (!string.IsNullOrEmpty(tag.Key) && tag.Value != null)
                        sb.AppendLine($"{tag.Key}: {string.Join(", ", tag.Value)}");

            sb.AppendLine(Url ?? "");

            return sb.ToString();
        }

        public string ToMarkdown()
        {
            var sb = new StringBuilder();

            if (Title != null) sb.AppendLine($"**{Utils.MarkdownEscape(Title)}**");

            if (Tags != null)
                foreach (var tag in Tags)
                    if (!string.IsNullOrEmpty(tag.Key) && tag.Value != null)
                        sb.AppendLine($"**{tag.Key}**: {string.Join(", ", tag.Value)}");

            if (Url != null) sb.AppendLine($"[URL]({Url})");

            return sb.ToString();
        }

        public void Dispose()
        {
            Cover?.Dispose();
        }
    }
}