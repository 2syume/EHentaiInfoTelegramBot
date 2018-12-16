using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace EHentaiInfoTelegramBot.Model
{
    public class EHentaiInfoModel : IDisposable
    {
        public string Title { get; set; }
        public IDictionary<string, IList<string>> Tags { get; set; }
        //public string CoverUrl { get; set; }
        public MemoryStream Cover { get; set; }
        public string Id { get; set; }
        public string EHUrl => string.IsNullOrEmpty(Id) ? null : $"[E-H URL](https://e-hentai.org/g/{Id})";
        public string EXUrl => string.IsNullOrEmpty(Id) ? null : $"[ExH URL](https://exhentai.org/g/{Id})";

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine(Title ?? "");

            if (Tags != null)
                foreach (var tag in Tags)
                {
                    if (!string.IsNullOrEmpty(tag.Key) && tag.Value != null)
                        sb.AppendLine($"{tag.Key}: {string.Join(", ", tag.Value)}");
                }

            sb.AppendLine(EHUrl ?? "");
            sb.AppendLine(EXUrl ?? "");

            return sb.ToString();
        }

        public void Dispose()
        {
            Cover?.Dispose();
        }
    }
}
