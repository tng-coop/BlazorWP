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
        //Console.WriteLine("[OnInitializedAsync] starting");
        var draftsJson = await JS.InvokeAsync<string?>("localStorage.getItem", DraftsKey);
        DraftInfo? latestDraft = null;
        if (!string.IsNullOrEmpty(draftsJson))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<DraftInfo>>(draftsJson);
                latestDraft = list?.OrderByDescending(d => d.LastUpdated).FirstOrDefault();
            }
            catch
            {
                // ignore deserialization errors
            }
        }

        if (latestDraft != null)
        {
            postId = latestDraft.PostId;
            postTitle = latestDraft.Title ?? string.Empty;
            _content = latestDraft.Content ?? string.Empty;
            lastSavedTitle = postTitle;
            lastSavedContent = _content;
            hasPersistedContent = true;
        }
        else
        {
            ResetEditorState();
            hasPersistedContent = false;
        }

        var trashedSetting = await JS.InvokeAsync<string?>("localStorage.getItem", ShowTrashedKey);
        if (!string.IsNullOrEmpty(trashedSetting) && bool.TryParse(trashedSetting, out var trashed))
        {
            showTrashed = trashed;
        }

        await SetupWordPressClientAsync();
        currentPage = 1;
        hasMore = true;
        if (!int.TryParse(selectedRefreshCount, out var initCount))
        {
            initCount = 10;
        }
        await LoadPosts(currentPage, perPageOverride: initCount);
        if (postId != null && !posts.Any(p => p.Id == postId))
        {
            posts.Add(new PostSummary
            {
                Id = postId.Value,
                Title = postTitle,
                Author = 0,
                AuthorName = string.Empty,
                Status = string.Empty,
                Date = null
            });
        }
        UpdateDirty();
        //Console.WriteLine("[OnInitializedAsync] completed");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            //Console.WriteLine("[OnAfterRenderAsync] firstRender");
            mediaSources = await JwtService.GetSiteInfoKeysAsync();
            selectedMediaSource = await JS.InvokeAsync<string?>("localStorage.getItem", "mediaSource");
            if (!string.IsNullOrEmpty(selectedMediaSource))
            {
                await JS.InvokeVoidAsync("setTinyMediaSource", selectedMediaSource);
            }
            await JS.InvokeVoidAsync("initEditSplit");
            StateHasChanged();
        }
    }

    private async Task SetupWordPressClientAsync()
    {
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            client = null;
            baseUrl = null;
            //Console.WriteLine("[SetupWordPressClientAsync] no endpoint configured");
            return;
        }

        baseUrl = endpoint.TrimEnd('/') + "/wp-json/";
        //Console.WriteLine($"[SetupWordPressClientAsync] baseUrl={baseUrl}");
        client = new WordPressClient(baseUrl);
        var token = await JwtService.GetCurrentJwtAsync();
        if (!string.IsNullOrEmpty(token))
        {
            client.Auth.SetJWToken(token);
            //Console.WriteLine("[SetupWordPressClientAsync] token configured");
        }
    }
}
