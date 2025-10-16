using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DockBar.AvaloniaApp.Structs;

public sealed partial class DockItemViewData : ObservableObject
{
    [ObservableProperty]
    public partial Guid Id { get; set; }
    
    [ObservableProperty]
    public partial string? ShowName { get; set; } 
    
    [ObservableProperty]
    public partial byte[]? IconData { get; set; }
}
