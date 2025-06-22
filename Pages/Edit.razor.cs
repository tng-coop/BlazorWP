using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components;

namespace BlazorWP.Pages;

public partial class Edit : IAsyncDisposable
{
    private const string DraftKey = "currentDraft";

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
    private bool canChangeStatus = false;
    private readonly string[] availableStatuses = new[] { "draft", "pending", "publish", "private" };

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
    }

    private class PostSummary
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public int Author { get; set; }
        public string? AuthorName { get; set; }
        public string? Status { get; set; }
    }

    protected override async Task OnInitializedAsync()
    {
        var json = await JS.InvokeAsync<string?>("localStorage.getItem", DraftKey);
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                var info = JsonSerializer.Deserialize<DraftInfo>(json);
                if (info != null)
                {
                    postId = info.PostId;
                    postTitle = info.Title ?? string.Empty;
                    _content = info.Content ?? string.Empty;
                    lastSavedTitle = postTitle;
                    lastSavedContent = _content;
                }
            }
            catch
            {
                // ignore deserialization errors
            }
        }

        currentPage = 1;
        hasMore = true;
        await LoadPosts(currentPage);
        UpdateDirty();
        await LoadUserRoles();
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
            await LoadUserRoles();
            StateHasChanged();
        }

    }

    private int? postId;

    private async Task SaveDraft()
    {
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            status = "No WordPress endpoint configured.";
            await SaveLocalDraftAsync();
            return;
        }

        var title = string.IsNullOrWhiteSpace(postTitle)
            ? $"Draft {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : postTitle;
        var payload = new
        {
            title,
            content = _content,
            status = "draft"
        };

        var url = postId == null
            ? CombineUrl(endpoint, "/wp/v2/posts")
            : CombineUrl(endpoint, $"/wp/v2/posts/{postId}");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Http.PostAsJsonAsync(url, payload, cancellationToken: cts.Token);
            if (response.IsSuccessStatusCode)
            {
                if (postId == null)
                {
                    var json = await response.Content.ReadAsStringAsync(cts.Token);
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        postId = doc.RootElement.GetProperty("id").GetInt32();
                    }
                    catch
                    {
                        // ignore parse errors
                    }
                    status = "Draft created";
                }
                else
                {
                    status = "Draft updated";
                }
            }
            else
            {
                status = $"Error: {response.StatusCode}";
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
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            status = "No WordPress endpoint configured.";
            await SaveLocalDraftAsync();
            return;
        }

        var title = string.IsNullOrWhiteSpace(postTitle)
            ? $"Draft {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"
            : postTitle;
        var payload = new
        {
            title,
            content = _content,
            status = "pending"
        };

        var url = postId == null
            ? CombineUrl(endpoint, "/wp/v2/posts")
            : CombineUrl(endpoint, $"/wp/v2/posts/{postId}");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Http.PostAsJsonAsync(url, payload, cancellationToken: cts.Token);
            if (response.IsSuccessStatusCode)
            {
                if (postId == null)
                {
                    var json = await response.Content.ReadAsStringAsync(cts.Token);
                    try
                    {
                        using var doc = JsonDocument.Parse(json);
                        postId = doc.RootElement.GetProperty("id").GetInt32();
                    }
                    catch
                    {
                        // ignore parse errors
                    }
                    status = "Submitted for review";
                }
                else
                {
                    status = "Updated and submitted for review";
                }
            }
            else
            {
                status = $"Error: {response.StatusCode}";
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
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        if (postId == null)
        {
            status = "No post to retract.";
            return;
        }

        var payload = new { status = "draft" };
        var url = CombineUrl(endpoint, $"/wp/v2/posts/{postId}");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Http.PostAsJsonAsync(url, payload, cancellationToken: cts.Token);
            if (response.IsSuccessStatusCode)
            {
                status = "Review request retracted";
                showRetractReview = false;
                currentPage = 1;
                hasMore = true;
                await LoadPosts(currentPage);
            }
            else
            {
                status = $"Error: {response.StatusCode}";
            }
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
        await JS.InvokeVoidAsync("localStorage.removeItem", DraftKey);
        UpdateDirty();
        await InvokeAsync(StateHasChanged);
    }

    private async Task SaveLocalDraftAsync()
    {
        var info = new DraftInfo
        {
            PostId = postId,
            Title = postTitle,
            Content = _content
        };
        var json = JsonSerializer.Serialize(info);
        await JS.InvokeVoidAsync("localStorage.setItem", DraftKey, json);
    }

    private async Task LoadPosts(int page = 1, bool append = false)
    {
        if (page == 1 && !append)
        {
            posts.Clear();
        }
        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            return;
        }

        var url = CombineUrl(endpoint, $"/wp/v2/posts?context=edit&status=any&page={page}&_embed");
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Http.GetAsync(url, cts.Token);
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(json);
                int count = 0;
                foreach (var el in doc.RootElement.EnumerateArray())
                {
                    var id = el.GetProperty("id").GetInt32();
                    var title = el.GetProperty("title").GetProperty("rendered").GetString();
                    var postStatus = el.TryGetProperty("status", out var st) ? st.GetString() : null;
                    var author = el.TryGetProperty("author", out var au) ? au.GetInt32() : 0;
                    string? authorName = null;
                    if (el.TryGetProperty("_embedded", out var emb) &&
                        emb.TryGetProperty("author", out var authors) &&
                        authors.ValueKind == JsonValueKind.Array &&
                        authors.GetArrayLength() > 0)
                    {
                        var a = authors[0];
                        authorName = a.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                    }
                    posts.Add(new PostSummary { Id = id, Title = title, Author = author, AuthorName = authorName, Status = postStatus });
                    count++;
                }
                hasMore = count > 0;
            }
            else
            {
                hasMore = false;
            }
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

    private async Task OnMediaSourceChanged(ChangeEventArgs e)
    {
        selectedMediaSource = e.Value?.ToString();
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

    private void TitleInput(ChangeEventArgs e)
    {
        postTitle = e.Value?.ToString() ?? string.Empty;
        UpdateDirty();
    }

    private void ContentChanged(string value)
    {
        _content = value;
        UpdateDirty();
    }

    private void UpdateDirty()
    {
        isDirty = postTitle != lastSavedTitle || _content != lastSavedContent;
    }

    private async Task ChangeStatus(PostSummary post, string newStatus)
    {
        if (post.Id <= 0) return;

        var endpoint = await JS.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            status = "No WordPress endpoint configured.";
            return;
        }

        var payload = new { status = newStatus };
        var url = CombineUrl(endpoint, $"/wp/v2/posts/{post.Id}");

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var response = await Http.PostAsJsonAsync(url, payload, cancellationToken: cts.Token);
            if (response.IsSuccessStatusCode)
            {
                status = $"Status changed to {newStatus}";
                post.Status = newStatus;
            }
            else
            {
                status = $"Error: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            status = $"Error: {ex.Message}";
        }
    }

    private async Task LoadUserRoles()
    {
        var roles = await JwtService.GetCurrentUserRolesAsync();
        canChangeStatus = roles.Any(r =>
            string.Equals(r, "administrator", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(r, "editor", StringComparison.OrdinalIgnoreCase));
    }

    private static string CombineUrl(string site, string path)
    {
        path = NormalizePath(path);
        var trimmed = site.TrimEnd('/');

        if (trimmed.EndsWith("/wp-json/wp/v2", StringComparison.OrdinalIgnoreCase) &&
            path.StartsWith("/wp/v2", StringComparison.OrdinalIgnoreCase))
        {
            var baseUrl = trimmed[..^"/wp/v2".Length];
            return baseUrl.TrimEnd('/') + path;
        }

        if (trimmed.EndsWith("/wp-json", StringComparison.OrdinalIgnoreCase) &&
            path.StartsWith("/wp/v2", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed + path;
        }

        if (!trimmed.Contains("/wp-json", StringComparison.OrdinalIgnoreCase) &&
            path.StartsWith("/wp/v2", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed + "/wp-json" + path;
        }

        return trimmed + (path.StartsWith("/") ? path : "/" + path);
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        if (path.StartsWith("~/"))
        {
            path = path[1..];
        }

        if (path.StartsWith("/routes/", StringComparison.OrdinalIgnoreCase))
        {
            path = path["/routes".Length..];
        }
        else if (path.StartsWith("routes/", StringComparison.OrdinalIgnoreCase))
        {
            path = "/" + path["routes/".Length..];
        }

        var idx = path.IndexOf("/routes/", StringComparison.OrdinalIgnoreCase);
        if (idx >= 0)
        {
            var prefix = path[..idx];
            var after = path[(idx + "/routes".Length)..];
            if (after.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                path = prefix + after[prefix.Length..];
            }
        }

        return path;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}
