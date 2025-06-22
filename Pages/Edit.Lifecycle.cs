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
        }
        else
        {
            ResetEditorState();
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
            StateHasChanged();
        }
        else
        {
            //Console.WriteLine("[OnAfterRenderAsync] subsequent render");
            await JS.InvokeVoidAsync("setTinyEditorContent", _content);
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
