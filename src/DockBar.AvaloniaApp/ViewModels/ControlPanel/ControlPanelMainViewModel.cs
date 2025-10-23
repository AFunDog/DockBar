using System;

namespace DockBar.AvaloniaApp.ViewModels.ControlPanel;

public sealed partial class ControlPanelMainViewModel : ViewModelBase
{
    public Version AppVersion => Program.AppVersion;
}
