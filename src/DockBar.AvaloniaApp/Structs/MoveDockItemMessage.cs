using System;

namespace DockBar.AvaloniaApp.Structs;

internal record MoveDockItemMessage(Guid Id, int TargetIndex);