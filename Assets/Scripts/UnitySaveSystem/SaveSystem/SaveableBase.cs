using System;
using UnityEngine;

namespace SaveSystem
{
    // ════════════════════════════════════════════════════════════════════════════
    //  ISaveData  —  Contract every saveable data class must fulfill
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Implement this on any data class you want to persist.
    /// The slot name identifies the JSON file on disk.
    /// </summary>
    public interface ISaveData
    {
        /// <summary>Unique file-system slot name (no extension, no spaces).</summary>
        string SlotName { get; }

        /// <summary>Called once when a fresh instance is created (no existing save found).</summary>
        void SetDefaults();

        /// <summary>Optional validation hook — sanitise values after load.</summary>
        void OnAfterLoad();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  SaveableBase<T>  —  Self-contained base that wraps SaveDataManager
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Inherit from this to get automatic Save / Load / Delete on any data class.
    ///
    /// <code>
    /// [System.Serializable]
    /// public class PlayerPrefs : SaveableBase&lt;PlayerPrefs&gt;
    /// {
    ///     public string playerName = "Hero";
    ///     public int    coins      = 0;
    ///
    ///     public override string SlotName => "player_prefs";
    ///     public override void   SetDefaults() { playerName = "Hero"; coins = 0; }
    /// }
    /// </code>
    /// </summary>
    [Serializable]
    public abstract class SaveableBase<T> : ISaveData where T : SaveableBase<T>, new()
    {
        // ── ISaveData ────────────────────────────────────────────────────────────
        public abstract string SlotName { get; }
        public virtual  void   SetDefaults()  { }   // override to set default field values
        public virtual  void   OnAfterLoad()  { }   // override to validate / migrate data

        // ── Static API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Load from disk (or create defaults if no file exists).
        /// </summary>
        public static T Load()
        {
            T instance = SaveDataManager.Load<T>(new T().SlotName);
            instance.OnAfterLoad();
            return instance;
        }

        /// <summary>Save this instance to disk.</summary>
        public bool Save() => SaveDataManager.Save(this, SlotName);

        /// <summary>Delete the save file from disk.</summary>
        public void Delete() => SaveDataManager.Delete(SlotName);

        /// <summary>True if a save file exists for this type.</summary>
        public static bool HasSave() => SaveDataManager.Exists(new T().SlotName);

        /// <summary>
        /// Creates a fresh instance with default values (does NOT touch disk).
        /// </summary>
        public static T CreateDefault()
        {
            T instance = new T();
            instance.SetDefaults();
            return instance;
        }
    }
}
