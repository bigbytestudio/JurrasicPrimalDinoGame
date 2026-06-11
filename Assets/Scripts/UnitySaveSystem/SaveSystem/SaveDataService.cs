using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaveSystem
{
    // ════════════════════════════════════════════════════════════════════════════
    //  SaveDataService  —  MonoBehaviour singleton
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Drop this on a persistent GameObject (or let it self-create).
    /// It auto-saves all registered ISaveData instances when the app is paused
    /// or quit — critical for mobile where the OS can kill the app without warning.
    ///
    /// Usage:
    ///   SaveDataService.Instance.Register(myData);
    ///   SaveDataService.Instance.SaveAll();
    /// </summary>
    public class SaveDataService : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────────────────────────

        private static SaveDataService _instance;
        public static SaveDataService Instance
        {
            get
            {
                if (_instance == null) CreateInstance();
                return _instance;
            }
        }

        private static void CreateInstance()
        {
            GameObject go = new GameObject("[SaveDataService]");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SaveDataService>();
        }

        // ── Fields ───────────────────────────────────────────────────────────────

        [Header("Auto-Save Settings")]
        [Tooltip("Automatically save all registered data when app loses focus (recommended for mobile).")]
        [SerializeField] private bool autoSaveOnPause = true;

        [Tooltip("Automatically save all registered data when app quits.")]
        [SerializeField] private bool autoSaveOnQuit  = true;

        [Tooltip("Interval in seconds for periodic auto-save (0 = disabled).")]
        [SerializeField] private float autoSaveInterval = 60f;

        private readonly List<ISaveData> _registry = new List<ISaveData>();
        private float _autoSaveTimer;

        // ── Events ───────────────────────────────────────────────────────────────

        /// <summary>Fired after SaveAll() completes. bool = any failure occurred.</summary>
        public event Action<bool> OnSaveAllCompleted;

        // ── Unity Lifecycle ──────────────────────────────────────────────────────

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            if (autoSaveInterval <= 0f) return;

            _autoSaveTimer += Time.unscaledDeltaTime;
            if (_autoSaveTimer >= autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                SaveAll();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            // On mobile, pausing = user switched app / home button pressed
            if (paused && autoSaveOnPause)
                SaveAll();
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit)
                SaveAll();
        }

        // ── Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Register a data object so it is included in SaveAll() / auto-saves.
        /// Duplicate registrations are ignored.
        /// </summary>
        public void Register(ISaveData data)
        {
            if (data == null) return;
            if (!_registry.Contains(data))
            {
                _registry.Add(data);
                Debug.Log($"[SaveDataService] Registered '{data.SlotName}'.");
            }
        }

        /// <summary>Unregister a data object (e.g. on scene unload).</summary>
        public void Unregister(ISaveData data)
        {
            _registry.Remove(data);
        }

        /// <summary>
        /// Saves all registered data objects. Returns true if ALL succeeded.
        /// </summary>
        public bool SaveAll()
        {
            bool anyFailure = false;
            foreach (ISaveData data in _registry)
            {
                bool ok = SaveDataManager.Save(data, data.SlotName);
                if (!ok) anyFailure = true;
            }
            OnSaveAllCompleted?.Invoke(anyFailure);
            return !anyFailure;
        }

        /// <summary>
        /// Explicitly save a single registered slot by its slot name.
        /// </summary>
        public bool SaveSlot(string slotName)
        {
            ISaveData target = _registry.Find(d => d.SlotName == slotName);
            if (target == null)
            {
                Debug.LogWarning($"[SaveDataService] Slot '{slotName}' is not registered.");
                return false;
            }
            return SaveDataManager.Save(target, slotName);
        }

        /// <summary>Returns true if any save file exists for the given slot.</summary>
        public bool SlotExists(string slotName) => SaveDataManager.Exists(slotName);

        /// <summary>Delete a slot from disk AND unregister it.</summary>
        public void DeleteSlot(string slotName)
        {
            ISaveData target = _registry.Find(d => d.SlotName == slotName);
            if (target != null) _registry.Remove(target);
            SaveDataManager.Delete(slotName);
        }

        /// <summary>How many data objects are currently registered.</summary>
        public int RegisteredCount => _registry.Count;
    }
}
