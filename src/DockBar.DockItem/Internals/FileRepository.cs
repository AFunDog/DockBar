using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using DockBar.DockItem.Contacts;
using MessagePack;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;

namespace DockBar.DockItem.Internals;

public class FileRepository<T> : IRepository<T>, IDisposable where T : IRepositoryItem
{
    private ILogger Logger { get; }
    protected Dictionary<Guid, T> ItemTable { get; } = [];

    public event EventHandler<RepositoryItemChangedEventArgs<T>>? ItemsChanged;

    public string? FilePath { get; set; }
    public bool IsAutoSave { get; set; } = true;

    private IDisposable? SaveObservable { get; set; }
    private bool IsLoading { get; set; } = false;

    public FileRepository(ILogger logger)
    {
        Logger = logger.ForContext<FileRepository<T>>();
        // Configuration = configuration;

        SaveObservable = Observable
            .FromEventPattern<RepositoryItemChangedEventArgs<T>>(h => ItemsChanged += h, h => ItemsChanged -= h)
            .Select(x => Unit.Default)
            .Where(x => IsAutoSave && !IsLoading)
            .Throttle(TimeSpan.FromSeconds(0.1))
            .Subscribe(ObservableSave);

        Task.Run(async () => await Load());
        
        async void ObservableSave(Unit x)
        {
            try
            {
                await Save();
            }
            catch (Exception e)
            {
                Logger.Trace().Error(e, "");
            }
        }
    }


    public async Task Load(CancellationToken cancellationToken = default)
    {
        if (IsLoading || string.IsNullOrEmpty(FilePath))
            return;
        IsLoading = true;
        Logger.Trace().Information("开始加载数据 {Path}", FilePath);
        try
        {
            if (File.Exists(FilePath) is false)
                return;
            using var fs = File.OpenRead(FilePath);
            var data = await MessagePackSerializer.DeserializeAsync<T[]>(fs, cancellationToken: cancellationToken);
            if (data.Length is 0)
                return;
            
            await InsertOrUpdate(data, cancellationToken);
            Logger.Trace().Information("加载数据完成 {Path}", FilePath);
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task Save(CancellationToken cancellationToken = default)
    {
        if (IsLoading || string.IsNullOrEmpty(FilePath))
            return;
        Logger.Trace().Information("开始保存数据 {Path}", FilePath);
        try
        {
            // if (Directory.CreateDirectory() is false)
            //     return;
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath) ?? string.Empty);
            using var fs = File.OpenWrite(FilePath);
            await MessagePackSerializer.SerializeAsync(fs, ItemTable.Values.ToArray(), cancellationToken: cancellationToken);
            Logger.Trace().Information("保存数据完成 {Path}", FilePath);
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e,"");
        }
        

        // return StorageService.SaveData(
        //     FilePath,
        //     JsonSerializer.SerializeToUtf8Bytes(ItemTable.Values.ToArray(), JsonTypeInfo),
        //     cancellationToken
        // );
    }

    public Task<IEnumerable<T>> SelectAll(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<T>>(ItemTable.Values);
    }

    public Task<IReadOnlyDictionary<Guid, T>> SelectAllToTable(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, T>>(ItemTable);
    }

    public Task<T?> Select(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<T?>(ItemTable.GetValueOrDefault(id));
    }

    public Task<bool> Insert(T item, CancellationToken cancellationToken = default)
    {
        var res = ItemTable.TryAdd(item.Id, item);
        if (res)
        {
            ItemsChanged?.Invoke(
                this,
                new RepositoryItemChangedEventArgs<T>() { Type = RepositoryItemChangedTypeEnum.Insert, Item = item }
            );
        }

        return Task.FromResult<bool>(res);
    }

    public Task<int> Insert(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        var itemList = items.ToArray();
        return Task.FromResult<int>(itemList.Count(x => Insert(x, cancellationToken).Result));
    }

    public Task<bool> InsertOrUpdate(T item, CancellationToken cancellationToken = default)
    {
        var contain = ItemTable.ContainsKey(item.Id);
        ItemTable[item.Id] = item;
        ItemsChanged?.Invoke(
            this,
            contain
                ? new RepositoryItemChangedEventArgs<T>() { Type = RepositoryItemChangedTypeEnum.Update, Item = item }
                : new RepositoryItemChangedEventArgs<T>() { Type = RepositoryItemChangedTypeEnum.Insert, Item = item }
        );

        return Task.FromResult<bool>(true);
    }

    public Task<int> InsertOrUpdate(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        var itemList = items.ToArray();
        return Task.FromResult<int>(itemList.Count(x => InsertOrUpdate(x, cancellationToken).Result));
    }

    public Task<bool> Update(T item, CancellationToken cancellationToken = default)
    {
        if (ItemTable.TryGetValue(item.Id, out var value))
        {
            ItemTable[item.Id] = item;
            ItemsChanged?.Invoke(
                this,
                new RepositoryItemChangedEventArgs<T>() { Type = RepositoryItemChangedTypeEnum.Update, Item = item }
            );
            return Task.FromResult<bool>(true);
        }

        return Task.FromResult<bool>(false);
    }

    public Task<int> Update(Predicate<T> where, Func<T, T> update, CancellationToken cancellationToken = default)
    {
        var updateList = ItemTable.Values.Where(x => where(x)).ToArray();
        return Task.FromResult<int>(updateList.Count(x => Update(update(x), cancellationToken).Result));
    }

    public Task<bool> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        var res = ItemTable.Remove(id, out var item);
        if (res && item is not null)
        {
            ItemsChanged?.Invoke(
                this,
                new RepositoryItemChangedEventArgs<T>() { Type = RepositoryItemChangedTypeEnum.Delete, Item = item }
            );
        }

        return Task.FromResult(res);
    }

    public Task<int> Delete(Predicate<T> where, CancellationToken cancellationToken = default)
    {
        var deleteList = ItemTable.Values.Where(x => where(x)).ToArray();
        return Task.FromResult<int>(deleteList.Count(x => Delete(x.Id, cancellationToken).Result));
    }

    public void Dispose()
    {
        SaveObservable?.Dispose();
        SaveObservable = null;
    }
}