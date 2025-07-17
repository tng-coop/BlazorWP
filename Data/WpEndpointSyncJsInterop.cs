using Microsoft.JSInterop;

namespace BlazorWP;

public class WpEndpointSyncJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public WpEndpointSyncJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/wpEndpointSync.js");
        }
        return _module;
    }

    public async ValueTask RegisterAsync<T>(DotNetObjectReference<T> objRef) where T : class
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("register", objRef);
    }

    public async ValueTask UnregisterAsync()
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("unregister");
    }

    public async ValueTask SetAsync(string value)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("set", value);
    }

    public async ValueTask<string?> GetAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string?>("get");
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
