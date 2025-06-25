using PanoramicData.Blazor.Interfaces;
using PanoramicData.Blazor.Models;
using BlazorWP.Pages;

namespace BlazorWP.Data;

public class PostSummaryDataProvider : IDataProviderService<Edit.PostSummary>
{
    private readonly Func<List<Edit.PostSummary>> _getItems;

    public PostSummaryDataProvider(Func<List<Edit.PostSummary>> getItems)
    {
        _getItems = getItems;
    }

    public Task<DataResponse<Edit.PostSummary>> GetDataAsync(DataRequest<Edit.PostSummary> request, CancellationToken cancellationToken)
    {
        var items = _getItems();
        return Task.FromResult(new DataResponse<Edit.PostSummary>(items, items.Count));
    }

    public Task<OperationResponse> DeleteAsync(Edit.PostSummary item, CancellationToken cancellationToken)
    {
        var items = _getItems();
        var existing = items.FirstOrDefault(p => p.Id == item.Id);
        if (existing != null)
        {
            items.Remove(existing);
            return Task.FromResult(new OperationResponse { Success = true });
        }
        return Task.FromResult(new OperationResponse { ErrorMessage = "Item not found" });
    }

    public Task<OperationResponse> UpdateAsync(Edit.PostSummary item, IDictionary<string, object?> delta, CancellationToken cancellationToken)
    {
        var items = _getItems();
        var existing = items.FirstOrDefault(p => p.Id == item.Id);
        if (existing != null)
        {
            foreach (var kvp in delta)
            {
                var prop = typeof(Edit.PostSummary).GetProperty(kvp.Key);
                prop?.SetValue(existing, kvp.Value);
            }
            return Task.FromResult(new OperationResponse { Success = true });
        }
        return Task.FromResult(new OperationResponse { ErrorMessage = "Item not found" });
    }

    public Task<OperationResponse> CreateAsync(Edit.PostSummary item, CancellationToken cancellationToken)
    {
        _getItems().Add(item);
        return Task.FromResult(new OperationResponse { Success = true });
    }
}
