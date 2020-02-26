using FluentCache;
using FluentCache.Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Logging;
using NLog.Targets;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace EHentaiInfoTelegramBot
{
    public class Startup
    {
        private IConfiguration _configuration;

        public Startup(string[] args)
        {
            Args = args;
        }

        private string[] Args { get; }

        public ServiceProvider ConfigureServices()
        {
            InitLogging();
            InitConfiguration();

            return new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.SetMinimumLevel(LogLevel.Trace);
                    builder.AddNLog(new NLogProviderOptions
                    {
                        CaptureMessageTemplates = true,
                        CaptureMessageProperties = true
                    });
                })
                .AddSingleton<ICache>(new FluentMemoryCache())
                .AddSingleton(_configuration)
                .AddSingleton<IHentaiInfo, EHentaiInfo>(services => new EHentaiInfo(
                    services.GetService<IConfiguration>(),
                    services.GetService<ILogger<EHentaiInfo>>(), services.GetService<ICache>(),
                    services.GetService<ITagTranslationInfo>()))
                .AddSingleton<IHentaiInfo, NHentaiInfo>(services => new NHentaiInfo(
                    services.GetService<IConfiguration>(),
                    services.GetService<ILogger<EHentaiInfo>>(), services.GetService<ICache>(),
                    services.GetService<ITagTranslationInfo>()))
                .AddSingleton<ITagTranslationInfo>(services => new TagTranslationInfo(services.GetService<ICache>(),
                    services.GetService<ILogger<TagTranslationInfo>>()))
                .AddTransient<Bot>()
                .BuildServiceProvider();
        }

        private void InitConfiguration()
        {
            var configBuilder = new ConfigurationBuilder();
            // ReSharper disable once StringLiteralTypo
            configBuilder.AddEnvironmentVariables("ehtg");
            configBuilder.AddJsonFile("config.json", true);
            configBuilder.AddCommandLine(Args);
            _configuration = configBuilder.Build();
        }

        private void InitLogging()
        {
            var config = new LoggingConfiguration();

            var fileLogging = new FileTarget("fileLogging")
            {
                FileName = "log.txt",
                MaxArchiveFiles = 2,
                ArchiveAboveSize = 100 * 1024
            };
            var consoleLogging = new ConsoleTarget("consoleLogging");

            config.AddRule(NLog.LogLevel.Warn, NLog.LogLevel.Fatal, fileLogging);
            config.AddRule(NLog.LogLevel.Info, NLog.LogLevel.Fatal, consoleLogging);

            LogManager.Configuration = config;
        }
    }
}