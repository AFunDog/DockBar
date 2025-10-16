using Serilog;

namespace DockBar.AvaloniaApp.ViewModels;

internal sealed partial class MainViewModel : ViewModelBase
{
    public ILogger Logger { get; }

    public string AppVersion { get; } = Program.AppVersion.ToString();

    public MainViewModel() : this(Log.Logger)
    {
    }

    public MainViewModel(ILogger logger)
    {
        Logger = logger;
    }
}