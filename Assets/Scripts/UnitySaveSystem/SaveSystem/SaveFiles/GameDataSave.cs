using System;
using UnityEngine;
using SaveSystem;

[Serializable]
public class GameDataSave : SaveableBase<GameDataSave>
{
    public static GameDataSave Instance { get; private set; }
    public static event Action CurrencyChanged;

    public string playerName;
    public int dnaCurrency;
    public int bonesCurrency;
    public string[] unlockedCreatureIds = Array.Empty<string>();
    public CreatureGrowthSaveEntry[] creatureGrowthLevels = Array.Empty<CreatureGrowthSaveEntry>();

    public override string SlotName => "game_data";

    public override void SetDefaults()
    {
        playerName = "Player";
        dnaCurrency = 0;
        bonesCurrency = 0;
        unlockedCreatureIds = Array.Empty<string>();
        creatureGrowthLevels = Array.Empty<CreatureGrowthSaveEntry>();
    }

    public int GetCreatureGrowthLevel(string creatureId, int defaultLevel)
    {
        if (string.IsNullOrWhiteSpace(creatureId) || creatureGrowthLevels == null)
            return Mathf.Max(1, defaultLevel);

        for (int i = 0; i < creatureGrowthLevels.Length; i++)
        {
            if (creatureGrowthLevels[i].creatureId == creatureId)
                return Mathf.Max(1, creatureGrowthLevels[i].growthLevel);
        }

        return Mathf.Max(1, defaultLevel);
    }

    public void SetCreatureGrowthLevel(string creatureId, int growthLevel)
    {
        if (string.IsNullOrWhiteSpace(creatureId))
            return;

        int sanitizedLevel = Mathf.Max(1, growthLevel);
        if (creatureGrowthLevels == null)
            creatureGrowthLevels = Array.Empty<CreatureGrowthSaveEntry>();

        for (int i = 0; i < creatureGrowthLevels.Length; i++)
        {
            if (creatureGrowthLevels[i].creatureId != creatureId)
                continue;

            if (creatureGrowthLevels[i].growthLevel == sanitizedLevel)
                return;

            creatureGrowthLevels[i].growthLevel = sanitizedLevel;
            Save();
            return;
        }

        int length = creatureGrowthLevels.Length;
        CreatureGrowthSaveEntry[] updated = new CreatureGrowthSaveEntry[length + 1];
        for (int i = 0; i < length; i++)
            updated[i] = creatureGrowthLevels[i];

        updated[length] = new CreatureGrowthSaveEntry
        {
            creatureId = creatureId,
            growthLevel = sanitizedLevel
        };

        creatureGrowthLevels = updated;
        Save();
    }

    public bool IsCreatureUnlocked(string creatureId)
    {
        if (string.IsNullOrWhiteSpace(creatureId) || unlockedCreatureIds == null)
            return false;

        for (int i = 0; i < unlockedCreatureIds.Length; i++)
        {
            if (unlockedCreatureIds[i] == creatureId)
                return true;
        }

        return false;
    }

    public void UnlockCreature(string creatureId)
    {
        if (string.IsNullOrWhiteSpace(creatureId) || IsCreatureUnlocked(creatureId))
            return;

        int length = unlockedCreatureIds?.Length ?? 0;
        string[] updated = new string[length + 1];
        for (int i = 0; i < length; i++)
            updated[i] = unlockedCreatureIds[i];

        updated[length] = creatureId;
        unlockedCreatureIds = updated;
        Save();
    }

    public static void Bind(GameDataSave data)
    {
        Instance = data;
        CurrencyChanged?.Invoke();
    }

    public void SetDnaCurrency(int amount)
    {
        dnaCurrency = Mathf.Max(0, amount);
        NotifyCurrencyChanged();
    }

    public void SetBonesCurrency(int amount)
    {
        bonesCurrency = Mathf.Max(0, amount);
        NotifyCurrencyChanged();
    }

    public void AddDnaCurrency(int amount)
    {
        if (amount == 0)
            return;

        SetDnaCurrency(dnaCurrency + amount);
    }

    public void AddBonesCurrency(int amount)
    {
        if (amount == 0)
            return;

        SetBonesCurrency(bonesCurrency + amount);
    }

    private void NotifyCurrencyChanged()
    {
        Save();
        CurrencyChanged?.Invoke();
    }
}

[Serializable]
public struct CreatureGrowthSaveEntry
{
    public string creatureId;
    public int growthLevel;
}
