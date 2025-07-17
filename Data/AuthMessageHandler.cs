using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace BlazorWP;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly JwtService _jwtService;
    private readonly HostInWpJsInterop _hostJs;
    private readonly WpNonceJsInterop _nonceJs;

    public AuthMessageHandler(JwtService jwtService, HostInWpJsInterop hostJs, WpNonceJsInterop nonceJs)
    {
        _jwtService = jwtService;
        _hostJs = hostJs;
        _nonceJs = nonceJs;
        InnerHandler = new HttpClientHandler();
    }

    private static bool ShouldSkipAuth(HttpRequestMessage request)
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
        var hostPref = await _hostJs.GetAsync();
        var useNonce = hostPref == true;

        if (useNonce)
        {
            var nonce = await _nonceJs.GetNonceAsync();
            if (!string.IsNullOrWhiteSpace(nonce))
            {
                if (!ShouldSkipAuth(request))
                {
                    request.Headers.Remove("Authorization");
                    request.Headers.Remove("X-WP-Nonce");
                    request.Headers.Add("X-WP-Nonce", nonce);
                }
            }
        }
        else if (!ShouldSkipAuth(request))
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
