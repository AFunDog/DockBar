using System.Collections.Generic;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace DockBar.AvaloniaApp.Structs;

public class OpenFilePickerMessage : AsyncRequestMessage<IReadOnlyList<IStorageFile>>
{
    public required FilePickerOpenOptions Options { get; set; }
}