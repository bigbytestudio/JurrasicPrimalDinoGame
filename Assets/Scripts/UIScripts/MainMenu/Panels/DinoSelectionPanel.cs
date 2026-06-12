using System;
using System.Collections;
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

        [Header("Actions")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button startHuntButton;
        [SerializeField] private Button watchAdButton;
        [SerializeField] private Button upgradeDinoButton;
        [SerializeField] private Button dinoGrowthButton;
        [SerializeField] private GameObject startHuntLabelRoot;
        [SerializeField] private TMP_Text startHuntLabelText;
        [SerializeField] private GameObject buyNowItemsRoot;
        [SerializeField] private TMP_Text buyPriceText;
        [SerializeField] private TMP_Text watchAdRewardText;
        [SerializeField] private RectTransform actionButtonsLayoutRoot;

        [Header("Cards")]
        [SerializeField] private Transform cardContainer;
        [SerializeField] private DinoSelectionCreatureCardView cardTemplate;
        [SerializeField] private DinoSelectionProfileView profileView;

        [Header("Sub Panels")]
        [SerializeField] private GameObject dinoGrowthPanelPrefab;
        [SerializeField] private GameObject upgradeDinoPanelPrefab;

        [Header("Rewards")]
        [SerializeField] private int watchAdBoneReward = 100;

        [Header("Card Domino")]
        [SerializeField] private bool playCardDomino = true;
        [SerializeField] private float cardDominoStagger = 0.07f;
        [SerializeField] private float cardDominoDuration = 0.4f;
        [SerializeField] private float cardDominoStartRotation = 16f;

        [Header("Panel Reveal")]
        [SerializeField] private bool playPanelReveal = true;
        [SerializeField] private float revealStagger = 0.1f;
        [SerializeField] private float revealDuration = 0.38f;
        [SerializeField] private float revealStartScale = 0.9f;
        [SerializeField] private float closeDuration = 0.22f;
        [SerializeField] private float cardRevealDelay = 0.18f;

        private readonly List<DinoSelectionCreatureCardView> spawnedCards = new();
        private readonly List<RectTransform> animatedCreatureCards = new();
        private readonly List<RectTransform> animatedSections = new();
        private CreatureProfile selectedProfile;
        private bool subPanelOpen;
        private bool isClosing;
        private Coroutine cardDominoRoutine;
        private Coroutine panelRevealRoutine;

        public override MenuPanelId PanelId => MenuPanelId.DinoSelection;

        private void Awake()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(HandleCloseRequested);

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
            RebuildActionButtonsLayout();

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
            StopPanelRevealRoutine();

            if (isClosing)
            {
                UIDominoTween.StopTweens(animatedCreatureCards);
                animatedCreatureCards.Clear();
                UIDominoTween.StopTweens(animatedSections);
                animatedSections.Clear();
                return;
            }

            StopCardDomino();
            StopPanelReveal();
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            isClosing = false;
            RefreshCreatureList();
            StopPanelRevealRoutine();
            panelRevealRoutine = StartCoroutine(PlayPanelRevealRoutine());
        }

        private void HandleCloseRequested()
        {
            if (isClosing)
                return;

            isClosing = true;
            PlayPanelClose(CloseSelf);
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

            HandleCloseRequested();
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

            PrepareSpawnedCardsHidden();
            PlayCardDomino();
        }

        private void PlayCardDomino()
        {
            if (!playCardDomino || spawnedCards.Count == 0)
                return;

            if (cardDominoRoutine != null)
                StopCoroutine(cardDominoRoutine);

            cardDominoRoutine = StartCoroutine(PlayCardDominoRoutine());
        }

        private IEnumerator PlayPanelRevealRoutine()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();

            PreparePanelRevealBeforeFirstFrame();
            PlayPanelReveal();
            panelRevealRoutine = null;
        }

        private void StopPanelRevealRoutine()
        {
            if (panelRevealRoutine == null)
                return;

            StopCoroutine(panelRevealRoutine);
            panelRevealRoutine = null;
        }

        private IEnumerator PlayCardDominoRoutine()
        {
            if (playPanelReveal)
                yield return new WaitForSeconds(cardRevealDelay);
            else
                yield return null;

            animatedCreatureCards.Clear();
            animatedCreatureCards.AddRange(CollectSpawnedCardRects());

            if (animatedCreatureCards.Count == 0)
            {
                cardDominoRoutine = null;
                yield break;
            }

            UIDominoTween.Play(animatedCreatureCards, BuildCardDominoConfig());
            cardDominoRoutine = null;
        }

        private void StopCardDomino(bool keepRoutine = false)
        {
            if (!keepRoutine && cardDominoRoutine != null)
            {
                StopCoroutine(cardDominoRoutine);
                cardDominoRoutine = null;
            }

            UIDominoTween.Kill(animatedCreatureCards);
            animatedCreatureCards.Clear();
        }

        private void PreparePanelRevealBeforeFirstFrame()
        {
            if (!playPanelReveal)
                return;

            StopPanelReveal();
            animatedSections.AddRange(CollectSectionAnimationTargets());
            UIDominoTween.PrepareHidden(animatedSections, BuildRevealConfig());
        }

        private void PlayPanelReveal()
        {
            if (!playPanelReveal || animatedSections.Count == 0)
                return;

            UIDominoTween.Play(animatedSections, BuildRevealConfig());
        }

        private void PlayPanelClose(Action onComplete)
        {
            StopPanelRevealRoutine();

            if (cardDominoRoutine != null)
            {
                StopCoroutine(cardDominoRoutine);
                cardDominoRoutine = null;
            }

            UIDominoTween.StopTweens(animatedCreatureCards);
            animatedCreatureCards.Clear();
            UIDominoTween.StopTweens(animatedSections);

            List<RectTransform> closingTargets = BuildClosingTargets();
            if (!playPanelReveal || closingTargets.Count == 0)
            {
                onComplete?.Invoke();
                return;
            }

            UIDominoTween.PlayClose(
                closingTargets,
                revealStagger * 0.55f,
                closeDuration,
                revealStartScale,
                onComplete);
        }

        private List<RectTransform> BuildClosingTargets()
        {
            List<RectTransform> closingTargets = new(animatedSections.Count + spawnedCards.Count);
            closingTargets.AddRange(animatedSections);

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null && spawnedCards[i].transform is RectTransform rect)
                    closingTargets.Add(rect);
            }

            return closingTargets;
        }

        private void StopPanelReveal(bool clearTargets = true)
        {
            UIDominoTween.Kill(animatedSections);

            if (clearTargets)
                animatedSections.Clear();
        }

        private UIStaggerConfig BuildRevealConfig()
        {
            return UIStaggerConfig.FadeScale(revealStagger, revealDuration, revealStartScale);
        }

        private UIStaggerConfig BuildCardDominoConfig()
        {
            return UIStaggerConfig.Domino(cardDominoStagger, cardDominoDuration, cardDominoStartRotation);
        }

        private void PrepareSpawnedCardsHidden()
        {
            if (!playCardDomino)
                return;

            List<RectTransform> cards = CollectSpawnedCardRects();
            if (cards.Count == 0)
                return;

            UIDominoTween.PrepareHidden(cards, BuildCardDominoConfig());
        }

        private List<RectTransform> CollectSpawnedCardRects()
        {
            List<RectTransform> cards = new(spawnedCards.Count);

            for (int i = 0; i < spawnedCards.Count; i++)
            {
                if (spawnedCards[i] != null && spawnedCards[i].transform is RectTransform rect)
                    cards.Add(rect);
            }

            return cards;
        }

        private List<RectTransform> CollectSectionAnimationTargets()
        {
            List<RectTransform> sections = new(3);
            Transform root = transform;

            AddSectionIfFound(sections, root, "TopBar");
            AddSectionIfFound(sections, root, "StatsPanel");
            AddSectionIfFound(sections, root, "RightSide");

            return sections;
        }

        private static void AddSectionIfFound(List<RectTransform> sections, Transform root, string path)
        {
            if (root == null)
                return;

            Transform target = root.Find(path);
            if (target is RectTransform rect)
                sections.Add(rect);
        }

        private CreatureProfile[] ResolveSelectableCreatures()
        {
            if (CreatureRegistry.Instance != null)
            {
                CreatureProfile[] registryCreatures = CreatureRegistry.Instance.Creatures;
                if (registryCreatures != null && registryCreatures.Length > 0)
                    return registryCreatures;
            }

            CreatureRegistry fallbackRegistry = CreatureRegistry.EnsureExists();
            if (fallbackRegistry != null)
            {
                CreatureProfile[] registryCreatures = fallbackRegistry.Creatures;
                if (registryCreatures != null && registryCreatures.Length > 0)
                    return registryCreatures;
            }

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

            if (startHuntLabelRoot != null)
                startHuntLabelRoot.SetActive(unlocked);

            if (startHuntLabelText != null && unlocked)
                startHuntLabelText.text = StartHuntLabel;

            if (buyNowItemsRoot != null)
                buyNowItemsRoot.SetActive(hasSelection && !unlocked);

            if (buyPriceText != null && hasSelection && !unlocked)
                buyPriceText.text = selectedProfile.bonePurchaseCost.ToString();

            if (upgradeDinoButton != null)
                upgradeDinoButton.gameObject.SetActive(unlocked);

            if (dinoGrowthButton != null)
                dinoGrowthButton.gameObject.SetActive(unlocked);

            RebuildActionButtonsLayout();
        }

        private void RebuildActionButtonsLayout()
        {
            RectTransform layoutRoot = ResolveActionButtonsLayoutRoot();
            if (layoutRoot == null)
                return;

            LayoutRebuilder.ForceRebuildLayoutImmediate(layoutRoot);
        }

        private RectTransform ResolveActionButtonsLayoutRoot()
        {
            if (actionButtonsLayoutRoot != null)
                return actionButtonsLayoutRoot;

            Transform found = transform.Find("RightSide/ForContentAdjustment");
            if (found != null)
                actionButtonsLayoutRoot = found as RectTransform;

            return actionButtonsLayoutRoot;
        }

        private void RefreshWatchAdLabel()
        {
            if (watchAdRewardText != null)
                watchAdRewardText.text = watchAdBoneReward.ToString();
        }

        private void ClearSpawnedCards()
        {
            StopCardDomino();

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

            if (startHuntButton != null)
            {
                Transform startHuntRoot = startHuntButton.transform;

                if (startHuntLabelRoot == null)
                {
                    Transform startTxt = startHuntRoot.Find("StartTxt");
                    if (startTxt != null)
                        startHuntLabelRoot = startTxt.gameObject;
                }

                if (startHuntLabelText == null && startHuntLabelRoot != null)
                    startHuntLabelText = startHuntLabelRoot.GetComponent<TMP_Text>();

                if (buyNowItemsRoot == null)
                {
                    Transform buyNowItems = startHuntRoot.Find("buyNowItems");
                    if (buyNowItems != null)
                        buyNowItemsRoot = buyNowItems.gameObject;
                }

                if (buyPriceText == null && buyNowItemsRoot != null)
                {
                    Transform priceTransform = buyNowItemsRoot.transform.Find("priceTxt");
                    if (priceTransform != null)
                        buyPriceText = priceTransform.GetComponent<TMP_Text>();
                }
            }

            if (watchAdRewardText == null && watchAdButton != null)
            {
                Transform amountTransform = watchAdButton.transform.Find("amountTxt");
                if (amountTransform != null)
                    watchAdRewardText = amountTransform.GetComponent<TMP_Text>();
            }

            if (actionButtonsLayoutRoot == null)
            {
                Transform layoutRoot = transform.Find("RightSide/ForContentAdjustment");
                if (layoutRoot != null)
                    actionButtonsLayoutRoot = layoutRoot as RectTransform;
            }
        }
    }
}
