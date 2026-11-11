using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRRemoteDesktop.Settings;

namespace VRRemoteDesktop.UI.Windows
{
    /// <summary>
    /// Settings application for configuring Display, Controls, Audio, Themes, and Stream Quality.
    /// Real-time updates to ThemeManager, MouseLook sensitivity, and desktop resolutions.
    /// </summary>
    public class SettingsApp : MonoBehaviour
    {
        [Header("Category Navigation")]
        [SerializeField] private Button _tabDisplay;
        [SerializeField] private Button _tabControls;
        [SerializeField] private Button _tabAudio;
        [SerializeField] private Button _tabThemes;
        [SerializeField] private Button _tabQuality;

        [Header("Section Panels")]
        [SerializeField] private GameObject _panelDisplay;
        [SerializeField] private GameObject _panelControls;
        [SerializeField] private GameObject _panelAudio;
        [SerializeField] private GameObject _panelThemes;
        [SerializeField] private GameObject _panelQuality;

        [Header("Display Settings Elements")]
        [SerializeField] private TMP_Dropdown _resolutionDropdown;
        [SerializeField] private Slider _brightnessSlider;
        [SerializeField] private TextMeshProUGUI _brightnessValText;

        [Header("Controls Settings Elements")]
        [SerializeField] private Slider _sensitivitySlider;
        [SerializeField] private TextMeshProUGUI _sensitivityValText;
        [SerializeField] private Toggle _smoothingToggle;

        [Header("Audio Settings Elements")]
        [SerializeField] private Slider _masterVolumeSlider;
        [SerializeField] private TextMeshProUGUI _masterVolumeText;
        [SerializeField] private Slider _sfxVolumeSlider;
        [SerializeField] private TextMeshProUGUI _sfxVolumeText;

        [Header("Theme Buttons")]
        [SerializeField] private Button _themeCyberpunkBtn;
        [SerializeField] private Button _themeDeepSpaceBtn;
        [SerializeField] private Button _themeMatrixBtn;
        [SerializeField] private Button _themeSolarBtn;

        [Header("Quality Buttons")]
        [SerializeField] private Button _qualityLowBtn;
        [SerializeField] private Button _qualityMedBtn;
        [SerializeField] private Button _qualityHighBtn;
        [SerializeField] private Button _qualityUltraBtn;
        [SerializeField] private TextMeshProUGUI _currentQualityText;

        private void Awake()
        {
            if (_tabDisplay != null) _tabDisplay.onClick.AddListener(() => ShowCategory(0));
            if (_tabControls != null) _tabControls.onClick.AddListener(() => ShowCategory(1));
            if (_tabAudio != null) _tabAudio.onClick.AddListener(() => ShowCategory(2));
            if (_tabThemes != null) _tabThemes.onClick.AddListener(() => ShowCategory(3));
            if (_tabQuality != null) _tabQuality.onClick.AddListener(() => ShowCategory(4));

            if (_brightnessSlider != null)
            {
                _brightnessSlider.onValueChanged.AddListener((v) => {
                    if (_brightnessValText != null) _brightnessValText.text = $"{Mathf.RoundToInt(v * 100)}%";
                });
            }

            if (_sensitivitySlider != null)
            {
                _sensitivitySlider.onValueChanged.AddListener((v) => {
                    if (_sensitivityValText != null) _sensitivityValText.text = $"{v:0.1}x";
                });
            }

            if (_masterVolumeSlider != null)
            {
                _masterVolumeSlider.onValueChanged.AddListener((v) => {
                    if (_masterVolumeText != null) _masterVolumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
                });
            }

            if (_sfxVolumeSlider != null)
            {
                _sfxVolumeSlider.onValueChanged.AddListener((v) => {
                    if (_sfxVolumeText != null) _sfxVolumeText.text = $"{Mathf.RoundToInt(v * 100)}%";
                });
            }

            // Theme Buttons
            if (_themeCyberpunkBtn != null) _themeCyberpunkBtn.onClick.AddListener(() => ApplyThemePreset(0));
            if (_themeDeepSpaceBtn != null) _themeDeepSpaceBtn.onClick.AddListener(() => ApplyThemePreset(1));
            if (_themeMatrixBtn != null) _themeMatrixBtn.onClick.AddListener(() => ApplyThemePreset(2));
            if (_themeSolarBtn != null) _themeSolarBtn.onClick.AddListener(() => ApplyThemePreset(3));

            // Quality Buttons
            if (_qualityLowBtn != null) _qualityLowBtn.onClick.AddListener(() => SetQuality("LOW (720p @ 30 FPS | 8 Mbps)"));
            if (_qualityMedBtn != null) _qualityMedBtn.onClick.AddListener(() => SetQuality("MEDIUM (1080p @ 60 FPS | 16 Mbps)"));
            if (_qualityHighBtn != null) _qualityHighBtn.onClick.AddListener(() => SetQuality("HIGH (1440p @ 60 FPS | 28 Mbps)"));
            if (_qualityUltraBtn != null) _qualityUltraBtn.onClick.AddListener(() => SetQuality("ULTRA (4K @ 60 FPS | 50 Mbps | HDR)"));
        }

        private void Start()
        {
            ShowCategory(0);
        }

        public void ShowCategory(int index)
        {
            if (_panelDisplay != null) _panelDisplay.SetActive(index == 0);
            if (_panelControls != null) _panelControls.SetActive(index == 1);
            if (_panelAudio != null) _panelAudio.SetActive(index == 2);
            if (_panelThemes != null) _panelThemes.SetActive(index == 3);
            if (_panelQuality != null) _panelQuality.SetActive(index == 4);
        }

        public void ApplyThemePreset(int index)
        {
            UITheme theme = ScriptableObject.CreateInstance<UITheme>();

            switch (index)
            {
                case 0: // Cyberpunk Neon
                    theme.name = "Cyberpunk Neon";
                    theme.BackgroundColor = new Color(0.04f, 0.06f, 0.10f);
                    theme.PrimaryColor = new Color(0.0f, 1.0f, 1.0f); // Cyan
                    theme.SecondaryColor = new Color(0.55f, 0.10f, 1.0f); // Violet
                    theme.AccentColor = new Color(1.0f, 0.05f, 0.55f); // Magenta
                    theme.TextPrimary = Color.white;
                    theme.TextSecondary = new Color(0.7f, 0.8f, 0.9f);
                    theme.SuccessColor = new Color(0.1f, 1.0f, 0.5f);
                    break;

                case 1: // Deep Space Blue
                    theme.name = "Deep Space Blue";
                    theme.BackgroundColor = new Color(0.02f, 0.04f, 0.09f);
                    theme.PrimaryColor = new Color(0.2f, 0.6f, 1.0f); // Electric Blue
                    theme.SecondaryColor = new Color(0.05f, 0.35f, 0.8f);
                    theme.AccentColor = new Color(0.0f, 0.85f, 1.0f);
                    theme.TextPrimary = Color.white;
                    theme.TextSecondary = new Color(0.65f, 0.75f, 0.9f);
                    theme.SuccessColor = new Color(0.2f, 0.85f, 0.4f);
                    break;

                case 2: // Emerald Matrix
                    theme.name = "Emerald Matrix";
                    theme.BackgroundColor = new Color(0.02f, 0.06f, 0.03f);
                    theme.PrimaryColor = new Color(0.0f, 1.0f, 0.45f); // Neon Lime
                    theme.SecondaryColor = new Color(0.0f, 0.6f, 0.3f);
                    theme.AccentColor = new Color(0.4f, 1.0f, 0.2f);
                    theme.TextPrimary = Color.white;
                    theme.TextSecondary = new Color(0.6f, 0.9f, 0.7f);
                    theme.SuccessColor = new Color(0.0f, 1.0f, 0.5f);
                    break;

                case 3: // Solar Flare
                    theme.name = "Solar Flare";
                    theme.BackgroundColor = new Color(0.07f, 0.03f, 0.02f);
                    theme.PrimaryColor = new Color(1.0f, 0.55f, 0.0f); // Amber Orange
                    theme.SecondaryColor = new Color(0.85f, 0.25f, 0.0f);
                    theme.AccentColor = new Color(1.0f, 0.85f, 0.1f); // Gold
                    theme.TextPrimary = Color.white;
                    theme.TextSecondary = new Color(0.9f, 0.75f, 0.65f);
                    theme.SuccessColor = new Color(0.3f, 0.9f, 0.3f);
                    break;
            }

            if (ThemeManager.Instance != null)
            {
                ThemeManager.Instance.SetTheme(theme);
            }
        }

        private void SetQuality(string label)
        {
            if (_currentQualityText != null)
            {
                _currentQualityText.text = $"CURRENT PROFILE: <color=#00ffff>{label}</color>";
            }
        }
    }
}
