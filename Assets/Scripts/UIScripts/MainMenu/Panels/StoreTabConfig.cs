using System;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    [Serializable]
    public sealed class StoreTabConfig
    {
        public StoreTab tab;
        public Button button;
        public GameObject selectedState;
        public GameObject unselectedState;
        public GameObject contentPanel;
        public RectTransform scaleTarget;

        public RectTransform ScaleTransform => scaleTarget != null
            ? scaleTarget
            : button != null ? button.transform as RectTransform : null;
    }
}
