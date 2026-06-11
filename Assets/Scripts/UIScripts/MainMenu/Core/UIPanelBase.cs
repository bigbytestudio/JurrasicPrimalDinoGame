using UnityEngine;

namespace DinoGame.UI.Menu
{
    public abstract class UIPanelBase : MonoBehaviour, IMenuPanel
    {
        [SerializeField] private bool destroyOnClose = true;

        public abstract MenuPanelId PanelId { get; }
        public bool DestroyOnClose => destroyOnClose;

        protected MenuManager Menu { get; private set; }

        internal void BindMenuManager(MenuManager manager)
        {
            Menu = manager;
        }

        public virtual void OnPanelOpened(MenuContext context)
        {
            gameObject.SetActive(true);
        }

        public virtual void OnPanelClosed()
        {
            gameObject.SetActive(false);
        }

        protected void CloseSelf()
        {
            Menu?.CloseCurrentPanel();
        }

        protected void OpenPanel(MenuPanelId panelId, MenuContext context = null)
        {
            Menu?.OpenPanel(panelId, context);
        }
    }
}
