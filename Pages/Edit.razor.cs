using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Pages;

public partial class Edit : IAsyncDisposable
{
    private const string DraftsKey = "editorDrafts";
    private const string ShowTrashedKey = "showTrashed";

    private string? status;
    private string postTitle = string.Empty;
    private string lastSavedTitle = string.Empty;
    private string lastSavedContent = string.Empty;
    private bool isDirty = false;
    private bool showRetractReview = false;
    private List<string> mediaSources = new();
    private string? selectedMediaSource;
    private List<PostSummary> posts = new();
    private bool hasMore = true;
    private int currentPage = 1;
    private bool isLoading = false;
    private string _content = "<p>Hello, world!</p>";
    private bool showControls = true;
    private bool showTable = true;
    private bool showTrashed = false;
    private readonly string[] availableStatuses = new[] { "draft", "pending", "publish", "private", "trash" };
    private WordPressClient? client;
    private string? baseUrl;

    private IEnumerable<PostSummary> DisplayPosts
    {
        get
        {
            var list = new List<PostSummary>();

            if (postId == null)
            {
                var title = string.IsNullOrWhiteSpace(postTitle)
                    ? "(Not saved yet)"
                    : $"{postTitle} (not saved yet)";
                list.Add(new PostSummary { Id = -1, Title = title, Author = 0, AuthorName = string.Empty });
            }
            else
            {
                var current = posts.FirstOrDefault(p => p.Id == postId);
                if (current != null)
                {
                    list.Add(current);
                }
            }

            foreach (var p in posts)
            {
                if (postId != null && p.Id == postId)
                {
                    continue;
                }
                if (!showTrashed && string.Equals(p.Status, "trash", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
                list.Add(p);
            }

            return list;
        }
    }

    private class DraftInfo
    {
        public int? PostId { get; set; }
        public string? Title { get; set; }
        public string? Content { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    private class PostSummary
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int Author { get; set; }
        public string? AuthorName { get; set; }
        public string? Status { get; set; }
        public DateTime? Date { get; set; }
    }

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

    }

    private int? postId;

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

    private async Task SaveDraft()
    {
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
    }

    private async Task SubmitForReview()
    {
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
    }

    private async Task RetractReview()
    {
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
    }

    private async Task CloseEditor()
    {
        postId = null;
        postTitle = string.Empty;
        _content = string.Empty;
        lastSavedTitle = string.Empty;
        lastSavedContent = string.Empty;
        showRetractReview = false;
        var list = await LoadDraftStatesAsync();
        list.RemoveAll(d => d.PostId == postId);
        await SaveDraftStatesAsync(list);
        UpdateDirty();
        await InvokeAsync(StateHasChanged);
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
            Page = page,
            PerPage = 100,
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
        await JS.InvokeVoidAsync("setTinyMediaSource", selectedMediaSource);
    }

    private void OnTitleChanged()
    {
        UpdateDirty();
    }

    private void OnContentChanged()
    {
        UpdateDirty();
    }

    private void UpdateDirty()
    {
        isDirty = postTitle != lastSavedTitle || _content != lastSavedContent;
    }

    private void ToggleControls()
    {
        showControls = !showControls;
    }

    private void ToggleTable()
    {
        showTable = !showTable;
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
            }
            else
            {
                var postUpdate = new Post { Id = post.Id, Status = Enum.TryParse<Status>(newStatus, true, out var st) ? st : Status.Draft };
                await client.Posts.UpdateAsync(postUpdate);
                status = $"Status changed to {newStatus}";
                post.Status = newStatus;
            }
            await InvokeAsync(StateHasChanged);
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }
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



    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
