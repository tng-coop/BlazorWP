namespace BlazorWP.Data;

public class PostSummary
{
    public int Id { get; set; }
    public string? Title { get; set; }
    public int Author { get; set; }
    public string? AuthorName { get; set; }
    public string? Status { get; set; }
    public DateTime? Date { get; set; }
    public string? Content { get; set; }
}
