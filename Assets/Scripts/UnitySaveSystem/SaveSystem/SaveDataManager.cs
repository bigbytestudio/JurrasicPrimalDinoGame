using System;
using System.IO;
using UnityEngine;

namespace SaveSystem
{
    /// <summary>
    /// Core manager for saving and loading any serializable data class to/from JSON.
    /// Handles mobile-safe paths, atomic writes, and error recovery.
    /// </summary>
    public static class SaveDataManager
    {
        // ── Constants ────────────────────────────────────────────────────────────
        private const string FILE_EXTENSION   = ".json";
        private const string BACKUP_EXTENSION = ".bak";

        private static readonly object SaveLock = new();

        // ── Path Resolution ──────────────────────────────────────────────────────

        /// <summary>
        /// Returns the full path for a given save slot name.
        /// Uses Application.persistentDataPath which is safe on iOS, Android, PC, etc.
        /// </summary>
        public static string GetFilePath(string slotName)
        {
            return Path.Combine(Application.persistentDataPath, slotName + FILE_EXTENSION);
        }

        private static string GetBackupPath(string slotName)
        {
            return Path.Combine(Application.persistentDataPath, slotName + BACKUP_EXTENSION);
        }

        // ── Save ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Serializes <paramref name="data"/> to JSON and writes it to disk atomically.
        /// A backup (.bak) of the previous save is kept so recovery is possible.
        /// </summary>
        /// <typeparam name="T">Any class decorated with [System.Serializable].</typeparam>
        /// <param name="data">The data object to persist.</param>
        /// <param name="slotName">File name (without extension) for this save slot.</param>
        /// <returns>True on success, false on failure.</returns>
        public static bool Save<T>(T data, string slotName = "savedata") where T : class
        {
            if (data == null)
            {
                Debug.LogError($"[SaveDataManager] Cannot save null data to slot '{slotName}'.");
                return false;
            }

            lock (SaveLock)
            {
                try
                {
                    string filePath   = GetFilePath(slotName);
                    string backupPath = GetBackupPath(slotName);
                    string tempPath   = filePath + ".tmp";

                    // Rotate existing file to backup before overwriting
                    if (File.Exists(filePath))
                        File.Copy(filePath, backupPath, overwrite: true);

                    string json = JsonUtility.ToJson(data, prettyPrint: Debug.isDebugBuild);

                    // Atomic write: write to temp → rename (avoids partial-write corruption)
                    TryDelete(tempPath);
                    File.WriteAllText(tempPath, json, System.Text.Encoding.UTF8);
                    ReplaceFile(tempPath, filePath);

                    Debug.Log($"[SaveDataManager] Saved '{slotName}' to {filePath}");
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SaveDataManager] Save failed for slot '{slotName}': {e.Message}");
                    return false;
                }
            }
        }

        // ── Load ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Loads and deserializes a JSON file into a <typeparamref name="T"/> instance.
        /// Falls back to the backup file if the primary file is corrupt.
        /// Returns a new default <typeparamref name="T"/> when no file exists.
        /// </summary>
        public static T Load<T>(string slotName = "savedata") where T : class, new()
        {
            string filePath   = GetFilePath(slotName);
            string backupPath = GetBackupPath(slotName);

            // Try primary file first
            if (File.Exists(filePath))
            {
                T result = TryDeserialize<T>(filePath, slotName);
                if (result != null) return result;

                Debug.LogWarning($"[SaveDataManager] Primary file corrupt for '{slotName}'. Trying backup...");
            }

            // Fall back to backup
            if (File.Exists(backupPath))
            {
                T result = TryDeserialize<T>(backupPath, slotName + " (backup)");
                if (result != null)
                {
                    Debug.LogWarning($"[SaveDataManager] Recovered '{slotName}' from backup.");
                    return result;
                }
            }

            // No file found — return fresh instance
            Debug.Log($"[SaveDataManager] No save file found for '{slotName}'. Returning defaults.");
            return new T();
        }

        // ── Delete ───────────────────────────────────────────────────────────────

        /// <summary>Deletes both the save file and its backup for a given slot.</summary>
        public static void Delete(string slotName = "savedata")
        {
            TryDelete(GetFilePath(slotName));
            TryDelete(GetBackupPath(slotName));
            Debug.Log($"[SaveDataManager] Deleted save slot '{slotName}'.");
        }

        /// <summary>Returns true if a save file exists for the given slot.</summary>
        public static bool Exists(string slotName = "savedata")
        {
            return File.Exists(GetFilePath(slotName));
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        private static T TryDeserialize<T>(string path, string label) where T : class
        {
            try
            {
                string json = File.ReadAllText(path, System.Text.Encoding.UTF8);
                T obj = JsonUtility.FromJson<T>(json);
                if (obj == null) throw new Exception("Deserialized object was null.");
                Debug.Log($"[SaveDataManager] Loaded from {label}.");
                return obj;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataManager] Failed to deserialize '{label}': {e.Message}");
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (Exception e) { Debug.LogWarning($"[SaveDataManager] Could not delete '{path}': {e.Message}"); }
        }

        private static void ReplaceFile(string sourcePath, string destinationPath)
        {
            // File.Move cannot replace an existing file on Windows (.NET Standard 2.0).
            // Primary save is already backed up to .bak before this runs.
            if (File.Exists(destinationPath))
                File.Delete(destinationPath);

            File.Move(sourcePath, destinationPath);
        }
    }
}
