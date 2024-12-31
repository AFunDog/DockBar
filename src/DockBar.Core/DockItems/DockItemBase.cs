using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapster;
using MessagePack;

namespace DockBar.Core.DockItems;

public abstract partial class DockItemBase : ObservableObject, IDisposable
{
    public int Key { get; internal set; }

    [ObservableProperty]
    public partial string? ShowName { get; set; }

    [IgnoreMember]
    public virtual MemoryStream? IconDataStream { get; protected set; }

    protected byte[]? IconData
    {
        get => IconDataStream?.ToArray();
        set
        {
            IconDataStream?.Dispose();
            if (value != null)
                IconDataStream = new MemoryStream(value);

            OnPropertyChanged(nameof(IconDataStream));
        }
    }

    public abstract void Start();

    public virtual void Dispose()
    {
        IconDataStream?.Dispose();
        IconDataStream = null;
    }
}
