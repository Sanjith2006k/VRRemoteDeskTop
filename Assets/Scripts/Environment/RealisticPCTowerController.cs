using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Controls the realistic desktop workstation/gaming PC tower beside the monitor.
    /// Manages rotating fan blade animations, dynamic ARGB lighting, front I/O activity LED,
    /// and simulated CPU/pump telemetry display on the AIO cooler pump OLED screen.
    /// </summary>
    [ExecuteAlways]
    public class RealisticPCTowerController : MonoBehaviour
    {
        [Header("Animated Fan Blades")]
        [SerializeField] private List<Transform> _fanBlades = new List<Transform>();
        [SerializeField] private float _fanRpm = 1200f;

        [Header("Simulated Telemetry Display")]
        [SerializeField] private TextMeshPro _telemetryText;
        [SerializeField] private Renderer _pumpScreenRenderer;

        [Header("Front I/O")]
        [SerializeField] private Renderer _powerLedRenderer;
        [SerializeField] private Renderer _driveLedRenderer;

        [Header("RGB Lighting")]
        [SerializeField] private List<Renderer> _rgbRenderers = new List<Renderer>();
        [SerializeField] private Light _internalLedLight;
        [SerializeField] private bool _cycleRgb = true;
        [SerializeField] private float _rgbSpeed = 0.5f;

        private MaterialPropertyBlock _propBlock;
        private float _telemetryUpdateTimer = 0f;
        private float _driveBlinkTimer = 0f;
        private bool _driveBlinkState = false;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureRealisticTower()
        {
            GameObject pcTower = GameObject.Find("PC_Tower_Battlestation");
            if (pcTower != null)
            {
                var controller = pcTower.GetComponent<RealisticPCTowerController>();
                if (controller == null) controller = pcTower.AddComponent<RealisticPCTowerController>();
                if (pcTower.transform.Find("Chassis_Frame") != null || pcTower.transform.Find("ChassisHousing") == null)
                {
                    controller.BuildRealisticChassis(null, null, null, null, null, null);
                }
            }
        }

        private void Awake()
        {
            _propBlock = new MaterialPropertyBlock();
            EnsureProperCollision();

            if (transform.Find("Chassis_Frame") != null || transform.Find("ChassisHousing") == null)
            {
                BuildRealisticChassis(null, null, null, null, null, null);
            }
        }

        private void Start()
        {
            UpdateSimulatedTelemetry(true);
        }

        private void Update()
        {
            // Rotate all fan blades smoothly
            if (_fanBlades != null && _fanBlades.Count > 0)
            {
                float degPerSec = (_fanRpm * 360f / 60f) * Time.deltaTime;
                for (int i = 0; i < _fanBlades.Count; i++)
                {
                    if (_fanBlades[i] != null)
                    {
                        // Rotate around local Z axis (axis of fan cylinder)
                        _fanBlades[i].Rotate(Vector3.forward, degPerSec, Space.Self);
                    }
                }
            }

            // Animate ARGB cycling
            if (_cycleRgb)
            {
                float hue = Mathf.Repeat(Time.time * _rgbSpeed * 0.2f, 1f);
                Color rgbColor = Color.HSVToRGB(hue, 0.85f, 1.0f);

                if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

                for (int i = 0; i < _rgbRenderers.Count; i++)
                {
                    if (_rgbRenderers[i] != null)
                    {
                        _rgbRenderers[i].GetPropertyBlock(_propBlock);
                        _propBlock.SetColor("_BaseColor", rgbColor);
                        _propBlock.SetColor("_EmissionColor", rgbColor * 2.8f);
                        _rgbRenderers[i].SetPropertyBlock(_propBlock);
                    }
                }

                if (_internalLedLight != null)
                {
                    _internalLedLight.color = Color.Lerp(rgbColor, Color.cyan, 0.4f);
                }
            }

            // Blinking drive activity LED (random flickers simulating SSD read/write)
            _driveBlinkTimer -= Time.deltaTime;
            if (_driveBlinkTimer <= 0f)
            {
                _driveBlinkTimer = Random.Range(0.08f, 0.35f);
                _driveBlinkState = Random.value > 0.4f;
                if (_driveLedRenderer != null)
                {
                    if (_propBlock == null) _propBlock = new MaterialPropertyBlock();
                    _driveLedRenderer.GetPropertyBlock(_propBlock);
                    Color blinkColor = _driveBlinkState ? new Color(1f, 0.6f, 0.1f) * 3f : Color.black;
                    _propBlock.SetColor("_BaseColor", blinkColor);
                    _propBlock.SetColor("_EmissionColor", blinkColor);
                    _driveLedRenderer.SetPropertyBlock(_propBlock);
                }
            }

            // Update simulated CPU telemetry on the AIO cooler mini OLED
            _telemetryUpdateTimer += Time.deltaTime;
            if (_telemetryUpdateTimer >= 0.5f)
            {
                _telemetryUpdateTimer = 0f;
                UpdateSimulatedTelemetry(false);
            }
        }

        private void UpdateSimulatedTelemetry(bool force)
        {
            if (_telemetryText == null) return;

            // Generate believable, dynamic simulated values clearly labeled as simulated
            float t = Time.time;
            float simulatedTemp = 42.5f + Mathf.Sin(t * 0.25f) * 2.8f + (Mathf.PerlinNoise(t * 0.5f, 0f) - 0.5f) * 1.5f;
            float simulatedPump = 2410f + Mathf.Sin(t * 0.4f) * 35f;
            float simulatedCpuLoad = 18f + Mathf.PingPong(t * 4f, 15f);

            _telemetryText.text =
                $"<size=55%><color=#888888>[SIMULATED TELEMETRY]</color></size>\n" +
                $"<color=#00ffff><b>CPU:</b> {simulatedTemp:F1} degC</color> <color=#aaaaaa>({simulatedCpuLoad:F0}%)</color>\n" +
                $"<color=#ff00ff><b>AIO:</b> {simulatedPump:F0} RPM</color>";
        }

        private void EnsureProperCollision()
        {
            var col = GetComponent<BoxCollider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(0.24f, 0.52f, 0.48f);
            col.center = Vector3.zero;
        }

        public void RegisterFanBlade(Transform blade)
        {
            if (blade != null && !_fanBlades.Contains(blade))
            {
                _fanBlades.Add(blade);
            }
        }

        public void RegisterRgbRenderer(Renderer r)
        {
            if (r != null && !_rgbRenderers.Contains(r))
            {
                _rgbRenderers.Add(r);
            }
        }

        public void SetReferences(TextMeshPro telemetryText, Renderer powerLed, Renderer driveLed, Light internalLight)
        {
            _telemetryText = telemetryText;
            _powerLedRenderer = powerLed;
            _driveLedRenderer = driveLed;
            _internalLedLight = internalLight;
        }

        /// <summary>
        /// Procedurally builds or refines the realistic tower components directly under this GameObject.
        /// Destroys any old opaque Chassis_Frame cubes and constructs hollow panels, motherboard,
        /// AIO cooler, DDR5 RAM, GPU with anti-sag bracket, PSU shroud with viewing window,
        /// tempered glass side panel with thumb screws, and 6 animated spinning fans.
        /// </summary>
        [ContextMenu("Rebuild Realistic PC Tower")]
        public void BuildRealisticChassis(Material matPC, Material matGlass, Material matChrome,
                                          Material matCyan, Material matMagenta, Material matViolet)
        {
            // 1. Clear old children
            while (transform.childCount > 0)
            {
                Transform child = transform.GetChild(0);
                child.SetParent(null);
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }

            _fanBlades.Clear();
            _rgbRenderers.Clear();

            EnsureProperCollision();

            // Materials fallback
            if (matPC == null) matPC = CreateMaterial("Mat_PCTowerCase", new Color(0.05f, 0.05f, 0.06f), 0.85f, 0.35f);
            if (matChrome == null) matChrome = CreateMaterial("Mat_PCChrome", new Color(0.85f, 0.88f, 0.92f), 0.95f, 0.95f);
            if (matGlass == null) matGlass = CreateGlassMaterial("Mat_PCTemperedGlass", new Color(0.08f, 0.12f, 0.16f, 0.28f));
            if (matCyan == null) matCyan = CreateEmissionMaterial("Mat_PCCyan", new Color(0f, 0.85f, 1f), 3.0f);
            if (matMagenta == null) matMagenta = CreateEmissionMaterial("Mat_PCMagenta", new Color(1f, 0.1f, 0.7f), 3.0f);
            if (matViolet == null) matViolet = CreateEmissionMaterial("Mat_PCViolet", new Color(0.7f, 0.2f, 1f), 3.0f);

            // --- 2. CHASSIS OUTER HOUSING (Hollow Structure) ---
            GameObject chassisRoot = new GameObject("ChassisHousing");
            chassisRoot.transform.SetParent(transform, false);

            // A. Right Side Solid Steel Panel (x = +0.118m)
            CreateCube("Right_Steel_SidePanel", chassisRoot.transform,
                       new Vector3(0.118f, 0f, 0f),
                       new Vector3(0.006f, 0.50f, 0.46f), matPC);

            // B. Rear I/O & Exhaust Panel (z = +0.228m)
            CreateCube("Rear_Chassis_Panel", chassisRoot.transform,
                       new Vector3(0f, 0f, 0.228f),
                       new Vector3(0.24f, 0.50f, 0.006f), matPC);

            // C. Top Panel with Recessed Magnetic Mesh Vent (y = +0.248m)
            CreateCube("Top_Chassis_Panel", chassisRoot.transform,
                       new Vector3(0f, 0.248f, 0f),
                       new Vector3(0.24f, 0.008f, 0.46f), matPC);

            // Magnetic mesh filter on top
            CreateCube("Top_Magnetic_Mesh_Filter", chassisRoot.transform,
                       new Vector3(0f, 0.253f, 0.02f),
                       new Vector3(0.18f, 0.003f, 0.38f), matChrome);

            // D. Bottom Panel with 4 Non-Slip Rubber Feet (y = -0.248m)
            CreateCube("Bottom_Chassis_Panel", chassisRoot.transform,
                       new Vector3(0f, -0.248f, 0f),
                       new Vector3(0.24f, 0.008f, 0.46f), matPC);

            for (int fx = -1; fx <= 1; fx += 2)
            {
                for (int fz = -1; fz <= 1; fz += 2)
                {
                    GameObject foot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    foot.name = $"Chassis_Foot_{fx}_{fz}";
                    foot.transform.SetParent(chassisRoot.transform, false);
                    foot.transform.localPosition = new Vector3(fx * 0.09f, -0.258f, fz * 0.18f);
                    foot.transform.localScale = new Vector3(0.035f, 0.012f, 0.035f);
                    foot.GetComponent<Renderer>().sharedMaterial = matPC;
                    DestroyImmediate(foot.GetComponent<Collider>());
                }
            }

            // E. Front Bezel & I/O Hub (z = -0.232m)
            GameObject frontBezel = CreateCube("Front_Bezel", chassisRoot.transform,
                                              new Vector3(0f, 0f, -0.232f),
                                              new Vector3(0.24f, 0.50f, 0.012f), matPC);

            // Front Mesh Intake Grill (center cutout)
            GameObject frontMesh = CreateCube("Front_Intake_Mesh", frontBezel.transform,
                                              new Vector3(0f, -0.04f, -0.008f),
                                              new Vector3(0.16f, 0.38f, 0.004f), matChrome);

            // Front I/O Top Header: Power Button with LED halo ring, 2x USB-A, 1x USB-C, 3.5mm jack
            BuildFrontIO(frontBezel.transform, matChrome, matCyan);

            // F. Lower PSU Shroud with Side Viewing Window
            GameObject psuShroud = CreateCube("PSU_Shroud_Chamber", chassisRoot.transform,
                                              new Vector3(0.02f, -0.18f, 0f),
                                              new Vector3(0.20f, 0.12f, 0.44f), matPC);

            // PSU Cutout Window on tempered glass side (x = -0.082f)
            GameObject psuBadge = CreateCube("PSU_1000W_Platinum_Badge", psuShroud.transform,
                                             new Vector3(-0.505f, 0f, -0.05f),
                                             new Vector3(0.01f, 0.45f, 0.38f), matChrome);

            // Illuminated badge text (Quad with emission)
            GameObject badgeQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            badgeQuad.name = "Badge_Text_Illuminated";
            badgeQuad.transform.SetParent(psuBadge.transform, false);
            badgeQuad.transform.localPosition = new Vector3(-0.55f, 0f, 0f);
            badgeQuad.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
            badgeQuad.transform.localScale = new Vector3(0.8f, 0.6f, 1f);
            badgeQuad.GetComponent<Renderer>().sharedMaterial = matCyan;
            RegisterRgbRenderer(badgeQuad.GetComponent<Renderer>());
            DestroyImmediate(badgeQuad.GetComponent<Collider>());

            // --- 3. TEMPERED GLASS SIDE PANEL (Inward facing desk side at x = -0.120m) ---
            GameObject glassPanel = CreateCube("Tempered_Glass_SidePanel", chassisRoot.transform,
                                               new Vector3(-0.120f, 0.01f, 0f),
                                               new Vector3(0.005f, 0.47f, 0.44f), matGlass);

            // 4 Knurled Thumbscrews on glass corners
            float[] screwY = new float[] { -0.21f, 0.21f };
            float[] screwZ = new float[] { -0.19f, 0.19f };
            for (int sy = 0; sy < 2; sy++)
            {
                for (int sz = 0; sz < 2; sz++)
                {
                    GameObject screw = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    screw.name = $"Thumbscrew_{sy}_{sz}";
                    screw.transform.SetParent(glassPanel.transform, false);
                    screw.transform.localPosition = new Vector3(-0.6f, screwY[sy], screwZ[sz]);
                    screw.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    screw.transform.localScale = new Vector3(0.018f, 0.008f, 0.018f);
                    screw.GetComponent<Renderer>().sharedMaterial = matChrome;
                    DestroyImmediate(screw.GetComponent<Collider>());
                }
            }

            // --- 4. INTERNAL HARDWARE (Visible through tempered glass) ---
            GameObject internals = new GameObject("InteriorHardware");
            internals.transform.SetParent(transform, false);

            // A. ATX Motherboard (PCB against right tray at x = 0.085m)
            GameObject mobo = CreateCube("Motherboard_ATX", internals.transform,
                                         new Vector3(0.085f, 0.04f, 0.02f),
                                         new Vector3(0.012f, 0.31f, 0.29f), matPC);

            // VRM Dual Aluminum Heatsinks
            CreateCube("VRM_Heatsink_Top", mobo.transform,
                       new Vector3(-0.6f, 0.38f, 0.12f),
                       new Vector3(0.4f, 0.12f, 0.36f), matChrome);
            CreateCube("VRM_Heatsink_Left", mobo.transform,
                       new Vector3(-0.6f, 0.14f, 0.38f),
                       new Vector3(0.4f, 0.35f, 0.10f), matChrome);

            // M.2 NVMe Thermal Armor
            CreateCube("M2_Shield_ThermalArmor", mobo.transform,
                       new Vector3(-0.55f, -0.06f, 0.15f),
                       new Vector3(0.3f, 0.08f, 0.26f), matChrome);

            // Motherboard 24-Pin Braided Power Cable (curves from PSU shroud)
            for (int c = 0; c < 3; c++)
            {
                GameObject cable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cable.name = $"Mobo_24Pin_Cable_{c}";
                cable.transform.SetParent(internals.transform, false);
                cable.transform.localPosition = new Vector3(0.065f, -0.02f + (c * 0.03f), -0.09f);
                cable.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);
                cable.transform.localScale = new Vector3(0.008f, 0.05f, 0.008f);
                cable.GetComponent<Renderer>().sharedMaterial = matPC;
                DestroyImmediate(cable.GetComponent<Collider>());
            }

            // B. Dual DDR5 RGB RAM Modules
            for (int r = 0; r < 2; r++)
            {
                GameObject ram = CreateCube($"RAM_DDR5_{r}", internals.transform,
                                            new Vector3(0.062f, 0.11f, -0.025f + (r * 0.022f)),
                                            new Vector3(0.010f, 0.055f, 0.008f), matChrome);

                // Frosted RGB Lightbar on top edge of RAM
                GameObject ramRgb = CreateCube("RGB_Lightbar", ram.transform,
                                               new Vector3(-0.55f, 0.42f, 0f),
                                               new Vector3(0.2f, 0.18f, 0.95f), matCyan);
                RegisterRgbRenderer(ramRgb.GetComponent<Renderer>());
            }

            // C. Premium 280mm CPU AIO Cooler Block with Mini OLED Display
            BuildAIOCooler(internals.transform, matPC, matChrome, matCyan);

            // D. Triple-Fan Gaming GPU with Anti-Sag Support Bracket
            BuildHighEndGPU(internals.transform, matPC, matChrome, matMagenta);

            // E. Case Fans with Animated Spinning Blades
            BuildCaseFans(internals.transform, matPC, matChrome, matCyan, matViolet, matMagenta);

            // F. Internal LED Point Light (Casts soft ARGB glow through glass onto desk surface)
            GameObject pcLightObj = new GameObject("PC_Internal_LED");
            pcLightObj.transform.SetParent(internals.transform, false);
            pcLightObj.transform.localPosition = new Vector3(-0.02f, 0.08f, 0f);
            Light pcLight = pcLightObj.AddComponent<Light>();
            pcLight.type = LightType.Point;
            pcLight.range = 0.85f;
            pcLight.intensity = 2.2f;
            pcLight.color = new Color(0.2f, 0.8f, 1.0f);
            _internalLedLight = pcLight;
        }

        private void BuildFrontIO(Transform parent, Material matChrome, Material matCyan)
        {
            GameObject ioRoot = new GameObject("Front_IO_Hub");
            ioRoot.transform.SetParent(parent, false);
            ioRoot.transform.localPosition = new Vector3(0f, 0.22f, -0.008f);

            // Power button (Cylinder with glowing LED ring)
            GameObject pwrBtn = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pwrBtn.name = "Power_Button";
            pwrBtn.transform.SetParent(ioRoot.transform, false);
            pwrBtn.transform.localPosition = new Vector3(-0.065f, 0f, 0f);
            pwrBtn.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            pwrBtn.transform.localScale = new Vector3(0.018f, 0.004f, 0.018f);
            pwrBtn.GetComponent<Renderer>().sharedMaterial = matChrome;
            DestroyImmediate(pwrBtn.GetComponent<Collider>());

            GameObject pwrRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pwrRing.name = "Power_LED_Halo";
            pwrRing.transform.SetParent(pwrBtn.transform, false);
            pwrRing.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            pwrRing.transform.localScale = new Vector3(0.85f, 0.1f, 0.85f);
            pwrRing.GetComponent<Renderer>().sharedMaterial = matCyan;
            _powerLedRenderer = pwrRing.GetComponent<Renderer>();
            RegisterRgbRenderer(_powerLedRenderer);
            DestroyImmediate(pwrRing.GetComponent<Collider>());

            // 2x USB 3.2 Type-A Ports (blue tongue)
            for (int u = 0; u < 2; u++)
            {
                GameObject usbA = CreateCube($"USB_3_Port_{u}", ioRoot.transform,
                                             new Vector3(-0.028f + (u * 0.024f), 0f, 0f),
                                             new Vector3(0.014f, 0.006f, 0.004f), matChrome);

                GameObject tongue = CreateCube("Blue_Tongue", usbA.transform,
                                               new Vector3(0f, 0f, -0.5f),
                                               new Vector3(0.6f, 0.3f, 0.2f), matCyan);
            }

            // USB-C Port
            CreateCube("USB_C_Port", ioRoot.transform,
                       new Vector3(0.035f, 0f, 0f),
                       new Vector3(0.010f, 0.004f, 0.004f), matChrome);

            // 3.5mm Headphone Jack (gold/chrome ring)
            GameObject jack = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            jack.name = "Audio_35mm_Jack";
            jack.transform.SetParent(ioRoot.transform, false);
            jack.transform.localPosition = new Vector3(0.065f, 0f, 0f);
            jack.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            jack.transform.localScale = new Vector3(0.008f, 0.004f, 0.008f);
            jack.GetComponent<Renderer>().sharedMaterial = matChrome;
            DestroyImmediate(jack.GetComponent<Collider>());

            // Storage Drive Activity LED
            GameObject driveLed = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            driveLed.name = "Drive_Activity_LED";
            driveLed.transform.SetParent(ioRoot.transform, false);
            driveLed.transform.localPosition = new Vector3(0.088f, 0f, 0f);
            driveLed.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            driveLed.transform.localScale = new Vector3(0.004f, 0.003f, 0.004f);
            driveLed.GetComponent<Renderer>().sharedMaterial = matCyan;
            _driveLedRenderer = driveLed.GetComponent<Renderer>();
            DestroyImmediate(driveLed.GetComponent<Collider>());
        }

        private void BuildAIOCooler(Transform parent, Material matPC, Material matChrome, Material matCyan)
        {
            GameObject aioRoot = new GameObject("AIO_Cooler_280mm");
            aioRoot.transform.SetParent(parent, false);

            // CPU Pump Block (Cylinder facing tempered glass on -X)
            GameObject pumpBlock = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pumpBlock.name = "Pump_Block";
            pumpBlock.transform.SetParent(aioRoot.transform, false);
            pumpBlock.transform.localPosition = new Vector3(0.052f, 0.045f, 0.045f);
            pumpBlock.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            pumpBlock.transform.localScale = new Vector3(0.075f, 0.028f, 0.075f);
            pumpBlock.GetComponent<Renderer>().sharedMaterial = matPC;
            DestroyImmediate(pumpBlock.GetComponent<Collider>());

            // Infinity Mirror ARGB Ring
            GameObject aioRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            aioRing.name = "Infinity_Mirror_GlowRing";
            aioRing.transform.SetParent(pumpBlock.transform, false);
            aioRing.transform.localPosition = new Vector3(0f, -0.52f, 0f);
            aioRing.transform.localScale = new Vector3(0.92f, 0.06f, 0.92f);
            aioRing.GetComponent<Renderer>().sharedMaterial = matCyan;
            RegisterRgbRenderer(aioRing.GetComponent<Renderer>());
            DestroyImmediate(aioRing.GetComponent<Collider>());

            // OLED Mini Display Screen for Simulated Telemetry
            GameObject screenObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            screenObj.name = "Pump_OLED_Screen";
            screenObj.transform.SetParent(pumpBlock.transform, false);
            screenObj.transform.localPosition = new Vector3(0f, -0.55f, 0f);
            screenObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            screenObj.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
            screenObj.GetComponent<Renderer>().sharedMaterial = matPC;
            _pumpScreenRenderer = screenObj.GetComponent<Renderer>();
            DestroyImmediate(screenObj.GetComponent<Collider>());

            // TextMeshPro on Pump Screen
            GameObject textObj = new GameObject("SimulatedTelemetryText");
            textObj.transform.SetParent(screenObj.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.01f);
            textObj.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            _telemetryText = textObj.AddComponent<TextMeshPro>();
            _telemetryText.fontSize = 0.65f;
            _telemetryText.alignment = TextAlignmentOptions.Center;
            _telemetryText.rectTransform.sizeDelta = new Vector2(1.2f, 1.2f);
            _telemetryText.color = Color.white;

            // Braided Coolant Hoses (Routing to top radiator)
            for (int t = 0; t < 2; t++)
            {
                GameObject tube = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tube.name = $"Coolant_Hose_{t}";
                tube.transform.SetParent(aioRoot.transform, false);
                tube.transform.localPosition = new Vector3(0.040f, 0.14f, 0.035f + (t * 0.032f));
                tube.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
                tube.transform.localScale = new Vector3(0.012f, 0.10f, 0.012f);
                tube.GetComponent<Renderer>().sharedMaterial = matPC;
                DestroyImmediate(tube.GetComponent<Collider>());
            }

            // Top Radiator Core
            CreateCube("Radiator_Core_280mm", aioRoot.transform,
                       new Vector3(0f, 0.225f, 0.02f),
                       new Vector3(0.14f, 0.022f, 0.32f), matPC);
        }

        private void BuildHighEndGPU(Transform parent, Material matPC, Material matChrome, Material matMagenta)
        {
            GameObject gpuRoot = new GameObject("GPU_RTX_Workstation");
            gpuRoot.transform.SetParent(parent, false);

            // Heavy Triple-Slot GPU Body (x = 0.02f, y = -0.045f, z = 0.04f)
            GameObject gpuBody = CreateCube("GPU_Chassis", gpuRoot.transform,
                                           new Vector3(0.02f, -0.045f, 0.04f),
                                           new Vector3(0.13f, 0.055f, 0.29f), matPC);

            // Brushed Metal Backplate on top of GPU with geometric heat cutout
            CreateCube("GPU_Metal_Backplate", gpuBody.transform,
                       new Vector3(0f, 0.52f, 0f),
                       new Vector3(0.98f, 0.06f, 0.98f), matChrome);

            // Side ARGB Illuminated Logo strip facing tempered glass (-X)
            GameObject gpuLogo = CreateCube("GPU_ARGB_SideStrip", gpuBody.transform,
                                            new Vector3(-0.52f, 0f, 0f),
                                            new Vector3(0.04f, 0.40f, 0.90f), matMagenta);
            RegisterRgbRenderer(gpuLogo.GetComponent<Renderer>());

            // 16-Pin 12VHPWR Power Cable with Cable Comb
            GameObject pwrCable = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pwrCable.name = "GPU_12VHPWR_PowerCable";
            pwrCable.transform.SetParent(gpuRoot.transform, false);
            pwrCable.transform.localPosition = new Vector3(-0.045f, -0.075f, 0.04f);
            pwrCable.transform.localRotation = Quaternion.Euler(0f, 0f, 25f);
            pwrCable.transform.localScale = new Vector3(0.012f, 0.035f, 0.012f);
            pwrCable.GetComponent<Renderer>().sharedMaterial = matPC;
            DestroyImmediate(pwrCable.GetComponent<Collider>());

            // Aluminum Anti-Sag Bracket (Mounted to PSU shroud at GPU corner)
            GameObject bracket = CreateCube("Anti_Sag_Support_Bracket", gpuRoot.transform,
                                            new Vector3(-0.045f, -0.09f, -0.09f),
                                            new Vector3(0.012f, 0.065f, 0.016f), matChrome);
        }

        private void BuildCaseFans(Transform parent, Material matPC, Material matChrome,
                                   Material matCyan, Material matViolet, Material matMagenta)
        {
            GameObject fansRoot = new GameObject("CaseFans");
            fansRoot.transform.SetParent(parent, false);

            // 1. Rear 120mm Exhaust Fan (z = +0.21f, y = +0.10f)
            BuildFanUnit("Rear_Exhaust_Fan_120mm", fansRoot.transform,
                         new Vector3(0.025f, 0.10f, 0.212f),
                         Quaternion.Euler(90f, 0f, 0f),
                         0.11f, matPC, matCyan);

            // 2. Dual Top 140mm Radiator Fans (y = +0.218f, z = -0.05f & +0.09f)
            for (int f = 0; f < 2; f++)
            {
                Material m = (f == 0) ? matCyan : matViolet;
                BuildFanUnit($"Top_Radiator_Fan_{f}", fansRoot.transform,
                             new Vector3(0f, 0.218f, -0.05f + (f * 0.14f)),
                             Quaternion.identity,
                             0.115f, matPC, m);
            }

            // 3. Triple 120mm Front Intake Fans (z = -0.215f, y = +0.12f, 0f, -0.12f)
            float[] frontY = new float[] { 0.13f, 0.01f, -0.11f };
            for (int f = 0; f < 3; f++)
            {
                Material m = (f == 0) ? matCyan : (f == 1 ? matMagenta : matViolet);
                BuildFanUnit($"Front_Intake_Fan_{f}", fansRoot.transform,
                             new Vector3(0f, frontY[f], -0.215f),
                             Quaternion.Euler(90f, 0f, 0f),
                             0.11f, matPC, m);
            }
        }

        private void BuildFanUnit(string name, Transform parent, Vector3 localPos, Quaternion localRot,
                                  float diameter, Material matCase, Material matGlow)
        {
            GameObject fanRoot = new GameObject(name);
            fanRoot.transform.SetParent(parent, false);
            fanRoot.transform.localPosition = localPos;
            fanRoot.transform.localRotation = localRot;

            // Outer Square Fan Frame
            GameObject frame = CreateCube("Fan_Housing_Frame", fanRoot.transform,
                                          Vector3.zero,
                                          new Vector3(diameter, 0.008f, diameter), matCase);

            // Glowing ARGB Diffuser Ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "ARGB_Diffuser_Halo";
            ring.transform.SetParent(fanRoot.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(diameter * 0.95f, 0.009f, diameter * 0.95f);
            ring.GetComponent<Renderer>().sharedMaterial = matGlow;
            RegisterRgbRenderer(ring.GetComponent<Renderer>());
            DestroyImmediate(ring.GetComponent<Collider>());

            // Central Rotating Blade Hub
            GameObject hub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hub.name = "Rotating_Blade_Hub";
            hub.transform.SetParent(fanRoot.transform, false);
            hub.transform.localPosition = Vector3.zero;
            hub.transform.localScale = new Vector3(diameter * 0.35f, 0.011f, diameter * 0.35f);
            hub.GetComponent<Renderer>().sharedMaterial = matCase;
            DestroyImmediate(hub.GetComponent<Collider>());

            // Translucent Fan Blades (5 blades arranged radially)
            for (int b = 0; b < 5; b++)
            {
                float angle = b * (360f / 5f);
                GameObject blade = GameObject.CreatePrimitive(PrimitiveType.Cube);
                blade.name = $"Blade_{b}";
                blade.transform.SetParent(hub.transform, false);
                blade.transform.localRotation = Quaternion.Euler(0f, angle, 25f);
                blade.transform.localPosition = blade.transform.localRotation * new Vector3(0.55f, 0f, 0f);
                blade.transform.localScale = new Vector3(0.55f, 0.15f, 0.22f);
                blade.GetComponent<Renderer>().sharedMaterial = matGlow;
                DestroyImmediate(blade.GetComponent<Collider>());
            }

            // Register this fan blade hub to animate continuous rotation
            RegisterFanBlade(hub.transform);
        }

        private static GameObject CreateCube(string name, Transform parent, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = localPos;
            obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = mat;
            DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }

        private static Material CreateMaterial(string name, Color color, float smoothness, float metallic)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(litShader) { name = name };
            mat.color = color;
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Metallic", metallic);
            return mat;
        }

        private static Material CreateEmissionMaterial(string name, Color color, float intensity)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(litShader) { name = name };
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            return mat;
        }

        private static Material CreateGlassMaterial(string name, Color color)
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material mat = new Material(litShader) { name = name };
            mat.SetFloat("_Surface", 1); // Transparent
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.color = color;
            mat.SetFloat("_Smoothness", 0.95f);
            mat.SetFloat("_Metallic", 0.1f);
            return mat;
        }
    }
}
