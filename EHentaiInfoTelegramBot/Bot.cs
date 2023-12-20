using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace EHentaiInfoTelegramBot;

public class Bot(IConfiguration configuration, ILogger<Bot> logger, IEnumerable<IHentaiInfo> hentaiInfos)
{
    private bool CheckSecret()
    {
        if (string.IsNullOrEmpty(configuration["secret"]))
        {
            logger.LogCritical("Cannot find the bot secret. Please put your bot secret in the json file.");
            return false;
        }

        return true;
    }

    public async Task RunAsync()
    {
        if (!CheckSecret()) return;

        try
        {
            var bot = new TelegramBotClient(configuration["secret"]);
            var result = await bot.GetMeAsync();
            logger.LogInformation($"Running as {result.Username} with id {result.Id}");

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
                    foreach (var hentaiInfo in hentaiInfos)
                    {
                        if (!hentaiInfo.UrlRegex.IsMatch(update.Message.Text)) continue;
                        await botClient.SendChatActionAsync(update.Message.Chat.Id, ChatAction.Typing,
                            cancellationToken: ctsToken);
                        logger.LogInformation(
                            $"Receives: {hentaiInfo.UrlRegex.Match(update.Message.Text).Value} from {update.Message.Chat.Id}");

                        using var info =
                            await hentaiInfo.GetInfoAsync(hentaiInfo.UrlRegex.Match(update.Message.Text).Value);
                        await botClient.SendPhotoAsync(
                            update.Message.Chat,
                            new InputFileStream(info.Cover, "cover.jpg"),
                            caption: info.ToMarkdown(),
                            disableNotification: true,
                            replyToMessageId: update.Message.MessageId,
                            parseMode: ParseMode.Markdown,
                            cancellationToken: ctsToken);
                    }
                },
                (_, ex, _) => { logger.LogError(ex, ex.ToString()); }, receiveOptions, cts.Token);
        }
        catch (Exception e)
        {
            logger.LogError(e, e.ToString());
        }
    }
}