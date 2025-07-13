using System.Net.Http.Headers;
using Microsoft.JSInterop;

namespace BlazorWP;

public class AuthMessageHandler : DelegatingHandler
{
    private readonly JwtService _jwtService;
    private readonly IJSRuntime _js;
    private const string HostInWpKey = "hostInWp";

    public AuthMessageHandler(JwtService jwtService, IJSRuntime js)
    {
        _jwtService = jwtService;
        _js = js;
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
        var hostPref = await _js.InvokeAsync<string?>("localStorage.getItem", HostInWpKey);
        var useNonce = !string.IsNullOrEmpty(hostPref) && bool.TryParse(hostPref, out var hv) && hv;

        if (useNonce)
        {
            var nonce = await _js.InvokeAsync<string?>("wpNonce.getNonce");
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
