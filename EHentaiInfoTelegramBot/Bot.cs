using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
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

                var receiveOptions = new ReceiverOptions
                {
                    AllowedUpdates = new[]
                    {
                        UpdateType.Message
                    }
                };

                var cts = new CancellationTokenSource();

                bot.StartReceiving(async (botClient, update, ctsToken) =>
                    {
                        if (update.Message?.Text == null) return;
                        foreach (var hentaiInfo in _hentaiInfos)
                        {
                            if (!hentaiInfo.UrlRegex.IsMatch(update.Message.Text)) continue;
                            await botClient.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing, ctsToken);
                            _logger.LogInformation(
                                $"Receives: {hentaiInfo.UrlRegex.Match(update.Message.Text).Value} from {update.Message.Chat.Id}");

                            using var info =
                                await hentaiInfo.GetInfoAsync(hentaiInfo.UrlRegex.Match(update.Message.Text).Value);
                            await botClient.SendPhotoAsync(
                                update.Message.Chat,
                                new InputMedia(info.Cover, "cover.jpg"),
                                info.ToMarkdown(),
                                disableNotification: true,
                                replyToMessageId: update.Message.MessageId,
                                parseMode: ParseMode.Markdown,
                                cancellationToken: ctsToken);
                        }
                    },
                    (_, ex, _) => { _logger.LogError(ex, ex.ToString()); }, receiveOptions, cts.Token);

            }
            catch (Exception e)
            {
                _logger.LogError(e, e.ToString());
            }
        }
    }
}