using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;

namespace BlazorWP.Data;

public class TreeDataProvider : IDataProviderService<TreeItem>
{
    private readonly List<TreeItem> _items = new();

    public TreeDataProvider()
    {
        _items.Add(new TreeItem { Id = 2, Name = "Sales", ParentId = null, IsGroup = true, Order = 1 });
        _items.Add(new TreeItem { Id = 11, Name = "Alice", ParentId = 2, Order = 1 });
        _items.Add(new TreeItem { Id = 16, Name = "Fred", ParentId = 2, Order = 2 });
        _items.Add(new TreeItem { Id = 21, Name = "Kevin", ParentId = 2, Order = 3 });
        _items.Add(new TreeItem { Id = 14, Name = "Dave", ParentId = 2, Order = 4 });

        _items.Add(new TreeItem { Id = 3, Name = "Marketing", ParentId = null, IsGroup = true, Order = 2 });
        _items.Add(new TreeItem { Id = 12, Name = "Bob", ParentId = 3, Order = 1 });
        _items.Add(new TreeItem { Id = 13, Name = "Carol", ParentId = 3, Order = 2 });
        _items.Add(new TreeItem { Id = 18, Name = "Harry", ParentId = 3, Order = 3 });

        _items.Add(new TreeItem { Id = 4, Name = "Finance", ParentId = null, IsGroup = true, Order = 3 });
        _items.Add(new TreeItem { Id = 15, Name = "Emma", ParentId = 4, Order = 1 });
        _items.Add(new TreeItem { Id = 17, Name = "Gina", ParentId = 4, Order = 2 });
        _items.Add(new TreeItem { Id = 19, Name = "Ian", ParentId = 4, Order = 3 });
        _items.Add(new TreeItem { Id = 20, Name = "Janet", ParentId = 4, Order = 4 });
    }

    public Task<DataResponse<TreeItem>> GetDataAsync(DataRequest<TreeItem> request, CancellationToken cancellationToken)
        => Task.FromResult(new DataResponse<TreeItem>(_items, _items.Count));

    public Task<OperationResponse> DeleteAsync(TreeItem item, CancellationToken cancellationToken)
        => Task.FromResult(new OperationResponse { Success = true });

    public Task<OperationResponse> UpdateAsync(TreeItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
        => Task.FromResult(new OperationResponse { Success = true });

    public Task<OperationResponse> CreateAsync(TreeItem item, CancellationToken cancellationToken)
        => Task.FromResult(new OperationResponse { Success = true });
}
