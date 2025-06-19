using System.Text.Json;
using Microsoft.JSInterop;

namespace BlazorWP;

public class JwtService
{
    private readonly IJSRuntime _js;
    private const string WpEndpointKey = "wpEndpoint";
    private const string SiteInfoKey = "siteinfo";

    public JwtService(IJSRuntime js)
    {
        _js = js;
    }

    private class JwtInfo
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Token { get; set; }
    }

    private static string GetJwtInfoKey(string endpoint) => endpoint;
    private static string GetOldJwtInfoKey(string endpoint) => $"jwtInfo:{endpoint}";
    private static string GetJwtTokenKey(string endpoint) => $"jwtToken:{endpoint}";

    private async Task<Dictionary<string, JwtInfo>> LoadSiteInfoAsync()
    {
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", SiteInfoKey);
        if (string.IsNullOrEmpty(json))
        {
            return new();
        }
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, JwtInfo>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private Task SaveSiteInfoAsync(Dictionary<string, JwtInfo> data)
    {
        var json = JsonSerializer.Serialize(data);
        return _js.InvokeVoidAsync("localStorage.setItem", SiteInfoKey, json).AsTask();
    }

    private async Task<JwtInfo?> LoadJwtInfoAsync(string endpoint)
    {
        var data = await LoadSiteInfoAsync();
        if (data.TryGetValue(endpoint, out var info))
        {
            return info;
        }

        var key = GetJwtInfoKey(endpoint);
        var json = await _js.InvokeAsync<string?>("localStorage.getItem", key);
        if (string.IsNullOrEmpty(json))
        {
            var oldKey = GetOldJwtInfoKey(endpoint);
            json = await _js.InvokeAsync<string?>("localStorage.getItem", oldKey);
        }

        if (string.IsNullOrEmpty(json))
        {
            var token = await _js.InvokeAsync<string?>("localStorage.getItem", GetJwtTokenKey(endpoint));
            if (!string.IsNullOrEmpty(token))
            {
                info = new JwtInfo { Token = token };
                data[endpoint] = info;
                await SaveSiteInfoAsync(data);
                return info;
            }
            return null;
        }

        try
        {
            info = JsonSerializer.Deserialize<JwtInfo>(json);
        }
        catch
        {
            info = new JwtInfo { Token = json };
        }

        if (info != null)
        {
            data[endpoint] = info;
            await SaveSiteInfoAsync(data);
        }

        return info;
    }

    public async Task<string?> GetCurrentJwtAsync()
    {
        var endpoint = await _js.InvokeAsync<string?>("localStorage.getItem", WpEndpointKey);
        if (string.IsNullOrEmpty(endpoint))
        {
            return null;
        }

        var info = await LoadJwtInfoAsync(endpoint);
        return info?.Token;
    }
}
