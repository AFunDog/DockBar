using System;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Avalonia.Controls;
using DockBar.AvaloniaApp.Contacts;
using DockBar.Core.Contacts;
using DockBar.Core.Structs;
using Serilog;
using Zeng.CoreLibrary.Toolkit.Logging;
using static Windows.Win32.PInvoke;

namespace DockBar.AvaloniaApp.Services;

public partial class GlobalHotKeyManager : IGlobalHotKeyManager
{
    private ILogger Logger { get; }

    private AppSetting AppSetting { get; }

    private sealed class HotKeyData
    {
        public HotKeyInfo HotKey { get; set; }
        public int Id { get; set; } = -1;

        public string Name { get; set; } = string.Empty;
    }

    // private Dictionary<string, HotKeyData> HotKeyTable { get; set; } = [];

    private Dictionary<string, HashSet<Action>> HotKeyActionTable { get; } = [];
    private Dictionary<int, HotKeyData> HotKeyTable { get; } = [];

    private Dictionary<string, HotKeyData> HotKeyNameTable { get; } = [];

    /// <summary>
    /// 下一次获取到的 id 每次获取自动加一
    /// </summary>
    public int NextId
    {
        get
        {
            var id = field;
            field = (id + 1) % int.MaxValue;
            return id;
        }
    }

    public bool IsEnabled { get; set; } = true;

    public TopLevel? Host
    {
        get => field;
        set
        {
            if (field == value)
                return;
            UnAttachTopLevel(field);
            field = value;
            AttachTopLevel(field);
        }
    }

    public GlobalHotKeyManager() : this(Log.Logger, IAppSettingWrapper.Empty)
    {
    }

    public GlobalHotKeyManager(ILogger logger, IAppSettingWrapper appSettingWrapper)
    {
        Logger = logger;
        AppSetting = appSettingWrapper.Data;

        void OnAppSettingPropertyChanged(object? s, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppSetting.IsEnableGlobalHotKey))
            {
                IsEnabled = AppSetting.IsEnableGlobalHotKey;
            }
            else if (e.PropertyName == nameof(AppSetting.KeepMainWindowHotKey))
            {
                if (AppSetting.KeepMainWindowHotKey.IsValid())
                    Register(
                        nameof(AppSetting.KeepMainWindowHotKey),
                        AppSetting.KeepMainWindowHotKey.Modifiers,
                        AppSetting.KeepMainWindowHotKey.Key
                    );
                else
                    Unregister(nameof(AppSetting.KeepMainWindowHotKey));
            }
        }

        AppSetting.PropertyChanged += OnAppSettingPropertyChanged;

        OnAppSettingPropertyChanged(AppSetting, new(nameof(AppSetting.IsEnableGlobalHotKey)));
        OnAppSettingPropertyChanged(AppSetting, new(nameof(AppSetting.KeepMainWindowHotKey)));
    }

    private void UnAttachTopLevel(TopLevel? topLevel)
    {
        if (topLevel is null)
            return;
        Win32Properties.RemoveWndProcHookCallback(topLevel, WndProcHook);
        foreach (var hotKeyInfo in HotKeyTable.Values)
            RemoveHotKeyFromTopLevel(topLevel, hotKeyInfo);
    }

    private void AttachTopLevel(TopLevel? topLevel)
    {
        if (topLevel is null)
            return;
        Win32Properties.AddWndProcHookCallback(topLevel, WndProcHook);
        foreach (var hotKeyInfo in HotKeyTable.Values)
            AddHotKeyToTopLevel(topLevel, hotKeyInfo);
    }

    private void AddHotKeyToTopLevel(TopLevel topLevel, HotKeyData hotKeyData)
    {
        if (!hotKeyData.HotKey.IsValid())
        {
            Logger.Trace().Warning("跳过 {Key} 为 TopLevel {TopLevel} 注册全局热键 因为不可用", hotKeyData.Id, topLevel.GetType().Name);
            return;
        }

        try
        {
            if (topLevel.TryGetPlatformHandle() is { Handle: { } handle })
            {
                if (RegisterHotKey(
                        new(handle),
                        hotKeyData.Id,
                        (HOT_KEY_MODIFIERS)hotKeyData.HotKey.Modifiers,
                        hotKeyData.HotKey.Key
                    ))
                    Logger.Trace().Verbose("为 TopLevel {TopLevel} 注册全局热键", topLevel.GetType().Name);
                else
                    throw new InvalidOperationException("注册热键失败，可能时按键冲突了");
            }
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "为 TopLevel {TopLevel} 注册全局热键失败", topLevel.GetType().Name);
        }
    }

    private void RemoveHotKeyFromTopLevel(TopLevel topLevel, HotKeyData hotKeyData)
    {
        try
        {
            if (topLevel.TryGetPlatformHandle() is { Handle: { } handle })
            {
                UnregisterHotKey(new(handle), hotKeyData.Id);
                HotKeyTable.Remove(hotKeyData.Id);
            }
        }
        catch (Exception e)
        {
            Logger.Trace().Error(e, "为 TopLevel {TopLevel} 注销全局热键失败", topLevel.GetType().Name);
        }
    }

    private nint WndProcHook(
        nint hWnd,
        uint msg,
        nint wParam,
        nint lParam,
        ref bool handled)
    {
        switch (msg)
        {
            case WM_HOTKEY:
                if (IsEnabled && HotKeyTable.TryGetValue((int)wParam, out var hotKeyInfo))
                    if (HotKeyActionTable.TryGetValue(hotKeyInfo.Name, out var actions))
                    {
                        foreach (var action in actions)
                            try
                            {
                                action();
                            }
                            catch (Exception e)
                            {
                                Logger.Trace().Error(e, "执行 {Key} 全局热键时出错 {Name}", hotKeyInfo.Name, action.Method.Name);
                            }

                        handled = true;
                    }

                break;
        }

        return nint.Zero;
    }

    public void Register(string key, uint hotKeyModifiers, uint hotKey)
    {
        // if (HotKeyTable.TryGetValue(id, out var hotKeyInfo))
        // {
        //     hotKeyInfo.ClearBind();
        // }
        if (HotKeyNameTable.TryGetValue(key, out var hotKeyInfo))
        {
            Logger.Trace().Verbose("注册全局热键 {Key} 已存在 HotKeyInfo 先清除再重新注册", key);
            Unregister(key);
        }

        var id = NextId;
        HotKeyTable[id] = new() { Name = key, Id = id, HotKey = new(hotKeyModifiers, hotKey) };
        HotKeyNameTable[key] = HotKeyTable[id];
        if (Host is not null)
            AddHotKeyToTopLevel(Host, HotKeyTable[id]);

        Logger.Trace().Debug("注册全局热键完毕 {Key} {Id} {Mod} {HotKey}", key, id, hotKeyModifiers, hotKey);
    }

    public void Unregister(string key)
    {
        if (HotKeyNameTable.TryGetValue(key, out var hotKeyData))
        {
            if (Host is not null)
                RemoveHotKeyFromTopLevel(Host, hotKeyData);

            HotKeyTable.Remove(hotKeyData.Id);
            Logger.Trace().Debug("注销全局热键完毕 {Key}", key);
        }
        else
        {
            Logger.Trace().Warning("注销全局热键 {Key} 不存在跳过", key);
        }
    }

    public void Bind(string key, Action action)
    {
        if (HotKeyActionTable.TryGetValue(key, out var actions))
        {
            actions.Add(action);
            Logger.Trace().Debug("绑定热键 {Key}", key);
        }
        else
        {
            HotKeyActionTable[key] = [action];
        }
    }

    public void Unbind(string key, Action? action)
    {
        if (HotKeyActionTable.TryGetValue(key, out var actions))
        {
            if (action is not null)
            {
                actions.Remove(action);
                Logger.Trace().Debug("取消一个热键绑定方法 {Key}", key);
            }
            else
            {
                HotKeyActionTable.Remove(key);
                Logger.Trace().Debug("清空热键绑定 {Key}", key);
            }
        }
        else
        {
            Logger.Trace().Warning("取消绑定热键不存在，请先注册 {Key}", key);
        }
    }
}