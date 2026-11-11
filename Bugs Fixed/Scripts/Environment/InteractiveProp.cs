using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using VRRemoteDesktop.Computer;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Base component for interactive environmental objects (Robot, Coffee Machine, Buttons, Hologram).
    /// Provides distance checks, player camera raycast aiming, floating interaction prompts,
    /// cooldown management, and unified 'E' key triggering in Exploration mode.
    /// </summary>
    public class InteractiveProp : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [Tooltip("Text displayed in the floating prompt (e.g. 'TALK', 'BREW COFFEE', 'PRESS').")]
        [SerializeField] private string _promptAction = "INTERACT";

        [Tooltip("Keyboard key prompt label.")]
        [SerializeField] private string _keyLabel = "E";

        [Tooltip("Maximum interaction range in meters.")]
        [SerializeField] protected float _interactionRange = 2.8f;

        [Tooltip("Minimum time between interactions in seconds.")]
        [SerializeField] protected float _cooldownSeconds = 0.4f;

        [Tooltip("Vertical offset for the floating prompt.")]
        [SerializeField] protected Vector3 _promptOffset = new Vector3(0f, 0.45f, 0f);

        [Header("Visual Feedback (Optional)")]
        [Tooltip("Renderer to highlight when targeted.")]
        [SerializeField] private Renderer _highlightRenderer;

        public event Action OnInteracted;

        private float _lastInteractionTime = -10f;
        private static float _globalLastInteractionTime = -10f;
        public const float GLOBAL_INTERACTION_COOLDOWN = 0.35f;

        private bool _isTargeted = false;
        private GameObject _promptRoot;
        private TextMeshProUGUI _promptText;
        private CanvasGroup _promptCanvasGroup;
        private Camera _mainCamera;

        public string PromptAction => _promptAction;
        public bool IsTargeted => _isTargeted;
        public virtual bool AllowInteractionInCinemaMode => false;
        public bool CanInteract => Time.time >= _lastInteractionTime + _cooldownSeconds &&
                                   Time.time >= _globalLastInteractionTime + GLOBAL_INTERACTION_COOLDOWN;

        protected virtual void Awake()
        {
            BuildFloatingPrompt();
        }

        protected virtual void Start()
        {
            _mainCamera = Camera.main;
        }

        private static readonly System.Collections.Generic.List<InteractiveProp> _allProps = new System.Collections.Generic.List<InteractiveProp>();
        private static InteractiveProp _currentBestProp = null;
        public static InteractiveProp CurrentBestProp => _currentBestProp;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _allProps.Clear();
            _currentBestProp = null;
        }

        protected virtual void OnEnable()
        {
            if (!_allProps.Contains(this))
            {
                _allProps.Add(this);
            }
        }

        protected virtual void OnDisable()
        {
            _allProps.Remove(this);
            if (_currentBestProp == this) _currentBestProp = null;
            SetTargeted(false);
        }

        protected virtual void OnDestroy()
        {
            _allProps.Remove(this);
            if (_currentBestProp == this) _currentBestProp = null;
        }

        protected virtual void Update()
        {
            // Only active during Exploration mode, OR props explicitly allowed in Cinema Mode (like Sofa and armrest buttons)
            bool inComputer = (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode);
            bool inCinema = (TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode);

            if (inComputer || (inCinema && !AllowInteractionInCinemaMode))
            {
                SetTargeted(false);
                if (_promptRoot != null && _promptRoot.activeSelf) _promptRoot.SetActive(false);
                return;
            }

            // Arbitrate best focused prop once per frame using the first active prop in the list
            _allProps.RemoveAll(p => p == null);
            InteractiveProp firstActive = _allProps.Find(p => p != null && p.isActiveAndEnabled);
            if (firstActive == this)
            {
                UpdateGlobalPropFocus();
            }

            bool isMeBest = (_currentBestProp == this);
            SetTargeted(isMeBest);

            if (isMeBest && CanInteract)
            {
                bool ePressed = false;
                var kb = Keyboard.current;
                if (kb != null && kb.eKey.wasPressedThisFrame)
                {
                    ePressed = true;
                }

                try
                {
                    if (!ePressed && UnityEngine.Input.GetKeyDown(KeyCode.E))
                    {
                        ePressed = true;
                    }
                }
                catch {}

                if (ePressed)
                {
                    ExecuteInteraction();
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (_promptRoot != null && _promptRoot.activeSelf)
            {
                if (_mainCamera == null) _mainCamera = Camera.main;
                if (_mainCamera != null)
                {
                    Vector3 toCamera = _mainCamera.transform.position - _promptRoot.transform.position;
                    if (toCamera.sqrMagnitude > 0.001f)
                    {
                        _promptRoot.transform.rotation = Quaternion.LookRotation(-toCamera.normalized, Vector3.up);
                    }
                }
            }
        }

        private static void UpdateGlobalPropFocus()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camObj = GameObject.FindWithTag("MainCamera");
                if (camObj != null) cam = camObj.GetComponent<Camera>();
            }
            if (cam == null) return;

            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;

            bool inCinema = (TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode);

            InteractiveProp bestProp = null;
            float bestScore = float.MaxValue;

            // 1. First priority: Direct Forward Raycast (ignoring player colliders & triggers)
            Ray ray = new Ray(camPos, camFwd);
            RaycastHit[] hits = Physics.RaycastAll(ray, 4.5f, ~0, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var hit in hits)
                {
                    if (hit.collider == null) continue;

                    // Skip player's own CharacterController and avatar body colliders
                    if (hit.collider is CharacterController || hit.collider.CompareTag("Player") ||
                        hit.collider.GetComponentInParent<VRRemoteDesktop.Player.PlayerController>() != null)
                    {
                        continue;
                    }

                    InteractiveProp prop = hit.collider.GetComponentInParent<InteractiveProp>();
                    if (prop != null && prop.gameObject.activeInHierarchy && hit.distance <= prop._interactionRange)
                    {
                        if (inCinema && !prop.AllowInteractionInCinemaMode) continue;
                        bestProp = prop;
                        break;
                    }

                    // If a solid structural wall or closed door is directly in front, ray cannot pass through
                    string hitName = hit.collider.gameObject.name.ToLower();
                    if (hitName.Contains("wall") || hitName.Contains("door") || hitName.Contains("partition"))
                    {
                        break;
                    }
                }
            }

            // 2. Second priority: Proximity & Aim Cone (55-degree cone)
            if (bestProp == null)
            {
                foreach (var prop in _allProps)
                {
                    if (prop == null || !prop.gameObject.activeInHierarchy) continue;
                    if (inCinema && !prop.AllowInteractionInCinemaMode) continue;

                    Vector3 targetPos = prop.transform.position + prop._promptOffset * 0.5f;
                    Vector3 toTarget = targetPos - camPos;
                    float dist = toTarget.magnitude;

                    if (dist > prop._interactionRange) continue;

                    float angle = Vector3.Angle(camFwd, toTarget.normalized);
                    if (angle <= 55f)
                    {
                        // Check if an actual room wall is between player and prop
                        bool occludedByWall = false;
                        RaycastHit[] losHits = Physics.RaycastAll(camPos, toTarget.normalized, dist, ~0, QueryTriggerInteraction.Ignore);
                        if (losHits != null && losHits.Length > 0)
                        {
                            foreach (var losHit in losHits)
                            {
                                if (losHit.collider == null) continue;
                                if (losHit.collider is CharacterController || losHit.collider.CompareTag("Player") ||
                                    losHit.collider.GetComponentInParent<VRRemoteDesktop.Player.PlayerController>() != null)
                                {
                                    continue;
                                }
                                // Ignore the prop itself and any parent/child hierarchy of the prop (e.g. buttons on sofa)
                                if (losHit.collider.transform.IsChildOf(prop.transform) || prop.transform.IsChildOf(losHit.collider.transform))
                                {
                                    continue;
                                }
                                string hName = losHit.collider.gameObject.name.ToLower();
                                if (hName.Contains("wall") || hName.Contains("door") || hName.Contains("partition"))
                                {
                                    if (losHit.distance < dist - 0.25f)
                                    {
                                        occludedByWall = true;
                                        break;
                                    }
                                }
                            }
                        }

                        if (occludedByWall) continue;

                        // Score combines angle and distance; lower score is better alignment
                        float score = angle * 2.0f + dist * 5f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestProp = prop;
                        }
                    }
                }
            }

            _currentBestProp = bestProp;
        }

        public void SetTargeted(bool targeted)
        {
            _isTargeted = targeted;

            if (_promptRoot != null)
            {
                _promptRoot.SetActive(_isTargeted && CanInteract);
            }
        }

        public void ExecuteInteraction()
        {
            if (!CanInteract) return;
            _lastInteractionTime = Time.time;
            _globalLastInteractionTime = Time.time;

            if (_promptRoot != null) _promptRoot.SetActive(false);

            OnInteracted?.Invoke();
            OnInteract();
        }

        protected virtual void OnInteract()
        {
            // Subclasses override for specific behavior
        }

        public void SetPromptAction(string newAction)
        {
            _promptAction = newAction;
            if (_promptText != null)
            {
                _promptText.text = $"<color=#00ffff>[{_keyLabel}]</color> {_promptAction}";
            }
        }

        public void SetPromptOffset(Vector3 offset)
        {
            _promptOffset = offset;
            if (_promptRoot != null)
            {
                _promptRoot.transform.localPosition = _promptOffset;
            }
        }

        private void BuildFloatingPrompt()
        {
            _promptRoot = new GameObject("FloatingPrompt", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            _promptRoot.transform.SetParent(transform, false);
            _promptRoot.transform.localPosition = _promptOffset;

            Canvas c = _promptRoot.GetComponent<Canvas>();
            c.renderMode = RenderMode.WorldSpace;
            c.overrideSorting = true;
            c.sortingOrder = 200;
            RectTransform rt = _promptRoot.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(340f, 68f);
            rt.localScale = Vector3.one * 0.0025f;

            _promptCanvasGroup = _promptRoot.GetComponent<CanvasGroup>();
            _promptCanvasGroup.blocksRaycasts = false;

            // Sleek dark pill background
            GameObject bgObj = new GameObject("PromptBg", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            bgObj.transform.SetParent(_promptRoot.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            var bgImg = bgObj.GetComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.04f, 0.07f, 0.12f, 0.94f);

            GameObject textObj = new GameObject("PromptText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(_promptRoot.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(10f, 0f);
            trt.offsetMax = new Vector2(-10f, 0f);

            _promptText = textObj.GetComponent<TextMeshProUGUI>();
            _promptText.text = $"<color=#00ffff>[{_keyLabel}]</color> {_promptAction}";
            _promptText.fontSize = 28;
            _promptText.fontStyle = FontStyles.Bold;
            _promptText.alignment = TextAlignmentOptions.Center;
            _promptText.color = Color.white;
            _promptText.textWrappingMode = TextWrappingModes.NoWrap;

            _promptRoot.SetActive(false);
        }
    }
}
