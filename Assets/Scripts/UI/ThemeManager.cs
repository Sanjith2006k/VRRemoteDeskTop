using System;
using UnityEngine;
using VRRemoteDesktop.Settings;

namespace VRRemoteDesktop.UI
{
    /// <summary>
    /// Singleton manager that distributes the current UITheme to UI components.
    /// </summary>
    public class ThemeManager : MonoBehaviour
    {
        public static ThemeManager Instance { get; private set; }

        [SerializeField] private UITheme _currentTheme;
        public UITheme CurrentTheme => _currentTheme;

        public event Action OnThemeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private int _themeIndex = 0;

        public void SetTheme(UITheme newTheme)
        {
            if (newTheme == null) return;
            _currentTheme = newTheme;
            OnThemeChanged?.Invoke();
        }

        public void CycleTheme()
        {
            _themeIndex = (_themeIndex + 1) % 4;
            var settings = UnityEngine.Object.FindAnyObjectByType<VRRemoteDesktop.UI.Windows.SettingsApp>();
            if (settings != null)
            {
                settings.ApplyThemePreset(_themeIndex);
            }
        }
    }
}
