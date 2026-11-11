using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.Player;

namespace VRRemoteDesktop.Environment
{
    public enum DrinkEffectType
    {
        None,
        CaffeineOverload,
        ZenMode,
        ChaosMode
    }

    /// <summary>
    /// Master coordinator for Coffee and Tea beverage effects.
    /// Manages 30-second timers, funny robot assistant reactions, dynamic room lighting shifts,
    /// floating zen leaves, camera jitter/calm, humorous HUD banners, and the secret 15-second Chaos Mode.
    /// Guarantees clean state restoration, prevents overlapping timers, and preserves player movement.
    /// </summary>
    public class DrinkEffectManager : MonoBehaviour
    {
        public static DrinkEffectManager Instance { get; private set; }

        [Header("State")]
        [SerializeField] private DrinkEffectType _currentEffect = DrinkEffectType.None;
        [SerializeField] private float _remainingTime = 0f;

        private float _lastCoffeeTime = -999f;
        private float _lastTeaTime = -999f;
        private Coroutine _activeEffectRoutine;

        // Saved environmental state for guaranteed restoration
        private LightingPreset _savedLightingPreset = LightingPreset.CyberpunkNeon;
        private bool _hasSavedLighting = false;

        // Floating Zen Leaves & Particles
        private GameObject _leavesContainer;
        private List<Transform> _floatingLeaves = new List<Transform>();

        // HUD Banner
        private GameObject _hudBannerObj;
        private TextMeshProUGUI _hudBannerText;
        private CanvasGroup _hudBannerGroup;
        private UnityEngine.UI.Image _hudBorderImg;

        // Camera jitter tracking
        private Camera _mainCamera;
        private Vector3 _originalCamLocalPos;
        private Transform _camTransform;

        public DrinkEffectType CurrentEffect => _currentEffect;
        public float RemainingTime => _remainingTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var existing = UnityEngine.Object.FindAnyObjectByType<DrinkEffectManager>();
                if (existing == null)
                {
                    GameObject go = new GameObject("DrinkEffectManager");
                    go.AddComponent<DrinkEffectManager>();
                    DontDestroyOnLoad(go);
                }
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            BuildHUDBanner();
            BuildFloatingLeavesSystem();
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null)
            {
                _camTransform = _mainCamera.transform;
            }
        }

        private void Update()
        {
            if (_camTransform == null && Camera.main != null)
            {
                _mainCamera = Camera.main;
                _camTransform = _mainCamera.transform;
            }

            // Animate floating zen leaves if active
            if ((_currentEffect == DrinkEffectType.ZenMode || _currentEffect == DrinkEffectType.ChaosMode) && _leavesContainer != null && _leavesContainer.activeSelf)
            {
                UpdateFloatingLeaves();
            }
        }

        // =========================================================================
        // PUBLIC INGESTION APIS
        // =========================================================================

        public void ConsumeCoffee()
        {
            float now = Time.time;
            _lastCoffeeTime = now;
            Debug.Log("[DrinkEffectManager] Consumed Coffee.");

            // Check for Secret Combination (Coffee + Tea within 15 seconds)
            if (Mathf.Abs(now - _lastTeaTime) <= 15.0f)
            {
                TriggerEffect(DrinkEffectType.ChaosMode);
            }
            else
            {
                TriggerEffect(DrinkEffectType.CaffeineOverload);
            }
        }

        public void ConsumeTea()
        {
            float now = Time.time;
            _lastTeaTime = now;
            Debug.Log("[DrinkEffectManager] Consumed Tea.");

            // Check for Secret Combination (Tea + Coffee within 15 seconds)
            if (Mathf.Abs(now - _lastCoffeeTime) <= 15.0f)
            {
                TriggerEffect(DrinkEffectType.ChaosMode);
            }
            else
            {
                TriggerEffect(DrinkEffectType.ZenMode);
            }
        }

        // =========================================================================
        // EFFECT STATE MACHINE
        // =========================================================================

        public void TriggerEffect(DrinkEffectType effectType)
        {
            // Cleanly stop any existing routine and save pristine baseline lighting if not already saved
            if (_activeEffectRoutine != null)
            {
                StopCoroutine(_activeEffectRoutine);
                _activeEffectRoutine = null;
            }

            if (!_hasSavedLighting && RoomLightingController.Instance != null && !RoomLightingController.Instance.IsDiscoActive)
            {
                _savedLightingPreset = RoomLightingController.Instance.CurrentPreset;
                _hasSavedLighting = true;
            }

            _currentEffect = effectType;

            switch (effectType)
            {
                case DrinkEffectType.CaffeineOverload:
                    _activeEffectRoutine = StartCoroutine(CaffeineOverloadRoutine());
                    break;

                case DrinkEffectType.ZenMode:
                    _activeEffectRoutine = StartCoroutine(ZenModeRoutine());
                    break;

                case DrinkEffectType.ChaosMode:
                    _activeEffectRoutine = StartCoroutine(ChaosModeRoutine());
                    break;

                default:
                    RestoreEnvironment();
                    break;
            }
        }

        // =========================================================================
        // 1. CAFFEINE OVERLOAD (30 SECONDS)
        // =========================================================================
        private IEnumerator CaffeineOverloadRoutine()
        {
            float duration = 30.0f;
            _remainingTime = duration;

            // Audio & HUD
            AudioManager.Instance?.PlayConnectChime();
            ShowHUDBanner("<b><color=#ffaa00>[ CAFFEINE OVERLOAD ACTIVATED ]</color></b>  <size=20><color=#ffffff>(30s)</color></size>\n<size=16><color=#ffe8b0>Heart Rate: <b>195 BPM</b>  |  Clock: <b>10.0 GHz</b>  |  Robot Assistant: <b>Hyperactive!</b></color></size>", duration, new Color(1.0f, 0.65f, 0.10f, 0.95f));

            // Configure Robot Assistant
            var bot = FindRobotAssistant();
            if (bot != null)
            {
                bot.SetSpecialMode(RobotAssistant.RobotMode.Hyperactive);
            }

            // Strobe Room Lighting (Alternates between energetic Sunset Amber and Neon Cyan)
            var lighting = RoomLightingController.Instance;

            float elapsed = 0f;
            float pulseTimer = 0f;
            int lightFlip = 0;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _remainingTime = duration - elapsed;
                pulseTimer += Time.deltaTime;

                // Subtle fast energetic room lighting pulse
                if (lighting != null && pulseTimer > 0.45f)
                {
                    pulseTimer = 0f;
                    lightFlip = (lightFlip + 1) % 2;
                    lighting.ApplyPreset((lightFlip == 0) ? LightingPreset.SunsetAmber : LightingPreset.CyberpunkNeon);
                }

                yield return null;
            }

            // Conclude
            ShowHUDBanner("<b><color=#55ff99>[ CAFFEINE SPIKE SUBSIDING ]</color></b>\n<size=16><color=#d0d8e0>Heart rate and clock speeds returning to nominal parameters.</color></size>", 3.0f, new Color(0.2f, 0.85f, 0.5f, 0.9f));
            RestoreEnvironment();
        }

        // =========================================================================
        // 2. ZEN MODE (30 SECONDS)
        // =========================================================================
        private IEnumerator ZenModeRoutine()
        {
            float duration = 30.0f;
            _remainingTime = duration;

            // Audio & Music: Play prompt chime and 30-second serene peaceful music
            AudioManager.Instance?.PlayPromptTone();
            AudioManager.Instance?.PlayPeacefulMusic(duration);

            ShowHUDBanner("<b><color=#55ff99>[ ZEN MODE ACTIVATED ]</color></b>  <size=20><color=#ffffff>(30s)</color></size>\n<size=16><color=#d6ffe4>Inner Peace: <b>100%</b>  |  Blood Pressure: <b>Serene</b>  |  Robot Assistant: <b>Meditating</b></color></size>", duration, new Color(0.15f, 0.95f, 0.55f, 0.95f));

            // Configure Robot Assistant
            var bot = FindRobotAssistant();
            if (bot != null)
            {
                bot.SetSpecialMode(RobotAssistant.RobotMode.Zen);
            }

            // Calming Ambient Lighting: Serene Jade Twilight
            var lighting = RoomLightingController.Instance;
            if (lighting != null)
            {
                lighting.ApplyZenTranquilLighting();
            }

            // Activate Floating Zen Leaves
            if (_leavesContainer != null)
            {
                _leavesContainer.SetActive(true);
                ResetFloatingLeaves();
            }

            if (_camTransform != null)
            {
                _originalCamLocalPos = _camTransform.localPosition;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _remainingTime = duration - elapsed;

                // Subtle, rhythmic, tranquil breathing motion (deep calming breath cycle)
                if (_camTransform != null)
                {
                    float breath = Mathf.Sin(elapsed * 0.785f) * 0.010f;
                    _camTransform.localPosition = _originalCamLocalPos + new Vector3(0f, breath, 0f);
                }

                yield return null;
            }

            // Conclude
            ShowHUDBanner("<b><color=#00e5ff>[ RETURNING FROM MEDITATION ]</color></b>\n<size=16><color=#d0d8e0>Inner tranquility successfully compiled into daily workflow.</color></size>", 3.0f, new Color(0f, 0.85f, 0.95f, 0.9f));
            AudioManager.Instance?.StopPeacefulMusic(2.5f);
            RestoreEnvironment();
        }

        // =========================================================================
        // 3. CHAOS MODE (COFFEE + TEA WITHIN 15s) (30 SECONDS)
        // =========================================================================
        private IEnumerator ChaosModeRoutine()
        {
            float duration = 30.0f;
            _remainingTime = duration;

            // Unlock Secret Achievement
            AchievementManager.Instance?.UnlockAchievement("Balanced Diet? Never Heard of It.", "Drank both coffee and tea within 15 seconds. May your heart rate rest in peace.");

            // Audio Celebration Chime
            AudioManager.Instance?.PlayConnectChime();

            // HUD Banner
            ShowHUDBanner("<b><color=#ff2288>* CHAOS MODE UNLOCKED *</color></b>  <size=20><color=#ffffff>(30s)</color></size>\n<size=16><color=#ffd6eb>COFFEE + TEA MERGED!  DISCO PROTOCOL ONLINE!  PARTY ROBOT!</color></size>", duration, new Color(1.0f, 0.1f, 0.5f, 0.98f));

            // Configure Robot Assistant: Breakdance Party
            var bot = FindRobotAssistant();
            if (bot != null)
            {
                bot.SetSpecialMode(RobotAssistant.RobotMode.Dancing);
            }

            // Activate Full Disco Party Lighting!
            var lighting = RoomLightingController.Instance;
            if (lighting != null)
            {
                lighting.StartDiscoMode();
            }

            // Activate Floating Leaves with energetic swirling
            if (_leavesContainer != null)
            {
                _leavesContainer.SetActive(true);
                ResetFloatingLeaves();
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _remainingTime = duration - elapsed;
                yield return null;
            }

            // Conclude
            ShowHUDBanner("<b><color=#55ff99>* CHAOS MODE COMPLETE *</color></b>\n<size=16><color=#d0d8e0>Workstation equilibrium restored. Drink responsibly!</color></size>", 3.5f, new Color(0.2f, 0.9f, 0.5f, 0.9f));
            RestoreEnvironment();
        }

        // =========================================================================
        // ENVIRONMENT RESTORATION
        // =========================================================================

        public void RestoreEnvironment()
        {
            _currentEffect = DrinkEffectType.None;
            _remainingTime = 0f;

            if (_activeEffectRoutine != null)
            {
                StopCoroutine(_activeEffectRoutine);
                _activeEffectRoutine = null;
            }

            // Restore Room Lighting
            if (RoomLightingController.Instance != null)
            {
                if (RoomLightingController.Instance.IsDiscoActive)
                {
                    RoomLightingController.Instance.StopDiscoMode();
                }
                RoomLightingController.Instance.ApplyPreset(_savedLightingPreset);
            }
            _hasSavedLighting = false;

            // Restore Robot Assistant
            var bot = FindRobotAssistant();
            if (bot != null)
            {
                bot.ResetToNormalMode();
            }

            // Hide Floating Leaves
            if (_leavesContainer != null)
            {
                _leavesContainer.SetActive(false);
            }

            // Stop Zen Music if playing
            AudioManager.Instance?.StopPeacefulMusic(1.5f);

            // Restore Camera position
            if (_camTransform != null && _originalCamLocalPos != Vector3.zero)
            {
                _camTransform.localPosition = _originalCamLocalPos;
            }
        }

        private RobotAssistant FindRobotAssistant()
        {
            var bot = RobotAssistant.FindAnyObjectByType<RobotAssistant>();
            return bot;
        }

        // =========================================================================
        // FLOATING ZEN LEAVES SYSTEM (PROCEDURAL)
        // =========================================================================

        private void BuildFloatingLeavesSystem()
        {
            if (_leavesContainer != null) return;

            _leavesContainer = new GameObject("FloatingZenLeaves");
            _leavesContainer.transform.SetParent(transform, false);

            Material matLeaf = GetLeafMaterial();

            // Spawn 24 floating green tea leaves scattered around the room
            for (int i = 0; i < 24; i++)
            {
                GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
                leaf.name = $"Leaf_{i}";
                leaf.transform.SetParent(_leavesContainer.transform, false);
                leaf.transform.localScale = new Vector3(0.06f, 0.03f, 0.01f);
                leaf.GetComponent<Renderer>().sharedMaterial = matLeaf;
                Destroy(leaf.GetComponent<Collider>());
                _floatingLeaves.Add(leaf.transform);
            }

            _leavesContainer.SetActive(false);
        }

        private void ResetFloatingLeaves()
        {
            Vector3 center = new Vector3(0f, 1.4f, 0f);
            for (int i = 0; i < _floatingLeaves.Count; i++)
            {
                Transform t = _floatingLeaves[i];
                t.position = center + new Vector3(UnityEngine.Random.Range(-4.5f, 4.5f), UnityEngine.Random.Range(0.2f, 2.5f), UnityEngine.Random.Range(-4.5f, 4.5f));
                t.rotation = UnityEngine.Random.rotation;
            }
        }

        private void UpdateFloatingLeaves()
        {
            float speed = (_currentEffect == DrinkEffectType.ChaosMode) ? 1.8f : 0.45f;
            float t = Time.time * speed;

            for (int i = 0; i < _floatingLeaves.Count; i++)
            {
                Transform leaf = _floatingLeaves[i];
                if (leaf == null) continue;

                Vector3 pos = leaf.position;
                pos.y -= 0.08f * speed * Time.deltaTime;
                pos.x += Mathf.Sin(t + i) * 0.15f * Time.deltaTime;
                pos.z += Mathf.Cos(t + i * 1.3f) * 0.15f * Time.deltaTime;

                // Wrap vertically if fallen below floor
                if (pos.y < 0.2f)
                {
                    pos.y = 2.6f;
                    pos.x = UnityEngine.Random.Range(-4.5f, 4.5f);
                    pos.z = UnityEngine.Random.Range(-4.5f, 4.5f);
                }

                leaf.position = pos;
                leaf.Rotate(Vector3.up, 45f * speed * Time.deltaTime, Space.Self);
                leaf.Rotate(Vector3.forward, 25f * speed * Time.deltaTime, Space.Self);
            }
        }

        private Material GetLeafMaterial()
        {
            var mats = Resources.FindObjectsOfTypeAll<Material>();
            foreach (var m in mats)
            {
                if (m != null && m.name == "Mat_PlantLeafEmerald") return m;
            }

            Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(s);
            mat.name = "Mat_FloatingTeaLeaf";
            mat.color = new Color(0.12f, 0.75f, 0.35f, 0.95f);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.12f, 0.75f, 0.35f, 0.95f));
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.6f);
            return mat;
        }

        // =========================================================================
        // HUD BANNER / HUMOROUS NOTIFICATIONS
        // =========================================================================

        private void BuildHUDBanner()
        {
            if (_hudBannerObj != null) return;

            // ScreenSpace HUD Canvas
            GameObject canvasObj = new GameObject("DrinkEffectHUDCanvas");
            canvasObj.transform.SetParent(transform, false);

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;

            var scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            // Outer Frame (Acts as dynamic colored glowing border)
            _hudBannerObj = new GameObject("DrinkBannerBox");
            _hudBannerObj.transform.SetParent(canvasObj.transform, false);

            RectTransform rt = _hudBannerObj.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, -28f);
            rt.sizeDelta = new Vector2(840f, 96f);

            _hudBorderImg = _hudBannerObj.AddComponent<UnityEngine.UI.Image>();
            _hudBorderImg.color = new Color(0.15f, 0.95f, 0.55f, 0.95f);

            _hudBannerGroup = _hudBannerObj.AddComponent<CanvasGroup>();
            _hudBannerGroup.alpha = 0f;

            // Inner Card (Solid dark obsidian background ensuring 100% legibility)
            GameObject innerCard = new GameObject("InnerCard");
            innerCard.transform.SetParent(_hudBannerObj.transform, false);
            RectTransform irt = innerCard.AddComponent<RectTransform>();
            irt.anchorMin = Vector2.zero;
            irt.anchorMax = Vector2.one;
            irt.offsetMin = new Vector2(3f, 3f);
            irt.offsetMax = new Vector2(-3f, -3f);

            var innerImg = innerCard.AddComponent<UnityEngine.UI.Image>();
            innerImg.color = new Color(0.035f, 0.055f, 0.09f, 0.98f); // Deep dark obsidian glass

            // Text inside InnerCard
            GameObject textObj = new GameObject("BannerText");
            textObj.transform.SetParent(innerCard.transform, false);
            RectTransform trt = textObj.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(24f, 10f);
            trt.offsetMax = new Vector2(-24f, -10f);

            _hudBannerText = textObj.AddComponent<TextMeshProUGUI>();
            _hudBannerText.fontSize = 22;
            _hudBannerText.fontStyle = FontStyles.Bold;
            _hudBannerText.alignment = TextAlignmentOptions.Center;
            _hudBannerText.color = Color.white;
            _hudBannerText.lineSpacing = 8f;
        }

        public void ShowHUDBanner(string message, float displayDuration, Color? borderColor = null)
        {
            if (_hudBannerText != null)
            {
                _hudBannerText.text = message;
            }
            if (_hudBorderImg != null && borderColor.HasValue)
            {
                _hudBorderImg.color = borderColor.Value;
            }
            StopCoroutine(nameof(AnimateBanner));
            StartCoroutine(AnimateBanner(displayDuration));
        }

        private IEnumerator AnimateBanner(float duration)
        {
            if (_hudBannerGroup == null) yield break;

            // Fade In
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                _hudBannerGroup.alpha = Mathf.Lerp(0f, 1f, t / 0.25f);
                yield return null;
            }
            _hudBannerGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(Mathf.Max(0.5f, duration - 0.5f));

            // Fade Out
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                _hudBannerGroup.alpha = Mathf.Lerp(1f, 0f, t / 0.35f);
                yield return null;
            }
            _hudBannerGroup.alpha = 0f;
        }
    }
}
