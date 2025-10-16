namespace DockBar.DockItem.Contacts;

public enum RepositoryItemChangedTypeEnum
{
    Insert,
    Update,
    Delete
}

public sealed class RepositoryItemChangedEventArgs<T> : EventArgs where T : IRepositoryItem
{
    public required RepositoryItemChangedTypeEnum Type { get; init; }
    public required T Item { get; init; }
}

public interface IRepository<T> : IReadonlyRepository<T> where T : IRepositoryItem
{
    Task<bool> Insert(T item, CancellationToken cancellationToken = default);
    Task<int> Insert(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task<bool> InsertOrUpdate(T item, CancellationToken cancellationToken = default);
    Task<int> InsertOrUpdate(IEnumerable<T> items, CancellationToken cancellationToken = default);
    Task<bool> Update(T item, CancellationToken cancellationToken = default);
    Task<int> Update(Predicate<T> where, Func<T, T> update, CancellationToken cancellationToken = default);
    Task<bool> Delete(Guid id, CancellationToken cancellationToken = default);
    Task<int> Delete(Predicate<T> where, CancellationToken cancellationToken = default);

    Task Save(CancellationToken cancellationToken = default);
    Task Load(CancellationToken cancellationToken = default);
}