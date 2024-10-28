using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CSTI_AutoBackup;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class AutoBackup : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    private static string GameFilesDirectoryPath;
    private static string BackupPath;
    private static readonly Dictionary<int, List<GameSaveInfo>> GameSaves = new();
    private static int Slot = -1;
    private bool ShowGUI;
    private bool QuickSaveLoad = false;
    private Vector2 FilesListScrollView;

    [System.Diagnostics.Conditional("DEBUG")]
    public static void LogInfo(string message)
    {
        Logger.LogInfo(message);
    }

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        AutoBackupConfig.Init(Config);
        Harmony.CreateAndPatchAll(typeof(AutoBackup));
        BackupPath = Path.Combine(Application.persistentDataPath, "BackUps");
        GameFilesDirectoryPath = Traverse.Create(GameLoad.Instance).Property("GameFilesDirectoryPath").GetValue<string>();
        LoadSaveFiles();
    }

    private void Update()
    {
        if (AutoBackupConfig.OpenMenuKey.Value.IsDown())
            // LogInfo("key pressed");
            ShowGUI = !ShowGUI;
        if(!QuickSaveLoad) return;
        if (AutoBackupConfig.AutoSaveKey.Value.IsDown())
            SaveGame();
        if (AutoBackupConfig.AutoLoadKey.Value.IsDown())
            GameLoad.Instance.AutoLoadGame();
    }

    public static void SaveGame()
    {
        if (!MBSingleton<GameManager>.Instance) return;
        GameLoad.Instance.AutoSaveGame(false);
        NotifyGameSaved();
    }

    public static void LoadGame(GameSaveInfo info)
    {
        var originSaveFile = Path.Combine(GameFilesDirectoryPath, info.OriginName);
        var backUpSaveFile = Path.Combine(BackupPath, $"Slot_{Slot + 1}", info.FileName);
        try
        {
            File.Copy(backUpSaveFile, originSaveFile, true);
        }
        catch (Exception e)
        {
            Logger.LogError($"Load Backup File Error: {e.Message}");
        }

        Traverse.Create(GameLoad.Instance).Method("LoadGameFile", originSaveFile).GetValue(originSaveFile);
        GameLoad.Instance.LoadGame(Slot);
    }

    public static void BackUpSave()
    {
        Slot = GameLoad.Instance.CurrentGameDataIndex;
        var GameData = GameLoad.Instance.Games[Slot];
        var gameDay = GameData.MainData.CurrentDay;
        var gameHour = GameData.MainData.DaytimeToHour.Replace(":", "_");
        var realTime = DateTime.Now.ToString(GameSaveInfo.timestampFormat);
        var gameTIme = $"{gameDay}Days_{gameHour}";
        var saveName = $"{realTime}_{gameTIme}.json";
        var savePath = Path.Combine(BackupPath, $"Slot_{Slot + 1}");
        if (!Directory.CreateDirectory(savePath).Exists)
        {
            Logger.LogError($"Create Directory Error: {savePath}");
        }

        var originSaveFile = Path.Combine(GameFilesDirectoryPath, GameData.FileName);
        if (File.Exists(originSaveFile))
        {
            File.Copy(originSaveFile, Path.Combine(savePath, saveName));
        }
        else
        {
            Logger.LogError($"Save File Not Found: {originSaveFile}");
        }

        if (!GameSaves.ContainsKey(Slot)) GameSaves.Add(Slot, []);
        GameSaves[Slot].Add(new GameSaveInfo(GameData, saveName));
        GameSaves[Slot].Sort((a, b) => b.realTime.CompareTo(a.realTime));
        if (GameSaves[Slot].Count <= AutoBackupConfig.KeepCount.Value) return;
        for (var i = GameSaves[Slot].Count - 1; i >= AutoBackupConfig.KeepCount.Value; i--)
        {
            DeleteSave(GameSaves[Slot][i]);
        }
    }

    public static void LoadSaveFiles()
    {
        foreach (var dir in new DirectoryInfo(BackupPath).GetDirectories())
        {
            if (!dir.Name.StartsWith("Slot_")) continue;
            foreach (var saveFile in dir.GetFiles("*.json"))
            {
                LoadSaveFile(saveFile.FullName);
            }
        }

        foreach (var gameSaveInfos in GameSaves.Values)
        {
            gameSaveInfos.Sort((a, b) => b.realTime.CompareTo(a.realTime));
        }
    }

    public static void LoadSaveFile(string path)
    {
        try
        {
            var saveData = JsonUtility.FromJson<GameSaveFile>(File.ReadAllText(path));
            if (!GameSaves.ContainsKey(saveData.SlotIndex)) GameSaves.Add(saveData.SlotIndex, []);
            var saveInfo = new GameSaveInfo(saveData, Path.GetFileName(path));
            if (GameSaves[saveData.SlotIndex].Contains(saveInfo)) return;
            GameSaves[saveData.SlotIndex].Add(saveInfo);
        }
        catch (Exception e)
        {
            Logger.LogError($"Load Save File Error: {e.Message}");
        }
    }

    public static void DeleteSave(GameSaveInfo info)
    {
        var backUpSaveFile = Path.Combine(BackupPath, $"Slot_{info.Slot + 1}", info.FileName);
        File.Delete(backUpSaveFile);
        GameSaves[Slot].Remove(info);
    }

    private void OnGUI()
    {
        if (!ShowGUI) return;
        GUILayout.BeginArea(new Rect(Screen.width * 0.2f, Screen.height * 0.2f, Screen.width * 0.25f, Screen.height * 0.68f), "", "box");
        MenuGui();
        FilesGUI();
        GUILayout.EndArea();
    }

    private void MenuGui()
    {
        var height = GUILayout.Height((float)(Screen.height * 0.7 * 0.05));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Save And Backup", height))
            SaveGame();
        if (GUILayout.Button("Load Last Game", height))
            GameLoad.Instance.AutoLoadGame();
        if (GUILayout.Button("Refresh Saves", height))
            LoadSaveFiles();
        if (GUILayout.Button("Close", height))
            ShowGUI = false;
        GUILayout.EndHorizontal();
    }

    private void FilesGUI()
    {
        GUILayout.BeginVertical("box");

        var height = GUILayout.Height((float)(Screen.height * 0.5 * 0.05));
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Back", height)) Slot = -1;
        GUILayout.Label(Slot == -1 ? "" : $"Slot: {Slot + 1}", height);
        GUILayout.FlexibleSpace();
        QuickSaveLoad = GUILayout.Toggle(QuickSaveLoad, "Enable Quick Save/Load", height);
        GUILayout.EndHorizontal();


        FilesListScrollView = GUILayout.BeginScrollView(FilesListScrollView, GUILayout.ExpandHeight(true));
        if (Slot == -1)
        {
            foreach (var slot in GameSaves.Keys.OrderBy(num => num))
            {
                if (GUILayout.Button($"slot {slot + 1}", height))
                    Slot = slot;
            }
        }
        else
        {
            foreach (var saveInfo in GameSaves[Slot].ToList())
            {
                GUILayout.BeginHorizontal("box");
                // 左边信息显示
                GUILayout.BeginVertical();
                //现实时间
                GUILayout.Label(saveInfo.realTime.ToString("yyyy-MM-dd HH:mm:ss"));
                GUILayout.BeginHorizontal();
                //游戏时间
                GUILayout.Label($"{saveInfo.Day}Days {saveInfo.Hour}");
                GUILayout.FlexibleSpace();
                //人物
                GUILayout.Label(saveInfo.getCharacterName());
                GUILayout.EndHorizontal();
                //地点
                GUILayout.Label(saveInfo.getEnvName());
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                //右边按钮
                GUILayout.BeginVertical();
                if (GUILayout.Button("Load")) LoadGame(saveInfo);
                if (GUILayout.Button("Delete")) DeleteSave(saveInfo);
                GUILayout.EndVertical();
                GUILayout.EndHorizontal();
            }
        }

        GUILayout.EndScrollView();
        GUILayout.EndVertical();
    }

    [HarmonyPatch(typeof(GameLoad), "SaveGameDataToFile")]
    [HarmonyPostfix]
    private static void SaveGameDataToFilePatch(GameLoad __instance)
    {
        BackUpSave();
    }

    [HarmonyPatch(typeof(GameManager), "ActionRoutine")]
    [HarmonyPostfix]
    private static IEnumerator ActionRoutinePatch(IEnumerator result, GameManager __instance)
    {
        yield return result;
        var gameData = GameLoad.Instance.Games[GameLoad.Instance.CurrentGameDataIndex];
        var DailyPoints = __instance.DaySettings.DailyPoints;
        var PointToHours = __instance.DaySettings.PointToHours;
        var dataTotalTick = gameData.MainData.CurrentDay * DailyPoints - gameData.MainData.CurrentDayTimePoints;
        var timerValue = AutoBackupConfig.TimerValue.Value;
        switch (AutoBackupConfig.TimerType.Value)
        {
            case TimerTypes.Hour:
                if (__instance.CurrentTickInfo.z - dataTotalTick >= timerValue / PointToHours) SaveGame();
                break;
            case TimerTypes.Tick:
                if (__instance.CurrentTickInfo.z - dataTotalTick >= timerValue) SaveGame();
                break;
            default:
                Logger.LogError($"Unknown Timer Type: {AutoBackupConfig.TimerType.Value}");
                throw new ArgumentOutOfRangeException();
        }
    }

    public static void NotifyGameSaved()
    {
        var instance = MBSingleton<GraphicsManager>.Instance;
        instance.PlayMessage(instance.TimeSpentWheel.transform.position, "Game Saved", instance.TimeSpentWheel.transform);
    }
}