using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Net.WebSockets;
using MessagePack;
using MessagePack.Formatters;
using ObservableCollections;

namespace DockBar.DockItem.Structs;

public enum DockItemMetadataChangedTypeEnum
{
    Update, Delete
}

public partial class DockItemMetadataChangedEventArgs : EventArgs
{
    public DockItemMetadataChangedTypeEnum DockItemMetadataChangedType { get; }
    public string Key { get; }

    public DockItemMetadataChangedEventArgs(DockItemMetadataChangedTypeEnum dockItemMetadataChangedType, string key)
    {
        DockItemMetadataChangedType = dockItemMetadataChangedType;
        Key = key;
    }
}

[MessagePackObject(AllowPrivate = true)]
public partial class DockItemMetadata : IDictionary<string, string>, IReadOnlyDictionary<string, string>
{
    public event EventHandler<DockItemMetadataChangedEventArgs>? MetadataChanged;

    [IgnoreMember]
    // [Key(0)]
    // [MessagePackFormatter(typeof(DictionaryFormatter<string, string>))]
    private ObservableDictionary<string, string> Metadata
    {
        get => field;
        set
        {
            if (field == value)
                return;
            if (field is not null)
                field.CollectionChanged -= OnMetadataCollectionChanged;
            field = value;
            field.CollectionChanged += OnMetadataCollectionChanged;
        }
    }

    // [Key(0)]
    // private KeyValuePair<string,string>[] MetadataStorage
    // {
    //     get => Metadata.ToArray();
    //     set => Metadata = new(value);
    // }

    [Key(0)]
    [MessagePackFormatter(typeof(DictionaryFormatter<string, string>))]
    private Dictionary<string, string> MetadataStorage
    {
        get => Metadata.ToDictionary();
        set => Metadata = new(value);
    }

    public DockItemMetadata() : this(new Dictionary<string, string>()) { }

    public DockItemMetadata(IReadOnlyDictionary<string, string> metadata)
    {
        Metadata = new(metadata);
    }

    private void OnMetadataCollectionChanged(in NotifyCollectionChangedEventArgs<KeyValuePair<string, string>> e)
    {
        if (e.Action is NotifyCollectionChangedAction.Remove)
        {
            MetadataChanged?.Invoke(
                this,
                new DockItemMetadataChangedEventArgs(DockItemMetadataChangedTypeEnum.Delete, e.OldItem.Key)
            );
        }
        else
        {
            MetadataChanged?.Invoke(
                this,
                new DockItemMetadataChangedEventArgs(DockItemMetadataChangedTypeEnum.Update, e.NewItem.Key)
            );
        }
    }

    #region IDictionary<string,string> IReadOnlyDictionary<string,string>

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Metadata.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Metadata).GetEnumerator();

    public void Add(KeyValuePair<string, string> item)
    {
        ((ICollection<KeyValuePair<string, string>>)Metadata).Add(item);
    }

    public void Clear()
    {
        Metadata.Clear();
    }

    public bool Contains(KeyValuePair<string, string> item) => Metadata.Contains(item);

    public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, string>>)Metadata).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<string, string> item) => Metadata.Remove(item.Key);

    [IgnoreMember]
    public int Count => Metadata.Count;

    [IgnoreMember]
    public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)Metadata).IsReadOnly;

    public void Add(string key, string value)
    {
        Metadata.Add(key, value);
    }

    public bool ContainsKey(string key) => Metadata.ContainsKey(key);

    public bool Remove(string key) => Metadata.Remove(key);

    public bool TryGetValue(string key, [MaybeNullWhen(false)] out string value)
        => Metadata.TryGetValue(key, out value);

    public string this[string key]
    {
        get => Metadata[key];
        set => Metadata[key] = value;
    }

    [IgnoreMember]
    IEnumerable<string> IReadOnlyDictionary<string, string>.Keys => Keys;
    [IgnoreMember]
    IEnumerable<string> IReadOnlyDictionary<string, string>.Values => Values;

    [IgnoreMember]
    public ICollection<string> Keys => ((IDictionary<string, string>)Metadata).Keys;

    [IgnoreMember]
    public ICollection<string> Values => ((IDictionary<string, string>)Metadata).Values;

    #endregion
}