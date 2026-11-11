using System.Collections;
using UnityEngine;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive physical button console fulfilling Section 7 (Prop C).
    /// Provides 4 physical pressable buttons:
    /// - Button 0 (Cyan): Cycle Room Lighting Presets
    /// - Button 1 (Magenta): Surprise Disco Party Mode
    /// - Button 2 (Green): Play System Audio Chime
    /// - Button 3 (Violet): Toggle / Cycle Holographic Core
    /// </summary>
    public class InteractiveButtonPanel : InteractiveProp
    {
        [Header("Physical Buttons")]
        [SerializeField] private Transform[] _buttonTransforms;

        private bool[] _isPressing;
        private int _currentButtonIndex = 0;

        public override bool AllowInteractionInCinemaMode => true;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 3.2f;
            SetPromptOffset(new Vector3(0f, 0.30f, 0f));
            SetPromptAction("PRESS BUTTON (CHANGE LIGHTING)");
            _isPressing = new bool[_buttonTransforms != null ? _buttonTransforms.Length : 4];
        }

        protected override void OnInteract()
        {
            PressNextButton();
        }

        public void PressNextButton()
        {
            int count = _buttonTransforms != null && _buttonTransforms.Length > 0 ? _buttonTransforms.Length : 4;
            int index = _currentButtonIndex;
            _currentButtonIndex = (_currentButtonIndex + 1) % count;
            PressButton(index);
        }

        public void PressButton(int index)
        {
            if (_buttonTransforms == null || index < 0 || index >= _buttonTransforms.Length) return;
            if (_isPressing != null && index < _isPressing.Length && _isPressing[index]) return;

            StartCoroutine(ButtonPressRoutine(index));
        }

        private IEnumerator ButtonPressRoutine(int index)
        {
            _isPressing[index] = true;
            Transform btn = _buttonTransforms[index];

            // Click audio
            AudioManager.Instance?.PlayClickTone();

            // Depress button
            Vector3 originalLocalPos = btn.localPosition;
            Vector3 pressedPos = originalLocalPos - new Vector3(0f, 0.018f, 0f);

            float elapsed = 0f;
            while (elapsed < 0.08f)
            {
                elapsed += Time.deltaTime;
                btn.localPosition = Vector3.Lerp(originalLocalPos, pressedPos, elapsed / 0.08f);
                yield return null;
            }

            // Perform assigned action
            ExecuteButtonAction(index);

            // Pop back up
            elapsed = 0f;
            while (elapsed < 0.12f)
            {
                elapsed += Time.deltaTime;
                btn.localPosition = Vector3.Lerp(pressedPos, originalLocalPos, elapsed / 0.12f);
                yield return null;
            }
            btn.localPosition = originalLocalPos;

            yield return new WaitForSeconds(0.2f);
            _isPressing[index] = false;
        }

        private void ExecuteButtonAction(int index)
        {
            switch (index)
            {
                case 0: // Multi-Environment Theme Switcher
                    if (EnvironmentThemeController.Instance != null)
                        EnvironmentThemeController.Instance.CycleTheme();
                    else
                        RoomLightingController.Instance?.CyclePreset();
                    break;

                case 1: // Disco mode
                    RoomLightingController.Instance?.ToggleDiscoMode();
                    break;

                case 2: // Room Speaker Relaxing Music & Peaceful Lighting
                    AudioManager.Instance?.PlayConnectChime();
                    RoomSpeakerController.Instance?.TogglePlayPause();
                    RoomLightingController.Instance?.ApplyZenTranquilLighting();
                    break;

                case 3: // Hologram mode
                    HologramController.Instance?.CycleMode();
                    break;
            }

            AchievementManager.Instance?.UnlockAchievement("Button Tester", "Pushed tactile buttons on the workstation control deck.");
        }
    }
}
