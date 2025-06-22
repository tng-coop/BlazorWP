using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace BlazorWP.Pages;

public partial class Edit
{
    private DotNetObjectReference<Edit>? _scrollRef;
    private ElementReference scrollAnchor;

    [JSInvokable]
    public async Task OnIntersection()
    {
        await PullMore();
    }

    private async Task ObserveScrollAsync()
    {
        if (!showTable) return;
        _scrollRef ??= DotNetObjectReference.Create(this);
        await JS.InvokeVoidAsync("infiniteScroll.observe", scrollAnchor, _scrollRef);
    }

    private async Task DisconnectScrollAsync()
    {
        await JS.InvokeVoidAsync("infiniteScroll.disconnect");
        if (_scrollRef != null)
        {
            _scrollRef.Dispose();
            _scrollRef = null;
        }
    }
}
