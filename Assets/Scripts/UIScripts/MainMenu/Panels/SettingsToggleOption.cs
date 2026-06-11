using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    [Serializable]
    public sealed class SettingsToggleOption
    {
        private static readonly string[] SelectedChildNames = { "selectedImg", "select", "selected", "Selected" };
        private static readonly string[] UnselectedChildNames = { "unselect", "unselected", "UnSelected" };

        public Button button;
        public GameObject selectedState;
        public GameObject unselectedState;

        public void Prepare()
        {
            if (button == null)
                return;

            Transform buttonTransform = button.transform;

            if (selectedState == null)
                selectedState = FindChild(buttonTransform, SelectedChildNames);

            if (unselectedState == null)
                unselectedState = FindChild(buttonTransform, UnselectedChildNames);
        }

        public void SetSelected(bool selected)
        {
            if (selectedState != null)
                selectedState.SetActive(selected);

            if (unselectedState != null)
                unselectedState.SetActive(!selected);
        }

        private static GameObject FindChild(Transform parent, string[] names)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                for (int j = 0; j < names.Length; j++)
                {
                    if (string.Equals(child.name, names[j], StringComparison.OrdinalIgnoreCase))
                        return child.gameObject;
                }
            }

            return null;
        }
    }
}
