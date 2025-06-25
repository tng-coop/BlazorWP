using System.Text.Json;
using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    private async Task OnMediaSourceChanged()
    {
        if (string.IsNullOrEmpty(selectedMediaSource))
        {
            await JS.InvokeVoidAsync("localStorage.removeItem", "mediaSource");
        }
        else
        {
            await JS.InvokeVoidAsync("localStorage.setItem", "mediaSource", selectedMediaSource);
        }
        //Console.WriteLine($"[OnMediaSourceChanged] source='{selectedMediaSource}'");
        await JS.InvokeVoidAsync("setTinyMediaSource", selectedMediaSource);
    }

    private void OnTitleChanged()
    {
        //Console.WriteLine($"[OnTitleChanged] length={postTitle.Length}");
        UpdateDirty();
    }

    [JSInvokable]
    public async Task OnEditorBlur()
    {
        await SaveLocalDraftAsync();
        lastSavedContent = _content;
        UpdateDirty();
    }

    [JSInvokable]
    public void OnEditorDirty()
    {
        isDirty = true;
    }

    private void UpdateDirty()
    {
        isDirty = postTitle != lastSavedTitle || _content != lastSavedContent;
        //Console.WriteLine($"[UpdateDirty] isDirty={isDirty}");
    }

    private void ToggleControls()
    {
        showControls = !showControls;
    }

    private async Task ToggleTable()
    {
        showTable = !showTable;
        if (showTable)
        {
            await ObserveScrollAsync();
        }
        else
        {
            await DisconnectScrollAsync();
        }
    }

    private async Task OnShowTrashedChanged()
    {
        await JS.InvokeVoidAsync("localStorage.setItem", ShowTrashedKey, showTrashed.ToString().ToLowerInvariant());
    }

    private async Task ChangeStatus(PostSummary post, string newStatus)
    {
        if (post.Id <= 0) return;

        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        try
        {
            if (string.Equals(newStatus, "trash", StringComparison.OrdinalIgnoreCase))
            {
                await client.Posts.DeleteAsync(post.Id, true);
                status = "Post moved to trash";
                posts.Remove(post);
                if (postsTable != null)
                {
                    await postsTable.RefreshAsync();
                }
            }
            else
            {
                var postUpdate = new Post { Id = post.Id, Status = Enum.TryParse<Status>(newStatus, true, out var st) ? st : Status.Draft };
                await client.Posts.UpdateAsync(postUpdate);
                status = $"Status changed to {newStatus}";
                post.Status = newStatus;
            }
            if (postsTable != null)
            {
                await postsTable.RefreshAsync();
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }
    }

    private async Task NewPost()
    {
        if (postId != null)
        {
            if (isDirty)
            {
                await SaveLocalDraftAsync();
            }
        }
        else
        {
            var list = await LoadDraftStatesAsync();
            list.RemoveAll(d => d.PostId == null);
            await SaveDraftStatesAsync(list);
        }

        if (!await TryLoadDraftAsync(null))
        {
            ResetEditorState();
            await SetEditorContentAsync(_content);
        }

        showRetractReview = false;
        UpdateDirty();
        await InvokeAsync(StateHasChanged);
    }

    private static string GetStatusButtonClass(string? status)
    {
        return status switch
        {
            "draft" => "btn-outline-secondary",
            "pending" => "btn-outline-warning",
            "publish" => "btn-outline-success",
            "private" => "btn-outline-dark",
            "trash" => "btn-outline-danger",
            _ => "btn-outline-secondary",
        };
    }
}
