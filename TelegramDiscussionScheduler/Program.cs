using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TelegramDiscussionScheduler.Options;
using TelegramDiscussionScheduler.Services;

// ── Parse command ────────────────────────────────────────────────────
if (args.Length == 0)
{
    await Console.Error.WriteLineAsync("Usage: dotnet run [open|close]");
    return 1;
}

var command = args[0].ToLowerInvariant();
if (command is not ("open" or "close"))
{
    await Console.Error.WriteLineAsync($"Unknown command: '{command}'. Expected 'open' or 'close'.");
    return 1;
}

// ── Build and execute ────────────────────────────────────────────────
try
{
    var host = CreateHostBuilder(args).Build();

    var logger = host.Services.GetRequiredService<ILoggerFactory>()
        .CreateLogger("TelegramDiscussionScheduler");

    logger.LogInformation("Telegram Discussion Scheduler starting...");
    logger.LogInformation("Command: {Command}", command.ToUpper());

    var discussionService = host.Services.GetRequiredService<IDiscussionService>();
    using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

    if (command == "open")
        await discussionService.OpenAsync(cts.Token);
    else
        await discussionService.CloseAsync(cts.Token);

    logger.LogInformation("Scheduler finished successfully.");
    return 0;
}
catch (Exception ex) when (ex.Message.Contains("DataAnnotation validation failed"))
{
    await Console.Error.WriteLineAsync(
        "❌ Configuration Error: BotToken is required.\n\n" +
        "   Set it via one of:\n" +
        "   1. Environment variable: Telegram__BotToken=your_token_here\n" +
        "   2. Edit appsettings.json and set Telegram.BotToken\n" +
        "   3. GitHub Actions: add BOT_TOKEN as a repository secret\n\n" +
        $"   Details: {ex.Message}");
    return 1;
}
catch (Exception ex)
{
    await Console.Error.WriteLineAsync($"❌ Fatal error: {ex.Message}");
    return 1;
}

// ── Host builder ─────────────────────────────────────────────────────
static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((context, config) =>
        {
            // Host.CreateDefaultBuilder already adds:
            //   appsettings.json, appsettings.{Environment}.json,
            //   environment variables, and command-line args.
            //
            // The double-underscore convention maps env vars to nested config:
            //   Telegram__BotToken         → Telegram:BotToken
            //   Telegram__Groups__0__ChatId → Telegram:Groups:0:ChatId
            //
            // We re-add env vars here to ensure they take highest precedence.
            config.AddEnvironmentVariables();
        })
        .ConfigureLogging((context, logging) =>
        {
            logging.ClearProviders();
            logging.AddConsole(options =>
            {
                options.LogToStandardErrorThreshold = LogLevel.Error;
            });

            var logLevel = context.Configuration.GetValue<LogLevel>("Logging:LogLevel:Default");
            logging.SetMinimumLevel(logLevel);
        })
        .ConfigureServices((context, services) =>
        {
            services
                .AddOptions<TelegramOptions>()
                .Bind(context.Configuration.GetSection(TelegramOptions.SectionName))
                .ValidateDataAnnotations();

            services.AddHttpClient<ITelegramService, TelegramService>(client =>
            {
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.Timeout = TimeSpan.FromSeconds(15);
            });

            services.AddSingleton<IDiscussionService, DiscussionService>();
        });



//https://github.com/irequestresult1515-web