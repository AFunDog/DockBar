using System.Collections.Generic;

namespace DockBar.AvaloniaApp.Structs;

public record AddDockItemMessage(string Name,byte[] IconData,string ParentPath,int Index,string Type,IReadOnlyDictionary<string,string> Metadata);
