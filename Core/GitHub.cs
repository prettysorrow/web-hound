using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Core;

public class GitHub
{
    private readonly Uri _baseURI = new("https://api.github.com");
    private string _pat { get; init; }
    private IRequestsProvider _requestsProvider { get; init; }

    public GitHub(string pat, IRequestsProvider requestsProvider)
    {
        this._requestsProvider = requestsProvider;
        this._pat = pat;
    }

    private void AddGitHubHeaders(HttpRequestMessage request)
    {
        request.Headers.Clear();
        request.Headers.Add("X-GitHub-Api-Version", "2026-03-10");
        request.Headers.Accept.Add(new("application/vnd.github+json"));
        request.Headers.Authorization = new("Bearer", _pat);
        request.Headers.UserAgent.TryParseAdd("MyApp/1.0");
    }

    public Task<HttpResponseMessage> GetAsync(string relativeURI)
    {
        var uri = new Uri(baseUri: _baseURI, relativeUri: relativeURI);
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        AddGitHubHeaders(request);
        return _requestsProvider.MakeRequestsAsync(request);
    }

    public async Task<List<SummaryUser>> GetFollowers(string username)
    {
        var endpoint = $"/users/{username}/followers";
        var response = await GetAsync(endpoint);
        return await Deserialize<List<SummaryUser>>(response);
    }

    public async Task<List<SummaryUser>> GetFollowing(string username)
    {
        var endpoint = $"/users/{username}/following";
        var response = await GetAsync(endpoint);
        return await Deserialize<List<SummaryUser>>(response);
    }

    public async Task<VerboseUser> GetUser(string username)
    {
        var endpoint = $"/users/{username}";
        var response = GetAsync(endpoint);
        var followers = GetFollowers(username);
        var following = GetFollowing(username);

        await Task.WhenAll(response, followers, following);

        var summary = await Deserialize<SummaryUser>(await response);

        return new(summary.Login, summary.Id, await followers, await following);
    }

    public record VerboseUser(string Login, long Id, List<SummaryUser> Followers, List<SummaryUser> Following);
    public record SummaryUser(string Login, long Id);

    private readonly JsonSerializerOptions serializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<T> Deserialize<T>(HttpResponseMessage response)
    {
        string json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, serializerOptions) ?? throw new Exception("unexpected null");
    }
}
