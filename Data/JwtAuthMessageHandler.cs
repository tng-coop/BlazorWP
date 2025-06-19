using System.Net.Http.Headers;

namespace BlazorWP;

public class JwtAuthMessageHandler : DelegatingHandler
{
    private readonly JwtService _jwtService;

    public JwtAuthMessageHandler(JwtService jwtService)
    {
        _jwtService = jwtService;
        InnerHandler = new HttpClientHandler();
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await _jwtService.GetCurrentJwtAsync();
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
