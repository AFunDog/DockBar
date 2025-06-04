using System;
using System.Collections.ObjectModel;

namespace DockBar.AvaloniaApp.Structs;

public sealed class ObservableCollectionWrapper<T, TSource> : ObservableCollection<T>
{
    public required ObservableCollection<TSource> Source
    {
        get => field;
        set
        {
            if (field == value)
                return;
            field = value;
        }
    }

    public required Converter<T, TSource> Converter { get; set; }
}