#if UNITY_EDITOR
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SaveSystem.Editor
{
    /// <summary>
    /// Unity Editor window to inspect, open, and delete save files.
    /// Menu: Tools → Save System → Save File Inspector
    /// </summary>
    public class SaveFileInspector : EditorWindow
    {
        private string _slotInput   = "player_prefs";
        private string _jsonPreview = "";
        private Vector2 _scroll;

        [MenuItem("Tools/Save System/Save File Inspector")]
        public static void Open() => GetWindow<SaveFileInspector>("Save File Inspector");

        [MenuItem("Tools/Save System/Open Persistent Data Folder")]
        public static void OpenPersistentDataFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }

        private void OnGUI()
        {
            GUILayout.Label("Save File Inspector", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _slotInput = EditorGUILayout.TextField("Slot Name", _slotInput);
            string path = Path.Combine(Application.persistentDataPath, _slotInput + ".json");

            EditorGUILayout.LabelField("Path:", path, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Read File"))
            {
                if (File.Exists(path))
                    _jsonPreview = File.ReadAllText(path);
                else
                    _jsonPreview = "<No file found at this path>";
            }

            if (GUILayout.Button("Delete File"))
            {
                if (EditorUtility.DisplayDialog(
                    "Delete Save File",
                    $"Delete '{_slotInput}.json' and its backup? This cannot be undone.",
                    "Delete", "Cancel"))
                {
                    SaveDataManager.Delete(_slotInput);
                    _jsonPreview = "<Deleted>";
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            GUILayout.Label("JSON Content:", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_jsonPreview, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }
    }
}
#endif
