using System;
using System.Collections.Generic;
using UnityEngine;

namespace DinoGame.UI.Menu
{
    [CreateAssetMenu(menuName = "Dino Game/UI/Menu Panel Registry", fileName = "MenuPanelRegistry")]
    public sealed class MenuPanelRegistry : ScriptableObject
    {
        [Serializable]
        public struct PanelEntry
        {
            public MenuPanelId panelId;
            public UIPanelBase prefab;
            [Tooltip("When enabled, the panel is hidden instead of destroyed to avoid re-instantiation cost.")]
            public bool cacheInstance;
        }

        [SerializeField] private PanelEntry[] panels;

        private Dictionary<MenuPanelId, PanelEntry> lookup;

        public bool TryGetEntry(MenuPanelId panelId, out PanelEntry entry)
        {
            BuildLookup();

            if (lookup.TryGetValue(panelId, out entry) && entry.prefab != null)
                return true;

            entry = default;
            return false;
        }

        private void BuildLookup()
        {
            if (lookup != null)
                return;

            lookup = new Dictionary<MenuPanelId, PanelEntry>();

            if (panels == null)
                return;

            for (int i = 0; i < panels.Length; i++)
            {
                PanelEntry entry = panels[i];
                if (entry.prefab == null)
                    continue;

                lookup[entry.panelId] = entry;
            }
        }

        private void OnEnable() => lookup = null;
    }
}
