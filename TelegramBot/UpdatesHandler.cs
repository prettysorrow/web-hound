namespace TelegramBot;

using Core;
using Services;

using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using System.Text.RegularExpressions;
using System.Text;
using Telegram.Bot.Polling;
using System.Net.Http.Json;

public partial class UpdatesHandler : IUpdateHandler
{
    private IHttpClientFactory _httpClientFactory { get; init; }
    private ILogger _logger { get; init; }

    public UpdatesHandler(IHttpClientFactory httpClientFactory, ILogger logger)
    {
        this._httpClientFactory = httpClientFactory;
        this._logger = logger;
    }

    private async Task TrySendResponse(string text, ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.SendMessage(chatId: message.Chat.Id, text, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            this._logger.Write(LogMessageType.Warning, $"exception: {ex.Message}");
        }
    }

    private Task SendUsageMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var text = "usage:\n" +
                      "/start            <---  print this message\n" +
                      "/find <username>  <---  print all info using username\n" +
                      "/history          <---  your requests history\n";

        return TrySendResponse(text, botClient, message, cancellationToken);
    }

    private async Task HandleStartMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var username = message.Chat.Username ?? "guest";
        var greetings = $"hello, {username}!";

        this._logger.Write(LogMessageType.Info, $"received start message from {username}");

        await TrySendResponse(text: greetings, botClient, message, cancellationToken);

        this._logger.Write(LogMessageType.Info, $"sent greetings to {username}");

        await SendUsageMessage(botClient, message, cancellationToken);

        this._logger.Write(LogMessageType.Info, $"sent usage message to {username}");
    }

    private async Task HandleUnknownMessage(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var username = message.Chat.Username ?? "guest";
        var error = "unknown command";

        this._logger.Write(LogMessageType.Info, $"received invalid message from {username}");

        await TrySendResponse(text: error, botClient, message, cancellationToken);

        this._logger.Write(LogMessageType.Info, $"sent error message to {username}");

        await SendUsageMessage(botClient, message, cancellationToken);

        this._logger.Write(LogMessageType.Info, $"sent usage message to {username}");
    }

    private static void WriteVerboseUser(StringBuilder buf, VerboseUser user)
    {
        buf.Append($"username: {user.Login}\n");
        buf.Append($"uid: {user.Id}\n");

        var followers = user.Followers ?? [];
        buf.Append($"{followers.Count} followers:\n");
        foreach (var follower in followers)
        {
            buf.Append($"\t{follower.Login} with UID = {follower.Id}\n");
        }

        var followings = user.Following ?? [];
        buf.Append($"{followings.Count} following:\n");
        foreach (var following in followings)
        {
            buf.Append($"\t{following.Login} with UID = {following.Id}\n");
        }
    }

    private async Task HandleHistory(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        static string MakeResponse(Request request)
        {
            var text = new StringBuilder();
            text.Append($"time: {request.Time}\n");
            WriteVerboseUser(text, request.VerboseUser);
            return text.ToString();
        }

        var username = message.Chat.Username ?? "guest";
        this._logger.Write(LogMessageType.Info, $"received history message from {username}");

        var backend = _httpClientFactory.CreateClient("Backend");
        var requests = await backend.GetFromJsonAsync<List<Request>>("/github/history", cancellationToken);
        this._logger.Write(LogMessageType.Info, $"fetched history for {username}");

        if (requests is null || requests.Count == 0)
        {
            this._logger.Write(LogMessageType.Info, $"{username}'s history is empty");
            var text = "your history is empty";
            await TrySendResponse(text, botClient, message, cancellationToken);
            this._logger.Write(LogMessageType.Info, $"sent 'history is empty' message to {username}");
            return;
        }

        this._logger.Write(LogMessageType.Info, $"{username}'s history contains {requests.Count} requests");
        var tasks = requests.Select(request => TrySendResponse(MakeResponse(request), botClient, message, cancellationToken));
        await Task.WhenAll(tasks);
    }

    private async Task HandleFind(string username, ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var backend = _httpClientFactory.CreateClient("Backend");
        var user = await backend.GetFromJsonAsync<VerboseUser>($"github/find/{username}", cancellationToken);
        if (user is null) return;

        var text = new StringBuilder();
        WriteVerboseUser(text, user);
        await TrySendResponse(text.ToString(), botClient, message, cancellationToken);
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message)
        {
            this._logger.Write(LogMessageType.Warning, "unsupported feature (message expected)");
            return;
        }

        var message = update.Message;

        if (message is null || message.Text is null)
        {
            this._logger.Write(LogMessageType.Warning, "unexpected null (non empty message expected)");
            return;
        }

        var username = message.Chat.Username ?? "guest";
        var text = message.Text.Trim();

        this._logger.Write(LogMessageType.Info, $"received message from {username}: {text}");

        if (StartRegex().IsMatch(text))
        {
            await HandleStartMessage(botClient, message, cancellationToken);
        }
        else if (HistoryRegex().IsMatch(text))
        {
            await HandleHistory(botClient, message, cancellationToken);
        }
        else if (FindRegex().Match(text) is { Success: true } match)
        {
            var victim = match.Groups["username"].Value;
            await HandleFind(username: victim, botClient, message, cancellationToken);
        }
        else
        {
            await HandleUnknownMessage(botClient, message, cancellationToken);
        }

        this._logger.Write(LogMessageType.Info, $"handled message from {username}: {text}");
    }

    [GeneratedRegex(@"^/start$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex StartRegex();

    [GeneratedRegex(@"^/history$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex HistoryRegex();

    [GeneratedRegex(@"^/find\s+(?<username>[a-zA-Z0-9](?:[a-zA-Z0-9\-]*[a-zA-Z0-9])?)$", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex FindRegex();

    public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        _logger.Write(LogMessageType.Fatal, exception.Message);
        return Task.CompletedTask;
    }
}
