using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class StorePanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private Text titleText;
        [SerializeField] private GameObject dnaSection;
        [SerializeField] private GameObject bonesSection;
        [SerializeField] private GameObject generalSection;

        public override MenuPanelId PanelId => MenuPanelId.Store;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            ApplyStoreTab(context?.StoreTab ?? StoreTab.General);
        }

        private void ApplyStoreTab(StoreTab tab)
        {
            if (titleText != null)
            {
                titleText.text = tab switch
                {
                    StoreTab.Dna => "DNA Store",
                    StoreTab.Bones => "Bones Store",
                    _ => "Store"
                };
            }

            if (generalSection != null)
                generalSection.SetActive(tab == StoreTab.General);

            if (dnaSection != null)
                dnaSection.SetActive(tab == StoreTab.Dna);

            if (bonesSection != null)
                bonesSection.SetActive(tab == StoreTab.Bones);
        }
    }
}
