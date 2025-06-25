using Microsoft.JSInterop;
using WordPressPCL;
using WordPressPCL.Models;
using WordPressPCL.Utility;

namespace BlazorWP.Data;

public class PostService
{
    private readonly JwtService _jwtService;
    private readonly IJSRuntime _js;
    private WordPressClient? _client;
    private string? _baseUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public PostService(JwtService jwtService, IJSRuntime js)
    {
        _jwtService = jwtService;
        _js = js;
    }

    private async Task EnsureClientAsync()
    {
        if (_client != null) return;
        var endpoint = await _js.InvokeAsync<string?>("localStorage.getItem", "wpEndpoint");
        if (string.IsNullOrEmpty(endpoint))
        {
            _client = null;
            _baseUrl = null;
            return;
        }

        _baseUrl = endpoint.TrimEnd('/') + "/wp-json/";
        _client = new WordPressClient(_baseUrl);
        var token = await _jwtService.GetCurrentJwtAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _client.Auth.SetJWToken(token);
        }
    }

    public async Task<List<PostSummary>> GetPostsAsync(int page, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureClientAsync();
            var result = new List<PostSummary>();
            if (_client == null) return result;

            var qb = new PostsQueryBuilder
            {
                Context = Context.Edit,
                Page = page,
                PerPage = page == 1 ? 10 : 20,
                Embed = true,
                Statuses = new List<Status>
                {
                    Status.Publish,
                    Status.Private,
                    Status.Draft,
                    Status.Pending,
                    Status.Future,
                    Status.Trash
                }
            };

            try
            {
                var posts = await _client.Posts.QueryAsync(qb, useAuth: true);
                foreach (var p in posts)
                {
                    result.Add(new PostSummary
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
            }
            catch
            {
                // ignored
            }

            return result;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Post?> GetPostAsync(int id, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            await EnsureClientAsync();
            if (_client == null) return null;
            try
            {
                return await _client.Posts.GetByIDAsync(id, true, true);
            }
            catch
            {
                return null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
