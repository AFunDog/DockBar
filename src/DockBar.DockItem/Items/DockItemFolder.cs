using System.CodeDom;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using CommunityToolkit.Mvvm.ComponentModel;
using MessagePack;

namespace DockBar.DockItem.Items;

[MessagePackObject(AllowPrivate = true)]
public sealed partial class DockItemFolder : DockItemBase, IDockItemGroup
{
    [Key(nameof(DockItemKeys))]
    private ObservableCollection<int> DockItemKeys { get; set; } = [];

    [IgnoreMember]
    public override IDockItemService DockItemService
    {
        get => field;
        internal set
        {
            if (field == value)
                return;
            field.DockItemUnregistered -= OnDockItemUnregistered;
            field = value;
            field.DockItemUnregistered += OnDockItemUnregistered;
        }
    } = IDockItemService.Empty;

    private void OnDockItemUnregistered(IDockItemService sender, DockItemBase item)
    {
        Remove(item.Key);
    }

    [ObservableProperty]
    [IgnoreMember]
    public override partial bool CanExecute { get; protected set; } = true;

    protected override void ExecuteCore()
    {
    }

    #region IList

    public IEnumerator<int> GetEnumerator() => DockItemKeys.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)DockItemKeys).GetEnumerator();

    public void Add(int item)
    {
        if (DockItemKeys.Contains(item))
            return;
        DockItemKeys.Add(item);
    }

    int IList.Add(object? value)
    {
        if (((IList)DockItemKeys).Contains(value))
            return -1;
        return ((IList)DockItemKeys).Add(value);
    }

    public void Clear()
    {
        DockItemKeys.Clear();
    }

    bool IList.Contains(object? value) => ((IList)DockItemKeys).Contains(value);

    int IList.IndexOf(object? value) => ((IList)DockItemKeys).IndexOf(value);

    void IList.Insert(int index, object? value)
    {
        var oldIndex = ((IList)DockItemKeys).IndexOf(value);
        if (oldIndex != -1)
        {
            DockItemKeys.Move(oldIndex, index);
            return;
        }

        ((IList)DockItemKeys).Insert(index, value);
    }

    void IList.Remove(object? value)
    {
        ((IList)DockItemKeys).Remove(value);
    }

    public bool Contains(int item) => DockItemKeys.Contains(item);

    public void CopyTo(int[] array, int arrayIndex)
    {
        DockItemKeys.CopyTo(array, arrayIndex);
    }

    public bool Remove(int item) => DockItemKeys.Remove(item);

    void ICollection.CopyTo(Array array, int index)
    {
        ((ICollection)DockItemKeys).CopyTo(array, index);
    }

    [IgnoreMember]
    public int Count => DockItemKeys.Count;

    [IgnoreMember]
    bool ICollection.IsSynchronized => ((ICollection)DockItemKeys).IsSynchronized;

    [IgnoreMember]
    object ICollection.SyncRoot => ((ICollection)DockItemKeys).SyncRoot;

    [IgnoreMember]
    public bool IsReadOnly => ((ICollection<int>)DockItemKeys).IsReadOnly;

    object? IList.this[int index]
    {
        get => ((IList)DockItemKeys)[index];
        set => ((IList)DockItemKeys)[index] = value;
    }

    public int IndexOf(int item) => DockItemKeys.IndexOf(item);

    /// <summary>
    /// 默认插入，如果已存在则移动
    /// </summary>
    /// <param name="index"></param>
    /// <param name="item"></param>
    public void Insert(int index, int item)
    {
        var oldIndex = DockItemKeys.IndexOf(item);
        if (oldIndex != -1)
        {
            DockItemKeys.Move(oldIndex, index);
            return;
        }

        DockItemKeys.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        DockItemKeys.RemoveAt(index);
    }

    [IgnoreMember]
    bool IList.IsFixedSize => ((IList)DockItemKeys).IsFixedSize;

    public int this[int index]
    {
        get => DockItemKeys[index];
        set
        {
            var oldIndex = DockItemKeys.IndexOf(value);
            if (oldIndex != -1)
            {
                DockItemKeys.Move(oldIndex, index);
                return;
            }

            DockItemKeys[index] = value;
        }
    }

    public event NotifyCollectionChangedEventHandler? CollectionChanged
    {
        add => DockItemKeys.CollectionChanged += value;
        remove => DockItemKeys.CollectionChanged -= value;
    }

    #endregion
}