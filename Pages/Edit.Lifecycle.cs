using System.Text.Json;
using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    protected override async Task OnInitializedAsync()
    {
        var draftsJson = await JS.InvokeAsync<string?>("localStorage.getItem", DraftsKey);
        if (!string.IsNullOrEmpty(draftsJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<DraftInfo>>(draftsJson);
                var latest = list?.OrderByDescending(d => d.LastUpdated).FirstOrDefault();
                if (latest != null)
                {
                    postId = latest.PostId;
                    postTitle = latest.Title ?? string.Empty;
                    _content = latest.Content ?? string.Empty;
                    lastSavedTitle = postTitle;
                    lastSavedContent = _content;
                }
            }
            catch
            {
                // ignore deserialization errors
            }
        }

        var trashedSetting = await JS.InvokeAsync<string?>("localStorage.getItem", ShowTrashedKey);
        if (!string.IsNullOrEmpty(trashedSetting) && bool.TryParse(trashedSetting, out var trashed))
        {
            showTrashed = trashed;
        }

        await SetupWordPressClientAsync();
        currentPage = 1;
        hasMore = true;
        await LoadPosts(currentPage);
        UpdateDirty();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            mediaSources = await JwtService.GetSiteInfoKeysAsync();
            selectedMediaSource = await JS.InvokeAsync<string?>("localStorage.getItem", "mediaSource");
            if (!string.IsNullOrEmpty(selectedMediaSource))
            {
                await JS.InvokeVoidAsync("setTinyMediaSource", selectedMediaSource);
            }
            StateHasChanged();
        }
        await ObserveScrollAsync();
    }

    private async Task SetupWordPressClientAsync()
    {
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            client = null;
            baseUrl = null;
            return;
        }

        baseUrl = endpoint.TrimEnd('/') + "/wp-json/";
        client = new WordPressClient(baseUrl);
        var token = await JwtService.GetCurrentJwtAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.Auth.SetJWToken(token);
        }
    }
}
