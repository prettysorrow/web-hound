using Telegram.Bot;
using TelegramBot;
using Services;
using EntityFramework;
using Core;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateDefaultBuilder(args);

// backend resp api services
builder.ConfigureServices((context, services) =>
{
    var backend_url = context.Configuration["backend_url"] ?? throw new IsNotSpecifiedException("Backend URL for Telegram Bot");
    services.AddHttpClient("Backend", client => client.BaseAddress = new Uri(backend_url));
}
);

// aux services
builder.ConfigureServices((context, services) =>
{
    services.AddHttpClient();
    services.AddSingleton<Services.ILogger, Services.Logger>();
});

// telegram bot services
builder.ConfigureServices((context, services) =>
{
    var telegram_bot_token = context.Configuration["telegram_bot_token"] ?? throw new IsNotSpecifiedException("Telegram bot token");
    services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(telegram_bot_token));
    services.AddSingleton<UpdatesHandler>();
    services.AddSingleton<Worker>();
    services.AddHostedService<Worker>();
}
);

// dependency validation
builder.UseDefaultServiceProvider(services =>
{
    services.ValidateOnBuild = true;
    services.ValidateScopes = true;
});

var app = builder.Build();

await app.RunAsync();
