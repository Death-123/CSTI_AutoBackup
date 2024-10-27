using System;

namespace CSTI_AutoBackup;

public class GameSaveInfo
{
    public readonly string FileName;
    public readonly string OriginName;
    public readonly int Slot;
    public readonly int Day;
    public readonly string Hour;
    public readonly string Env;
    public readonly string Character;
    public readonly string CharacterName;
    public readonly DateTime realTime;
    public const string timestampFormat = "yyyy_MM_dd_HH_mm_ss";

    public GameSaveInfo(GameSaveFile data, string saveName)
    {
        OriginName = data.FileName;
        FileName = saveName;
        Slot = data.SlotIndex;
        var mainData = data.MainData;
        Day = mainData.CurrentDay;
        Hour = mainData.DaytimeToHour;
        Env = mainData.CurrentEnvironmentCard.EnvironmentID;
        Character = mainData.CurrentCharacter;
        CharacterName = mainData.CharacterData?.CharacterName;
        realTime = DateTime.ParseExact(saveName.Substring(0, timestampFormat.Length), timestampFormat, null);
    }

    public string getEnvName()
    {
        return Env.Contains("(") ? Env.Substring(Env.IndexOf('(') + 1, Env.LastIndexOf(')') - Env.LastIndexOf('(') - 1) : Env;
    }

    public string getCharacterName()
    {
        if(Character.Contains("("))
            return Character.Substring(Character.IndexOf('(') + 1, Character.LastIndexOf(')') - Character.LastIndexOf('(') - 1);
        return Character == "Custom" ? CharacterName : Character;
    }

    public override bool Equals(object obj)
    {
        if (obj is GameSaveInfo other)
        {
            return FileName == other.FileName && OriginName == other.OriginName && Slot == other.Slot && Day == other.Day && Hour == other.Hour && Env == other.Env && Character == other.Character && CharacterName == other.CharacterName &&
                   realTime.Equals(other.realTime);
        }

        return false;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hashCode = (FileName != null ? FileName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (OriginName != null ? OriginName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ Slot;
            hashCode = (hashCode * 397) ^ Day;
            hashCode = (hashCode * 397) ^ (Hour != null ? Hour.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Env != null ? Env.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (Character != null ? Character.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ (CharacterName != null ? CharacterName.GetHashCode() : 0);
            hashCode = (hashCode * 397) ^ realTime.GetHashCode();
            return hashCode;
        }
    }
}