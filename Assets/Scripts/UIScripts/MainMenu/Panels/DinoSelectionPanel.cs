using UnityEngine;
using UnityEngine.UI;
using DinoGame.Data;
using DinoGame.Spawn;

namespace DinoGame.UI.Menu
{
    public sealed class DinoSelectionPanel : UIPanelBase
    {
        [SerializeField] private Button closeButton;
        [SerializeField] private CreatureProfile[] selectableCreatures;

        public override MenuPanelId PanelId => MenuPanelId.DinoSelection;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            // Populate creature cards / list from selectableCreatures.
        }

        public void SelectCreature(CreatureProfile profile)
        {
            if (profile == null)
                return;

            if (SpawnManager.Instance != null)
                SpawnManager.Instance.SelectPlayerProfile(profile);
            else
                PlayerPrefs.SetString(SpawnManager.SelectedCreaturePrefsKey, profile.creatureId);

            CloseSelf();
        }
    }
}
