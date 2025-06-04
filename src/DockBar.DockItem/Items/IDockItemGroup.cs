using System.Collections;
using System.Collections.Specialized;

namespace DockBar.DockItem.Items;

public interface IDockItemGroup : IList<int>, IList, INotifyCollectionChanged
{
    // IReadOnlyCollection<int> Keys { get; }
    //
    // /// <summary>
    // /// 在 <paramref name="index"/> 位置插入一个  <paramref name="key"/>
    // /// </summary>
    // /// <param name="key"></param>
    // /// <param name="index">如果为-1，那么默认插入在末尾</param>
    // void Insert(int key, int index = -1);
    //
    // void Remove(int key);
    // void Move(int key, int index);
}