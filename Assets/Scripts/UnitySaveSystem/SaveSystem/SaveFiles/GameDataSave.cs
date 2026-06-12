using System;
using UnityEngine;
using SaveSystem;

[Serializable]
public class GameDataSave : SaveableBase<GameDataSave>
{
    public static GameDataSave Instance { get; private set; }
    public static event Action CurrencyChanged;
    public static event Action ProfileStatsChanged;

    public string playerName;
    public int dnaCurrency;
    public int bonesCurrency;
    public int totalDinoKills;
    public float totalPlayTimeSeconds;
    public int playerRank = 1;
    public int playerXp;
    public int xpPerRank = 100;
    public string[] unlockedCreatureIds = Array.Empty<string>();
    public CreatureGrowthSaveEntry[] creatureGrowthLevels = Array.Empty<CreatureGrowthSaveEntry>();

    public override string SlotName => "game_data";

    public override void SetDefaults()
    {
        playerName = "Player";
        dnaCurrency = 0;
        bonesCurrency = 0;
        totalDinoKills = 0;
        totalPlayTimeSeconds = 0f;
        playerRank = 1;
        playerXp = 0;
        xpPerRank = 100;
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
        NotifyProfileStatsChanged();
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

    public void SetPlayerName(string name)
    {
        const int maxLength = 20;
        string trimmed = string.IsNullOrWhiteSpace(name) ? "Player" : name.Trim();
        if (trimmed.Length > maxLength)
            trimmed = trimmed.Substring(0, maxLength);

        if (playerName == trimmed)
            return;

        playerName = trimmed;
        NotifyProfileStatsChanged();
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

    public void AddPlayTime(float seconds)
    {
        if (seconds <= 0f)
            return;

        totalPlayTimeSeconds += seconds;
        NotifyProfileStatsChanged();
    }

    public void RegisterPlayerKill(int xpReward = 10)
    {
        totalDinoKills++;
        AddPlayerXp(xpReward);
    }

    public void AddPlayerXp(int amount)
    {
        if (amount <= 0)
            return;

        playerXp += amount;
        while (playerXp >= xpPerRank)
        {
            playerXp -= xpPerRank;
            playerRank++;
        }

        NotifyProfileStatsChanged();
    }

    public int GetUnlockedCreatureCount()
    {
        return unlockedCreatureIds?.Length ?? 0;
    }

    public float GetRankProgress01()
    {
        if (xpPerRank <= 0)
            return 1f;

        return Mathf.Clamp01(playerXp / (float)xpPerRank);
    }

    public int GetDnaBonusPercent(int totalCreatures)
    {
        if (totalCreatures <= 0)
            return 0;

        int unlocked = GetUnlockedCreatureCount();
        return Mathf.Clamp(unlocked * 5, 0, 50);
    }

    public int GetBonesBonusPercent(int totalCreatures)
    {
        if (totalCreatures <= 0)
            return 0;

        int unlocked = GetUnlockedCreatureCount();
        return Mathf.Clamp(unlocked * 3, 0, 30);
    }

    private void NotifyProfileStatsChanged()
    {
        Save();
        ProfileStatsChanged?.Invoke();
    }
}

[Serializable]
public struct CreatureGrowthSaveEntry
{
    public string creatureId;
    public int growthLevel;
}
