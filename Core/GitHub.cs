using System.Net.Http.Headers;
using System.Text.Json;

namespace Core;

public class GitHub
{
    private readonly HttpClient client;
    private readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public GitHub(string pat)
    {
        client = new HttpClient();
        client.BaseAddress = new Uri("https://api.github.com");
        client.DefaultRequestHeaders.Clear();
        client.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2026-03-10");
        client.DefaultRequestHeaders.Accept.Add(new("application/vnd.github+json"));
        client.DefaultRequestHeaders.Authorization = new("Bearer", pat);
        client.DefaultRequestHeaders.UserAgent.TryParseAdd("MyApp/1.0");
    }

    private const int attempts = 5;
    private const int delay = 1000;

    public async static Task<HttpResponseMessage> Many(Func<Task<HttpResponseMessage>> invoke)
    {
        for (var i = 0; ;)
        {
            try
            {
                var response = await invoke();
                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                var content = response.Content.ReadAsStringAsync();
                Console.Error.WriteLine($"[warning] attempt to connect to GitHub API failed with code {response.StatusCode}");
                Console.Error.WriteLine($"[warning] response content: {await content}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[warning] attempt to connect to GitHub API failed with exception");
                Console.Error.WriteLine($"[warning] exception message: {ex.Message}");
            }

            ++i;

            if (i >= attempts)
            {
                throw new TimeoutException($"attempts: {attempts}, delay: {delay}");
            }

            Console.Error.WriteLine($"[info] retrying to connect to GitHub API");
            await Task.Delay(delay);
        }
    }

    private async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, serializerOptions) ?? throw new Exception("unexpected null");
    }

    private async Task<HttpResponseMessage> GetManyAsync(string endpoint)
        => await Many(() => this.client.GetAsync(endpoint));

    public async Task<List<SummaryUser>> GetFollowers(string username)
    {
        var endpoint = $"/users/{username}/followers";
        var response = await GetManyAsync(endpoint);
        return await Deserialize<List<SummaryUser>>(response);
    }

    public async Task<List<SummaryUser>> GetFollowing(string username)
    {
        var endpoint = $"/users/{username}/following";
        var response = await GetManyAsync(endpoint);
        return await Deserialize<List<SummaryUser>>(response);
    }

    public async Task<VerboseUser> GetUser(string username)
    {
        var endpoint = $"/users/{username}";
        var response = GetManyAsync(endpoint);
        var followers = GetFollowers(username);
        var following = GetFollowing(username);

        await Task.WhenAll(response, followers, following);

        var summary = await Deserialize<SummaryUser>(await response);

        return new(summary.Login, summary.Id, await followers, await following);
    }

    public record VerboseUser(string Login, long Id, List<SummaryUser> Followers, List<SummaryUser> Following);
    public record SummaryUser(string Login, long Id);
}
