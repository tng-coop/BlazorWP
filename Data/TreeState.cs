using System.Text.Json;
using BlazorWP.Data;
using Microsoft.JSInterop;

namespace BlazorWP.Data;

public class TreeState
{
    public List<AntJsonNode>? Nodes { get; private set; }
    public string[] ExpandedKeys { get; set; } = Array.Empty<string>();
    public string? SelectedKey { get; set; }

    private readonly HttpClient _http;
    private readonly IJSRuntime _js;

    public TreeState(HttpClient http, IJSRuntime js)
    {
        _http = http;
        _js = js;
    }

    public async Task EnsureLoadedAsync()
    {
        if (Nodes != null)
        {
            return;
        }

        var endpoint = await _js.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            return;
        }

        var rootEndpoint = endpoint;
        if (rootEndpoint.EndsWith("/wp/v2", StringComparison.OrdinalIgnoreCase))
        {
            rootEndpoint = rootEndpoint[..^"/wp/v2".Length];
        }

        var json = await _http.GetStringAsync(rootEndpoint);
        using var doc = JsonDocument.Parse(json);
        var root = BuildNode(doc.RootElement, "root");
        Nodes = root.Children;
    }

    private static AntJsonNode BuildNode(JsonElement element, string name)
    {
        var node = new AntJsonNode { Id = Guid.NewGuid().ToString(), Title = name };
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    node.Children.Add(BuildNode(prop.Value, prop.Name));
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var val in element.EnumerateArray())
                {
                    node.Children.Add(BuildNode(val, $"[{index}]"));
                    index++;
                }
                break;
            default:
                node.Title = $"{name}: {element}";
                break;
        }
        return node;
    }
}
