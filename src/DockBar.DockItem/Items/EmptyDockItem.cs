namespace DockBar.DockItem.Items;

internal sealed partial class EmptyDockItem : DockItemBase
{
    public override bool CanExecute { get; protected set; } = false;

    protected override void ExecuteCore()
    {
    }
}