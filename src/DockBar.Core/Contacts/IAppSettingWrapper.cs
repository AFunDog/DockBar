using DockBar.Core.Structs;

namespace DockBar.Core.Contacts;

public interface IAppSettingWrapper
{
    AppSetting Data { get; }

    void Save(string filePath);

    public static IAppSettingWrapper Empty { get; } = new EmptyWrapper();

    sealed class EmptyWrapper : IAppSettingWrapper
    {
        public AppSetting Data { get; } = new();

        public void Save(string filePath)
        {
        }
    }
}