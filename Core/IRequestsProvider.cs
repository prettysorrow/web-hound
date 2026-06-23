namespace Core;

public interface IRequestsProvider
{
    public Task<HttpResponseMessage> MakeRequestsAsync(HttpRequestMessage request);
}
