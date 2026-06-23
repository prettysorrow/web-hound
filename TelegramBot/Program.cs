using Telegram.Bot;
using TelegramBot;
using Services;
using EntityFramework;
using Core;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateDefaultBuilder(args);

// database services
builder.ConfigureServices((context, services) =>
{
    var db_connection = context.Configuration["db_connection"] ?? throw new IsNotSpecifiedException("Database connection for Telegram Bot");
    services.AddDbContext<BackDbContext>(options => options.UseNpgsql(connectionString: db_connection));
}
);

// github rest api services
builder.ConfigureServices((context, services) =>
{
    var github_pat = context.Configuration["github_pat"] ?? throw new IsNotSpecifiedException("GitHub PAT for Telegram Bot");
    services.AddSingleton<GitHub>(services => new GitHub(github_pat, services.GetRequiredService<IRequestsProvider>()));
    services.AddSingleton<IRequestsProvider, RequestsProvider>();
});

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

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BackDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();
