using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
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
        private readonly ILogger<Bot> _logger;
        private readonly IEHentaiInfo _eHentaiInfo;
        private Regex EHentaiUrlRegex { get; }

        public Bot(IConfiguration configuration, ILogger<Bot> logger, IEHentaiInfo eHentaiInfo)
        {
            _configuration = configuration;
            _logger = logger;
            _eHentaiInfo = eHentaiInfo;

            EHentaiUrlRegex = new Regex(@"https?://e[x\-]hentai.org/g/(?<id>[\d\w]+?/[\d\w]+)", RegexOptions.Compiled);
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
                        if (e.Message.Text == null || !EHentaiUrlRegex.IsMatch(e.Message.Text)) return;
                        await bot.SendChatActionAsync(e.Message.Chat.Id, ChatAction.Typing);
                        _logger.LogInformation($"Receives: {e.Message.Text} from {e.Message.Chat.Id}");

                        using (var info = await _eHentaiInfo.GetInfoAsync(EHentaiUrlRegex.Match(e.Message.Text).Value))
                        {
                            await bot.SendPhotoAsync(
                                e.Message.Chat,
                                new InputMedia(info.Cover, "cover.jpg"),
                                info.ToString(),
                                disableNotification: true,
                                replyToMessageId: e.Message.MessageId,
                                parseMode: ParseMode.Markdown);
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
