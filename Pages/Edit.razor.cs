using System.Text.Json;
using Microsoft.JSInterop;
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
    private int? postId;

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




    public ValueTask DisposeAsync()
    {
        _ = DisconnectScrollAsync();
        return ValueTask.CompletedTask;
    }
}
