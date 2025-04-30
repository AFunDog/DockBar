using System;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Avalonia.Controls;

namespace DockBar.AvaloniaApp.Contacts;

public interface IGlobalHotKeyManager
{
    bool IsEnabled { get; set; }
    TopLevel? Host { get; set; }
    void Register(string key, uint hotKeyModifiers, uint hotKey);
    void Unregister(string key);
    void Bind(string key, Action action);
    void Unbind(string key,Action? action);

    public static IGlobalHotKeyManager Empty { get; } = new EmptyManager();

    sealed class EmptyManager : IGlobalHotKeyManager
    {
        public bool IsEnabled { get; set; }
        public TopLevel? Host { get; set; }

        public void Register(string key, uint hotKeyModifiers, uint hotKey)
        {
        }

        public void Unregister(string key)
        {
        }

        public void Bind(string key, Action action)
        {
        }

        public void Unbind(string key,Action? action)
        {
        }
    }
}