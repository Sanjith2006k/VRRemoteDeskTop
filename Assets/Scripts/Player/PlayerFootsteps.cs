using UnityEngine;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Environment;

namespace VRRemoteDesktop.Player
{
    /// <summary>
    /// Footstep audio system for first-person player locomotion.
    /// Procedurally synthesizes high-fidelity organic acoustic footstep impacts
    /// (wood/carpet/floor heel-toe dampening with pitch and volume variation)
    /// without relying on external sound files.
    /// Automatically synchronizes step frequency with walk vs sprint speeds.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerFootsteps : MonoBehaviour
    {
        [Header("Audio Settings")]
        [Range(0f, 1f)] [SerializeField] private float _footstepVolume = 0.85f;
        [SerializeField] private float _walkStepInterval = 0.44f;
        [SerializeField] private float _sprintStepInterval = 0.28f;

        private PlayerController _playerController;
        private CharacterController _characterController;
        private AudioSource _audioSource;
        private AudioClip[] _footstepClips;
        private float _stepTimer = 0f;
        private int _lastClipIndex = -1;

        private void Awake()
        {
            _playerController = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();

            EnsureAudioListener();
            SetupAudioSource();
            GenerateProceduralFootstepClips();
        }

        private void EnsureAudioListener()
        {
            AudioListener.volume = 1f;
            AudioListener.pause = false;

            var listeners = Object.FindObjectsByType<AudioListener>(FindObjectsInactive.Exclude);
            if (listeners == null || listeners.Length == 0)
            {
                Camera cam = Camera.main ?? GetComponentInChildren<Camera>();
                if (cam != null && cam.GetComponent<AudioListener>() == null)
                {
                    cam.gameObject.AddComponent<AudioListener>();
                    Debug.Log($"[PlayerFootsteps] Attached AudioListener to {cam.name}.");
                }
            }
        }

        private void SetupAudioSource()
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            // 0.12f spatial blend gives crisp, immediate first-person stereo presence
            // without being attenuated by distance falloff
            _audioSource.spatialBlend = 0.12f;
            _audioSource.minDistance = 2.0f;
            _audioSource.maxDistance = 20.0f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.volume = _footstepVolume;
        }

        private void Update()
        {
            if (_playerController == null || _characterController == null) return;

            // Silence footsteps while seated in computer mode or theater mode
            if (ComputerInteractionManager.Instance != null && ComputerInteractionManager.Instance.IsInComputerMode)
            {
                _stepTimer = 0f;
                return;
            }
            if (TheaterCinemaController.Instance != null && TheaterCinemaController.Instance.IsInCinemaMode)
            {
                _stepTimer = 0f;
                return;
            }

            // Check if player is actively moving horizontally
            Vector3 horizontalVel = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            float speed = horizontalVel.magnitude;
            if (speed < 0.2f && _playerController != null)
            {
                speed = _playerController.CurrentSpeed;
            }

            bool isGrounded = _characterController.isGrounded || (_playerController != null && _playerController.IsGrounded) || transform.position.y <= 1.25f;

            if (isGrounded && speed > 0.35f)
            {
                bool isSprinting = speed > 4.2f || (_playerController != null && _playerController.IsSprinting);
                float interval = isSprinting ? _sprintStepInterval : _walkStepInterval;

                _stepTimer += Time.deltaTime;
                if (_stepTimer >= interval)
                {
                    _stepTimer = 0f;
                    PlayFootstep(isSprinting);
                }
            }
            else
            {
                // Pre-charge timer so the first step produces audio promptly when beginning to move
                _stepTimer = Mathf.Min(_stepTimer, 0.20f);
            }
        }

        private void PlayFootstep(bool isSprinting)
        {
            if (_footstepClips == null || _footstepClips.Length == 0) return;

            // Pick a random clip avoiding immediate repetition
            int idx = Random.Range(0, _footstepClips.Length);
            if (idx == _lastClipIndex && _footstepClips.Length > 1)
            {
                idx = (idx + 1) % _footstepClips.Length;
            }
            _lastClipIndex = idx;

            float pitchVar = Random.Range(0.92f, 1.08f);
            float volVar = Random.Range(0.88f, 1.12f) * (isSprinting ? 1.25f : 1.0f);

            _audioSource.pitch = pitchVar;
            _audioSource.PlayOneShot(_footstepClips[idx], _footstepVolume * volVar);
        }

        /// <summary>
        /// Procedurally generates a bank of organic, satisfying acoustic footsteps
        /// using damped sinusoidal resonance combined with shaped white noise impact.
        /// </summary>
        private void GenerateProceduralFootstepClips()
        {
            _footstepClips = new AudioClip[4];
            int sampleRate = 44100;
            float duration = 0.16f; // 160ms acoustic heel-toe impact
            int sampleCount = (int)(sampleRate * duration);

            float[] baseFreqs = { 95f, 110f, 125f, 85f }; // Deep floor cavity resonances

            for (int c = 0; c < 4; c++)
            {
                float[] samples = new float[sampleCount];
                float baseFreq = baseFreqs[c];

                for (int i = 0; i < sampleCount; i++)
                {
                    float t = (float)i / sampleRate;
                    float progress = (float)i / sampleCount;

                    // Sharp exponential attack followed by warm natural decay
                    float env = Mathf.Exp(-progress * 16.0f);

                    // Low-frequency heel impact thud
                    float thud = Mathf.Sin(2f * Mathf.PI * baseFreq * t * (1.0f - progress * 0.3f));

                    // Micro-textured noise for sole friction / floor contact
                    float noise = (Random.value * 2f - 1f) * Mathf.Exp(-progress * 24.0f) * 0.55f;

                    // Mid tone wood/composite flooring resonance
                    float mid = Mathf.Sin(2f * Mathf.PI * (baseFreq * 2.4f) * t) * 0.35f * Mathf.Exp(-progress * 18.0f);

                    samples[i] = Mathf.Clamp((thud * 0.85f + noise + mid) * env, -1f, 1f);
                }

                _footstepClips[c] = AudioClip.Create($"Footstep_Proc_{c}", sampleCount, 1, sampleRate, false);
                _footstepClips[c].SetData(samples, 0);
            }
        }
    }
}
