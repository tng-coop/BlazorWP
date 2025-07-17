using Microsoft.JSInterop;

namespace BlazorWP;

public class HostInWpJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public HostInWpJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/hostInWp.js");
        }
        return _module;
    }

    public async ValueTask<bool?> GetAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<bool?>("get");
    }

    public async ValueTask SetAsync(bool value)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("set", value);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
