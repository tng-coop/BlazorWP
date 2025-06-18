namespace BlazorWP.Data;

public class AntJsonNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public List<AntJsonNode> Children { get; set; } = new();
}
