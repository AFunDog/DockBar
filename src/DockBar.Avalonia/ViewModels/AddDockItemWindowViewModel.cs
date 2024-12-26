using Avalonia.Media;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DockBar.Core;

namespace DockBar.Avalonia.ViewModels;

internal sealed partial class AddDockItemWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DockIcon))]
    private IDockItem? _currentDockItem;

    public IImage? DockIcon
    {
        get => string.IsNullOrEmpty(CurrentDockItem?.IconPath) ? null : new Bitmap(CurrentDockItem.IconPath);
    }
}
