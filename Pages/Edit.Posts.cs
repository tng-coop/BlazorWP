using System.Text.Json;
using Microsoft.JSInterop;
using System.Diagnostics;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit
{
    private async Task LoadPosts(int page = 1, bool append = false)
    {
        //Console.WriteLine($"[LoadPosts] page={page}, append={append}");
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
            Page = page,
            PerPage = page == 1 && !append ? 10 : 20,
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
                    Date = DateTime.SpecifyKind(p.DateGmt, DateTimeKind.Utc).ToLocalTime(),
                    Content = p.Content?.Rendered
                });
                count++;
            }
            //Console.WriteLine($"[LoadPosts] loaded {count} posts");
            hasMore = count > 0;
        }
        catch
        {
            //Console.WriteLine("[LoadPosts] error fetching posts");
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
        //Console.WriteLine($"[TryLoadDraftAsync] id={id}");
        var list = await LoadDraftStatesAsync();
        var item = list.FirstOrDefault(d => d.PostId == id);
        if (item != null)
        {
            //Console.WriteLine("[TryLoadDraftAsync] draft found");
            postId = item.PostId;
            postTitle = item.Title ?? string.Empty;
            _content = item.Content ?? string.Empty;
            //Console.WriteLine($"[TryLoadDraftAsync] loaded draft title length={postTitle.Length}, content length={_content.Length}");
            lastSavedTitle = postTitle;
            lastSavedContent = _content;
            await SetEditorContentAsync(_content);
            if (postId != null && !posts.Any(p => p.Id == postId))
            {
                posts.Add(new PostSummary
                {
                    Id = postId.Value,
                    Title = postTitle,
                    Author = 0,
                    AuthorName = string.Empty,
                    Status = string.Empty,
                    Date = null,
                    Content = _content
                });
            }
            return true;
        }
        //Console.WriteLine("[TryLoadDraftAsync] no draft found");
        return false;
    }

    private async Task LoadPostFromServerAsync(int id)
    {
        //Console.WriteLine($"[LoadPostFromServerAsync] id={id}");
        if (client == null)
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        try
        {
            var post = await client.Posts.GetByIDAsync(id, true, true);
            postId = id;
            postTitle = post.Title?.Rendered ?? string.Empty;
            _content = post.Content?.Rendered ?? string.Empty;
            //Console.WriteLine($"[LoadPostFromServerAsync] loaded title length={postTitle.Length}, content length={_content.Length}");
            lastSavedTitle = postTitle;
            lastSavedContent = _content;
            await SetEditorContentAsync(_content);
            var existing = posts.FirstOrDefault(p => p.Id == id);
            if (existing == null)
            {
                posts.Add(new PostSummary
                {
                    Id = post.Id,
                    Title = post.Title?.Rendered ?? postTitle,
                    Author = post.Author,
                    AuthorName = post.Embedded?.Author?.FirstOrDefault()?.Name ?? string.Empty,
                    Status = post.Status.ToString().ToLowerInvariant(),
                    Date = DateTime.SpecifyKind(post.DateGmt, DateTimeKind.Utc).ToLocalTime(),
                    Content = post.Content?.Rendered
                });
            }
            else
            {
                existing.Title = post.Title?.Rendered ?? postTitle;
                existing.Author = post.Author;
                existing.AuthorName = post.Embedded?.Author?.FirstOrDefault()?.Name ?? string.Empty;
                existing.Status = post.Status.ToString().ToLowerInvariant();
                existing.Date = DateTime.SpecifyKind(post.DateGmt, DateTimeKind.Utc).ToLocalTime();
                existing.Content = post.Content?.Rendered;
            }
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
            //Console.WriteLine($"[LoadPostFromServerAsync] exception: {ex.Message}");
        }
    }

    private async Task OpenPost(PostSummary post, bool forceReload = false)
    {
        var sw = Stopwatch.StartNew();
        //Console.WriteLine($"[OpenPost] click id={post.Id}, title={post.Title}");
        if (post.Id == postId && !forceReload)
        {
            sw.Stop();
            Console.WriteLine($"[Perf] OpenPost({post.Id}) took {sw.ElapsedMilliseconds} ms (noop)");
            return;
        }

        if (isDirty)
        {
            await SaveLocalDraftAsync();
            //Console.WriteLine("[OpenPost] autosaved dirty draft");
        }

        if (!await TryLoadDraftAsync(post.Id))
        {
            if (post.Id > 0)
            {
                if (!forceReload && !string.IsNullOrEmpty(post.Content))
                {
                    postId = post.Id;
                    postTitle = post.Title ?? string.Empty;
                    _content = post.Content;
                    lastSavedTitle = postTitle;
                    lastSavedContent = _content;
                }
                else
                {
                    //Console.WriteLine("[OpenPost] loading from server");
                    await LoadPostFromServerAsync(post.Id);
                }
            }
            else
            {
                //Console.WriteLine("[OpenPost] new empty post");
                ResetEditorState();
                await SetEditorContentAsync(_content);
            }
        }

        showRetractReview = post.Status == "pending";
        UpdateDirty();
        //Console.WriteLine($"[OpenPost] completed. postId={postId}");
        await InvokeAsync(StateHasChanged);
        sw.Stop();
        Console.WriteLine($"[Perf] OpenPost({post.Id}) took {sw.ElapsedMilliseconds} ms");
    }

    private async Task RefreshPosts()
    {
        // Instead of refreshing the UI, fetch the latest posts and compare
        // them with the current list, logging the differences.

        if (client == null)
        {
            return;
        }

        var pagesToLoad = currentPage;
        var statuses = new List<Status>
        {
            Status.Publish,
            Status.Private,
            Status.Draft,
            Status.Pending,
            Status.Future,
            Status.Trash
        };

        var fresh = new List<PostSummary>();

        for (int page = 1; page <= pagesToLoad; page++)
        {
            var qb = new PostsQueryBuilder
            {
                Context = Context.Edit,
                Page = page,
                PerPage = page == 1 ? 10 : 20,
                Embed = true,
                Statuses = statuses
            };

            try
            {
                var list = await client.Posts.QueryAsync(qb, useAuth: true);
                foreach (var p in list)
                {
                    fresh.Add(new PostSummary
                    {
                        Id = p.Id,
                        Title = p.Title?.Rendered ?? string.Empty,
                        Author = p.Author,
                        AuthorName = p.Embedded?.Author?.FirstOrDefault()?.Name,
                        Status = p.Status.ToString().ToLowerInvariant(),
                        Date = DateTime.SpecifyKind(p.DateGmt, DateTimeKind.Utc).ToLocalTime(),
                        Content = p.Content?.Rendered
                    });
                }
                if (list.Count == 0)
                {
                    break;
                }
            }
            catch
            {
                break;
            }
        }

        var existing = posts.ToDictionary(p => p.Id);

        foreach (var post in fresh)
        {
            if (existing.TryGetValue(post.Id, out var old))
            {
                if (old.Title != post.Title ||
                    old.Author != post.Author ||
                    old.AuthorName != post.AuthorName ||
                    old.Status != post.Status ||
                    old.Date != post.Date ||
                    old.Content != post.Content)
                {
                    await JS.InvokeVoidAsync("console.log", $"MODIFIED {post.Id}: {post.Title}");
                }
                existing.Remove(post.Id);
            }
            else
            {
                await JS.InvokeVoidAsync("console.log", $"ADDED {post.Id}: {post.Title}");
            }
        }

        foreach (var removed in existing.Values)
        {
            await JS.InvokeVoidAsync("console.log", $"REMOVED {removed.Id}: {removed.Title}");
        }
    }
}
