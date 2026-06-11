# Unity JSON Save System

A generic, mobile-safe, zero-dependency save system for Unity.  
Declare your data fields in a plain C# class → the system handles JSON  
serialization, atomic writes, backup-recovery, and auto-save on app pause/quit.

---

## Folder Structure

```
Assets/
└── SaveSystem/
    ├── SaveDataManager.cs      ← static core: read/write JSON to disk
    ├── SaveableBase.cs         ← base class + ISaveData interface
    ├── SaveDataService.cs      ← MonoBehaviour singleton, auto-save on pause/quit
    ├── Editor/
    │   └── SaveFileInspector.cs  ← Editor window (Tools → Save System)
    └── Examples/
        ├── ExampleDataClasses.cs ← PlayerPrefsData, SettingsData, GameProgressData
        └── GameBootstrap.cs      ← example MonoBehaviour showing all usage
```

---

## Quick Start

### 1. Create a data class

```csharp
using System;
using SaveSystem;

[Serializable]
public class PlayerPrefsData : SaveableBase<PlayerPrefsData>
{
    // ← Just declare your variables normally
    public string playerName = "Hero";
    public int    coins      = 0;
    public bool   tutorialDone = false;

    public override string SlotName => "player_prefs"; // file name (no extension)

    public override void SetDefaults()
    {
        playerName   = "Hero";
        coins        = 0;
        tutorialDone = false;
    }
}
```

### 2. Load, modify, save

```csharp
// Load (returns defaults if no file found)
PlayerPrefsData player = PlayerPrefsData.Load();

// Modify fields normally
player.coins += 100;
player.playerName = "Alice";

// Save to JSON
player.Save();

// Delete save file
player.Delete();

// Check if a save file exists
bool isFirstRun = !PlayerPrefsData.HasSave();
```

### 3. Register for auto-save (recommended)

```csharp
void Awake()
{
    PlayerPrefsData player = PlayerPrefsData.Load();

    // SaveDataService auto-saves on OnApplicationPause + OnApplicationQuit
    SaveDataService.Instance.Register(player);
}
```

---

## Mobile Notes

| Concern | How it's handled |
|---|---|
| App killed by OS | `OnApplicationPause` triggers `SaveAll()` automatically |
| Partial write / corruption | Atomic temp→rename write, `.bak` backup kept |
| Corrupt primary file | Falls back to `.bak`, logs a warning |
| File path | `Application.persistentDataPath` — correct on iOS, Android, PC |
| Background thread safety | Writes happen on main thread (Unity-safe). For large data use `async` variant (see below) |

---

## Supported Field Types

`JsonUtility` supports:

- `bool`, `int`, `float`, `double`, `string`
- `Vector2`, `Vector3`, `Vector4`, `Quaternion`, `Color`, `Rect`
- Arrays and Lists of the above (e.g. `int[]`, `List<string>`)
- Nested `[Serializable]` classes

**Not supported by JsonUtility:** `Dictionary`, `HashSet`, polymorphic types.  
For those, swap `JsonUtility.ToJson/FromJson` with **Newtonsoft.Json** (available via Unity Package Manager).

---

## API Reference

### `SaveableBase<T>` (inherit from this)

| Member | Description |
|---|---|
| `SlotName` (abstract) | File name without extension |
| `SetDefaults()` | Called when creating a fresh instance |
| `OnAfterLoad()` | Called after deserialization — sanitise/validate here |
| `T.Load()` | Static — load from disk or return defaults |
| `instance.Save()` | Write this instance to disk |
| `instance.Delete()` | Delete save file from disk |
| `T.HasSave()` | Static — true if save file exists |
| `T.CreateDefault()` | Static — new instance with defaults, does not touch disk |

### `SaveDataManager` (static utility)

| Method | Description |
|---|---|
| `Save<T>(data, slotName)` | Serialize and write atomically |
| `Load<T>(slotName)` | Read and deserialize, fallback to backup, fallback to new() |
| `Delete(slotName)` | Remove primary + backup files |
| `Exists(slotName)` | Check if file is on disk |
| `GetFilePath(slotName)` | Get the full path (useful for debugging) |

### `SaveDataService` (MonoBehaviour singleton)

| Member | Description |
|---|---|
| `Register(data)` | Add to auto-save pool |
| `Unregister(data)` | Remove from auto-save pool |
| `SaveAll()` | Immediately save all registered data |
| `SaveSlot(name)` | Save one specific slot by name |
| `DeleteSlot(name)` | Delete one slot and unregister it |
| `SlotExists(name)` | Check if a slot file exists |
| `autoSaveInterval` | Periodic interval in seconds (0 = off) |
| `OnSaveAllCompleted` | Event fired after SaveAll (bool = any failure) |

---

## Editor Tools

**Tools → Save System → Save File Inspector**  
Read and inspect the raw JSON of any save slot directly in the Editor.

**Tools → Save System → Open Persistent Data Folder**  
Reveals `Application.persistentDataPath` in Finder / Explorer.
