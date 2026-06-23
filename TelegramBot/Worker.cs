using System.Reflection.Metadata;
using EntityFramework;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace TelegramBot;

public class Worker : BackgroundService
{
    private UpdatesHandler _updatesHandler { get; init; }
    private ITelegramBotClient _client { get; init; }
    private Services.ILogger? _logger { get; init; }

    public Worker(ITelegramBotClient client, UpdatesHandler updatesHandler, Services.ILogger? logger = null)
    {
        this._updatesHandler = updatesHandler;
        this._client = client;
        this._logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        this._logger?.Write(Services.LogMessageType.Info, "telegram bot polling started");

        var options = new ReceiverOptions() { AllowedUpdates = [] };
        await _client.ReceiveAsync(
            updateHandler: _updatesHandler,
            receiverOptions: options,
            cancellationToken: cancellationToken
        );
    }
}
