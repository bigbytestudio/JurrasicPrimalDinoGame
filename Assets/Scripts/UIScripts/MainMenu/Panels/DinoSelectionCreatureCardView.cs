using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;

namespace DinoGame.UI.Menu
{
    public sealed class DinoSelectionCreatureCardView : MonoBehaviour
    {
        [SerializeField] private Image previewIconImage;
        [SerializeField] private TMP_Text creatureNameText;
        [SerializeField] private Image selectionHighlighter;
        [SerializeField] private GameObject unlockedIndicator;
        [SerializeField] private CanvasGroup cardCanvasGroup;
        [SerializeField] private Button selectButton;

        public CreatureProfile Profile { get; private set; }

        private Action<CreatureProfile> onSelected;

        private void Awake()
        {
            TryAutoBind();

            if (selectButton != null)
                selectButton.onClick.AddListener(HandleClick);
        }

        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleClick);
        }

        public void Bind(CreatureProfile profile, Action<CreatureProfile> selectedCallback)
        {
            Profile = profile;
            onSelected = selectedCallback;

            if (creatureNameText != null)
                creatureNameText.text = profile != null ? profile.displayName : string.Empty;

            CreaturePortraitUtility.ApplyCardPortrait(previewIconImage, profile);

            RefreshLockState();
        }

        public void RefreshLockState()
        {
            bool unlocked = CreatureUnlockUtility.IsUnlocked(Profile);

            if (unlockedIndicator != null)
                unlockedIndicator.SetActive(unlocked);

            if (cardCanvasGroup != null)
                cardCanvasGroup.alpha = unlocked ? 1f : 0.55f;
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlighter != null)
                selectionHighlighter.enabled = selected;
        }

        private void HandleClick()
        {
            if (Profile != null)
                onSelected?.Invoke(Profile);
        }

        private void TryAutoBind()
        {
            if (selectButton == null)
                selectButton = GetComponent<Button>();

            if (previewIconImage == null)
            {
                Transform iconTransform = transform.Find("Image");
                if (iconTransform != null)
                    previewIconImage = iconTransform.GetComponent<Image>();
            }

            if (creatureNameText == null)
            {
                Transform nameTransform = transform.Find("dinoName");
                if (nameTransform != null)
                    creatureNameText = nameTransform.GetComponent<TMP_Text>();
            }

            if (selectionHighlighter == null)
            {
                Transform highlightTransform = transform.Find("HighLighter");
                if (highlightTransform != null)
                    selectionHighlighter = highlightTransform.GetComponent<Image>();
            }

            if (unlockedIndicator == null)
            {
                Transform tickTransform = transform.Find("TickSign");
                if (tickTransform != null)
                    unlockedIndicator = tickTransform.gameObject;
            }

            if (cardCanvasGroup == null)
                cardCanvasGroup = GetComponent<CanvasGroup>();
        }
    }
}
