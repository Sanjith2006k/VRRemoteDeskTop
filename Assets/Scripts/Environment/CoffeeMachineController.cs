using System.Collections;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive Quantum Coffee Synthesizer fulfilling Section 7 (Prop B) of requirements.
    /// Manages brewing animations, nozzle illumination, procedural steam/dispense audio,
    /// animated cup delivery, and hilarious coffee diagnostics.
    /// </summary>
    public class CoffeeMachineController : InteractiveProp
    {
        [Header("Machine References")]
        [SerializeField] private Renderer _nozzleRenderer;
        [SerializeField] private GameObject _spawnedCup;
        [SerializeField] private Light _brewLight;
        [SerializeField] private TextMeshProUGUI _screenStatusText;
        [SerializeField] private ParticleSystem _steamParticleSystem;

        private static readonly string[] CoffeeDiagnoses = new string[]
        {
            "Quantum Espresso ready.\nProductivity may vary.",
            "98% hyper-caffeine,\n2% questionable life choices.",
            "Dark Roast extracted.\nSleep mode permanently postponed.",
            "Dark matter espresso ready.\nPhysics laws temporarily suspended.",
            "Overclocked caffeine ready.\nMay cause rapid mouse clicks."
        };

        private enum CoffeeState
        {
            Standby,
            Brewing,
            ReadyToDrink
        }

        private CoffeeState _state = CoffeeState.Standby;
        private int _brewCount = 0;
        private MaterialPropertyBlock _nozzlePropBlock;
        private Coroutine _steamRoutine;

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 2.2f;
            _promptOffset = new Vector3(0f, 0.35f, 0f);
            SetPromptAction("BREW COFFEE");
            _nozzlePropBlock = new MaterialPropertyBlock();
            if (_spawnedCup != null) _spawnedCup.SetActive(false);
            if (_brewLight != null) _brewLight.enabled = false;
            CreateSteamSystemIfNeeded();
            UpdateScreenText("QUANTUM BREW v3.4\nSTATUS: STANDBY");
        }

        protected override void OnInteract()
        {
            if (_state == CoffeeState.Brewing) return;

            if (_state == CoffeeState.Standby)
            {
                StartCoroutine(BrewRoutine());
            }
            else if (_state == CoffeeState.ReadyToDrink)
            {
                DrinkCoffee();
            }
        }

        private IEnumerator BrewRoutine()
        {
            _state = CoffeeState.Brewing;
            _brewCount++;

            UpdateScreenText("SYNTHESIZING...\nHEATING DARK ROAST");
            SetPromptAction("BREWING...");

            // Turn on warm amber brew light
            if (_brewLight != null)
            {
                _brewLight.enabled = true;
                _brewLight.color = new Color(1f, 0.6f, 0.1f);
            }

            // Start steam effect
            StartSteam(2.5f);

            // Audio: play procedural prompt tone
            AudioManager.Instance?.PlayPromptTone();

            // Pulse nozzle for 1.8 seconds
            float elapsed = 0f;
            float duration = 1.8f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 20f);
                Color c = Color.Lerp(new Color(0.2f, 0.1f, 0.05f), new Color(1f, 0.7f, 0.2f), pulse);
                SetNozzleColor(c * 3.5f);
                yield return null;
            }

            // Dispense cup onto tray
            if (_spawnedCup != null)
            {
                _spawnedCup.SetActive(true);
                Vector3 orig = _spawnedCup.transform.localPosition;
                _spawnedCup.transform.localPosition = orig + new Vector3(0f, 0.08f, 0f);
                float t = 0f;
                while (t < 0.25f)
                {
                    t += Time.deltaTime;
                    _spawnedCup.transform.localPosition = Vector3.Lerp(orig + new Vector3(0f, 0.08f, 0f), orig, t / 0.25f);
                    yield return null;
                }
                _spawnedCup.transform.localPosition = orig;
            }

            // Completion chime
            AudioManager.Instance?.PlayConnectChime();

            // Display diagnosis
            string diag = CoffeeDiagnoses[(_brewCount - 1) % CoffeeDiagnoses.Length];
            UpdateScreenText(diag);

            // Unlock initial brewing achievement
            AchievementManager.Instance?.UnlockAchievement("Caffeine Dependency", "Synthesized a cup of pure virtual hyper-caffeine.");

            // Switch to ReadyToDrink state
            _state = CoffeeState.ReadyToDrink;
            SetPromptAction("DRINK ESPRESSO");
        }

        private void DrinkCoffee()
        {
            // Play drinking slurp audio
            AudioManager.Instance?.PlaySlurp();

            // Hide cup
            if (_spawnedCup != null)
            {
                _spawnedCup.SetActive(false);
            }

            StopSteam();

            if (_brewLight != null) _brewLight.enabled = false;
            SetNozzleColor(new Color(0.1f, 0.2f, 0.3f) * 1.5f);
            UpdateScreenText("ESPRESSO CONSUMED!\nSTATUS: STANDBY");
            SetPromptAction("BREW COFFEE");
            _state = CoffeeState.Standby;

            // Trigger DrinkEffectManager
            DrinkEffectManager.Instance?.ConsumeCoffee();
        }

        private void CreateSteamSystemIfNeeded()
        {
            if (_steamParticleSystem != null) return;

            GameObject steamObj = new GameObject("CoffeeSteamSystem");
            steamObj.transform.SetParent(transform, false);
            steamObj.transform.localPosition = new Vector3(0f, 0.16f, -0.22f);

            _steamParticleSystem = steamObj.AddComponent<ParticleSystem>();
            var main = _steamParticleSystem.main;
            main.startLifetime = 1.0f;
            main.startSpeed = 0.18f;
            main.startSize = 0.05f;
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;

            var emission = _steamParticleSystem.emission;
            emission.rateOverTime = 16f;
            emission.enabled = false;

            var shape = _steamParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.02f;

            var colorOverLifetime = _steamParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0f), new GradientAlphaKey(0.4f, 0.3f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = _steamParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(1f, 1.8f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            var pRenderer = steamObj.GetComponent<ParticleSystemRenderer>();
            Shader unlit = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (unlit != null)
            {
                Material steamMat = new Material(unlit);
                pRenderer.material = steamMat;
            }
        }

        private void StartSteam(float duration)
        {
            if (_steamRoutine != null) StopCoroutine(_steamRoutine);
            _steamRoutine = StartCoroutine(SteamDurationRoutine(duration));
        }

        private IEnumerator SteamDurationRoutine(float duration)
        {
            if (_steamParticleSystem != null)
            {
                var emission = _steamParticleSystem.emission;
                emission.enabled = true;
                _steamParticleSystem.Play();
            }

            yield return new WaitForSeconds(duration);
            StopSteam();
        }

        private void StopSteam()
        {
            if (_steamParticleSystem != null)
            {
                var emission = _steamParticleSystem.emission;
                emission.enabled = false;
                _steamParticleSystem.Stop();
            }
            if (_steamRoutine != null)
            {
                StopCoroutine(_steamRoutine);
                _steamRoutine = null;
            }
        }

        private void SetNozzleColor(Color color)
        {
            if (_nozzleRenderer == null) return;
            _nozzleRenderer.GetPropertyBlock(_nozzlePropBlock);
            _nozzlePropBlock.SetColor("_BaseColor", color);
            _nozzlePropBlock.SetColor("_EmissionColor", color);
            _nozzleRenderer.SetPropertyBlock(_nozzlePropBlock);
        }

        private void UpdateScreenText(string text)
        {
            if (_screenStatusText != null)
            {
                _screenStatusText.text = text;
            }
        }
    }
}
