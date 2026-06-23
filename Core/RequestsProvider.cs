using System.Text.Json;

namespace Core;

public class RequestsProvider : IRequestsProvider
{
    private IHttpClientFactory _clientFactory { get; init; }
    private HttpClient _client => _clientFactory.CreateClient();
    private int _delay { get; init; }
    private int _attempts { get; init; }

    public RequestsProvider(IHttpClientFactory clientFactory, int attempts = 10, int delay = 1000)
    {
        _clientFactory = clientFactory;
        _delay = delay;
        _attempts = attempts;
    }

    public async Task<HttpResponseMessage> MakeRequestsAsync(HttpRequestMessage httpRequest)
    {
        for (var i = 0; ;)
        {
            try
            {
                var response = await _client.SendAsync(httpRequest);
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


            if (i >= _attempts)
            {
                throw new TimeoutException($"attempts: {_attempts}, delay: {_delay}");
            }

            Console.Error.WriteLine($"[info] retrying to connect to GitHub API");
            await Task.Delay(_delay);
        }
    }
}
