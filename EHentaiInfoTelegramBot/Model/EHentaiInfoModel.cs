using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EHentaiInfoTelegramBot.Model
{
    public class EHentaiInfoModel : IHentaiInfoModel
    {
        public EHentaiInfoModel()
        {
            Tags = new Dictionary<string, IList<string>>();
        }

        public string Id { get; set; }
        public string EHUrl => string.IsNullOrEmpty(Id) ? null : $"https://e-hentai.org/g/{Id}";
        public string EXUrl => string.IsNullOrEmpty(Id) ? null : $"https://exhentai.org/g/{Id}";
        public string Title { get; set; }
        public IDictionary<string, IList<string>> Tags { get; }
        public MemoryStream Cover { get; set; }
        public IDictionary<string, string> Urls => new Dictionary<string, string> {{"EHUrl", EHUrl}, {"EXUrl", EXUrl}};

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(Title ?? "");

            if (Tags != null)
                foreach (var tag in Tags)
                    if (!string.IsNullOrEmpty(tag.Key) && tag.Value != null)
                        sb.AppendLine($"{tag.Key}: {string.Join(", ", tag.Value)}");

            sb.AppendLine(EHUrl ?? "");
            sb.AppendLine(EXUrl ?? "");

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

            if (EHUrl != null) sb.AppendLine($"[E-H URL]({EHUrl})");
            if (EXUrl != null) sb.AppendLine($"[EXH URL]({EXUrl})");

            return sb.ToString();
        }

        public void Dispose()
        {
            Cover?.Dispose();
        }
    }
}