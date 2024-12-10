using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DockBar.DockItem.Structs;
using MessagePack;

namespace DockBar.DockItem.Internals
{
    internal sealed class DockItemService : IDockItemService
    {
        private Dictionary<string, IDockItem> Items { get; } = [];

        public IReadOnlyCollection<IDockItem> DockItems => Items.Values;

        public event EventHandler<DockItemChangedEventArgs>? DockItemChanged;

        public void AddDockLinkItem(string key, string linkPath)
        {
            if (Items.ContainsKey(key) is false)
            {
                var item = IDockItem.CreateDockItem(key, linkPath);
                Items.Add(key, item);
                DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = item, IsAdd = true });
            }
        }

        public void AddDockItem(IDockItem dockItem)
        {
            if (Items.ContainsKey(dockItem.Key) is false)
            {
                Items.Add(dockItem.Key, dockItem);
                DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = dockItem, IsAdd = true });
            }
        }

        public void RemoveDockItem(string key)
        {
            if (Items.TryGetValue(key, out IDockItem? item) && Items.Remove(key))
            {
                DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = item, IsAdd = false });
            }
        }

        public IDockItem? GetDockItem(string key)
        {
            if (Items.TryGetValue(key, out IDockItem? item))
            {
                return item;
            }
            return null;
        }

        public void ReadData(string filePath)
        {
            using FileStream? fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Read);
            var items = MessagePackSerializer.Typeless.Deserialize(fs) as IDockItem[];
            if (items is not null)
                foreach (var item in items)
                    AddDockItem(item);
        }

        public void SaveData(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
            MessagePackSerializer.Typeless.Serialize(fs, Items.Values.ToArray());
        }
    }
}
