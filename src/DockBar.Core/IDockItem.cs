namespace DockBar.Core;

public interface IDockItem
{
    string Key { get; }
    string ShowName { get; }
    string? IconPath { get; }

    void Start();
}
