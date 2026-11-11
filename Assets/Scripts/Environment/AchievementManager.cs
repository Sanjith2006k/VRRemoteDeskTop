using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    [System.Serializable]
    public class Achievement
    {
        public string Id;
        public string Title;
        public string Description;
        public bool IsUnlocked;
        public GameObject BoardBadge;
    }

    /// <summary>
    /// Master Achievement & Trophy coordinator fulfilling Section 7 (Prop E).
    /// Tracks user achievements in real-time, displays animated HUD toasts,
    /// and lights up trophies on the physical wall-mounted achievement board.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        [Header("Achievements Data")]
        [SerializeField] private List<Achievement> _achievements = new List<Achievement>();

        [Header("Toast UI (World/HUD)")]
        [SerializeField] private GameObject _toastRoot;
        [SerializeField] private TextMeshProUGUI _toastTitleText;
        [SerializeField] private TextMeshProUGUI _toastDescText;
        [SerializeField] private CanvasGroup _toastCanvasGroup;

        private Coroutine _toastRoutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeDefaultAchievements();
            BuildToastUI();
        }

        private void InitializeDefaultAchievements()
        {
            if (_achievements.Count == 0)
            {
                _achievements.Add(new Achievement { Id = "Desk Jockey", Title = "Desk Jockey", Description = "Entered Computer Interaction Mode at the workstation." });
                _achievements.Add(new Achievement { Id = "Chatty User", Title = "Chatty User", Description = "Consulted Bit-Bot for questionable life advice." });
                _achievements.Add(new Achievement { Id = "Caffeine Dependency", Title = "Caffeine Dependency", Description = "Synthesized a cup of pure virtual hyper-caffeine." });
                _achievements.Add(new Achievement { Id = "Button Tester", Title = "Button Tester", Description = "Pushed tactile buttons on the workstation control deck." });
                _achievements.Add(new Achievement { Id = "Hologram Master", Title = "Hologram Master", Description = "Explored spatial holographic visualization modes." });
                _achievements.Add(new Achievement { Id = "Party Animal", Title = "Party Animal", Description = "Activated the room's secret disco party mode!" });
            }
        }

        public void UnlockAchievement(string id, string defaultDescription = "")
        {
            Achievement ach = _achievements.Find(a => a.Id == id);
            if (ach == null)
            {
                ach = new Achievement { Id = id, Title = id, Description = defaultDescription, IsUnlocked = false };
                _achievements.Add(ach);
            }

            if (ach.IsUnlocked) return;

            ach.IsUnlocked = true;
            Debug.Log($"[AchievementManager] UNLOCKED: {ach.Title} - {ach.Description}");

            // Audio celebration chime
            AudioManager.Instance?.PlayConnectChime();

            // Light up board badge if present
            if (ach.BoardBadge != null)
            {
                ach.BoardBadge.SetActive(true);
            }

            // Show Toast
            ShowToast(ach.Title, ach.Description);
        }

        public void RegisterBadge(string achievementId, GameObject badgeObj)
        {
            Achievement ach = _achievements.Find(a => a.Id == achievementId);
            if (ach != null)
            {
                ach.BoardBadge = badgeObj;
                badgeObj.SetActive(ach.IsUnlocked);
            }
        }

        private void ShowToast(string title, string desc)
        {
            if (_toastRoutine != null) StopCoroutine(_toastRoutine);
            _toastRoutine = StartCoroutine(ToastRoutine(title, desc));
        }

        private IEnumerator ToastRoutine(string title, string desc)
        {
            if (_toastRoot == null || _toastTitleText == null || _toastCanvasGroup == null) yield break;

            _toastTitleText.text = $"<color=#00ffff>ACHIEVEMENT UNLOCKED:</color> {title}";
            if (_toastDescText != null) _toastDescText.text = desc;

            _toastRoot.SetActive(true);

            // Pop in
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                _toastCanvasGroup.alpha = elapsed / 0.25f;
                yield return null;
            }
            _toastCanvasGroup.alpha = 1f;

            yield return new WaitForSeconds(3.5f);

            // Fade out
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                _toastCanvasGroup.alpha = 1f - (elapsed / 0.5f);
                yield return null;
            }

            _toastRoot.SetActive(false);
            _toastRoutine = null;
        }

        private void BuildToastUI()
        {
            _toastRoot = new GameObject("AchievementToast", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            _toastRoot.transform.SetParent(transform, false);

            Canvas c = _toastRoot.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = 999;

            _toastCanvasGroup = _toastRoot.GetComponent<CanvasGroup>();
            _toastCanvasGroup.blocksRaycasts = false;

            GameObject card = new GameObject("Card", typeof(RectTransform));
            card.transform.SetParent(_toastRoot.transform, false);
            RectTransform crt = card.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 1f);
            crt.anchorMax = new Vector2(0.5f, 1f);
            crt.pivot = new Vector2(0.5f, 1f);
            crt.anchoredPosition = new Vector2(0f, -30f);
            crt.sizeDelta = new Vector2(460f, 75f);

            // Background Image
            var bgImg = card.AddComponent<UnityEngine.UI.Image>();
            bgImg.color = new Color(0.04f, 0.08f, 0.14f, 0.94f);

            // Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(card.transform, false);
            RectTransform trt = titleObj.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 0.5f);
            trt.anchorMax = new Vector2(1f, 1f);
            trt.offsetMin = new Vector2(16f, 0f);
            trt.offsetMax = new Vector2(-16f, -8f);

            _toastTitleText = titleObj.GetComponent<TextMeshProUGUI>();
            _toastTitleText.fontSize = 18;
            _toastTitleText.fontStyle = FontStyles.Bold;
            _toastTitleText.alignment = TextAlignmentOptions.MidlineLeft;

            // Desc
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(card.transform, false);
            RectTransform drt = descObj.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(0f, 0f);
            drt.anchorMax = new Vector2(1f, 0.5f);
            drt.offsetMin = new Vector2(16f, 8f);
            drt.offsetMax = new Vector2(-16f, 0f);

            _toastDescText = descObj.GetComponent<TextMeshProUGUI>();
            _toastDescText.fontSize = 14;
            _toastDescText.color = new Color(0.8f, 0.88f, 0.98f);
            _toastDescText.alignment = TextAlignmentOptions.MidlineLeft;

            _toastRoot.SetActive(false);
        }
    }
}
