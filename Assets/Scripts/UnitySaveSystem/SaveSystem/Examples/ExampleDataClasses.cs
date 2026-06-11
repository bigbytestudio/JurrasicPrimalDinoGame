using System;
using UnityEngine;

namespace SaveSystem.Examples
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Example 1 — Player Preferences
    //  Slot file: <persistentDataPath>/player_prefs.json
    // ════════════════════════════════════════════════════════════════════════════

    [Serializable]
    public class PlayerPrefsData : SaveableBase<PlayerPrefsData>
    {
        // ── Declare your variables here exactly like normal C# fields ────────────
        public string playerName    = "Hero";
        public int    coins         = 0;
        public int    highScore     = 0;
        public int    totalPlayTime = 0;    // seconds
        public bool   tutorialDone  = false;
        public string lastScene     = "MainMenu";

        // ── ISaveData implementation ─────────────────────────────────────────────
        public override string SlotName => "player_prefs";

        public override void SetDefaults()
        {
            playerName    = "Hero";
            coins         = 0;
            highScore     = 0;
            totalPlayTime = 0;
            tutorialDone  = false;
            lastScene     = "MainMenu";
        }

        public override void OnAfterLoad()
        {
            // Sanitise: ensure non-negative coins after load
            coins     = Mathf.Max(0, coins);
            highScore = Mathf.Max(0, highScore);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Example 2 — Settings / Audio-Video Preferences
    //  Slot file: <persistentDataPath>/settings.json
    // ════════════════════════════════════════════════════════════════════════════

    [Serializable]
    public class SettingsData : SaveableBase<SettingsData>
    {
        public float  masterVolume  = 1f;
        public float  musicVolume   = 0.8f;
        public float  sfxVolume     = 1f;
        public bool   vibrationOn   = true;
        public bool   notificationsOn = true;
        public int    qualityLevel  = 2;       // Unity quality setting index
        public string languageCode  = "en";
        public bool   fullscreen    = false;   // relevant on desktop builds

        public override string SlotName => "settings";

        public override void SetDefaults()
        {
            masterVolume  = 1f;
            musicVolume   = 0.8f;
            sfxVolume     = 1f;
            vibrationOn   = true;
            notificationsOn = true;
            qualityLevel  = 2;
            languageCode  = "en";
            fullscreen    = false;
        }

        public override void OnAfterLoad()
        {
            // Clamp volumes in case of corrupted data
            masterVolume = Mathf.Clamp01(masterVolume);
            musicVolume  = Mathf.Clamp01(musicVolume);
            sfxVolume    = Mathf.Clamp01(sfxVolume);
            qualityLevel = Mathf.Clamp(qualityLevel, 0, QualitySettings.names.Length - 1);
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Example 3 — Game Progress (levels, unlocks, achievements)
    //  Slot file: <persistentDataPath>/game_progress.json
    // ════════════════════════════════════════════════════════════════════════════

    [Serializable]
    public class GameProgressData : SaveableBase<GameProgressData>
    {
        public int      currentLevel    = 1;
        public int      maxLevelUnlocked = 1;
        public bool[]   levelsCompleted = new bool[50];
        public int[]    levelStars      = new int[50];   // 0-3 stars per level
        public string[] unlockedItems   = Array.Empty<string>();
        public bool     premiumUnlocked = false;

        public override string SlotName => "game_progress";

        public override void SetDefaults()
        {
            currentLevel     = 1;
            maxLevelUnlocked = 1;
            levelsCompleted  = new bool[50];
            levelStars       = new int[50];
            unlockedItems    = Array.Empty<string>();
            premiumUnlocked  = false;
        }

        public override void OnAfterLoad()
        {
            // Ensure arrays are never null (can happen with old save formats)
            if (levelsCompleted == null || levelsCompleted.Length < 50)
            {
                bool[] old = levelsCompleted ?? Array.Empty<bool>();
                levelsCompleted = new bool[50];
                Array.Copy(old, levelsCompleted, Math.Min(old.Length, 50));
            }
            if (levelStars == null || levelStars.Length < 50)
            {
                int[] old = levelStars ?? Array.Empty<int>();
                levelStars = new int[50];
                Array.Copy(old, levelStars, Math.Min(old.Length, 50));
            }
            if (unlockedItems == null)
                unlockedItems = Array.Empty<string>();

            currentLevel     = Mathf.Max(1, currentLevel);
            maxLevelUnlocked = Mathf.Max(1, maxLevelUnlocked);
        }
    }
}
