using System.Text.Json;
using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    private async Task LoadPosts(int page = 1, bool append = false)
    {
        if (page == 1 && !append)
        {
            posts.Clear();
        }
        if (client == null)
        {
            return;
        }

        var qb = new PostsQueryBuilder
        {
            Context = Context.Edit,
            Page = 1,
            PerPage = page == 1 && !append ? 10 : 20,
            Offset = page == 1 && !append ? 0 : posts.Count,
            Embed = true,
            Statuses = new List<Status> { Status.Publish, Status.Private, Status.Draft, Status.Pending, Status.Future, Status.Trash }
        };

        try
        {
            var list = await client.Posts.QueryAsync(qb, useAuth: true);
            int count = 0;
            foreach (var p in list)
            {
                posts.Add(new PostSummary
                {
                    Id = p.Id,
                    Title = p.Title?.Rendered ?? string.Empty,
                    Author = p.Author,
                    AuthorName = p.Embedded?.Author?.FirstOrDefault()?.Name,
                    Status = p.Status.ToString().ToLowerInvariant(),
                    Date = p.Date
                });
                count++;
            }
            hasMore = count > 0;
        }
        catch
        {
            hasMore = false;
        }

        showRetractReview = posts?.FirstOrDefault(p => p.Id == postId)?.Status == "pending";
        await InvokeAsync(StateHasChanged);
    }

    private async Task PullMore()
    {
        if (isLoading || !hasMore) return;
        isLoading = true;
        currentPage++;
        await LoadPosts(currentPage, append: true);
        isLoading = false;
    }

    private async Task<bool> TryLoadDraftAsync(int? id)
    {
        var list = await LoadDraftStatesAsync();
        var item = list.FirstOrDefault(d => d.PostId == id);
        if (item != null)
        {
            postId = item.PostId;
            postTitle = item.Title ?? string.Empty;
            _content = item.Content ?? string.Empty;
            lastSavedTitle = postTitle;
            lastSavedContent = _content;
            return true;
        }
        return false;
    }

    private async Task LoadPostFromServerAsync(int id)
    {
        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        try
        {
            var post = await client.Posts.GetByIDAsync(id, true, true);
            postId = id;
            postTitle = post.Title?.Raw ?? string.Empty;
            _content = post.Content?.Raw ?? string.Empty;
            lastSavedTitle = postTitle;
            lastSavedContent = _content;
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }
    }

    private async Task OpenPost(PostSummary post)
    {
        if (post.Id == postId)
        {
            return;
        }

        if (isDirty)
        {
            await SaveLocalDraftAsync();
        }

        if (!await TryLoadDraftAsync(post.Id))
        {
            if (post.Id > 0)
            {
                await LoadPostFromServerAsync(post.Id);
            }
            else
            {
                postId = null;
                postTitle = string.Empty;
                _content = string.Empty;
                lastSavedTitle = postTitle;
                lastSavedContent = _content;
            }
        }

        showRetractReview = post.Status == "pending";
        UpdateDirty();
        await InvokeAsync(StateHasChanged);
    }

    private async Task RefreshPosts()
    {
        currentPage = 1;
        hasMore = true;
        await DisconnectScrollAsync();
        await LoadPosts(currentPage);
        await ObserveScrollAsync();
    }
}
