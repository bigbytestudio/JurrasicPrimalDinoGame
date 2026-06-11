using System;
using SaveSystem;
using UnityEngine;

public enum GraphicsQualityLevel
{
    Low = 0,
    Medium = 1,
    High = 2
}

[Serializable]
public class SettingsSave : SaveableBase<SettingsSave>
{
    public static SettingsSave Instance { get; private set; }
    public static event Action SettingsChanged;

    public float musicVolume = 0.8f;
    public float sfxVolume = 1f;
    public GraphicsQualityLevel graphicsQuality = GraphicsQualityLevel.Medium;
    public bool vibrationEnabled = true;
    public string languageCode = "en";

    public override string SlotName => "settings";

    public override void SetDefaults()
    {
        musicVolume = 0.8f;
        sfxVolume = 1f;
        graphicsQuality = GraphicsQualityLevel.Medium;
        vibrationEnabled = true;
        languageCode = "en";
    }

    public override void OnAfterLoad()
    {
        musicVolume = Mathf.Clamp01(musicVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        ApplyGraphicsQuality();
    }

    public static void Bind(SettingsSave data)
    {
        Instance = data;
        data.ApplyGraphicsQuality();
        SettingsChanged?.Invoke();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        NotifyChanged();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        NotifyChanged();
    }

    public void SetGraphicsQuality(GraphicsQualityLevel quality)
    {
        graphicsQuality = quality;
        ApplyGraphicsQuality();
        NotifyChanged();
    }

    public void SetVibrationEnabled(bool enabled)
    {
        vibrationEnabled = enabled;
        NotifyChanged();
    }

    public void ApplyGraphicsQuality()
    {
        QualitySettings.SetQualityLevel(ResolveQualityIndex(graphicsQuality), true);
    }

    private static int ResolveQualityIndex(GraphicsQualityLevel level)
    {
        string[] names = QualitySettings.names;
        if (names == null || names.Length == 0)
            return 0;

        string target = level switch
        {
            GraphicsQualityLevel.Low => "Low",
            GraphicsQualityLevel.Medium => "Medium",
            GraphicsQualityLevel.High => "High",
            _ => "Medium"
        };

        for (int i = 0; i < names.Length; i++)
        {
            if (string.Equals(names[i], target, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return Mathf.Clamp((int)level, 0, names.Length - 1);
    }

    private void NotifyChanged()
    {
        Save();
        SettingsChanged?.Invoke();
    }
}
