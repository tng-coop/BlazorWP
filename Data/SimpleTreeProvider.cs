using PanoramicData.Blazor.Models;
using PanoramicData.Blazor.Services;

namespace BlazorWP.Data;

public class SimpleTreeProvider : IDataProviderService<TreeItem>
{
    private readonly List<TreeItem> _items = [];

    public SimpleTreeProvider()
    {
        _items.Add(new TreeItem { Id = 1, Name = "Group A", IsGroup = true, Order = 1 });
        _items.Add(new TreeItem { Id = 11, Name = "Alice", ParentId = 1, Order = 1 });
        _items.Add(new TreeItem { Id = 12, Name = "Bob", ParentId = 1, Order = 2 });

        _items.Add(new TreeItem { Id = 2, Name = "Group B", IsGroup = true, Order = 2 });
        _items.Add(new TreeItem { Id = 21, Name = "Carol", ParentId = 2, Order = 1 });
        _items.Add(new TreeItem { Id = 22, Name = "Dave", ParentId = 2, Order = 2 });
    }

    public Task<DataResponse<TreeItem>> GetDataAsync(DataRequest<TreeItem> request, CancellationToken cancellationToken)
        => Task.FromResult(new DataResponse<TreeItem>(_items, _items.Count));

    public Task<OperationResponse> CreateAsync(TreeItem item, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<OperationResponse> DeleteAsync(TreeItem item, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public Task<OperationResponse> UpdateAsync(TreeItem item, IDictionary<string, object?> delta, CancellationToken cancellationToken) =>
        throw new NotImplementedException();

    public void ReOrder(TreeItem item, TreeItem target, bool? before)
    {
        if (item.IsGroup && !target.IsGroup)
        {
            return;
        }

        var source = _items.First(x => x.Id == item.Id);
        var targetItem = _items.First(x => x.Id == target.Id);

        if (item.IsGroup)
        {
            var groups = _items.Where(x => x.IsGroup).OrderBy(x => x.Order).ToList();
            groups.Remove(source);
            var tIdx = groups.IndexOf(targetItem);
            groups.Insert(before == true ? tIdx : tIdx + 1, source);
            for (var i = 0; i < groups.Count; i++)
            {
                groups[i].Order = i + 1;
            }
        }
        else
        {
            if (targetItem.IsGroup && before == null)
            {
                source.ParentId = targetItem.Id;
                var siblings = _items.Where(x => x.ParentId == targetItem.Id).OrderBy(x => x.Order).ToList();
                siblings.Add(source);
                for (var i = 0; i < siblings.Count; i++)
                {
                    siblings[i].Order = i + 1;
                }
            }
            else if (!targetItem.IsGroup)
            {
                var parentId = targetItem.ParentId;
                source.ParentId = parentId;
                var list = _items.Where(x => x.ParentId == parentId).OrderBy(x => x.Order).ToList();
                list.Remove(source);
                var index = list.IndexOf(targetItem);
                list.Insert(before == true ? index : index + 1, source);
                for (var i = 0; i < list.Count; i++)
                {
                    list[i].Order = i + 1;
                }
            }
        }
    }
}
