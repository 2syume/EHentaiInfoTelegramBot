using System;
using System.Collections.Generic;
using System.IO;

namespace EHentaiInfoTelegramBot.Model
{
    public interface IHentaiInfoModel : IDisposable
    {
        string Title { get; set; }
        IDictionary<string, IList<string>> Tags { get; }
        MemoryStream Cover { get; set; }
        IDictionary<string, string> Urls { get; }
        string ToString();
        string ToMarkdown();
    }
}