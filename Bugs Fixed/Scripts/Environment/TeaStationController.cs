using System.Collections;
using UnityEngine;
using TMPro;
using VRRemoteDesktop.Audio;

namespace VRRemoteDesktop.Environment
{
    /// <summary>
    /// Interactive Ceremonial Zen Tea Station.
    /// Features a Japanese ceramic teapot, warm jade steeping glow, rising aromatic steam,
    /// animated tea cup delivery, and triggers the 30-second Zen Mode via DrinkEffectManager.
    /// </summary>
    public class TeaStationController : InteractiveProp
    {
        [Header("Station References")]
        [SerializeField] private Renderer _kettleRenderer;
        [SerializeField] private GameObject _spawnedCup;
        [SerializeField] private Light _zenLight;
        [SerializeField] private TextMeshProUGUI _screenStatusText;
        [SerializeField] private ParticleSystem _steamParticleSystem;

        private static readonly string[] TeaDiagnoses = new string[]
        {
            "<color=#55ff99><b>Ceremonial Gyokuro infused.</b></color>\n<size=15>Inner serenity restored. Extraction: 80°C.</size>",
            "<color=#55ff99><b>Organic Matcha extracted.</b></color>\n<size=15>Thermal equilibrium achieved. Antioxidants: 100%.</size>",
            "<color=#55ff99><b>Steeped to perfection.</b></color>\n<size=15>Transcending earthly distractions.</size>",
            "<color=#55ff99><b>Kyoto Sencha ready.</b></color>\n<size=15>Breathe in serenity, exhale latency.</size>",
            "<color=#55ff99><b>Zen elixir consumed.</b></color>\n<size=15>0% caffeine. 100% inner tranquility.</size>"
        };

        private enum TeaState
        {
            Standby,
            Steeping,
            ReadyToDrink
        }

        private TeaState _state = TeaState.Standby;
        private int _steepCount = 0;
        private MaterialPropertyBlock _kettlePropBlock;
        private Coroutine _steamRoutine;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoEnsureTeaStation()
        {
            if (FindAnyObjectByType<TeaStationController>() == null)
            {
                var coffee = FindAnyObjectByType<CoffeeMachineController>();
                Transform parent = coffee != null ? coffee.transform.parent : null;
                Vector3 teaPos = coffee != null ? coffee.transform.position + new Vector3(-1.20f, 0f, 0f) : new Vector3(3.70f, 0.85f, -4.30f);
                Quaternion teaRot = coffee != null ? coffee.transform.rotation : Quaternion.Euler(0f, 180f, 0f);

                BuildRuntimeTeaStation(parent, teaPos, teaRot);
            }
        }

        public static TeaStationController BuildRuntimeTeaStation(Transform parent, Vector3 pos, Quaternion rot)
        {
            GameObject teaObj = new GameObject("TeaStation");
            if (parent != null) teaObj.transform.SetParent(parent, false);
            teaObj.transform.position = pos;
            teaObj.transform.rotation = rot;

            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // Authentic Materials
            Material matCoffee = CreatePropMaterial(litShader, "Mat_TeaCounterWood", new Color(0.12f, 0.08f, 0.05f), 0.5f);
            Material matCastIron = CreatePropMaterial(litShader, "Mat_TetsubinCastIron", new Color(0.12f, 0.12f, 0.13f), 0.22f, 0.40f);
            Material matBrass = CreatePropMaterial(litShader, "Mat_TeaBrass", new Color(0.78f, 0.62f, 0.28f), 0.75f, 0.85f);
            Material matRosewood = CreatePropMaterial(litShader, "Mat_ChaPanRosewood", new Color(0.18f, 0.11f, 0.07f), 0.55f);
            Material matBamboo = CreatePropMaterial(litShader, "Mat_TeaBamboo", new Color(0.76f, 0.64f, 0.42f), 0.38f);
            Material matPorcelain = CreatePropMaterial(litShader, "Mat_YunomiPorcelain", new Color(0.94f, 0.93f, 0.89f), 0.88f, 0.02f);
            Material matPorcelainRim = CreatePropMaterial(litShader, "Mat_PorcelainRim", new Color(0.20f, 0.16f, 0.14f), 0.70f);
            Material matMatcha = CreatePropMaterial(litShader, "Mat_CeremonialMatcha", new Color(0.18f, 0.50f, 0.16f), 0.95f, 0.05f);
            Material matBlackLacquer = CreatePropMaterial(litShader, "Mat_NatsumeLacquer", new Color(0.04f, 0.04f, 0.05f), 0.96f, 0.15f);
            Material matBurnerEmber = CreatePropMaterial(litShader, "Mat_BurnerEmber", new Color(1.0f, 0.45f, 0.08f), 0.4f, 0f, new Color(1.0f, 0.45f, 0.08f) * 2.5f);

            // Counter Side Table matching beverage counter
            GameObject counter = GameObject.CreatePrimitive(PrimitiveType.Cube);
            counter.name = "Counter";
            counter.transform.SetParent(teaObj.transform, false);
            counter.transform.localPosition = new Vector3(0f, -0.42f, 0f);
            counter.transform.localScale = new Vector3(1.2f, 0.84f, 0.70f);
            counter.GetComponent<Renderer>().sharedMaterial = matCoffee;

            // 1. Ceremonial Cha Pan (Tea Serving Tray) with Rosewood Rim and Bamboo Slat Deck
            GameObject tray = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tray.name = "BambooTeaTray";
            tray.transform.SetParent(teaObj.transform, false);
            tray.transform.localPosition = new Vector3(0f, 0.02f, -0.05f);
            tray.transform.localScale = new Vector3(0.72f, 0.04f, 0.50f);
            tray.GetComponent<Renderer>().sharedMaterial = matRosewood;
            Destroy(tray.GetComponent<Collider>());

            // Inner Recessed Bamboo Deck
            GameObject trayDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            trayDeck.name = "TrayDeck";
            trayDeck.transform.SetParent(tray.transform, false);
            trayDeck.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            trayDeck.transform.localScale = new Vector3(0.94f, 0.15f, 0.92f);
            trayDeck.GetComponent<Renderer>().sharedMaterial = matBamboo;
            Destroy(trayDeck.GetComponent<Collider>());

            // Bamboo Drainage Slats
            for (int s = -2; s <= 2; s++)
            {
                GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slat.name = $"DrainSlat_{s}";
                slat.transform.SetParent(trayDeck.transform, false);
                slat.transform.localPosition = new Vector3(s * 0.18f, 0.52f, 0f);
                slat.transform.localScale = new Vector3(0.015f, 0.08f, 0.90f);
                slat.GetComponent<Renderer>().sharedMaterial = matRosewood;
                Destroy(slat.GetComponent<Collider>());
            }

            // 2. Cast Iron Tetsubin Warmer Trivet with Tea-Light Candle Ember
            GameObject trivet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trivet.name = "TetsubinTrivet";
            trivet.transform.SetParent(teaObj.transform, false);
            trivet.transform.localPosition = new Vector3(0.14f, 0.048f, -0.05f);
            trivet.transform.localScale = new Vector3(0.24f, 0.025f, 0.24f);
            trivet.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(trivet.GetComponent<Collider>());

            GameObject candleDish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            candleDish.name = "CandleDish";
            candleDish.transform.SetParent(trivet.transform, false);
            candleDish.transform.localPosition = new Vector3(0f, 0.35f, 0f);
            candleDish.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            candleDish.GetComponent<Renderer>().sharedMaterial = matPorcelain;
            Destroy(candleDish.GetComponent<Collider>());

            GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            flame.name = "CandleFlame";
            flame.transform.SetParent(trivet.transform, false);
            flame.transform.localPosition = new Vector3(0f, 0.72f, 0f);
            flame.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
            flame.GetComponent<Renderer>().sharedMaterial = matBurnerEmber;
            Destroy(flame.GetComponent<Collider>());

            // 3. Authentic Cast Iron Tetsubin Teapot
            GameObject teapot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teapot.name = "ZenTeapot";
            teapot.transform.SetParent(teaObj.transform, false);
            teapot.transform.localPosition = new Vector3(0.14f, 0.14f, -0.05f);
            teapot.transform.localScale = new Vector3(0.24f, 0.13f, 0.24f);
            teapot.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(teapot.GetComponent<Collider>());

            // Teapot Neck Rim
            GameObject neck = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neck.name = "TeapotNeck";
            neck.transform.SetParent(teapot.transform, false);
            neck.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            neck.transform.localScale = new Vector3(0.72f, 0.18f, 0.72f);
            neck.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(neck.GetComponent<Collider>());

            // Teapot Lid
            GameObject lid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lid.name = "TeapotLid";
            lid.transform.SetParent(teapot.transform, false);
            lid.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            lid.transform.localScale = new Vector3(0.65f, 0.14f, 0.65f);
            lid.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(lid.GetComponent<Collider>());

            // Brass Turned Knob on Lid
            GameObject lidKnob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            lidKnob.name = "TeapotLidKnob";
            lidKnob.transform.SetParent(teapot.transform, false);
            lidKnob.transform.localPosition = new Vector3(0f, 0.85f, 0f);
            lidKnob.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
            lidKnob.GetComponent<Renderer>().sharedMaterial = matBrass;
            Destroy(lidKnob.GetComponent<Collider>());

            // Teapot Spout pointing towards cup
            GameObject spout = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spout.name = "TeapotSpout";
            spout.transform.SetParent(teapot.transform, false);
            spout.transform.localPosition = new Vector3(-0.52f, 0.18f, -0.38f);
            spout.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);
            spout.transform.localScale = new Vector3(0.20f, 0.42f, 0.20f);
            spout.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(spout.GetComponent<Collider>());

            // Brass Spout Collar Ring
            GameObject spoutCollar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            spoutCollar.name = "SpoutCollar";
            spoutCollar.transform.SetParent(teapot.transform, false);
            spoutCollar.transform.localPosition = new Vector3(-0.28f, 0.02f, -0.20f);
            spoutCollar.transform.localRotation = Quaternion.Euler(42f, -28f, 0f);
            spoutCollar.transform.localScale = new Vector3(0.22f, 0.08f, 0.22f);
            spoutCollar.GetComponent<Renderer>().sharedMaterial = matBrass;
            Destroy(spoutCollar.GetComponent<Collider>());

            // Traditional Arched Overhead Handle
            GameObject handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handle.name = "TeapotHandle";
            handle.transform.SetParent(teapot.transform, false);
            handle.transform.localPosition = new Vector3(0f, 0.84f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            handle.transform.localScale = new Vector3(0.05f, 0.95f, 0.05f);
            handle.GetComponent<Renderer>().sharedMaterial = matCastIron;
            Destroy(handle.GetComponent<Collider>());

            // Bamboo/Rattan Wrapped Center Grip on Handle
            GameObject handleGrip = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            handleGrip.name = "HandleBambooGrip";
            handleGrip.transform.SetParent(handle.transform, false);
            handleGrip.transform.localPosition = Vector3.zero;
            handleGrip.transform.localScale = new Vector3(1.7f, 0.52f, 1.7f);
            handleGrip.GetComponent<Renderer>().sharedMaterial = matBamboo;
            Destroy(handleGrip.GetComponent<Collider>());

            // 4. Glazed Ivory Porcelain Yunomi Tea Cup
            GameObject cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = "SpawnedTeaCup";
            cup.transform.SetParent(teaObj.transform, false);
            cup.transform.localPosition = new Vector3(-0.16f, 0.085f, -0.08f);
            cup.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            cup.GetComponent<Renderer>().sharedMaterial = matPorcelain;
            Destroy(cup.GetComponent<Collider>());

            // Dark Accent Rim
            GameObject cupRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cupRim.name = "CupRim";
            cupRim.transform.SetParent(cup.transform, false);
            cupRim.transform.localPosition = new Vector3(0f, 0.49f, 0f);
            cupRim.transform.localScale = new Vector3(1.02f, 0.04f, 1.02f);
            cupRim.GetComponent<Renderer>().sharedMaterial = matPorcelainRim;
            Destroy(cupRim.GetComponent<Collider>());

            // Rich Matcha Liquid Surface inside cup
            GameObject teaLiquid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            teaLiquid.name = "MatchaLiquidSurface";
            teaLiquid.transform.SetParent(cup.transform, false);
            teaLiquid.transform.localPosition = new Vector3(0f, 0.38f, 0f);
            teaLiquid.transform.localScale = new Vector3(0.92f, 0.06f, 0.92f);
            teaLiquid.GetComponent<Renderer>().sharedMaterial = matMatcha;
            Destroy(teaLiquid.GetComponent<Collider>());

            // Companion Guest Yunomi Cup resting on tray
            GameObject guestCup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            guestCup.name = "GuestTeaCup";
            guestCup.transform.SetParent(teaObj.transform, false);
            guestCup.transform.localPosition = new Vector3(-0.24f, 0.085f, 0.05f);
            guestCup.transform.localScale = new Vector3(0.09f, 0.09f, 0.09f);
            guestCup.GetComponent<Renderer>().sharedMaterial = matPorcelain;
            Destroy(guestCup.GetComponent<Collider>());

            GameObject guestRim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            guestRim.name = "GuestRim";
            guestRim.transform.SetParent(guestCup.transform, false);
            guestRim.transform.localPosition = new Vector3(0f, 0.49f, 0f);
            guestRim.transform.localScale = new Vector3(1.02f, 0.04f, 1.02f);
            guestRim.GetComponent<Renderer>().sharedMaterial = matPorcelainRim;
            Destroy(guestRim.GetComponent<Collider>());

            // 5. Ceremonial Utensils: Chasen (Bamboo Whisk), Natsume (Tea Caddy), Chashaku (Scoop)
            // Bamboo Whisk in Porcelain Whisk Keeper (Kusenaoshi)
            GameObject whiskStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whiskStand.name = "WhiskStand";
            whiskStand.transform.SetParent(teaObj.transform, false);
            whiskStand.transform.localPosition = new Vector3(-0.06f, 0.065f, 0.12f);
            whiskStand.transform.localScale = new Vector3(0.065f, 0.05f, 0.065f);
            whiskStand.GetComponent<Renderer>().sharedMaterial = matPorcelain;
            Destroy(whiskStand.GetComponent<Collider>());

            GameObject whiskProngs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whiskProngs.name = "WhiskProngs";
            whiskProngs.transform.SetParent(whiskStand.transform, false);
            whiskProngs.transform.localPosition = new Vector3(0f, 0.65f, 0f);
            whiskProngs.transform.localScale = new Vector3(0.72f, 0.85f, 0.72f);
            whiskProngs.GetComponent<Renderer>().sharedMaterial = matBamboo;
            Destroy(whiskProngs.GetComponent<Collider>());

            GameObject whiskHandle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            whiskHandle.name = "WhiskHandle";
            whiskHandle.transform.SetParent(whiskStand.transform, false);
            whiskHandle.transform.localPosition = new Vector3(0f, 1.25f, 0f);
            whiskHandle.transform.localScale = new Vector3(0.32f, 0.55f, 0.32f);
            whiskHandle.GetComponent<Renderer>().sharedMaterial = matBamboo;
            Destroy(whiskHandle.GetComponent<Collider>());

            // Black Lacquer Natsume (Matcha Caddy)
            GameObject caddy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            caddy.name = "TeaCaddy_Natsume";
            caddy.transform.SetParent(teaObj.transform, false);
            caddy.transform.localPosition = new Vector3(0.06f, 0.082f, 0.12f);
            caddy.transform.localScale = new Vector3(0.085f, 0.085f, 0.085f);
            caddy.GetComponent<Renderer>().sharedMaterial = matBlackLacquer;
            Destroy(caddy.GetComponent<Collider>());

            GameObject caddyLid = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            caddyLid.name = "CaddyLid";
            caddyLid.transform.SetParent(caddy.transform, false);
            caddyLid.transform.localPosition = new Vector3(0f, 0.52f, 0f);
            caddyLid.transform.localScale = new Vector3(1.03f, 0.20f, 1.03f);
            caddyLid.GetComponent<Renderer>().sharedMaterial = matBlackLacquer;
            Destroy(caddyLid.GetComponent<Collider>());

            GameObject caddyGold = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            caddyGold.name = "CaddyGoldCrest";
            caddyGold.transform.SetParent(caddy.transform, false);
            caddyGold.transform.localPosition = new Vector3(0f, 0.66f, 0f);
            caddyGold.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
            caddyGold.GetComponent<Renderer>().sharedMaterial = matBrass;
            Destroy(caddyGold.GetComponent<Collider>());

            // Bamboo Tea Scoop (Chashaku)
            GameObject scoop = GameObject.CreatePrimitive(PrimitiveType.Cube);
            scoop.name = "BambooTeaScoop";
            scoop.transform.SetParent(teaObj.transform, false);
            scoop.transform.localPosition = new Vector3(0.18f, 0.048f, 0.12f);
            scoop.transform.localRotation = Quaternion.Euler(0f, 22f, 0f);
            scoop.transform.localScale = new Vector3(0.018f, 0.008f, 0.14f);
            scoop.GetComponent<Renderer>().sharedMaterial = matBamboo;
            Destroy(scoop.GetComponent<Collider>());

            // 6. Modern Zen Infuser Console / Display
            GameObject screenBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
            screenBase.name = "ZenScreenBase";
            screenBase.transform.SetParent(teaObj.transform, false);
            screenBase.transform.localPosition = new Vector3(0f, 0.22f, 0.18f);
            screenBase.transform.localRotation = Quaternion.Euler(-20f, 0f, 0f);
            screenBase.transform.localScale = new Vector3(0.38f, 0.18f, 0.04f);
            screenBase.GetComponent<Renderer>().sharedMaterial = matCoffee;
            Destroy(screenBase.GetComponent<Collider>());

            // Status Text on Screen
            GameObject textObj = new GameObject("ScreenStatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(screenBase.transform, false);
            textObj.transform.localPosition = new Vector3(0f, 0f, -0.52f);
            textObj.transform.localRotation = Quaternion.identity;
            RectTransform srt = textObj.GetComponent<RectTransform>();
            srt.sizeDelta = new Vector2(340f, 160f);
            srt.localScale = Vector3.one * 0.001f;
            var stmp = textObj.GetComponent<TextMeshProUGUI>();
            stmp.text = "<color=#44ff88><b>ZEN TEA INFUSER // CEREMONIAL</b></color>\n<size=15>GYOKURO IMPERIAL GRADE | 80°C\nSTATUS: TRANQUIL EQUILIBRIUM</size>";
            stmp.fontSize = 20;
            stmp.alignment = TextAlignmentOptions.Center;
            stmp.color = Color.white;

            // Zen Jade Light
            GameObject lightObj = new GameObject("ZenLight");
            lightObj.transform.SetParent(teaObj.transform, false);
            lightObj.transform.localPosition = new Vector3(0f, 0.25f, -0.05f);
            Light zenLight = lightObj.AddComponent<Light>();
            zenLight.type = LightType.Point;
            zenLight.range = 1.0f;
            zenLight.intensity = 1.8f;
            zenLight.color = new Color(0.25f, 0.95f, 0.50f);
            zenLight.enabled = false;

            // Box Collider for Interaction
            var col = teaObj.AddComponent<BoxCollider>();
            col.size = new Vector3(0.85f, 0.85f, 0.75f);
            col.center = new Vector3(0f, 0.25f, 0f);

            var comp = teaObj.AddComponent<TeaStationController>();
            comp._kettleRenderer = teapot.GetComponent<Renderer>();
            comp._spawnedCup = cup;
            comp._zenLight = zenLight;
            comp._screenStatusText = stmp;

            return comp;
        }

        private static Material CreatePropMaterial(Shader shader, string name, Color color, float smoothness = 0.5f, float metallic = 0f, Color? emission = null)
        {
            Material m = new Material(shader) { name = name };
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", metallic);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        public void UpgradeVisualsRealism()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            Material matCastIron = CreatePropMaterial(litShader, "Mat_TetsubinCastIron", new Color(0.12f, 0.12f, 0.13f), 0.22f, 0.40f);
            Material matBrass = CreatePropMaterial(litShader, "Mat_TeaBrass", new Color(0.78f, 0.62f, 0.28f), 0.75f, 0.85f);
            Material matRosewood = CreatePropMaterial(litShader, "Mat_ChaPanRosewood", new Color(0.18f, 0.11f, 0.07f), 0.55f);
            Material matBamboo = CreatePropMaterial(litShader, "Mat_TeaBamboo", new Color(0.76f, 0.64f, 0.42f), 0.38f);
            Material matPorcelain = CreatePropMaterial(litShader, "Mat_YunomiPorcelain", new Color(0.94f, 0.93f, 0.89f), 0.88f, 0.02f);
            Material matPorcelainRim = CreatePropMaterial(litShader, "Mat_PorcelainRim", new Color(0.20f, 0.16f, 0.14f), 0.70f);
            Material matMatcha = CreatePropMaterial(litShader, "Mat_CeremonialMatcha", new Color(0.18f, 0.50f, 0.16f), 0.95f, 0.05f);
            Material matBlackLacquer = CreatePropMaterial(litShader, "Mat_NatsumeLacquer", new Color(0.04f, 0.04f, 0.05f), 0.96f, 0.15f);
            Material matBurnerEmber = CreatePropMaterial(litShader, "Mat_BurnerEmber", new Color(1.0f, 0.45f, 0.08f), 0.4f, 0f, new Color(1.0f, 0.45f, 0.08f) * 2.5f);

            // Update Teapot to Cast Iron
            var teapotTrans = transform.Find("ZenTeapot");
            if (teapotTrans != null)
            {
                var r = teapotTrans.GetComponent<Renderer>();
                if (r != null) r.sharedMaterial = matCastIron;
                _kettleRenderer = r;

                var lid = teapotTrans.Find("TeapotLid");
                if (lid != null)
                {
                    var lr = lid.GetComponent<Renderer>();
                    if (lr != null) lr.sharedMaterial = matCastIron;

                    if (teapotTrans.Find("TeapotLidKnob") == null)
                    {
                        GameObject knob = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        knob.name = "TeapotLidKnob";
                        knob.transform.SetParent(teapotTrans, false);
                        knob.transform.localPosition = new Vector3(0f, 0.85f, 0f);
                        knob.transform.localScale = new Vector3(0.14f, 0.28f, 0.14f);
                        knob.GetComponent<Renderer>().sharedMaterial = matBrass;
                        Destroy(knob.GetComponent<Collider>());
                    }
                }

                var spout = teapotTrans.Find("TeapotSpout");
                if (spout != null)
                {
                    var sr = spout.GetComponent<Renderer>();
                    if (sr != null) sr.sharedMaterial = matCastIron;
                }

                var handle = teapotTrans.Find("TeapotHandle");
                if (handle != null)
                {
                    var hr = handle.GetComponent<Renderer>();
                    if (hr != null) hr.sharedMaterial = matCastIron;
                }
            }

            // Update Tray to Dark Rosewood and add Bamboo Deck
            var trayTrans = transform.Find("BambooTeaTray");
            if (trayTrans != null)
            {
                var tr = trayTrans.GetComponent<Renderer>();
                if (tr != null) tr.sharedMaterial = matRosewood;

                if (trayTrans.Find("TrayDeck") == null)
                {
                    GameObject trayDeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    trayDeck.name = "TrayDeck";
                    trayDeck.transform.SetParent(trayTrans, false);
                    trayDeck.transform.localPosition = new Vector3(0f, 0.52f, 0f);
                    trayDeck.transform.localScale = new Vector3(0.94f, 0.15f, 0.92f);
                    trayDeck.GetComponent<Renderer>().sharedMaterial = matBamboo;
                    Destroy(trayDeck.GetComponent<Collider>());

                    for (int s = -2; s <= 2; s++)
                    {
                        GameObject slat = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        slat.name = $"DrainSlat_{s}";
                        slat.transform.SetParent(trayDeck.transform, false);
                        slat.transform.localPosition = new Vector3(s * 0.18f, 0.52f, 0f);
                        slat.transform.localScale = new Vector3(0.015f, 0.08f, 0.90f);
                        slat.GetComponent<Renderer>().sharedMaterial = matRosewood;
                        Destroy(slat.GetComponent<Collider>());
                    }
                }
            }

            // Update Cup to Ivory Porcelain
            var cupTrans = transform.Find("SpawnedTeaCup");
            if (cupTrans != null)
            {
                var cr = cupTrans.GetComponent<Renderer>();
                if (cr != null) cr.sharedMaterial = matPorcelain;

                var liquid = cupTrans.Find("MatchaLiquidSurface");
                if (liquid != null)
                {
                    var lr = liquid.GetComponent<Renderer>();
                    if (lr != null) lr.sharedMaterial = matMatcha;
                }
            }

            // Ensure Trivet Burner
            if (transform.Find("TetsubinTrivet") == null)
            {
                GameObject trivet = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                trivet.name = "TetsubinTrivet";
                trivet.transform.SetParent(transform, false);
                trivet.transform.localPosition = new Vector3(0.14f, 0.048f, -0.05f);
                trivet.transform.localScale = new Vector3(0.24f, 0.025f, 0.24f);
                trivet.GetComponent<Renderer>().sharedMaterial = matCastIron;
                Destroy(trivet.GetComponent<Collider>());

                GameObject flame = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                flame.name = "CandleFlame";
                flame.transform.SetParent(trivet.transform, false);
                flame.transform.localPosition = new Vector3(0f, 0.72f, 0f);
                flame.transform.localScale = new Vector3(0.12f, 0.35f, 0.12f);
                flame.GetComponent<Renderer>().sharedMaterial = matBurnerEmber;
                Destroy(flame.GetComponent<Collider>());
            }

            // Ensure Ceremonial Utensils
            if (transform.Find("WhiskStand") == null)
            {
                GameObject whiskStand = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                whiskStand.name = "WhiskStand";
                whiskStand.transform.SetParent(transform, false);
                whiskStand.transform.localPosition = new Vector3(-0.06f, 0.065f, 0.12f);
                whiskStand.transform.localScale = new Vector3(0.065f, 0.05f, 0.065f);
                whiskStand.GetComponent<Renderer>().sharedMaterial = matPorcelain;
                Destroy(whiskStand.GetComponent<Collider>());

                GameObject whiskProngs = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                whiskProngs.name = "WhiskProngs";
                whiskProngs.transform.SetParent(whiskStand.transform, false);
                whiskProngs.transform.localPosition = new Vector3(0f, 0.65f, 0f);
                whiskProngs.transform.localScale = new Vector3(0.72f, 0.85f, 0.72f);
                whiskProngs.GetComponent<Renderer>().sharedMaterial = matBamboo;
                Destroy(whiskProngs.GetComponent<Collider>());
            }

            if (transform.Find("TeaCaddy_Natsume") == null)
            {
                GameObject caddy = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                caddy.name = "TeaCaddy_Natsume";
                caddy.transform.SetParent(transform, false);
                caddy.transform.localPosition = new Vector3(0.06f, 0.082f, 0.12f);
                caddy.transform.localScale = new Vector3(0.085f, 0.085f, 0.085f);
                caddy.GetComponent<Renderer>().sharedMaterial = matBlackLacquer;
                Destroy(caddy.GetComponent<Collider>());

                GameObject caddyGold = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                caddyGold.name = "CaddyGoldCrest";
                caddyGold.transform.SetParent(caddy.transform, false);
                caddyGold.transform.localPosition = new Vector3(0f, 0.66f, 0f);
                caddyGold.transform.localScale = new Vector3(0.24f, 0.08f, 0.24f);
                caddyGold.GetComponent<Renderer>().sharedMaterial = matBrass;
                Destroy(caddyGold.GetComponent<Collider>());
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _interactionRange = 2.2f;
            _promptOffset = new Vector3(0f, 0.35f, 0f);
            SetPromptAction("STEEP ZEN TEA");
            _kettlePropBlock = new MaterialPropertyBlock();
            UpgradeVisualsRealism();
            if (_spawnedCup != null) _spawnedCup.SetActive(false);
            if (_zenLight != null) _zenLight.enabled = false;
            CreateSteamSystemIfNeeded();
            UpdateScreenText("<color=#44ff88><b>ZEN TEA INFUSER // CEREMONIAL</b></color>\n<size=15>GYOKURO IMPERIAL GRADE | 80°C\nSTATUS: TRANQUIL EQUILIBRIUM</size>");
        }

        protected override void OnInteract()
        {
            if (_state == TeaState.Steeping) return;

            if (_state == TeaState.Standby)
            {
                StartCoroutine(SteepRoutine());
            }
            else if (_state == TeaState.ReadyToDrink)
            {
                DrinkTea();
            }
        }

        private IEnumerator SteepRoutine()
        {
            _state = TeaState.Steeping;
            _steepCount++;

            UpdateScreenText("INFUSING HERBALS...\nSTEEPING CEREMONIAL LEAVES");
            SetPromptAction("STEEPING...");

            // Turn on calm jade green light
            if (_zenLight != null)
            {
                _zenLight.enabled = true;
                _zenLight.color = new Color(0.25f, 0.95f, 0.45f);
            }

            // Start steam
            StartSteam(2.5f);

            // Audio: peaceful chime
            AudioManager.Instance?.PlayPromptTone();

            // Pulse teapot base glow for 1.8 seconds
            float elapsed = 0f;
            float duration = 1.8f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pulse = 0.5f + 0.5f * Mathf.Sin(elapsed * 10f);
                Color c = Color.Lerp(new Color(0.05f, 0.2f, 0.1f), new Color(0.2f, 1.0f, 0.4f), pulse);
                SetKettleGlow(c * 2.5f);
                yield return null;
            }

            // Deliver tea cup
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

            // Peaceful chime
            AudioManager.Instance?.PlayConnectChime();

            // Update status
            string diag = TeaDiagnoses[(_steepCount - 1) % TeaDiagnoses.Length];
            UpdateScreenText(diag);

            // Unlock Achievement
            AchievementManager.Instance?.UnlockAchievement("Way of the Leaf", "Steeped an authentic cup of virtual Zen tea.");

            // Switch to ReadyToDrink state
            _state = TeaState.ReadyToDrink;
            SetPromptAction("DRINK ZEN TEA");
        }

        private void DrinkTea()
        {
            // Play gentle drinking slurp audio
            AudioManager.Instance?.PlaySlurp();

            // Hide cup
            if (_spawnedCup != null)
            {
                _spawnedCup.SetActive(false);
            }

            StopSteam();

            if (_zenLight != null) _zenLight.enabled = false;
            SetKettleGlow(new Color(0.05f, 0.15f, 0.08f) * 1.5f);
            UpdateScreenText("ZEN TEA CONSUMED.\nSTATUS: TRANQUIL");
            SetPromptAction("STEEP ZEN TEA");
            _state = TeaState.Standby;

            // Trigger DrinkEffectManager
            DrinkEffectManager.Instance?.ConsumeTea();
        }

        private void CreateSteamSystemIfNeeded()
        {
            if (_steamParticleSystem != null) return;

            GameObject steamObj = new GameObject("TeaSteamSystem");
            steamObj.transform.SetParent(transform, false);
            steamObj.transform.localPosition = new Vector3(0f, 0.18f, -0.20f);

            _steamParticleSystem = steamObj.AddComponent<ParticleSystem>();
            var main = _steamParticleSystem.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 0.14f;
            main.startSize = 0.06f;
            main.startColor = new Color(0.95f, 1f, 0.95f, 0.30f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;

            var emission = _steamParticleSystem.emission;
            emission.rateOverTime = 14f;
            emission.enabled = false;

            var shape = _steamParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.02f;

            var colorOverLifetime = _steamParticleSystem.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(new Color(0.9f, 1f, 0.9f), 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.0f, 0f), new GradientAlphaKey(0.35f, 0.25f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            var sizeOverLifetime = _steamParticleSystem.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 0.4f);
            curve.AddKey(1f, 1.9f);
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

        private void SetKettleGlow(Color color)
        {
            if (_kettleRenderer == null) return;
            _kettleRenderer.GetPropertyBlock(_kettlePropBlock);
            _kettlePropBlock.SetColor("_EmissionColor", color);
            _kettleRenderer.SetPropertyBlock(_kettlePropBlock);
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
