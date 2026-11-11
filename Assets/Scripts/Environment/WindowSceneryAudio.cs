using System.Collections;
using UnityEngine;

namespace VRRemoteDesktop.Environment
{
    public enum WeatherType
    {
        Sunny,
        Rain,
        Snow,
        Autumn
    }

    /// <summary>
    /// Dynamic audio controller for the Observation Window Scenery.
    /// Synthesizes procedural nature audio tailored for each weather type (Sunny, Rain, Snow, Autumn)
    /// and dynamically modulates volume as the player walks near the window.
    /// </summary>
    public class WindowSceneryAudio : MonoBehaviour
    {
        [Header("Audio Settings")]
        [SerializeField] private float _maxVolume = 0.75f;
        [SerializeField] private float _minVolume = 0.03f;
        [SerializeField] private float _fadeDistanceNear = 1.8f;
        [SerializeField] private float _fadeDistanceFar = 5.8f;

        private AudioSource _audioSource;
        private Transform _playerTransform;
        private float _currentVolume = 0f;
        private WeatherType _currentWeather = WeatherType.Sunny;

        // Cached clips
        private AudioClip _sunnyClip;
        private AudioClip _rainClip;
        private AudioClip _snowClip;
        private AudioClip _autumnClip;

        private void Awake()
        {
            SetupAudioSource();
        }

        private void Start()
        {
            FindPlayer();
            if (_audioSource != null && !_audioSource.isPlaying)
            {
                _audioSource.Play();
            }
        }

        private void SetupAudioSource()
        {
            _audioSource = gameObject.GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            _audioSource.loop = true;
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0.45f;
            _audioSource.rolloffMode = AudioRolloffMode.Linear;
            _audioSource.minDistance = 1.5f;
            _audioSource.maxDistance = 7.0f;
            _audioSource.volume = _minVolume;

            _sunnyClip = GenerateSunnyClip();
            _audioSource.clip = _sunnyClip;
        }

        public void SetWeather(WeatherType weather)
        {
            _currentWeather = weather;
            AudioClip targetClip = null;
            switch (weather)
            {
                case WeatherType.Sunny:
                    if (_sunnyClip == null) _sunnyClip = GenerateSunnyClip();
                    targetClip = _sunnyClip;
                    break;
                case WeatherType.Rain:
                    if (_rainClip == null) _rainClip = GenerateRainClip();
                    targetClip = _rainClip;
                    break;
                case WeatherType.Snow:
                    if (_snowClip == null) _snowClip = GenerateSnowClip();
                    targetClip = _snowClip;
                    break;
                case WeatherType.Autumn:
                    if (_autumnClip == null) _autumnClip = GenerateAutumnClip();
                    targetClip = _autumnClip;
                    break;
            }

            if (_audioSource != null && targetClip != null)
            {
                float time = _audioSource.time;
                _audioSource.clip = targetClip;
                _audioSource.time = Mathf.Clamp(time, 0f, targetClip.length - 0.1f);
                _audioSource.Play();
            }
        }

        private void FindPlayer()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                _playerTransform = cam.transform;
            }
            else
            {
                var pc = FindAnyObjectByType<VRRemoteDesktop.Player.PlayerController>();
                if (pc != null) _playerTransform = pc.transform;
            }
        }

        private void Update()
        {
            if (_playerTransform == null)
            {
                FindPlayer();
                if (_playerTransform == null) return;
            }

            Vector3 windowCenter = transform.position;
            float distX = Mathf.Abs(_playerTransform.position.x - windowCenter.x);
            float distTotal = Vector3.Distance(_playerTransform.position, windowCenter);
            float effectiveDist = Mathf.Min(distX * 1.1f, distTotal);

            float t = Mathf.InverseLerp(_fadeDistanceFar, _fadeDistanceNear, effectiveDist);
            float targetVolume = Mathf.Lerp(_minVolume, _maxVolume, t * t);

            // Gaze boost when looking towards the window (+X)
            Vector3 toWindow = (windowCenter - _playerTransform.position).normalized;
            float lookDot = Vector3.Dot(_playerTransform.forward, toWindow);
            if (lookDot > 0.3f)
            {
                targetVolume *= Mathf.Lerp(1.0f, 1.30f, (lookDot - 0.3f) / 0.7f);
            }

            targetVolume = Mathf.Clamp(targetVolume, 0f, 1.0f);
            _currentVolume = Mathf.MoveTowards(_currentVolume, targetVolume, Time.deltaTime * 0.90f);
            if (_audioSource != null)
            {
                _audioSource.volume = _currentVolume;
            }
        }

        #region Soundscape Generators
        private AudioClip GenerateSunnyClip()
        {
            int sampleRate = 44100;
            float duration = 16.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            var birdEvents = new[]
            {
                new { start = 1.15f, dur = 0.11f, f0 = 2900f, f1 = 3700f, vibF = 0f, vibD = 0f, gain = 0.42f },
                new { start = 1.32f, dur = 0.15f, f0 = 3300f, f1 = 4100f, vibF = 18f, vibD = 80f, gain = 0.45f },
                new { start = 3.50f, dur = 0.20f, f0 = 3800f, f1 = 3000f, vibF = 24f, vibD = 130f, gain = 0.48f },
                new { start = 3.78f, dur = 0.14f, f0 = 3400f, f1 = 4300f, vibF = 0f, vibD = 0f, gain = 0.44f },
                new { start = 4.00f, dur = 0.17f, f0 = 4000f, f1 = 2900f, vibF = 20f, vibD = 90f, gain = 0.42f },
                new { start = 6.80f, dur = 0.10f, f0 = 3100f, f1 = 3600f, vibF = 0f, vibD = 0f, gain = 0.40f },
                new { start = 6.96f, dur = 0.11f, f0 = 3400f, f1 = 3900f, vibF = 0f, vibD = 0f, gain = 0.43f },
                new { start = 7.14f, dur = 0.18f, f0 = 3700f, f1 = 4500f, vibF = 28f, vibD = 110f, gain = 0.46f },
                new { start = 10.20f, dur = 0.22f, f0 = 2600f, f1 = 3400f, vibF = 15f, vibD = 60f, gain = 0.38f },
                new { start = 10.50f, dur = 0.16f, f0 = 3500f, f1 = 3100f, vibF = 22f, vibD = 75f, gain = 0.38f },
                new { start = 12.80f, dur = 0.12f, f0 = 3600f, f1 = 4400f, vibF = 32f, vibD = 120f, gain = 0.44f },
                new { start = 13.00f, dur = 0.16f, f0 = 4200f, f1 = 3300f, vibF = 26f, vibD = 100f, gain = 0.42f },
                new { start = 15.10f, dur = 0.14f, f0 = 3200f, f1 = 3800f, vibF = 0f, vibD = 0f, gain = 0.40f }
            };

            float[] birdPhases = new float[birdEvents.Length];
            float rF1 = 0f, rF2 = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Mathf.Sin(i * 12.9898f + 78.233f) * 43758.5453f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                // River flow
                rF1 += (noise - rF1) * 0.18f;
                rF2 += (rF1 - rF2) * 0.22f;
                float water = rF2 * 0.26f * (Mathf.Sin(t * 3.6f) * 0.15f + 1f);

                // Warm breeze
                float breeze = Mathf.Sin(2f * Mathf.PI * 160f * t) * 0.03f * (Mathf.Sin(t * 0.4f) * 0.3f + 0.7f);

                // Songbirds
                float birds = 0f;
                for (int b = 0; b < birdEvents.Length; b++)
                {
                    var ev = birdEvents[b];
                    if (t >= ev.start && t < ev.start + ev.dur)
                    {
                        float localT = (t - ev.start) / ev.dur;
                        float f = Mathf.Lerp(ev.f0, ev.f1, localT);
                        if (ev.vibF > 0f) f += Mathf.Sin(2f * Mathf.PI * ev.vibF * (t - ev.start)) * ev.vibD;
                        birdPhases[b] += 2f * Mathf.PI * f / sampleRate;
                        float env = Mathf.Sin(localT * Mathf.PI);
                        birds += Mathf.Sin(birdPhases[b]) * env * env * ev.gain;
                    }
                }

                samples[i] = Mathf.Clamp((water + breeze + birds) * 0.75f, -0.92f, 0.92f);
            }

            AudioClip clip = AudioClip.Create("Audio_Sunny", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateRainClip()
        {
            int sampleRate = 44100;
            float duration = 12.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float rF1 = 0f, rF2 = 0f;
            float rainF1 = 0f, rainF2 = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Mathf.Sin(i * 14.1234f + 31.98f) * 39812.31f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                // River rush
                rF1 += (noise - rF1) * 0.22f;
                rF2 += (rF1 - rF2) * 0.25f;
                float river = rF2 * 0.32f;

                // Steady rain shower rush
                rainF1 += (noise - rainF1) * 0.65f;
                rainF2 += (rainF1 - rainF2) * 0.65f;
                float rainRush = (rainF1 - rainF2) * 0.24f;

                // Droplet taps on window glass
                float dropProb = Mathf.Sin(i * 91.312f) * 29182.1f;
                dropProb = dropProb - Mathf.Floor(dropProb);
                float drop = 0f;
                if (dropProb > 0.9972f)
                {
                    drop = (dropProb - 0.9972f) * 220f * Mathf.Sin(2f * Mathf.PI * 2800f * t);
                }

                // Subtle distant thunder roll at 5.5s
                float thunder = 0f;
                if (t >= 5.0f && t < 7.5f)
                {
                    float thT = (t - 5.0f) / 2.5f;
                    float thNoise = Mathf.Sin(i * 1.8f) * 0.12f;
                    thunder = thNoise * Mathf.Sin(thT * Mathf.PI) * 0.18f;
                }

                samples[i] = Mathf.Clamp((river + rainRush + drop + thunder) * 0.70f, -0.92f, 0.92f);
            }

            AudioClip clip = AudioClip.Create("Audio_Rain", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateSnowClip()
        {
            int sampleRate = 44100;
            float duration = 14.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Mathf.Sin(i * 11.234f + 42.1f) * 41235.1f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                // Whistling winter mountain wind
                float windLfo = Mathf.Sin(t * 0.45f) * 0.4f + 0.6f;
                float whistle = Mathf.Sin(2f * Mathf.PI * 340f * t + Mathf.Sin(t * 2.2f) * 1.5f) * 0.045f * windLfo;
                float deepWind = Mathf.Sin(2f * Mathf.PI * 95f * t) * 0.08f * windLfo;

                // Gentle iced crystal chime at 3.2s and 9.5s
                float chime = 0f;
                if ((t > 3.2f && t < 4.2f) || (t > 9.5f && t < 10.5f))
                {
                    float chT = (t > 6f) ? (t - 9.5f) : (t - 3.2f);
                    chime = Mathf.Sin(2f * Mathf.PI * 2400f * t) * Mathf.Exp(-chT * 4f) * 0.08f;
                }

                samples[i] = Mathf.Clamp((whistle + deepWind + chime + noise * 0.025f) * 0.70f, -0.92f, 0.92f);
            }

            AudioClip clip = AudioClip.Create("Audio_Snow", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private AudioClip GenerateAutumnClip()
        {
            int sampleRate = 44100;
            float duration = 14.0f;
            int totalSamples = (int)(sampleRate * duration);
            float[] samples = new float[totalSamples];

            float rF1 = 0f, rF2 = 0f;

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float noise = (Mathf.Sin(i * 13.567f + 53.2f) * 38921.4f);
                noise = (noise - Mathf.Floor(noise)) * 2f - 1f;

                // Autumn river
                rF1 += (noise - rF1) * 0.16f;
                rF2 += (rF1 - rF2) * 0.20f;
                float water = rF2 * 0.22f;

                // Rustling wind through dry autumn foliage
                float rustleLfo = Mathf.Sin(t * 0.6f) * 0.35f + 0.65f;
                float rustle = Mathf.Sin(2f * Mathf.PI * 190f * t) * 0.05f * rustleLfo;

                // Occasional autumn finch whistle at 4.2s and 10.5s
                float whistle = 0f;
                if ((t > 4.2f && t < 4.6f) || (t > 10.5f && t < 10.9f))
                {
                    float wT = (t > 7f) ? (t - 10.5f) / 0.4f : (t - 4.2f) / 0.4f;
                    whistle = Mathf.Sin(2f * Mathf.PI * 3100f * t) * Mathf.Sin(wT * Mathf.PI) * 0.24f;
                }

                samples[i] = Mathf.Clamp((water + rustle + whistle) * 0.72f, -0.92f, 0.92f);
            }

            AudioClip clip = AudioClip.Create("Audio_Autumn", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
        #endregion
    }
}
