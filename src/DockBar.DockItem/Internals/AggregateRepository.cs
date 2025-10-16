using System.Collections.Frozen;
using DockBar.DockItem.Contacts;

namespace DockBar.DockItem.Internals;

internal partial class AggregateRepository<T> : IReadonlyRepository<T> where T : IRepositoryItem
{
    public event EventHandler<RepositoryItemChangedEventArgs<T>>? ItemsChanged;

    public Task<IEnumerable<T>> SelectAll(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<T>());
    }

    public Task<IReadOnlyDictionary<Guid, T>> SelectAllToTable(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, T>>(new Dictionary<Guid, T>());
    }

    public Task<T?> Select(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(default);
    }
}