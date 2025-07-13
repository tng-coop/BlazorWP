using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWP;

public static class HttpMessageUtils
{
    public static async Task<string> FormatRawRequest(HttpRequestMessage request)
    {
        var sb = new StringBuilder();
        var uri = request.RequestUri ?? new Uri("/", UriKind.Relative);
        var requestUri = uri.IsAbsoluteUri ? uri.ToString() : uri.PathAndQuery;
        sb.Append($"{request.Method} {requestUri} HTTP/{request.Version.Major}.{request.Version.Minor}\r\n");

        if (!request.Headers.Contains("Host") && uri.Host.Length > 0)
        {
            sb.Append($"Host: {uri.Host}\r\n");
        }

        foreach (var header in request.Headers)
        {
            sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
        }

        if (request.Content != null)
        {
            foreach (var header in request.Content.Headers)
            {
                sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
            }
        }
        sb.Append("\r\n");
        if (request.Content != null)
        {
            var bytes = await request.Content.ReadAsByteArrayAsync();
            Encoding encoding = Encoding.UTF8;
            if (request.Content.Headers.ContentType?.CharSet is string cs)
            {
                try { encoding = Encoding.GetEncoding(cs); } catch { }
            }
            sb.Append(encoding.GetString(bytes));
        }
        return sb.ToString();
    }

    public static async Task<string> FormatRawResponse(HttpResponseMessage response)
    {
        var sb = new StringBuilder();
        sb.Append($"HTTP/{response.Version.Major}.{response.Version.Minor} {(int)response.StatusCode} {response.ReasonPhrase}\r\n");
        foreach (var header in response.Headers)
        {
            sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
        }
        foreach (var header in response.Content.Headers)
        {
            sb.Append($"{header.Key}: {string.Join(", ", header.Value)}\r\n");
        }
        sb.Append("\r\n");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Encoding encoding = Encoding.UTF8;
        if (response.Content.Headers.ContentType?.CharSet is string cs)
        {
            try { encoding = Encoding.GetEncoding(cs); } catch { }
        }
        sb.Append(encoding.GetString(bytes));
        return sb.ToString();
    }
}
