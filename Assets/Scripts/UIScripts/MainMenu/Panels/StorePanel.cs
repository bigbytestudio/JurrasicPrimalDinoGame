using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class StorePanel : UIPanelBase
    {
        private static readonly string[] SelectedChildNames = { "selectedImg", "select", "selected" };
        private static readonly string[] UnselectedChildNames = { "unselect", "unselected" };

        [Header("Tabs")]
        [SerializeField] private Transform tabButtonsRoot;
        [SerializeField] private Transform tabPanelsRoot;
        [SerializeField] private StoreTabConfig[] tabs = Array.Empty<StoreTabConfig>();
        [SerializeField] private StoreTab defaultTab = StoreTab.Bones;

        [Header("DOTween (optional)")]
        [SerializeField] private bool animateSelectedScale = true;
        [SerializeField] private float selectedScale = 1.08f;
        [SerializeField] private float unselectedScale = 1f;
        [SerializeField] private float scaleDuration = 0.2f;

        [Header("Panel")]
        [SerializeField] private Button closeButton;

        private readonly List<StoreTabConfig> runtimeTabs = new();
        private StoreTabConfig activeTabConfig;

        public override MenuPanelId PanelId => MenuPanelId.Store;
        public StoreTab ActiveTab { get; private set; }

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);

            BuildRuntimeTabs();
            BindTabButtons();
        }

        private void OnDestroy()
        {
            for (int i = 0; i < runtimeTabs.Count; i++)
            {
                StoreTabConfig tab = runtimeTabs[i];
                if (tab?.button == null)
                    continue;

                tab.button.onClick.RemoveAllListeners();
                StoreTabTween.Kill(tab.ScaleTransform);
            }
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            SelectTab(context?.StoreTab ?? defaultTab);
        }

        public void SelectTab(StoreTab tab)
        {
            if (runtimeTabs.Count == 0)
                BuildRuntimeTabs();

            StoreTabConfig target = FindTab(tab) ?? runtimeTabs[0];
            if (target == null)
                return;

            ApplyTabSelection(target);
        }

        private void BuildRuntimeTabs()
        {
            runtimeTabs.Clear();

            if (tabs != null && tabs.Length > 0)
            {
                for (int i = 0; i < tabs.Length; i++)
                {
                    StoreTabConfig configured = PrepareTab(tabs[i]);
                    if (configured != null)
                        runtimeTabs.Add(configured);
                }

                return;
            }

            AutoAssignTabs(replaceExisting: false);
        }

        private void AutoAssignTabs(bool replaceExisting)
        {
            Transform buttonsRoot = tabButtonsRoot != null ? tabButtonsRoot : transform.Find("TabButtons");
            Transform panelsRoot = tabPanelsRoot != null ? tabPanelsRoot : transform.Find("TabPanels");
            if (buttonsRoot == null)
                return;

            if (replaceExisting)
                runtimeTabs.Clear();

            for (int i = 0; i < buttonsRoot.childCount; i++)
            {
                Transform buttonTransform = buttonsRoot.GetChild(i);
                Button button = buttonTransform.GetComponent<Button>();
                if (button == null)
                    continue;

                StoreTab tabId = ResolveTabId(buttonTransform.name);
                StoreTabConfig config = new StoreTabConfig
                {
                    tab = tabId,
                    button = button,
                    selectedState = FindChildByNames(buttonTransform, SelectedChildNames),
                    unselectedState = FindChildByNames(buttonTransform, UnselectedChildNames),
                    contentPanel = panelsRoot != null ? FindPanelForTab(panelsRoot, tabId) : null,
                    scaleTarget = buttonTransform as RectTransform
                };

                StoreTabConfig prepared = PrepareTab(config);
                if (prepared == null)
                    continue;

                int existingIndex = IndexOfTab(tabId);
                if (existingIndex >= 0)
                    runtimeTabs[existingIndex] = prepared;
                else
                    runtimeTabs.Add(prepared);
            }
        }

        private StoreTabConfig PrepareTab(StoreTabConfig config)
        {
            if (config?.button == null)
                return null;

            Transform buttonTransform = config.button.transform;

            if (config.selectedState == null)
                config.selectedState = FindChildByNames(buttonTransform, SelectedChildNames);

            if (config.unselectedState == null)
                config.unselectedState = FindChildByNames(buttonTransform, UnselectedChildNames);

            if (config.scaleTarget == null)
                config.scaleTarget = buttonTransform as RectTransform;

            if (config.contentPanel == null && tabPanelsRoot != null)
                config.contentPanel = FindPanelForTab(tabPanelsRoot, config.tab);

            return config;
        }

        private void BindTabButtons()
        {
            for (int i = 0; i < runtimeTabs.Count; i++)
            {
                StoreTabConfig tab = runtimeTabs[i];
                if (tab?.button == null)
                    continue;

                StoreTab capturedTab = tab.tab;
                tab.button.onClick.RemoveAllListeners();
                tab.button.onClick.AddListener(() => SelectTab(capturedTab));
            }
        }

        private void ApplyTabSelection(StoreTabConfig selected)
        {
            activeTabConfig = selected;
            ActiveTab = selected.tab;

            for (int i = 0; i < runtimeTabs.Count; i++)
            {
                StoreTabConfig tab = runtimeTabs[i];
                bool isSelected = tab == selected;

                if (tab.selectedState != null)
                    tab.selectedState.SetActive(isSelected);

                if (tab.unselectedState != null)
                    tab.unselectedState.SetActive(!isSelected);

                if (tab.contentPanel != null)
                    tab.contentPanel.SetActive(isSelected);

                float scale = isSelected ? selectedScale : unselectedScale;
                StoreTabTween.AnimateScale(tab.ScaleTransform, scale, scaleDuration, animateSelectedScale);
            }
        }

        private StoreTabConfig FindTab(StoreTab tab)
        {
            int index = IndexOfTab(tab);
            return index >= 0 ? runtimeTabs[index] : null;
        }

        private int IndexOfTab(StoreTab tab)
        {
            for (int i = 0; i < runtimeTabs.Count; i++)
            {
                if (runtimeTabs[i].tab == tab)
                    return i;
            }

            return -1;
        }

        private void MergeManualTabOverrides(StoreTabConfig[] previousTabs)
        {
            if (previousTabs == null)
                return;

            for (int i = 0; i < previousTabs.Length; i++)
            {
                StoreTabConfig previous = previousTabs[i];
                if (previous == null)
                    continue;

                StoreTabConfig current = FindTab(previous.tab);
                if (current == null)
                    continue;

                if (current.contentPanel == null && previous.contentPanel != null)
                    current.contentPanel = previous.contentPanel;

                if (current.scaleTarget == null && previous.scaleTarget != null)
                    current.scaleTarget = previous.scaleTarget;
            }
        }

        private static StoreTab ResolveTabId(string tabName)
        {
            string normalized = tabName.ToLowerInvariant();

            if (normalized.Contains("bone"))
                return StoreTab.Bones;

            if (normalized.Contains("dna"))
                return StoreTab.Dna;

            if (normalized.Contains("offer"))
                return StoreTab.Offer;

            if (normalized.Contains("free"))
                return StoreTab.Free;

            return StoreTab.Bones;
        }

        private static GameObject FindPanelForTab(Transform panelsRoot, StoreTab tab)
        {
            string panelName = tab switch
            {
                StoreTab.Bones => "BonesPanel",
                StoreTab.Dna => "DNAPanel",
                StoreTab.Offer => "OfferPanel",
                StoreTab.Free => "FreePanel",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(panelName))
                return null;

            for (int i = 0; i < panelsRoot.childCount; i++)
            {
                Transform child = panelsRoot.GetChild(i);
                if (string.Equals(child.name, panelName, StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }

            return null;
        }

        private static GameObject FindChildByNames(Transform parent, string[] names)
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

#if UNITY_EDITOR
        public void EditorAutoAssignTabs()
        {
            StoreTabConfig[] previousTabs = tabs;

            if (tabButtonsRoot == null)
                tabButtonsRoot = transform.Find("TabButtons");

            if (tabPanelsRoot == null)
                tabPanelsRoot = transform.Find("TabPanels");

            runtimeTabs.Clear();
            AutoAssignTabs(replaceExisting: true);
            MergeManualTabOverrides(previousTabs);

            tabs = new StoreTabConfig[runtimeTabs.Count];
            runtimeTabs.CopyTo(tabs);
        }
#endif
    }
}
