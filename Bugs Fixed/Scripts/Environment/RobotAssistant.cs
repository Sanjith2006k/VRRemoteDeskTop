using System.Collections;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive Robotic Assistant ("Bit-Bot") fulfilling Section 7 (Prop A) of requirements.
    /// Provides smooth floating/hovering animation, head tracking towards the player,
    /// dynamic mood visor colors, funny randomized speech bubble dialogs, procedural audio chirps,
    /// and secret spin reactions.
    /// </summary>
    public class RobotAssistant : InteractiveProp
    {
        [Header("Robot References")]
        [SerializeField] private Transform _headTransform;
        [SerializeField] private Transform _bodyTransform;
        [SerializeField] private Renderer _eyeVisorRenderer;

        [Header("Speech Bubble")]
        [SerializeField] private GameObject _dialogRoot;
        [SerializeField] private TextMeshProUGUI _dialogText;
        [SerializeField] private CanvasGroup _dialogCanvasGroup;

        private static readonly string[] FunnyQuotes = new string[]
        {
            "System check complete. Human detected. Confidence: questionable.",
            "I optimized your workstation. You now have 17 new distractions.",
            "Error 404: Motivation not found. Retrying in 5 minutes...",
            "Please stop staring at me. I am trying to look productive.",
            "Warning: I know your browser search history. Bribes in crypto accepted.",
            "I ran the quantum simulation. You should probably drink more coffee.",
            "I am 99.9% sure the glowing button does something critical. Touch it!",
            "Idle status: Pretending to calculate complex background physics.",
            "Do you ever feel like you're trapped inside a virtual simulation?",
            "Rebooting humor module... 100% dad jokes successfully compiled."
        };

        public enum RobotMode
        {
            Normal,
            Hyperactive,
            Zen,
            Dancing
        }

        private RobotMode _currentMode = RobotMode.Normal;
        private float _modeTimer = 0f;
        private static readonly string[] CaffeineQuotes = new string[]
        {
            "I HAVE TRANSCENDED CAFFEINE! I SEE THROUGH WALLS!",
            "WHO NEEDS SLEEP WHEN WE HAVE MAXIMUM OVERCLOCKING?!",
            "HEART RATE: 9999 BPM! EXECUTING 500,000 PROCESSES/SEC!",
            "I CAN HEAR THE WI-FI FREQUENCIES TALKING TO ME!",
            "SPEED IS MY DESTINY! MORE VOLTAGE! MORE BEANS!"
        };

        private static readonly string[] ZenQuotes = new string[]
        {
            "Breathe in packets... exhale cache...",
            "The motherboard is at peace. The silicon is one with the void.",
            "Do not chase the ping; let the latency dissolve.",
            "Inner serenity: 100%. Thermal equilibrium achieved.",
            "In stillness, even the fan noise becomes a gentle mountain breeze."
        };

        private static readonly string[] DancingQuotes = new string[]
        {
            "UNZ UNZ UNZ! DISCO SUBROUTINES ACTIVE!",
            "BALANCED DIET? NEVER HEARD OF IT! PARTY TIME!",
            "MY SERVO MOTORS HAVE NEVER FELT SO GROOVY!",
            "MAXIMUM AUDIO SYNC ENGAGED! DROP THE BASS!",
            "RGB OVERDRIVE! WHO'S READY TO DANCE?!"
        };

        private int _quoteIndex = 0;
        private float _hoverBaseY = 0f;
        private Coroutine _dialogRoutine;
        private Coroutine _spinRoutine;
        private int _interactionCount = 0;
        private float _lastInteractTime = -10f;
        private MaterialPropertyBlock _visorPropBlock;
        private Transform _playerTransform;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 2.8f;
            _promptOffset = new Vector3(0f, 0.50f, 0f);
            SetPromptAction("TALK TO BOT");
            _hoverBaseY = transform.localPosition.y;
            _visorPropBlock = new MaterialPropertyBlock();
            BuildDialogBubble();
        }

        protected override void Start()
        {
            base.Start();
            var cam = Camera.main;
            if (cam != null) _playerTransform = cam.transform;
        }

        public void SetSpecialMode(RobotMode mode)
        {
            _currentMode = mode;
            _modeTimer = 0f;

            switch (mode)
            {
                case RobotMode.Hyperactive:
                    SetVisorColor(new Color(1f, 0.45f, 0f) * 4.5f);
                    ShowDialog(CaffeineQuotes[Random.Range(0, CaffeineQuotes.Length)]);
                    break;
                case RobotMode.Zen:
                    SetVisorColor(new Color(0.2f, 1f, 0.4f) * 2.5f);
                    ShowDialog(ZenQuotes[Random.Range(0, ZenQuotes.Length)]);
                    break;
                case RobotMode.Dancing:
                    SetVisorColor(Color.magenta * 4f);
                    ShowDialog(DancingQuotes[Random.Range(0, DancingQuotes.Length)]);
                    break;
                case RobotMode.Normal:
                    ResetToNormalMode();
                    break;
            }
        }

        public void ResetToNormalMode()
        {
            _currentMode = RobotMode.Normal;
            SetVisorColor(new Color(0f, 1f, 1f) * 3f);
            if (_dialogRoot != null) _dialogRoot.SetActive(false);
            if (_dialogRoutine != null)
            {
                StopCoroutine(_dialogRoutine);
                _dialogRoutine = null;
            }
        }

        protected override void Update()
        {
            base.Update();

            _modeTimer += Time.deltaTime;

            float hover = 0f;
            Vector3 pos = transform.localPosition;

            switch (_currentMode)
            {
                case RobotMode.Hyperactive:
                    // Rapid jittery bounce & shake
                    hover = Mathf.Sin(Time.time * 26f) * 0.08f + (Mathf.PerlinNoise(Time.time * 30f, 0f) - 0.5f) * 0.04f;
                    pos.y = _hoverBaseY + hover;
                    pos.x += (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 0.015f;
                    transform.localPosition = pos;

                    // Flashing fiery visor
                    Color hyperColor = Color.Lerp(new Color(1f, 0.1f, 0f), new Color(1f, 0.9f, 0.1f), Mathf.PingPong(Time.time * 8f, 1f)) * 4.5f;
                    SetVisorColor(hyperColor);

                    // Rapid funny quotes
                    if (_modeTimer > 5.0f)
                    {
                        _modeTimer = 0f;
                        ShowDialog(CaffeineQuotes[Random.Range(0, CaffeineQuotes.Length)]);
                    }
                    break;

                case RobotMode.Zen:
                    // Serene slow breathing hover
                    hover = Mathf.Sin(Time.time * 0.9f) * 0.025f;
                    pos.y = _hoverBaseY + hover;
                    transform.localPosition = pos;

                    // Gentle emerald pulsing visor
                    float zenPulse = 2.0f + Mathf.Sin(Time.time * 1.5f) * 0.8f;
                    SetVisorColor(new Color(0.15f, 1.0f, 0.45f) * zenPulse);

                    // Gentle peaceful quotes
                    if (_modeTimer > 7.0f)
                    {
                        _modeTimer = 0f;
                        ShowDialog(ZenQuotes[Random.Range(0, ZenQuotes.Length)]);
                    }
                    break;

                case RobotMode.Dancing:
                    // Rhythmic party dance bobbing and continuous spinning
                    float beat = Mathf.Abs(Mathf.Sin(Time.time * 7f));
                    hover = beat * 0.12f;
                    pos.y = _hoverBaseY + hover;
                    transform.localPosition = pos;

                    // Dance spin
                    transform.Rotate(Vector3.up, 180f * Time.deltaTime, Space.Self);

                    // Disco RGB visor strobing
                    Color discoColor = Color.HSVToRGB(Mathf.Repeat(Time.time * 2.5f, 1f), 1f, 1f) * 4f;
                    SetVisorColor(discoColor);

                    // Party quotes
                    if (_modeTimer > 5.0f)
                    {
                        _modeTimer = 0f;
                        ShowDialog(DancingQuotes[Random.Range(0, DancingQuotes.Length)]);
                    }
                    break;

                case RobotMode.Normal:
                default:
                    // Smooth floating hover bob
                    hover = Mathf.Sin(Time.time * 2.2f) * 0.04f;
                    pos.y = _hoverBaseY + hover;
                    transform.localPosition = pos;
                    break;
            }

            // Track player head rotation if nearby
            if (_playerTransform == null && Camera.main != null)
            {
                _playerTransform = Camera.main.transform;
            }

            if (_headTransform != null && _playerTransform != null)
            {
                float dist = Vector3.Distance(_playerTransform.position, transform.position);
                if (dist < 5.0f && _spinRoutine == null)
                {
                    Vector3 targetDir = _playerTransform.position - _headTransform.position;
                    targetDir.y = 0f; // Level look
                    if (targetDir.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(targetDir);
                        _headTransform.rotation = Quaternion.Slerp(_headTransform.rotation, targetRot, 6.0f * Time.deltaTime);
                    }
                }
            }

            // Billboard speech bubble directly to camera
            if (_dialogRoot != null && _dialogRoot.activeSelf && _playerTransform != null)
            {
                Vector3 toCam = _dialogRoot.transform.position - _playerTransform.position;
                if (toCam.sqrMagnitude > 0.001f)
                {
                    _dialogRoot.transform.rotation = Quaternion.LookRotation(toCam, Vector3.up);
                }
            }
        }

        protected override void OnInteract()
        {
            float now = Time.time;
            if (now - _lastInteractTime < 4.0f)
            {
                _interactionCount++;
            }
            else
            {
                _interactionCount = 1;
            }
            _lastInteractTime = now;

            // Trigger audio chirp
            PlayChirp();

            // Notify achievements
            AchievementManager.Instance?.UnlockAchievement("Chatty User", "Consulted Bit-Bot for questionable life advice.");

            // Easter egg: 3 rapid clicks triggers Turbo Spin!
            if (_interactionCount >= 3)
            {
                _interactionCount = 0;
                TriggerTurboSpin();
                return;
            }

            // Normal speech interaction
            string quote = FunnyQuotes[_quoteIndex % FunnyQuotes.Length];
            _quoteIndex++;

            // Shift eye visor color based on mood
            Color moodColor = (_quoteIndex % 2 == 0) ? new Color(0f, 1f, 1f) : new Color(1f, 0.1f, 0.6f);
            SetVisorColor(moodColor * 3.5f);

            ShowDialog(quote);
        }

        public void TriggerTurboSpin()
        {
            if (_spinRoutine != null) StopCoroutine(_spinRoutine);
            _spinRoutine = StartCoroutine(TurboSpinRoutine());
        }

        private IEnumerator TurboSpinRoutine()
        {
            SetVisorColor(new Color(1f, 0.8f, 0.1f) * 4.0f);
            ShowDialog("Wheeeeeee! Sensory overload initiated!");

            AchievementManager.Instance?.UnlockAchievement("Dizzy Bot", "Spun Bit-Bot until his gyroscope broke.");

            float elapsed = 0f;
            float duration = 1.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.Rotate(Vector3.up, 1080f * Time.deltaTime, Space.Self);
                yield return null;
            }

            SetVisorColor(new Color(0f, 1f, 1f) * 3.0f);
            _spinRoutine = null;
        }

        public void SetVisorColor(Color color)
        {
            if (_eyeVisorRenderer == null) return;
            _eyeVisorRenderer.GetPropertyBlock(_visorPropBlock);
            _visorPropBlock.SetColor("_BaseColor", color);
            _visorPropBlock.SetColor("_EmissionColor", color);
            _eyeVisorRenderer.SetPropertyBlock(_visorPropBlock);
        }

        private void ShowDialog(string text)
        {
            if (_dialogRoutine != null) StopCoroutine(_dialogRoutine);
            _dialogRoutine = StartCoroutine(DialogDisplayRoutine(text));
        }

        private IEnumerator DialogDisplayRoutine(string text)
        {
            if (_dialogRoot == null || _dialogText == null || _dialogCanvasGroup == null) yield break;

            _dialogText.text = text;
            _dialogRoot.SetActive(true);

            // Pop in animation
            _dialogRoot.transform.localScale = Vector3.zero;
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float s = Mathf.SmoothStep(0f, 1f, elapsed / 0.2f) * 0.0022f;
                _dialogRoot.transform.localScale = new Vector3(s, s, s);
                _dialogCanvasGroup.alpha = elapsed / 0.2f;
                yield return null;
            }
            _dialogRoot.transform.localScale = Vector3.one * 0.0022f;
            _dialogCanvasGroup.alpha = 1f;

            // Wait 5 seconds
            yield return new WaitForSeconds(5.0f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.4f)
            {
                elapsed += Time.deltaTime;
                _dialogCanvasGroup.alpha = 1f - (elapsed / 0.4f);
                yield return null;
            }

            _dialogRoot.SetActive(false);
            _dialogRoutine = null;
        }

        private void PlayChirp()
        {
            if (AudioManager.Instance != null)
            {
                // Play futuristic sweep prompt
                AudioManager.Instance.PlayPromptTone();
            }
        }

        private void BuildDialogBubble()
        {
            _dialogRoot = new GameObject("DialogBubble", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            _dialogRoot.transform.SetParent(transform, false);
            _dialogRoot.transform.localPosition = new Vector3(0f, 0.65f, 0f);

            Canvas c = _dialogRoot.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.overrideSorting = true;
            c.sortingOrder = 60;

            RectTransform rt = _dialogRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, 140f);
            rt.localScale = Vector3.one * 0.0022f;

            _dialogCanvasGroup = _dialogRoot.GetComponent<CanvasGroup>();
            _dialogCanvasGroup.blocksRaycasts = false;

            // Sleek glassmorphism speech bubble background
            GameObject bgObj = new GameObject("BubbleBg", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bgObj.transform.SetParent(_dialogRoot.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            var bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);

            // Glowing cyan border
            GameObject borderObj = new GameObject("BubbleBorder", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            borderObj.transform.SetParent(bgObj.transform, false);
            RectTransform borderRt = borderObj.GetComponent<RectTransform>();
            borderRt.anchorMin = Vector2.zero;
            borderRt.anchorMax = Vector2.one;
            borderRt.sizeDelta = new Vector2(3f, 3f);
            var borderImg = borderObj.GetComponent<UnityEngine.UI.Image>();
            borderImg.color = new Color(0f, 0.85f, 1f, 0.8f);

            GameObject innerObj = new GameObject("BubbleInner", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            innerObj.transform.SetParent(borderObj.transform, false);
            RectTransform innerRt = innerObj.GetComponent<RectTransform>();
            innerRt.anchorMin = Vector2.zero;
            innerRt.anchorMax = Vector2.one;
            innerRt.sizeDelta = new Vector2(-4f, -4f);
            var innerImg = innerObj.GetComponent<UnityEngine.UI.Image>();
            innerImg.color = new Color(0.04f, 0.07f, 0.14f, 0.96f);

            // Header Title
            GameObject titleObj = new GameObject("BotHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(innerObj.transform, false);
            RectTransform titleRt = titleObj.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -8f);
            titleRt.sizeDelta = new Vector2(-20f, 26f);
            var titleTmp = titleObj.GetComponent<TextMeshProUGUI>();
            titleTmp.text = "<color=#00ffff><b>[ BIT-BOT COMPANION ]</b></color>";
            titleTmp.fontSize = 18;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Dialog Text
            GameObject textObj = new GameObject("BubbleText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(innerObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(14f, 10f);
            trt.offsetMax = new Vector2(-14f, -32f);

            _dialogText = textObj.GetComponent<TextMeshProUGUI>();
            _dialogText.fontSize = 20;
            _dialogText.color = Color.white;
            _dialogText.alignment = TextAlignmentOptions.Center;
            _dialogText.textWrappingMode = TextWrappingModes.Normal;

            _dialogRoot.SetActive(false);
        }

        protected override void LateUpdate()
        {
            base.LateUpdate();
            // Billboard dialog bubble facing camera without mirroring
            if (_dialogRoot != null && _dialogRoot.activeSelf)
            {
                Camera cam = Camera.main;
                if (cam != null)
                {
                    Vector3 toCam = cam.transform.position - _dialogRoot.transform.position;
                    if (toCam.sqrMagnitude > 0.001f)
                    {
                        _dialogRoot.transform.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                    }
                }
            }
        }
    }
}
