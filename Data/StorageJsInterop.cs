using Microsoft.JSInterop;

namespace BlazorWP;

public class StorageJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public StorageJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/storageUtils.js");
        }
        return _module;
    }

    public async ValueTask<string[]> KeysAsync()
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<string[]>("keys");
    }

    public async ValueTask<ItemInfo> ItemInfoAsync(string key)
    {
        var module = await GetModuleAsync();
        return await module.InvokeAsync<ItemInfo>("itemInfo", key);
    }

    public async ValueTask DeleteAsync(string key)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("deleteItem", key);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }

    public class ItemInfo
    {
        public string? Value { get; set; }
        public string? LastUpdated { get; set; }
    }
}
