#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace DinoGame.UI.Menu.Editor
{
    [CustomEditor(typeof(StorePanel))]
    public sealed class StorePanelEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            DrawDotweenStatus();
            EditorGUILayout.Space(4f);

            StorePanel panel = (StorePanel)target;
            if (GUILayout.Button("Auto-Assign Tabs From Hierarchy"))
            {
                panel.EditorAutoAssignTabs();
                EditorUtility.SetDirty(panel);
            }
        }

        private static void DrawDotweenStatus()
        {
            bool dotweenInstalled = StoreTabTween.IsDotweenAvailable;

            if (dotweenInstalled)
            {
                EditorGUILayout.HelpBox(
                    "DOTween is installed. Selected tab buttons can use animated scale when 'Animate Selected Scale' is enabled.",
                    MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox(
                "DOTween is not installed yet. Tab switching still works, but selected-button scale will apply instantly. " +
                "Import DOTween and enable 'Animate Selected Scale' to use tweens.",
                MessageType.Warning);
        }
    }
}
#endif
