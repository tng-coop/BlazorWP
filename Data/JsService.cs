using Microsoft.JSInterop;

namespace BlazorWP;

public interface IJsService
{
    ValueTask<T> InvokeAsync<T>(string identifier, params object?[]? args);
    ValueTask InvokeVoidAsync(string identifier, params object?[]? args);
}

public class JsService : IJsService
{
    private readonly IJSRuntime _js;

    public JsService(IJSRuntime js)
    {
        _js = js;
    }

    public ValueTask<T> InvokeAsync<T>(string identifier, params object?[]? args)
    {
        return _js.InvokeAsync<T>(identifier, args);
    }

    public ValueTask InvokeVoidAsync(string identifier, params object?[]? args)
    {
        return _js.InvokeVoidAsync(identifier, args);
    }
}
