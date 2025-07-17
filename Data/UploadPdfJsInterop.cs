using Microsoft.JSInterop;

namespace BlazorWP;

public class UploadPdfJsInterop : IAsyncDisposable
{
    private readonly IJSRuntime _jsRuntime;
    private IJSObjectReference? _module;

    public UploadPdfJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private async ValueTask<IJSObjectReference> GetModuleAsync()
    {
        if (_module == null)
        {
            _module = await _jsRuntime.InvokeAsync<IJSObjectReference>("import", "./js/upload-pdf.js");
        }
        return _module;
    }

    public async ValueTask InitializeAsync(string canvasId, string imgId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("initialize", canvasId, imgId);
    }

    public async ValueTask RenderFirstPageAsync(DotNetStreamReference streamRef, string canvasId, string imgId)
    {
        var module = await GetModuleAsync();
        await module.InvokeVoidAsync("renderFirstPageFromStream", streamRef, canvasId, imgId);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
    }
}
