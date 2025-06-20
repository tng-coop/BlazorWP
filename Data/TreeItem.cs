namespace BlazorWP.Data;

public class TreeItem
{
    public int Id { get; set; }
    public int? ParentId { get; set; }
    public int Order { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsGroup { get; set; }

    public override string ToString() => $"{Id} - {Name} {(IsGroup ? "[Folder]" : "[Person]")} (order {Order})";
}
