using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EHentaiInfoTelegramBot.Model;

namespace EHentaiInfoTelegramBot
{
    public interface IHentaiInfo
    {
        Regex UrlRegex { get; }
        Task<IHentaiInfoModel> GetInfoAsync(string url);
    }
}