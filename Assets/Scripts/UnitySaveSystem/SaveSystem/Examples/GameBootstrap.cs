using UnityEngine;
using SaveSystem;
using SaveSystem.Examples;

namespace SaveSystem.Examples
{
    /// <summary>
    /// Drop this on any GameObject in your first scene to bootstrap the save system.
    /// Shows all common usage patterns: load, modify, save, delete, auto-register.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        // ── Live references (kept in memory while the game runs) ─────────────────
        public static PlayerPrefsData  Player   { get; private set; }
        public static SettingsData     Settings { get; private set; }
        public static GameProgressData Progress { get; private set; }

        // ── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            // 1. Load all data from disk (returns defaults if no file exists)
            Player   = PlayerPrefsData.Load();
            Settings = SettingsData.Load();
            Progress = GameProgressData.Load();

            // 2. Register with the service so they are included in auto-saves
            //    (service self-creates a DontDestroyOnLoad GameObject automatically)
            SaveDataService.Instance.Register(Player);
            SaveDataService.Instance.Register(Settings);
            SaveDataService.Instance.Register(Progress);

            Debug.Log($"[GameBootstrap] Loaded save for: {Player.playerName}, " +
                      $"level {Progress.currentLevel}, coins {Player.coins}");
        }

        // ════════════════════════════════════════════════════════════════════════
        //  Usage Examples — call these from anywhere in your game
        // ════════════════════════════════════════════════════════════════════════

        // ── Modifying and saving player prefs ─────────────────────────────────

        /// <summary>Award coins to the player and persist immediately.</summary>
        public void AwardCoins(int amount)
        {
            Player.coins += amount;
            Player.Save();      // instant save to JSON
            Debug.Log($"Awarded {amount} coins. Total: {Player.coins}");
        }

        /// <summary>Update player name.</summary>
        public void SetPlayerName(string newName)
        {
            Player.playerName = newName;
            Player.Save();
        }

        // ── Modifying settings ────────────────────────────────────────────────

        public void SetMasterVolume(float volume)
        {
            Settings.masterVolume = volume;
            Settings.Save();
            // Apply to Unity AudioListener
            AudioListener.volume = volume;
        }

        public void SetVibration(bool enabled)
        {
            Settings.vibrationOn = enabled;
            Settings.Save();
        }

        // ── Level completion ──────────────────────────────────────────────────

        public void CompleteLevel(int levelIndex, int stars)
        {
            if (levelIndex < 0 || levelIndex >= Progress.levelsCompleted.Length) return;

            Progress.levelsCompleted[levelIndex] = true;
            Progress.levelStars[levelIndex]      = Mathf.Max(Progress.levelStars[levelIndex], stars);
            Progress.maxLevelUnlocked            = Mathf.Max(Progress.maxLevelUnlocked, levelIndex + 1);
            Progress.Save();

            Debug.Log($"Level {levelIndex + 1} completed with {stars} stars!");
        }

        // ── New-game / reset ──────────────────────────────────────────────────

        /// <summary>Wipe all save data and reset to defaults (confirm before calling!).</summary>
        public void ResetAllData()
        {
            // Unregister first so auto-save doesn't re-write during reset
            SaveDataService.Instance.Unregister(Player);
            SaveDataService.Instance.Unregister(Settings);
            SaveDataService.Instance.Unregister(Progress);

            Player.Delete();
            Settings.Delete();
            Progress.Delete();

            // Re-create with defaults
            Player   = PlayerPrefsData.CreateDefault();
            Settings = SettingsData.CreateDefault();
            Progress = GameProgressData.CreateDefault();

            // Re-register the fresh instances
            SaveDataService.Instance.Register(Player);
            SaveDataService.Instance.Register(Settings);
            SaveDataService.Instance.Register(Progress);

            Debug.Log("[GameBootstrap] All save data wiped and reset to defaults.");
        }

        // ── Manual save-all (e.g. before loading a new scene) ─────────────────

        public void SaveEverything()
        {
            bool success = SaveDataService.Instance.SaveAll();
            Debug.Log($"[GameBootstrap] SaveAll result: {(success ? "OK" : "FAILED")}");
        }

        // ── Check if this is the player's first run ───────────────────────────

        public bool IsFirstRun() => !PlayerPrefsData.HasSave();
    }
}
