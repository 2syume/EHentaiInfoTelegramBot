using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EHentaiInfoTelegramBot
{
    public class Bot
    {
        private readonly IConfiguration _configuration;
        private readonly IEnumerable<IHentaiInfo> _hentaiInfos;
        private readonly ILogger<Bot> _logger;

        public Bot(IConfiguration configuration, ILogger<Bot> logger, IEnumerable<IHentaiInfo> hentaiInfos)
        {
            _configuration = configuration;
            _logger = logger;
            _hentaiInfos = hentaiInfos;
        }

        private bool CheckSecret()
        {
            if (string.IsNullOrEmpty(_configuration["secret"]))
            {
                _logger.LogCritical("Cannot find the bot secret. Please put your bot secret in the json file.");
                return false;
            }

            return true;
        }

        public async Task RunAsync()
        {
            if (!CheckSecret()) return;

            try
            {
                var bot = new TelegramBotClient(_configuration["secret"]);
                var result = await bot.GetMeAsync();
                _logger.LogInformation($"Running as {result.Username} with id {result.Id}");
                bot.OnMessage += async (sender, e) =>
                {
                    try
                    {
                        if (e.Message.Text == null) return;
                        foreach (var hentaiInfo in _hentaiInfos)
                        {
                            if (!hentaiInfo.UrlRegex.IsMatch(e.Message.Text)) continue;
                            await bot.SendChatActionAsync(e.Message.Chat.Id, ChatAction.Typing);
                            _logger.LogInformation(
                                $"Receives: {hentaiInfo.UrlRegex.Match(e.Message.Text).Value} from {e.Message.Chat.Id}");

                            using (var info =
                                await hentaiInfo.GetInfoAsync(hentaiInfo.UrlRegex.Match(e.Message.Text).Value))
                            {
                                await bot.SendPhotoAsync(
                                    e.Message.Chat,
                                    new InputMedia(info.Cover, "cover.jpg"),
                                    info.ToMarkdown(),
                                    disableNotification: true,
                                    replyToMessageId: e.Message.MessageId,
                                    parseMode: ParseMode.Markdown);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, ex.ToString());
                    }
                };

                bot.StartReceiving(new[] {UpdateType.Message, UpdateType.EditedMessage});
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.ToString());
            }
        }
    }
}