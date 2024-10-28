using System;
using BepInEx.Configuration;
using UnityEngine;

namespace CSTI_AutoBackup;

public enum TimerTypes
{
    Hour,
    Tick
}

public static class AutoBackupConfig
{
    public static ConfigEntry<int> KeepCount;
    public static ConfigEntry<TimerTypes> TimerType;
    public  static ConfigEntry<float> TimerValue;
    public static ConfigEntry<KeyboardShortcut> OpenMenuKey;
    public static ConfigEntry<KeyboardShortcut> AutoSaveKey;
    public static ConfigEntry<KeyboardShortcut> AutoLoadKey;

    public static void Init(ConfigFile config)
    {
        KeepCount = config.Bind("AutoBackup", "KeepCount", 36, "备份文件保留的数量\nHow many save files to keep");
        TimerType = config.Bind("AutoBackup", "TimerType", TimerTypes.Hour, "计时器类型,按小时/tick进行自动保存\nTimer type,Auto-save by tick by hour");
        TimerValue = config.Bind("AutoBackup", "TimerValue", 2f, "计时器值,自动保存间隔\nTimer value, The interval at which the auto-save is made");
        OpenMenuKey = config.Bind("Keys", "OpenMenuKey", new KeyboardShortcut(KeyCode.F3, Array.Empty<KeyCode>()), "打开菜单\nOpen menu");
        AutoSaveKey = config.Bind("Keys", "AutoSaveKey", new KeyboardShortcut(KeyCode.F5, Array.Empty<KeyCode>()), "自动保存\nAuto save");
        AutoLoadKey = config.Bind("Keys", "AutoLoadKey", new KeyboardShortcut(KeyCode.F6, Array.Empty<KeyCode>()), "自动读取\nAuto load");
    }
    
}