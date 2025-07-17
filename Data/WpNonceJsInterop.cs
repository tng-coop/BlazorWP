using Microsoft.JSInterop;

namespace BlazorWP;

public class WpNonceJsInterop
{
    private readonly IJSRuntime _jsRuntime;

    public WpNonceJsInterop(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ValueTask<string?> GetNonceAsync()
    {
        return _jsRuntime.InvokeAsync<string?>("wpNonce.getNonce");
    }
}
