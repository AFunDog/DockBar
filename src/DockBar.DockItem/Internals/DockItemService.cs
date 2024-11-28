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
        private readonly List<IDockItem> _dockItems = [];

        public IReadOnlyCollection<IDockItem> DockItems => _dockItems;

        public event EventHandler<DockItemChangedEventArgs>? DockItemChanged;

        public void InsertDockLinkItem(string name, string linkPath, int index = -1)
        {
            if (_dockItems.Any(item => item.Name == name) is false)
            {
                var item = IDockItem.CreateDockItem(name, linkPath);
                if (index == -1)
                    _dockItems.Add(item);
                else
                    _dockItems.Insert(index, item);
                DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = item, IsAdd = true });
            }
        }

        public void RemoveDockLinkItem(string name)
        {
            IDockItem dockitem = _dockItems.First(item => item.Name == name);
            if (dockitem is not null && _dockItems.Remove(dockitem))
            {
                DockItemChanged?.Invoke(this, new DockItemChangedEventArgs { DockItem = dockitem, IsAdd = false });
            }
        }

        public void MoveDockLinkItem(string name, int index)
        {
            var dockitem = _dockItems.First(item => item.Name == name);
            if (dockitem is null)
                return;
            _dockItems.Remove(dockitem);
            _dockItems.Insert(index, dockitem);
        }

        public void ReadData(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var items = MessagePackSerializer.Deserialize<IEnumerable<DockItemData>>(
                    fs,
                    MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Options
                );
                if (items is not null)
                    foreach (var item in items)
                        InsertDockLinkItem(item.Name, item.LinkPath);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }

        public void SaveData(string filePath)
        {
            try
            {
                using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                MessagePackSerializer.Serialize(
                    fs,
                    _dockItems.Select(item => new DockItemData(item.Name, ((DockLinkItem)item).LinkPath)),
                    MessagePack.Resolvers.ContractlessStandardResolverAllowPrivate.Options
                );
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
            }
        }
    }
}
