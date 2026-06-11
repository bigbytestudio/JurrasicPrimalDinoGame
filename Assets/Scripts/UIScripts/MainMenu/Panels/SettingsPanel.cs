using SaveSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DinoGame.UI.Menu
{
    public sealed class SettingsPanel : UIPanelBase
    {
        [Header("Audio")]
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;
        [SerializeField] private TMP_Text musicValueText;
        [SerializeField] private TMP_Text sfxValueText;

        [Header("Graphics")]
        [SerializeField] private SettingsToggleOption lowGraphicsOption;
        [SerializeField] private SettingsToggleOption mediumGraphicsOption;
        [SerializeField] private SettingsToggleOption highGraphicsOption;

        [Header("Vibration")]
        [SerializeField] private SettingsToggleOption vibrationOnOption;
        [SerializeField] private SettingsToggleOption vibrationOffOption;

        [Header("Links")]
        [SerializeField] private Button languageButton;
        [SerializeField] private Button discordButton;
        [SerializeField] private Button termsButton;
        [SerializeField] private Button supportButton;
        [SerializeField] private Button privacyButton;
        [SerializeField] private Button rateUsButton;

        [Header("Panel")]
        [SerializeField] private Button closeButton;

        private bool suppressEvents;

        public override MenuPanelId PanelId => MenuPanelId.Settings;

        private void Awake()
        {
            EnsureSettingsLoaded();
            ResolveMissingReferences();
            PrepareToggleOptions();

            if (closeButton != null)
                closeButton.onClick.AddListener(CloseSelf);

            BindAudioSliders();
            BindGraphicsButtons();
            BindVibrationButtons();
            BindLinkButtons();
        }

        public override void OnPanelOpened(MenuContext context)
        {
            base.OnPanelOpened(context);
            RefreshFromSave();
        }

        private void OnDestroy()
        {
            UnbindAudioSliders();
        }

        private static void EnsureSettingsLoaded()
        {
            if (SettingsSave.Instance != null)
                return;

            SettingsSave settings = SettingsSave.Load();
            SettingsSave.Bind(settings);
            SaveDataService.Instance.Register(settings);
        }

        private void ResolveMissingReferences()
        {
            Transform root = transform;

            musicSlider ??= FindComponent<Slider>(root, "MusicRow/musicSlider");
            sfxSlider ??= FindComponent<Slider>(root, "SoundRow/musicSlider");
            musicValueText ??= FindComponent<TMP_Text>(root, "MusicRow/musicSliderValueTxt");
            sfxValueText ??= FindComponent<TMP_Text>(root, "SoundRow/musicSliderValueTxt");

            lowGraphicsOption.button ??= FindComponent<Button>(root, "Graphics/lowBtn");
            mediumGraphicsOption.button ??= FindComponent<Button>(root, "Graphics/mediumBtn");
            highGraphicsOption.button ??= FindComponent<Button>(root, "Graphics/HighBtn");

            vibrationOnOption.button ??= FindComponent<Button>(root, "Vibration/On");
            vibrationOffOption.button ??= FindComponent<Button>(root, "Vibration/OffBtn");

            languageButton ??= FindComponent<Button>(root, "Accounts/language Btn");
            discordButton ??= FindComponent<Button>(root, "Accounts/Discord");
            termsButton ??= FindComponent<Button>(root, "Accounts/terms");
            supportButton ??= FindComponent<Button>(root, "Accounts/Support");
            privacyButton ??= FindComponent<Button>(root, "Accounts/Policy");
            rateUsButton ??= FindComponent<Button>(root, "Accounts/RateUs");
        }

        private void PrepareToggleOptions()
        {
            lowGraphicsOption.Prepare();
            mediumGraphicsOption.Prepare();
            highGraphicsOption.Prepare();
            vibrationOnOption.Prepare();
            vibrationOffOption.Prepare();
        }

        private void BindAudioSliders()
        {
            if (musicSlider != null)
            {
                musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
                musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            }

            if (sfxSlider != null)
            {
                sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
                sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
            }
        }

        private void UnbindAudioSliders()
        {
            if (musicSlider != null)
                musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);

            if (sfxSlider != null)
                sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        }

        private void BindGraphicsButtons()
        {
            BindOption(lowGraphicsOption, () => SetGraphicsQuality(GraphicsQualityLevel.Low));
            BindOption(mediumGraphicsOption, () => SetGraphicsQuality(GraphicsQualityLevel.Medium));
            BindOption(highGraphicsOption, () => SetGraphicsQuality(GraphicsQualityLevel.High));
        }

        private void BindVibrationButtons()
        {
            BindOption(vibrationOnOption, () => SetVibration(true));
            BindOption(vibrationOffOption, () => SetVibration(false));
        }

        private void BindLinkButtons()
        {
            Bind(languageButton, OnLanguageClicked);
            Bind(discordButton, () => Menu?.OpenDiscord());
            Bind(termsButton, () => Menu?.OpenTerms());
            Bind(supportButton, () => Menu?.OpenSupport());
            Bind(privacyButton, () => Menu?.OpenPrivacyPolicy());
            Bind(rateUsButton, () => Menu?.OpenRateUs());
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null)
                return;

            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }

        private static void BindOption(SettingsToggleOption option, UnityEngine.Events.UnityAction action)
        {
            if (option?.button == null || action == null)
                return;

            option.button.onClick.RemoveListener(action);
            option.button.onClick.AddListener(action);
        }

        private void RefreshFromSave()
        {
            SettingsSave settings = SettingsSave.Instance;
            if (settings == null)
                return;

            suppressEvents = true;

            if (musicSlider != null)
                musicSlider.value = settings.musicVolume;

            if (sfxSlider != null)
                sfxSlider.value = settings.sfxVolume;

            UpdateVolumeLabel(musicValueText, settings.musicVolume);
            UpdateVolumeLabel(sfxValueText, settings.sfxVolume);
            ApplyGraphicsSelection(settings.graphicsQuality);
            ApplyVibrationSelection(settings.vibrationEnabled);

            suppressEvents = false;
        }

        private void OnMusicSliderChanged(float value)
        {
            UpdateVolumeLabel(musicValueText, value);

            if (suppressEvents)
                return;

            SettingsSave.Instance?.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            UpdateVolumeLabel(sfxValueText, value);

            if (suppressEvents)
                return;

            SettingsSave.Instance?.SetSfxVolume(value);
        }

        private void SetGraphicsQuality(GraphicsQualityLevel quality)
        {
            SettingsSave.Instance?.SetGraphicsQuality(quality);
            ApplyGraphicsSelection(quality);
        }

        private void SetVibration(bool enabled)
        {
            SettingsSave.Instance?.SetVibrationEnabled(enabled);
            ApplyVibrationSelection(enabled);
        }

        private void ApplyGraphicsSelection(GraphicsQualityLevel quality)
        {
            lowGraphicsOption.SetSelected(quality == GraphicsQualityLevel.Low);
            mediumGraphicsOption.SetSelected(quality == GraphicsQualityLevel.Medium);
            highGraphicsOption.SetSelected(quality == GraphicsQualityLevel.High);
        }

        private void ApplyVibrationSelection(bool enabled)
        {
            vibrationOnOption.SetSelected(enabled);
            vibrationOffOption.SetSelected(!enabled);
        }

        private static void UpdateVolumeLabel(TMP_Text label, float value)
        {
            if (label != null)
                label.text = Mathf.RoundToInt(value * 100f).ToString();
        }

        private static void OnLanguageClicked()
        {
            Debug.Log("[SettingsPanel] Language selection will be implemented later.");
        }

        private static T FindComponent<T>(Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return target != null ? target.GetComponent<T>() : null;
        }
    }
}
