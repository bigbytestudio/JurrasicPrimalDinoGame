using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DinoGame.Data;
using DinoGame.Spawn;

namespace DinoGame.UI.Menu
{
    public sealed class DinoSelectionPanel : UIPanelBase
    {
        private const string StartHuntLabel = "START HUNT";
        private const string BuyLabel = "BUY";

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startHuntButton;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button upgradeDinoButton;
        [SerializeField] private Button dinoGrowthButton;
        [SerializeField] private TMP_Text startHuntLabelText;
        [SerializeField] private TMP_Text watchAdRewardText;

        [Header("Cards")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private DinoSelectionCreatureCardView cardTemplate;
        [SerializeField] private DinoSelectionProfileView profileView;
        [SerializeField] private CreatureProfile[] selectableCreatures;

        [Header("Sub Panels")]
        [SerializeField] private GameObject dinoGrowthPanelPrefab;
        [SerializeField] private GameObject upgradeDinoPanelPrefab;

        [Header("Rewards")]
        [SerializeField] private int watchAdBoneReward = 100;

        private readonly List<DinoSelectionCreatureCardView> spawnedCards = new();
        private CreatureProfile selectedProfile;
        private bool subPanelOpen;

        public override MenuPanelId PanelId => MenuPanelId.DinoSelection;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);

            if (startHuntButton != null)
                startHuntButton.onClick.AddListener(HandlePrimaryAction);

            if (watchAdButton != null)
                watchAdButton.onClick.AddListener(HandleWatchAd);

            if (upgradeDinoButton != null)
                upgradeDinoButton.onClick.AddListener(HandleUpgradeDino);

            if (dinoGrowthButton != null)
                dinoGrowthButton.onClick.AddListener(HandleDinoGrowth);

            TryAutoBind();
            RefreshWatchAdLabel();

            if (cardTemplate != null)
                cardTemplate.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            CreatureUnlockUtility.UnlockStateChanged += HandleUnlockStateChanged;
            GrowthUpgradeUtility.GrowthLevelChanged += HandleGrowthLevelChanged;
        }

        private void OnDisable()
        {
            CreatureUnlockUtility.UnlockStateChanged -= HandleUnlockStateChanged;
            GrowthUpgradeUtility.GrowthLevelChanged -= HandleGrowthLevelChanged;
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            RefreshCreatureList();
        }

        public void SelectCreature(CreatureProfile profile)
        {
            if (profile == null)
                return;

            selectedProfile = profile;
            UpdateCardSelection(profile);
            profileView?.Bind(profile);
            RefreshActionButtons();
        }

        private void HandlePrimaryAction()
        {
            if (selectedProfile == null)
                return;

            if (CreatureUnlockUtility.IsUnlocked(selectedProfile))
                ConfirmSelection();
            else
                TryBuySelectedCreature();
        }

        private void ConfirmSelection()
        {
            if (selectedProfile == null)
                return;

            if (SpawnManager.Instance != null)
                SpawnManager.Instance.SelectPlayerProfile(selectedProfile);
            else
                PlayerPrefs.SetString(SpawnManager.SelectedCreaturePrefsKey, selectedProfile.creatureId);

            CloseSelf();
        }

        private void TryBuySelectedCreature()
        {
            if (selectedProfile == null)
                return;

            if (CreatureUnlockUtility.TryPurchase(selectedProfile))
            {
                RefreshCreatureList();
                SelectCreature(selectedProfile);
                return;
            }

            Debug.Log($"Not enough bones to unlock {selectedProfile.displayName}. Cost: {selectedProfile.bonePurchaseCost}.", this);
        }

        private void HandleWatchAd()
        {
            // Ad SDK integration will replace this placeholder reward grant.
            GameDataSave.Instance?.AddBonesCurrency(watchAdBoneReward);
        }

        private void HandleUpgradeDino()
        {
            if (selectedProfile == null)
                return;

            if (!CreatureUnlockUtility.IsUnlocked(selectedProfile))
                return;

            if (GrowthUpgradeUtility.IsMaxGrowth(selectedProfile))
            {
                Debug.Log($"{selectedProfile.displayName} is already at max growth level.", this);
                return;
            }

            GrowthUpgradePopupController.ShowConfirmation(selectedProfile, RefreshSelectedProfile);
        }

        private void HandleDinoGrowth()
        {
            if (selectedProfile == null || subPanelOpen)
                return;

            if (!CreatureUnlockUtility.IsUnlocked(selectedProfile))
                return;

            if (dinoGrowthPanelPrefab == null)
            {
                Debug.LogWarning("Dino growth panel prefab is not assigned on DinoSelectionPanel.", this);
                return;
            }

            subPanelOpen = true;
            DinoGrowthPanel.Open(transform, dinoGrowthPanelPrefab, selectedProfile, () => subPanelOpen = false);
        }

        private void HandleUnlockStateChanged()
        {
            RefreshCardLockStates();
            RefreshActionButtons();
        }

        private void HandleGrowthLevelChanged()
        {
            RefreshSelectedProfile();
        }

        private void RefreshSelectedProfile()
        {
            if (selectedProfile != null)
                profileView?.Bind(selectedProfile);
        }

        private void RefreshCreatureList()
        {
            ClearSpawnedCards();

            CreatureProfile[] creatures = ResolveSelectableCreatures();
            if (creatures == null || creatures.Length == 0 || cardTemplate == null || cardContainer == null)
                return;

            CreatureProfile firstValidProfile = null;

            for (int i = 0; i < creatures.Length; i++)
            {
                CreatureProfile profile = creatures[i];
                if (profile == null)
                    continue;

                DinoSelectionCreatureCardView card = Instantiate(cardTemplate, cardContainer);
                card.gameObject.SetActive(true);
                card.Bind(profile, SelectCreature);
                spawnedCards.Add(card);

                firstValidProfile ??= profile;
            }

            if (firstValidProfile != null)
                SelectCreature(firstValidProfile);
        }

        private CreatureProfile[] ResolveSelectableCreatures()
        {
            if (selectableCreatures != null && selectableCreatures.Length > 0)
                return selectableCreatures;

            return SpawnManager.Instance != null
                ? SpawnManager.Instance.GetSelectablePlayerProfiles()
                : System.Array.Empty<CreatureProfile>();
        }

        private void UpdateCardSelection(CreatureProfile profile)
        {
            for (int i = 0; i < spawnedCards.Count; i++)
            {
                DinoSelectionCreatureCardView card = spawnedCards[i];
                if (card == null)
                    continue;

                card.SetSelected(card.Profile == profile);
            }
        }

        private void RefreshCardLockStates()
        {
            for (int i = 0; i < spawnedCards.Count; i++)
                spawnedCards[i]?.RefreshLockState();
        }

        private void RefreshActionButtons()
        {
            bool hasSelection = selectedProfile != null;
            bool unlocked = hasSelection && CreatureUnlockUtility.IsUnlocked(selectedProfile);

            if (startHuntLabelText != null)
            {
                startHuntLabelText.text = unlocked
                    ? StartHuntLabel
                    : $"{BuyLabel} {selectedProfile.bonePurchaseCost}";
            }

            if (upgradeDinoButton != null)
                upgradeDinoButton.gameObject.SetActive(unlocked);

            if (dinoGrowthButton != null)
                dinoGrowthButton.gameObject.SetActive(unlocked);
        }

        private void RefreshWatchAdLabel()
        {
            if (watchAdRewardText != null)
                watchAdRewardText.text = watchAdBoneReward.ToString();
        }

        private void ClearSpawnedCards()
        {
            for (int i = spawnedCards.Count - 1; i >= 0; i--)
            {
                DinoSelectionCreatureCardView card = spawnedCards[i];
                if (card == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(card.gameObject);
                else
                    DestroyImmediate(card.gameObject);
            }

            spawnedCards.Clear();
        }

        private void TryAutoBind()
        {
            if (cardContainer == null)
            {
                Transform content = transform.Find("GameObject/Scroll View/Viewport/Content");
                if (content != null)
                    cardContainer = content;
            }

            if (cardTemplate == null && cardContainer != null)
            {
                Transform cardTransform = cardContainer.Find("characterCard");
                if (cardTransform != null)
                {
                    cardTemplate = cardTransform.GetComponent<DinoSelectionCreatureCardView>();
                    if (cardTemplate == null)
                        cardTemplate = cardTransform.gameObject.AddComponent<DinoSelectionCreatureCardView>();
                }
            }

            if (profileView == null)
            {
                profileView = GetComponentInChildren<DinoSelectionProfileView>(true);
                if (profileView == null)
                    profileView = gameObject.AddComponent<DinoSelectionProfileView>();
            }

            if (startHuntButton == null)
            {
                Transform startHunt = transform.Find("RightSide/startHunt");
                if (startHunt != null)
                    startHuntButton = startHunt.GetComponent<Button>();
            }

            if (watchAdButton == null)
            {
                Transform watchAd = transform.Find("RightSide/WatchAd");
                if (watchAd != null)
                    watchAdButton = watchAd.GetComponent<Button>();
            }

            if (upgradeDinoButton == null)
            {
                Transform upgradeDino = transform.Find("RightSide/upgradeDino");
                if (upgradeDino != null)
                    upgradeDinoButton = upgradeDino.GetComponent<Button>();
            }

            if (dinoGrowthButton == null)
            {
                Transform dinoGrowth = transform.Find("RightSide/dinoGrowthTxt");
                if (dinoGrowth != null)
                    dinoGrowthButton = dinoGrowth.GetComponent<Button>();
            }

            if (startHuntLabelText == null && startHuntButton != null)
                startHuntLabelText = startHuntButton.GetComponentInChildren<TMP_Text>(true);

            if (watchAdRewardText == null && watchAdButton != null)
            {
                Transform amountTransform = watchAdButton.transform.Find("amountTxt");
                if (amountTransform != null)
                    watchAdRewardText = amountTransform.GetComponent<TMP_Text>();
            }

            EnsurePrimaryActionLabel();
        }

        private void EnsurePrimaryActionLabel()
        {
            if (startHuntLabelText != null || startHuntButton == null)
                return;

            GameObject labelObject = new GameObject("actionLabel", typeof(RectTransform));
            labelObject.transform.SetParent(startHuntButton.transform, false);

            RectTransform rectTransform = labelObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            startHuntLabelText = labelObject.AddComponent<TextMeshProUGUI>();
            startHuntLabelText.alignment = TextAlignmentOptions.Center;
            startHuntLabelText.fontSize = 28;
            startHuntLabelText.fontStyle = FontStyles.Bold;
            startHuntLabelText.color = Color.white;
        }
    }
}
