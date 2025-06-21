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

    private static bool ShouldSkipJwt(HttpRequestMessage request)
    {
        var uri = request.RequestUri;
        if (uri == null)
        {
            return false;
        }
        var path = uri.AbsolutePath.TrimEnd('/');
        return path.EndsWith("/wp-json/wp/v2", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/wp-json/jwt-auth/v1/token", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/jwt-auth/v1/token", StringComparison.OrdinalIgnoreCase);
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!ShouldSkipJwt(request))
        {
            var token = await _jwtService.GetCurrentJwtAsync();
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
