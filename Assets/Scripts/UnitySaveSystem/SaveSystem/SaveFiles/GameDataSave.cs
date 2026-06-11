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

    public override string SlotName => "game_data";

    public override void SetDefaults()
    {
        playerName = "Player";
        dnaCurrency = 0;
        bonesCurrency = 0;
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
