using System.Text.Json;
using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    private async Task SaveDraft()
    {
        //Console.WriteLine("[SaveDraft] starting");

        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            await SaveLocalDraftAsync();
            return;
        }

        var title = string.IsNullOrWhiteSpace(postTitle)
            ? $"Draft {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : postTitle;

        try
        {
            if (postId == null)
            {
                var post = new Post
                {
                    Title = new Title(title),
                    Content = new Content(_content),
                    Status = Status.Draft
                };
                var created = await client.Posts.CreateAsync(post);
                postId = created.Id;
                status = "Draft created";
            }
            else
            {
                var post = new Post
                {
                    Id = postId.Value,
                    Title = new Title(title),
                    Content = new Content(_content),
                    Status = Status.Draft
                };
                await client.Posts.UpdateAsync(post);
                status = "Draft updated";
            }
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }

        await SaveLocalDraftAsync();
        lastSavedTitle = postTitle;
        lastSavedContent = _content;
        UpdateDirty();
        showRetractReview = false;
        //Console.WriteLine("[SaveDraft] completed");
    }

    private async Task SubmitForReview()
    {
        //Console.WriteLine("[SubmitForReview] starting");
        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            await SaveLocalDraftAsync();
            return;
        }

        var title = string.IsNullOrWhiteSpace(postTitle)
            ? $"Draft {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : postTitle;

        try
        {
            if (postId == null)
            {
                var post = new Post
                {
                    Title = new Title(title),
                    Content = new Content(_content),
                    Status = Status.Pending
                };
                var created = await client.Posts.CreateAsync(post);
                postId = created.Id;
                status = "Submitted for review";
            }
            else
            {
                var post = new Post
                {
                    Id = postId.Value,
                    Title = new Title(title),
                    Content = new Content(_content),
                    Status = Status.Pending
                };
                await client.Posts.UpdateAsync(post);
                status = "Updated and submitted for review";
            }
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }

        await SaveLocalDraftAsync();
        lastSavedTitle = postTitle;
        lastSavedContent = _content;
        UpdateDirty();
        currentPage = 1;
        hasMore = true;
        await LoadPosts(currentPage);
        showRetractReview = true;
        //Console.WriteLine("[SubmitForReview] completed");
    }

    private async Task RetractReview()
    {
        //Console.WriteLine("[RetractReview] starting");
        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        if (postId == null)
        {
            status = "No post to retract.";
            return;
        }

        try
        {
            var post = new Post { Id = postId.Value, Status = Status.Draft };
            await client.Posts.UpdateAsync(post);
            status = "Review request retracted";
            showRetractReview = false;
            currentPage = 1;
            hasMore = true;
            await LoadPosts(currentPage);
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }
        //Console.WriteLine("[RetractReview] completed");
    }

    private async Task CloseEditor()
    {
        //Console.WriteLine("[CloseEditor] starting");
        var closedId = postId;
        ResetEditorState();
        await SetEditorContentAsync(_content);
        var list = await LoadDraftStatesAsync();
        list.RemoveAll(d => d.PostId == closedId);
        await SaveDraftStatesAsync(list);
        if (closedId != null)
        {
            var placeholder = posts.FirstOrDefault(p => p.Id == closedId && string.IsNullOrEmpty(p.Status));
            if (placeholder != null)
            {
                posts.Remove(placeholder);
                if (postsTable != null)
                {
                    await postsTable.RefreshAsync();
                }
            }
        }
        UpdateDirty();
        await InvokeAsync(StateHasChanged);
        //Console.WriteLine("[CloseEditor] completed");
    }

    private async Task SaveLocalDraftAsync()
    {
        var list = await LoadDraftStatesAsync();
        var existing = list.FirstOrDefault(d => d.PostId == postId);
        if (existing == null)
        {
            existing = new DraftInfo { PostId = postId };
            list.Add(existing);
        }
        existing.Title = postTitle;
        existing.Content = _content;
        existing.LastUpdated = DateTime.UtcNow;
        list = list.OrderByDescending(d => d.LastUpdated).Take(3).ToList();
        await SaveDraftStatesAsync(list);
    }

    private async Task<List<DraftInfo>> LoadDraftStatesAsync()
    {
        var json = await JS.InvokeAsync<string?>("localStorage.getItem", DraftsKey);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var list = JsonSerializer.Deserialize<List<DraftInfo>>(json);
                if (list != null) return list;
            }
            catch { }
        }
        return new List<DraftInfo>();
    }

    private async Task SaveDraftStatesAsync(List<DraftInfo> list)
    {
        var json = JsonSerializer.Serialize(list);
        await JS.InvokeVoidAsync("localStorage.setItem", DraftsKey, json);
    }
}
