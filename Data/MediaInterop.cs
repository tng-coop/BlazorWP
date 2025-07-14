using Microsoft.JSInterop;

namespace BlazorWP;

public static class MediaInterop
{
    public static event Action<string>? MediaSelected;

    [JSInvokable]
    public static void OnMediaSelected(string json)
    {
        MediaSelected?.Invoke(json);
    }
}
