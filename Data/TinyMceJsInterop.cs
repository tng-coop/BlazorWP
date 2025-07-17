using Microsoft.JSInterop;

namespace BlazorWP;

public class TinyMceJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public TinyMceJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/tinyMceConfig.js");
        }
        return _module;
    }

    public async ValueTask InitializeAsync()
    {
        await GetModuleAsync();
    }

    public async ValueTask SetMediaSourceAsync(string? url)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("setTinyMediaSource", url);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
