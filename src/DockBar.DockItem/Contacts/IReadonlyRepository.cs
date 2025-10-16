namespace DockBar.DockItem.Contacts;

public interface IReadonlyRepository<T> where T : IRepositoryItem
{
    event EventHandler<RepositoryItemChangedEventArgs<T>> ItemsChanged;
    Task<IEnumerable<T>> SelectAll(CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, T>> SelectAllToTable(CancellationToken cancellationToken = default);
    Task<T?> Select(Guid id, CancellationToken cancellationToken = default);
}