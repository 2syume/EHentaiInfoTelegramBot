using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace EHentaiInfoTelegramBot
{
    class Program
    {
        static void Main(string[] args)
        {
            var startup = new Startup(args);
            var services = startup.ConfigureServices();
            var bot = services.GetRequiredService<Bot>();
            bot.RunAsync().Wait();

            var autoResetEvent = new AutoResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                autoResetEvent.Set();
            };
            autoResetEvent.WaitOne();
        }
    }
}
