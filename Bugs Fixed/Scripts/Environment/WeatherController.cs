using System;
using System.Collections;
using UnityEngine;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive Weather Station mounted beside the Observation Window.
    /// Fulfills user requirement for switching weather between:
    /// - SUNNY: Warm golden sun, emerald pine forest, cheerful birdsong & sparkling river.
    /// - RAIN: Overcast cool lighting, wet dark pines, rain particles & droplet sounds on glass.
    /// - SNOW: Crisp winter daylight, white snow blanket on valley & trees, drifting snowflakes, whistling wind.
    /// - AUTUMN: Golden sunset amber light, golden-orange russet foliage, drifting autumn leaves, rustling wind.
    /// </summary>
    public class WeatherController : InteractiveProp
    {
        [Header("Weather State")]
        [SerializeField] private WeatherType _currentWeather = WeatherType.Sunny;

        [Header("Lighting & Audio References")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private WindowSceneryAudio _sceneryAudio;

        [Header("Landscape Materials")]
        [SerializeField] private Material _matPineNeedles;
        [SerializeField] private Material _matPineNeedlesVibrant;
        [SerializeField] private Material _matTerrainGrass;
        [SerializeField] private Material _matMountainRock;
        [SerializeField] private Material _matMountainSnow;
        [SerializeField] private Material _matRiverWater;

        [Header("Weather Particles")]
        [SerializeField] private ParticleSystem _rainParticles;
        [SerializeField] private ParticleSystem _snowParticles;
        [SerializeField] private ParticleSystem _autumnParticles;

        [Header("Console Visuals")]
        [SerializeField] private Renderer _weatherIndicatorRenderer;
        [SerializeField] private Material _matIndicator;

        public WeatherType CurrentWeather => _currentWeather;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 2.4f;
            _promptOffset = new Vector3(0f, 0.40f, 0f);
            ResolveReferences();
        }

        protected override void Start()
        {
            base.Start();
            ApplyWeatherVisuals(_currentWeather, true);
            UpdatePrompt();
        }

        public void ResolveReferences()
        {
            // Ensure 3D text labels on the weather console face into the room and are unmirrored
            var title = transform.Find("TitleText");
            if (title != null) title.localRotation = Quaternion.Euler(0f, 180f, 0f);
            var btnLbl = transform.Find("ButtonLabel");
            if (btnLbl != null) btnLbl.localRotation = Quaternion.Euler(0f, 180f, 0f);

            if (_sunLight == null)
            {
                var sunObj = GameObject.Find("NaturalSunlight");
                if (sunObj != null) _sunLight = sunObj.GetComponent<Light>();
            }

            if (_sceneryAudio == null)
            {
                _sceneryAudio = FindAnyObjectByType<WindowSceneryAudio>();
            }

            // Load materials if null
            if (_matPineNeedles == null)
                _matPineNeedles = Resources.Load<Material>("Materials/Mat_PineNeedles") ??
                                  FindMaterialByName("Mat_PineNeedles");
            if (_matPineNeedlesVibrant == null)
                _matPineNeedlesVibrant = FindMaterialByName("Mat_PineNeedlesVibrant");
            if (_matTerrainGrass == null)
                _matTerrainGrass = FindMaterialByName("Mat_TerrainGrass");
            if (_matMountainRock == null)
                _matMountainRock = FindMaterialByName("Mat_MountainRock");
            if (_matMountainSnow == null)
                _matMountainSnow = FindMaterialByName("Mat_MountainSnow");
            if (_matRiverWater == null)
                _matRiverWater = FindMaterialByName("Mat_RiverWater");

            // Find particles
            if (_rainParticles == null)
            {
                var r = GameObject.Find("WeatherParticles_Rain");
                if (r != null) _rainParticles = r.GetComponent<ParticleSystem>();
            }
            if (_snowParticles == null)
            {
                var s = GameObject.Find("WeatherParticles_Snow");
                if (s != null) _snowParticles = s.GetComponent<ParticleSystem>();
            }
            if (_autumnParticles == null)
            {
                var a = GameObject.Find("WeatherParticles_Autumn");
                if (a != null) _autumnParticles = a.GetComponent<ParticleSystem>();
            }

            if (_weatherIndicatorRenderer == null)
            {
                var indObj = transform.Find("WeatherIndicatorGlow");
                if (indObj != null) _weatherIndicatorRenderer = indObj.GetComponent<Renderer>();
            }
        }

        private Material FindMaterialByName(string matName)
        {
            var renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include);
            foreach (var r in renderers)
            {
                if (r.sharedMaterial != null && r.sharedMaterial.name == matName)
                    return r.sharedMaterial;
            }
            return null;
        }

        protected override void OnInteract()
        {
            base.OnInteract();

            // Cycle: Sunny -> Rain -> Snow -> Autumn -> Sunny
            int next = ((int)_currentWeather + 1) % 4;
            SetWeather((WeatherType)next);
        }

        public void SetWeather(WeatherType weather)
        {
            _currentWeather = weather;
            StopAllCoroutines();
            StartCoroutine(TransitionWeatherRoutine(_currentWeather));
            UpdatePrompt();
        }

        private void UpdatePrompt()
        {
            int next = ((int)_currentWeather + 1) % 4;
            WeatherType nextWeather = (WeatherType)next;
            SetPromptAction($"WEATHER: {nextWeather.ToString().ToUpper()}");
        }

        private IEnumerator TransitionWeatherRoutine(WeatherType weather)
        {
            ResolveReferences();

            // Notify audio controller
            if (_sceneryAudio != null)
            {
                _sceneryAudio.SetWeather(weather);
            }

            // Target settings
            Color targetSunColor = Color.white;
            float targetSunIntensity = 18f;
            Color targetFoliageDark = Color.white;
            Color targetFoliageLight = Color.white;
            Color targetGrass = Color.white;
            Color targetIndicatorColor = Color.white;

            switch (weather)
            {
                case WeatherType.Sunny:
                    targetSunColor = new Color(1.0f, 0.96f, 0.88f);
                    targetSunIntensity = 18.0f;
                    targetFoliageDark = new Color(0.11f, 0.28f, 0.14f);
                    targetFoliageLight = new Color(0.16f, 0.42f, 0.20f);
                    targetGrass = new Color(0.22f, 0.42f, 0.18f);
                    targetIndicatorColor = new Color(1.0f, 0.75f, 0.15f);
                    break;

                case WeatherType.Rain:
                    targetSunColor = new Color(0.60f, 0.72f, 0.84f);
                    targetSunIntensity = 7.0f;
                    targetFoliageDark = new Color(0.06f, 0.18f, 0.09f);
                    targetFoliageLight = new Color(0.09f, 0.26f, 0.13f);
                    targetGrass = new Color(0.14f, 0.26f, 0.12f);
                    targetIndicatorColor = new Color(0.25f, 0.70f, 1.0f);
                    break;

                case WeatherType.Snow:
                    targetSunColor = new Color(0.85f, 0.92f, 1.0f);
                    targetSunIntensity = 14.0f;
                    targetFoliageDark = new Color(0.82f, 0.88f, 0.92f); // Snow frosted
                    targetFoliageLight = new Color(0.88f, 0.92f, 0.96f);
                    targetGrass = new Color(0.95f, 0.97f, 1.0f); // White winter snow
                    targetIndicatorColor = new Color(0.85f, 0.95f, 1.0f);
                    break;

                case WeatherType.Autumn:
                    targetSunColor = new Color(1.0f, 0.74f, 0.45f); // Golden sunset
                    targetSunIntensity = 15.0f;
                    targetFoliageDark = new Color(0.72f, 0.42f, 0.10f); // Golden amber russet
                    targetFoliageLight = new Color(0.85f, 0.52f, 0.12f);
                    targetGrass = new Color(0.58f, 0.46f, 0.20f); // Golden autumn meadow
                    targetIndicatorColor = new Color(1.0f, 0.48f, 0.05f);
                    break;
            }

            // Update particle systems
            if (_rainParticles != null)
            {
                if (weather == WeatherType.Rain && !_rainParticles.isPlaying) _rainParticles.Play();
                else if (weather != WeatherType.Rain && _rainParticles.isPlaying) _rainParticles.Stop();
            }
            if (_snowParticles != null)
            {
                if (weather == WeatherType.Snow && !_snowParticles.isPlaying) _snowParticles.Play();
                else if (weather != WeatherType.Snow && _snowParticles.isPlaying) _snowParticles.Stop();
            }
            if (_autumnParticles != null)
            {
                if (weather == WeatherType.Autumn && !_autumnParticles.isPlaying) _autumnParticles.Play();
                else if (weather != WeatherType.Autumn && _autumnParticles.isPlaying) _autumnParticles.Stop();
            }

            // Smooth crossfade over 1.2 seconds
            float duration = 1.2f;
            float elapsed = 0f;

            Color initSunColor = _sunLight != null ? _sunLight.color : targetSunColor;
            float initSunIntensity = _sunLight != null ? _sunLight.intensity : targetSunIntensity;
            Color initDark = _matPineNeedles != null ? _matPineNeedles.color : targetFoliageDark;
            Color initLight = _matPineNeedlesVibrant != null ? _matPineNeedlesVibrant.color : targetFoliageLight;
            Color initGrass = _matTerrainGrass != null ? _matTerrainGrass.color : targetGrass;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                if (_sunLight != null)
                {
                    _sunLight.color = Color.Lerp(initSunColor, targetSunColor, t);
                    _sunLight.intensity = Mathf.Lerp(initSunIntensity, targetSunIntensity, t);
                }

                if (_matPineNeedles != null) _matPineNeedles.color = Color.Lerp(initDark, targetFoliageDark, t);
                if (_matPineNeedlesVibrant != null) _matPineNeedlesVibrant.color = Color.Lerp(initLight, targetFoliageLight, t);
                if (_matTerrainGrass != null) _matTerrainGrass.color = Color.Lerp(initGrass, targetGrass, t);

                yield return null;
            }

            ApplyWeatherVisuals(weather, false);
        }

        private void ApplyWeatherVisuals(WeatherType weather, bool applySunImmediate)
        {
            if (_sunLight != null && applySunImmediate)
            {
                switch (weather)
                {
                    case WeatherType.Sunny:
                        _sunLight.color = new Color(1.0f, 0.96f, 0.88f);
                        _sunLight.intensity = 18.0f;
                        break;
                    case WeatherType.Rain:
                        _sunLight.color = new Color(0.60f, 0.72f, 0.84f);
                        _sunLight.intensity = 7.0f;
                        break;
                    case WeatherType.Snow:
                        _sunLight.color = new Color(0.85f, 0.92f, 1.0f);
                        _sunLight.intensity = 14.0f;
                        break;
                    case WeatherType.Autumn:
                        _sunLight.color = new Color(1.0f, 0.74f, 0.45f);
                        _sunLight.intensity = 15.0f;
                        break;
                }
            }

            // Material colors
            if (_matPineNeedles != null)
            {
                Color c = (weather == WeatherType.Snow) ? new Color(0.82f, 0.88f, 0.92f) :
                          (weather == WeatherType.Autumn) ? new Color(0.72f, 0.42f, 0.10f) :
                          (weather == WeatherType.Rain) ? new Color(0.06f, 0.18f, 0.09f) :
                          new Color(0.11f, 0.28f, 0.14f);
                _matPineNeedles.color = c;
            }

            if (_matPineNeedlesVibrant != null)
            {
                Color c = (weather == WeatherType.Snow) ? new Color(0.88f, 0.92f, 0.96f) :
                          (weather == WeatherType.Autumn) ? new Color(0.85f, 0.52f, 0.12f) :
                          (weather == WeatherType.Rain) ? new Color(0.09f, 0.26f, 0.13f) :
                          new Color(0.16f, 0.42f, 0.20f);
                _matPineNeedlesVibrant.color = c;
            }

            if (_matTerrainGrass != null)
            {
                Color c = (weather == WeatherType.Snow) ? new Color(0.95f, 0.97f, 1.0f) :
                          (weather == WeatherType.Autumn) ? new Color(0.58f, 0.46f, 0.20f) :
                          (weather == WeatherType.Rain) ? new Color(0.14f, 0.26f, 0.12f) :
                          new Color(0.22f, 0.42f, 0.18f);
                _matTerrainGrass.color = c;
            }

            if (_weatherIndicatorRenderer != null)
            {
                Color indColor = (weather == WeatherType.Sunny) ? new Color(1.0f, 0.75f, 0.15f) :
                                 (weather == WeatherType.Rain) ? new Color(0.25f, 0.70f, 1.0f) :
                                 (weather == WeatherType.Snow) ? new Color(0.85f, 0.95f, 1.0f) :
                                 new Color(1.0f, 0.48f, 0.05f);
                _weatherIndicatorRenderer.material.EnableKeyword("_EMISSION");
                _weatherIndicatorRenderer.material.SetColor("_EmissionColor", indColor * 2.5f);
                _weatherIndicatorRenderer.material.color = indColor;
            }
        }
    }
}
