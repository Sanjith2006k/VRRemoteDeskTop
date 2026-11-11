using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VRRemoteDesktop.Computer;

namespace VRRemoteDesktop.Player
{
    /// <summary>
    /// Realistic Human Avatar with natural skin tones, hair, facial features,
    /// stylish casual developer clothing (tech hoodie, denim jeans, sneakers),
    /// dynamic walking locomotion animation, and seamless seated synchronization
    /// with the workstation gaming chair.
    /// 
    /// All skeletal bone pivots maintain uniform scale (1, 1, 1) so limbs,
    /// hands, fingers, and head features never suffer from parent scale distortion.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAvatarBody : MonoBehaviour
    {
        public static PlayerAvatarBody Instance { get; private set; }

        private PlayerController _playerController;
        private CharacterController _characterController;

        private GameObject _avatarRoot;
        private Transform _hips;
        private Transform _spine;
        private Transform _neck;
        private Transform _head;
        private Transform _hair;

        // Limbs (Clean unscaled bone pivots)
        private Transform _leftUpperArm;
        private Transform _leftForearm;
        private Transform _leftHand;

        private Transform _rightUpperArm;
        private Transform _rightForearm;
        private Transform _rightHand;

        private Transform _leftThigh;
        private Transform _leftCalf;
        private Transform _leftFoot;

        private Transform _rightThigh;
        private Transform _rightCalf;
        private Transform _rightFoot;

        // Seated Chair tracking
        private Transform _chairTransform;
        private bool _wasInComputerMode = false;
        private bool _isSeated = false;

        private float _walkCycle = 0f;
        public const int AVATAR_LAYER = 8; // "PlayerAvatar" layer (torso, arms, hands, legs, shoes)
        public const int AVATAR_HEAD_LAYER = 9; // "PlayerAvatarHead" layer (head, face, eyes, hair)

        // Materials
        private Material _matSkin;
        private Material _matHair;
        private Material _matJacket;
        private Material _matJeans;
        private Material _matShoes;
        private Material _matSole;
        private Material _matEyes;
        private Material _matLips;

        public Transform AvatarRoot => _avatarRoot != null ? _avatarRoot.transform : transform;
        public Transform HeadTransform => _head;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _playerController = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();

            CreateHumanMaterials();
            BuildHumanAvatar();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            ConfigureCameraCulling();
        }

        private void LateUpdate()
        {
            // Keep first-person camera culling stable across scene loads and mode transitions
            if (Time.frameCount % 30 == 0)
            {
                ConfigureCameraCulling();
            }
        }

        /// <summary>
        /// Ensures first-person camera NEVER sees any avatar meshes (both Head on Layer 9 and Body on Layer 8).
        /// In FPV the view is 100% clean, clear, and unobstructed.
        /// External cameras (surveillance/CCTV/webcam) can still see the avatar body if configured.
        /// </summary>
        public void ConfigureCameraCulling()
        {
            if (_playerController != null)
            {
                var playerCams = _playerController.GetComponentsInChildren<Camera>(true);
                foreach (var cam in playerCams)
                {
                    if (cam == null) continue;
                    cam.cullingMask &= ~(1 << AVATAR_HEAD_LAYER); // Exclude head & facial meshes
                    cam.cullingMask &= ~(1 << AVATAR_LAYER);      // Exclude torso, arms, legs, shoes from FPV
                    cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, 0.05f);
                }
            }

            Camera mainCam = Camera.main;
            if (mainCam != null && !mainCam.name.Contains("Surveillance") && !mainCam.name.Contains("Webcam"))
            {
                mainCam.cullingMask &= ~(1 << AVATAR_HEAD_LAYER); // Exclude head & facial meshes
                mainCam.cullingMask &= ~(1 << AVATAR_LAYER);      // Exclude torso, arms, legs, shoes from FPV
                mainCam.nearClipPlane = Mathf.Min(mainCam.nearClipPlane, 0.05f);
            }
        }

        /// <summary>
        /// In first-person seated computer mode, the head & hair meshes are already ShadowsOnly
        /// and culled on Layer 9. We keep spine and arms active so hands stay visible on the desk!
        /// </summary>
        public void SetFirstPersonSeated(bool seated)
        {
            _isSeated = seated;
            if (seated)
            {
                UpdateSeatedAvatar();
            }
        }

        private void CreateHumanMaterials()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

            // 1. Natural Warm Human Skin Tone
            _matSkin = new Material(litShader);
            _matSkin.name = "Mat_Avatar_HumanSkin";
            Color skinColor = new Color(0.88f, 0.70f, 0.58f); // Warm natural caucasian/tan human skin
            _matSkin.color = skinColor;
            if (_matSkin.HasProperty("_BaseColor")) _matSkin.SetColor("_BaseColor", skinColor);
            if (_matSkin.HasProperty("_Smoothness")) _matSkin.SetFloat("_Smoothness", 0.32f);

            // 2. Natural Rich Dark Brown Hair
            _matHair = new Material(litShader);
            _matHair.name = "Mat_Avatar_HumanHair";
            Color hairColor = new Color(0.12f, 0.09f, 0.08f);
            _matHair.color = hairColor;
            if (_matHair.HasProperty("_BaseColor")) _matHair.SetColor("_BaseColor", hairColor);
            if (_matHair.HasProperty("_Smoothness")) _matHair.SetFloat("_Smoothness", 0.22f);

            // 3. Modern Tech Developer Jacket / Hoodie (Dark Slate Charcoal)
            _matJacket = new Material(litShader);
            _matJacket.name = "Mat_Avatar_Jacket";
            Color jacketColor = new Color(0.14f, 0.16f, 0.21f);
            _matJacket.color = jacketColor;
            if (_matJacket.HasProperty("_BaseColor")) _matJacket.SetColor("_BaseColor", jacketColor);
            if (_matJacket.HasProperty("_Smoothness")) _matJacket.SetFloat("_Smoothness", 0.25f);

            // 4. Dark Denim Jeans
            _matJeans = new Material(litShader);
            _matJeans.name = "Mat_Avatar_Jeans";
            Color jeansColor = new Color(0.13f, 0.16f, 0.23f);
            _matJeans.color = jeansColor;
            if (_matJeans.HasProperty("_BaseColor")) _matJeans.SetColor("_BaseColor", jeansColor);
            if (_matJeans.HasProperty("_Smoothness")) _matJeans.SetFloat("_Smoothness", 0.18f);

            // 5. Sneakers (Black Upper, White Rubber Sole)
            _matShoes = new Material(litShader);
            _matShoes.name = "Mat_Avatar_Shoes";
            Color shoeColor = new Color(0.08f, 0.08f, 0.10f);
            _matShoes.color = shoeColor;
            if (_matShoes.HasProperty("_BaseColor")) _matShoes.SetColor("_BaseColor", shoeColor);

            _matSole = new Material(litShader);
            _matSole.name = "Mat_Avatar_ShoeSole";
            Color soleColor = new Color(0.93f, 0.93f, 0.93f);
            _matSole.color = soleColor;
            if (_matSole.HasProperty("_BaseColor")) _matSole.SetColor("_BaseColor", soleColor);

            // 6. Eyes & Lips
            _matEyes = new Material(litShader);
            _matEyes.name = "Mat_Avatar_Eyes";
            Color eyeColor = new Color(0.06f, 0.05f, 0.05f);
            _matEyes.color = eyeColor;
            if (_matEyes.HasProperty("_BaseColor")) _matEyes.SetColor("_BaseColor", eyeColor);
            if (_matEyes.HasProperty("_Smoothness")) _matEyes.SetFloat("_Smoothness", 0.85f);

            _matLips = new Material(litShader);
            _matLips.name = "Mat_Avatar_Lips";
            Color lipColor = new Color(0.76f, 0.46f, 0.44f);
            _matLips.color = lipColor;
            if (_matLips.HasProperty("_BaseColor")) _matLips.SetColor("_BaseColor", lipColor);
        }

        private void BuildHumanAvatar()
        {
            _avatarRoot = new GameObject("HumanAvatarRoot");
            _avatarRoot.transform.SetParent(transform, false);
            _avatarRoot.transform.localPosition = Vector3.zero;
            _avatarRoot.transform.localRotation = Quaternion.identity;

            // 1. HIPS / PELVIS BONE (Pivot at center of hips, height 0.90m)
            GameObject hipsObj = new GameObject("Hips");
            hipsObj.transform.SetParent(_avatarRoot.transform, false);
            hipsObj.transform.localPosition = new Vector3(0f, 0.90f, 0f);
            hipsObj.transform.localScale = Vector3.one;
            _hips = hipsObj.transform;

            // Hips Visual Mesh
            GameObject hipsMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hipsMesh.name = "Hips_Mesh";
            hipsMesh.transform.SetParent(_hips, false);
            hipsMesh.transform.localPosition = Vector3.zero;
            hipsMesh.transform.localScale = new Vector3(0.34f, 0.18f, 0.22f);
            ConfigurePart(hipsMesh, _matJeans);

            // 2. SPINE BONE (Pivot at waist, height 0.14m above hips)
            GameObject spineObj = new GameObject("Spine");
            spineObj.transform.SetParent(_hips, false);
            spineObj.transform.localPosition = new Vector3(0f, 0.14f, 0f);
            spineObj.transform.localScale = Vector3.one;
            _spine = spineObj.transform;

            // Torso Jacket Visual Mesh
            GameObject torsoMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torsoMesh.name = "Torso_Jacket_Mesh";
            torsoMesh.transform.SetParent(_spine, false);
            torsoMesh.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            torsoMesh.transform.localScale = new Vector3(0.38f, 0.44f, 0.24f);
            ConfigurePart(torsoMesh, _matJacket);

            // Zipper Accent
            GameObject zipper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            zipper.name = "Jacket_Zipper";
            zipper.transform.SetParent(_spine, false);
            zipper.transform.localPosition = new Vector3(0f, 0.22f, 0.125f);
            zipper.transform.localScale = new Vector3(0.025f, 0.42f, 0.02f);
            ConfigurePart(zipper, _matSole);

            // Hood Collar Rim
            GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            collar.name = "Hood_Collar";
            collar.transform.SetParent(_spine, false);
            collar.transform.localPosition = new Vector3(0f, 0.44f, -0.02f);
            collar.transform.localScale = new Vector3(0.24f, 0.04f, 0.22f);
            ConfigurePart(collar, _matJacket);

            // 3. NECK BONE (Pivot at base of neck, unscaled Transform)
            GameObject neckObj = new GameObject("Neck");
            neckObj.transform.SetParent(_spine, false);
            neckObj.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            neckObj.transform.localScale = Vector3.one;
            _neck = neckObj.transform;

            // Neck Visual Cylinder
            GameObject neckMesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            neckMesh.name = "Neck_Mesh";
            neckMesh.transform.SetParent(_neck, false);
            neckMesh.transform.localPosition = new Vector3(0f, 0.04f, 0f);
            neckMesh.transform.localScale = new Vector3(0.11f, 0.05f, 0.11f);
            ConfigureHeadPart(neckMesh, _matSkin);

            // 4. HEAD BONE (Pivot at base of skull, unscaled Transform)
            GameObject headObj = new GameObject("Head");
            headObj.transform.SetParent(_neck, false);
            headObj.transform.localPosition = new Vector3(0f, 0.09f, 0f);
            headObj.transform.localScale = Vector3.one;
            _head = headObj.transform;

            // Cranium (Head Shape Sphere)
            GameObject craniumMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            craniumMesh.name = "Cranium_Skin_Mesh";
            craniumMesh.transform.SetParent(_head, false);
            craniumMesh.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            craniumMesh.transform.localScale = new Vector3(0.22f, 0.26f, 0.24f);
            ConfigureHeadPart(craniumMesh, _matSkin);

            // Jaw & Chin
            GameObject chinMesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            chinMesh.name = "Chin_Skin_Mesh";
            chinMesh.transform.SetParent(_head, false);
            chinMesh.transform.localPosition = new Vector3(0f, -0.02f, 0.05f);
            chinMesh.transform.localRotation = Quaternion.Euler(22f, 0f, 0f);
            chinMesh.transform.localScale = new Vector3(0.14f, 0.12f, 0.14f);
            ConfigureHeadPart(chinMesh, _matSkin);

            // Hair Volume (Top & Crown)
            GameObject hairObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hairObj.name = "Hair_Top_Mesh";
            hairObj.transform.SetParent(_head, false);
            hairObj.transform.localPosition = new Vector3(0f, 0.12f, -0.02f);
            hairObj.transform.localScale = new Vector3(0.24f, 0.22f, 0.25f);
            ConfigureHeadPart(hairObj, _matHair);
            _hair = hairObj.transform;

            // Hair Front Fringe / Parting
            GameObject fringe = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fringe.name = "Hair_Fringe_Mesh";
            fringe.transform.SetParent(_head, false);
            fringe.transform.localPosition = new Vector3(0f, 0.18f, 0.09f);
            fringe.transform.localRotation = Quaternion.Euler(26f, 0f, 0f);
            fringe.transform.localScale = new Vector3(0.20f, 0.07f, 0.10f);
            ConfigureHeadPart(fringe, _matHair);

            // Face Details: Eyes, Eyebrows, Nose, Lips, Ears
            CreateFaceFeatures(_head);

            // 5. ARMS & HANDS (Parented to _spine at shoulders, completely unscaled)
            // Left Arm
            _leftUpperArm = CreateArmBone("Arm_Upper_L", _spine, new Vector3(-0.25f, 0.38f, 0f), 0.28f, _matJacket);
            _leftForearm = CreateArmBone("Arm_Fore_L", _leftUpperArm, new Vector3(0f, -0.28f, 0f), 0.26f, _matJacket);
            _leftHand = CreateHand("Hand_L", _leftForearm, new Vector3(0f, -0.26f, 0f), true);

            // Right Arm
            _rightUpperArm = CreateArmBone("Arm_Upper_R", _spine, new Vector3(0.25f, 0.38f, 0f), 0.28f, _matJacket);
            _rightForearm = CreateArmBone("Arm_Fore_R", _rightUpperArm, new Vector3(0f, -0.28f, 0f), 0.26f, _matJacket);
            _rightHand = CreateHand("Hand_R", _rightForearm, new Vector3(0f, -0.26f, 0f), false);

            // 6. LEGS & SNEAKERS (Parented to _hips, completely unscaled)
            _leftThigh = CreateLegBone("Thigh_L", _hips, new Vector3(-0.11f, -0.06f, 0f), 0.40f, _matJeans);
            _leftCalf = CreateLegBone("Calf_L", _leftThigh, new Vector3(0f, -0.40f, 0f), 0.40f, _matJeans);
            _leftFoot = CreateSneaker("Sneaker_L", _leftCalf, new Vector3(0f, -0.40f, 0f));

            _rightThigh = CreateLegBone("Thigh_R", _hips, new Vector3(0.11f, -0.06f, 0f), 0.40f, _matJeans);
            _rightCalf = CreateLegBone("Calf_R", _rightThigh, new Vector3(0f, -0.40f, 0f), 0.40f, _matJeans);
            _rightFoot = CreateSneaker("Sneaker_R", _rightCalf, new Vector3(0f, -0.40f, 0f));

            // Set base layer to PlayerAvatar (8), and head bone sub-hierarchy to PlayerAvatarHead (9)
            SetLayerRecursively(_avatarRoot, AVATAR_LAYER);
            if (_neck != null) SetLayerRecursively(_neck.gameObject, AVATAR_HEAD_LAYER);
            if (_head != null) SetLayerRecursively(_head.gameObject, AVATAR_HEAD_LAYER);
        }

        private void CreateFaceFeatures(Transform head)
        {
            // Eyes with white sclera and dark pupils
            CreateEye(head, "Eye_L", new Vector3(-0.048f, 0.07f, 0.108f));
            CreateEye(head, "Eye_R", new Vector3(0.048f, 0.07f, 0.108f));

            // Eyebrows
            CreateEyebrow(head, "Eyebrow_L", new Vector3(-0.048f, 0.102f, 0.108f), -8f);
            CreateEyebrow(head, "Eyebrow_R", new Vector3(0.048f, 0.102f, 0.108f), 8f);

            // Nose
            GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            nose.name = "Nose_Mesh";
            nose.transform.SetParent(head, false);
            nose.transform.localPosition = new Vector3(0f, 0.045f, 0.126f);
            nose.transform.localScale = new Vector3(0.028f, 0.052f, 0.038f);
            ConfigureHeadPart(nose, _matSkin);

            // Lips / Mouth
            GameObject lips = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lips.name = "Lips_Mesh";
            lips.transform.SetParent(head, false);
            lips.transform.localPosition = new Vector3(0f, -0.005f, 0.110f);
            lips.transform.localScale = new Vector3(0.058f, 0.018f, 0.022f);
            ConfigureHeadPart(lips, _matLips);

            // Left & Right Ears
            CreateEar(head, "Ear_L", new Vector3(-0.118f, 0.07f, 0f), -12f);
            CreateEar(head, "Ear_R", new Vector3(0.118f, 0.07f, 0f), 12f);
        }

        private void CreateEye(Transform parent, string name, Vector3 pos)
        {
            // Sclera
            GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = name;
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = pos;
            eye.transform.localScale = new Vector3(0.030f, 0.026f, 0.024f);
            ConfigureHeadPart(eye, _matSole);

            // Pupil
            GameObject pupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pupil.name = $"{name}_Pupil";
            pupil.transform.SetParent(eye.transform, false);
            pupil.transform.localPosition = new Vector3(0f, 0f, 0.35f);
            pupil.transform.localScale = new Vector3(0.65f, 0.65f, 0.65f);
            ConfigureHeadPart(pupil, _matEyes);
        }

        private void CreateEyebrow(Transform parent, string name, Vector3 pos, float rollZ)
        {
            GameObject brow = GameObject.CreatePrimitive(PrimitiveType.Cube);
            brow.name = name;
            brow.transform.SetParent(parent, false);
            brow.transform.localPosition = pos;
            brow.transform.localRotation = Quaternion.Euler(0f, 0f, rollZ);
            brow.transform.localScale = new Vector3(0.042f, 0.010f, 0.016f);
            ConfigureHeadPart(brow, _matHair);
        }

        private void CreateEar(Transform parent, string name, Vector3 pos, float yaw)
        {
            GameObject ear = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ear.name = name;
            ear.transform.SetParent(parent, false);
            ear.transform.localPosition = pos;
            ear.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            ear.transform.localScale = new Vector3(0.020f, 0.060f, 0.038f);
            ConfigureHeadPart(ear, _matSkin);
        }

        private Transform CreateArmBone(string name, Transform parent, Vector3 localPos, float length, Material mat)
        {
            GameObject bone = new GameObject(name);
            bone.transform.SetParent(parent, false);
            bone.transform.localPosition = localPos;
            bone.transform.localScale = Vector3.one;

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = $"{name}_Mesh";
            mesh.transform.SetParent(bone.transform, false);
            mesh.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            mesh.transform.localScale = new Vector3(0.11f, length, 0.11f);
            ConfigurePart(mesh, mat);

            return bone.transform;
        }

        private Transform CreateHand(string name, Transform parent, Vector3 localPos, bool isLeft)
        {
            GameObject handBone = new GameObject(name);
            handBone.transform.SetParent(parent, false);
            handBone.transform.localPosition = localPos;
            handBone.transform.localScale = Vector3.one;

            // 1. Palm
            GameObject palm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            palm.name = $"{name}_Palm_Mesh";
            palm.transform.SetParent(handBone.transform, false);
            palm.transform.localPosition = new Vector3(0f, -0.045f, 0f);
            palm.transform.localScale = new Vector3(0.082f, 0.090f, 0.042f);
            ConfigurePart(palm, _matSkin);

            // 2. Thumb
            GameObject thumb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            thumb.name = $"{name}_Thumb_Mesh";
            thumb.transform.SetParent(handBone.transform, false);
            float thumbX = isLeft ? 0.046f : -0.046f;
            thumb.transform.localPosition = new Vector3(thumbX, -0.035f, 0.018f);
            thumb.transform.localScale = new Vector3(0.024f, 0.048f, 0.024f);
            thumb.transform.localRotation = Quaternion.Euler(22f, 0f, isLeft ? 28f : -28f);
            ConfigurePart(thumb, _matSkin);

            // 3. Fingers Section
            GameObject fingers = GameObject.CreatePrimitive(PrimitiveType.Cube);
            fingers.name = $"{name}_Fingers_Mesh";
            fingers.transform.SetParent(handBone.transform, false);
            fingers.transform.localPosition = new Vector3(0f, -0.110f, 0.008f);
            fingers.transform.localScale = new Vector3(0.076f, 0.060f, 0.030f);
            ConfigurePart(fingers, _matSkin);

            return handBone.transform;
        }

        private Transform CreateLegBone(string name, Transform parent, Vector3 localPos, float length, Material mat)
        {
            GameObject bone = new GameObject(name);
            bone.transform.SetParent(parent, false);
            bone.transform.localPosition = localPos;
            bone.transform.localScale = Vector3.one;

            GameObject mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mesh.name = $"{name}_Mesh";
            mesh.transform.SetParent(bone.transform, false);
            mesh.transform.localPosition = new Vector3(0f, -length * 0.5f, 0f);
            mesh.transform.localScale = new Vector3(0.14f, length, 0.14f);
            ConfigurePart(mesh, mat);

            return bone.transform;
        }

        private Transform CreateSneaker(string name, Transform parent, Vector3 localPos)
        {
            GameObject footBone = new GameObject(name);
            footBone.transform.SetParent(parent, false);
            footBone.transform.localPosition = localPos;
            footBone.transform.localScale = Vector3.one;

            // Sneaker Upper
            GameObject upper = GameObject.CreatePrimitive(PrimitiveType.Cube);
            upper.name = $"{name}_Upper_Mesh";
            upper.transform.SetParent(footBone.transform, false);
            upper.transform.localPosition = new Vector3(0f, -0.035f, 0.06f);
            upper.transform.localScale = new Vector3(0.12f, 0.07f, 0.25f);
            ConfigurePart(upper, _matShoes);

            // Rubber Midsole
            GameObject sole = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sole.name = $"{name}_Sole_Mesh";
            sole.transform.SetParent(footBone.transform, false);
            sole.transform.localPosition = new Vector3(0f, -0.068f, 0.06f);
            sole.transform.localScale = new Vector3(0.126f, 0.026f, 0.26f);
            ConfigurePart(sole, _matSole);

            return footBone.transform;
        }

        private void ConfigurePart(GameObject obj, Material mat)
        {
            var r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            obj.layer = AVATAR_LAYER;
        }

        private void ConfigureHeadPart(GameObject obj, Material mat)
        {
            var r = obj.GetComponent<Renderer>();
            if (r != null)
            {
                r.sharedMaterial = mat;
                // Visible in external cameras (webcam, surveillance, 3rd person)
                // Culled from first-person view by camera culling mask
                r.shadowCastingMode = ShadowCastingMode.On;
                r.receiveShadows = true;
            }
            var col = obj.GetComponent<Collider>();
            if (col != null) DestroyImmediate(col);
            obj.layer = AVATAR_HEAD_LAYER;
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private void Update()
        {
            if (_avatarRoot == null) return;

            bool inComputer = _isSeated || (ComputerInteractionManager.Instance != null && (ComputerInteractionManager.Instance.IsInComputerMode || ComputerInteractionManager.Instance.CurrentMode == VRRemoteDesktop.Computer.WorkstationMode.TransitioningToComputer));

            if (inComputer)
            {
                UpdateSeatedAvatar();
                _wasInComputerMode = true;
            }
            else
            {
                if (_wasInComputerMode)
                {
                    // Return avatar cleanly under player root and restore visibility
                    _avatarRoot.transform.SetParent(transform, true);
                    _avatarRoot.transform.localPosition = Vector3.zero;
                    _avatarRoot.transform.localRotation = Quaternion.identity;

                    _wasInComputerMode = false;
                }

                UpdateWalkingLocomotion();
            }
        }

        /// <summary>
        /// Synchronizes the human character into the workstation gaming chair.
        /// As the chair rolls backward/forward or slides laterally across monitors,
        /// the avatar moves with 100% precision with the chair.
        /// </summary>
        private void UpdateSeatedAvatar()
        {
            if (_chairTransform == null)
            {
                var chairObj = GameObject.Find("Workstation_GamingChair");
                if (chairObj != null) _chairTransform = chairObj.transform;
            }

            if (_chairTransform != null)
            {
                // Anchor the avatar root directly to the workstation gaming chair
                _avatarRoot.transform.position = _chairTransform.position;
                _avatarRoot.transform.rotation = _chairTransform.rotation;

                // Position Hips on the chair cushion
                _hips.localPosition = new Vector3(0f, 0.44f, 0.02f);
                _hips.localRotation = Quaternion.identity;
            }
            else
            {
                // Fallback default workstation seat
                _avatarRoot.transform.position = new Vector3(0f, 0.12f, 0.15f);
                _avatarRoot.transform.rotation = Quaternion.identity;
                _hips.localPosition = new Vector3(0f, 0.44f, 0.02f);
            }

            // 1. Ergonomic seated spine against chair backrest
            _spine.localRotation = Quaternion.Euler(-4f, 0f, 0f);

            // 2. Seated legs: thighs horizontal along seat cushion, knees bent at 90 degrees down to floor
            _leftThigh.localRotation = Quaternion.Euler(86f, -3f, 0f);
            _rightThigh.localRotation = Quaternion.Euler(86f, 3f, 0f);

            _leftCalf.localRotation = Quaternion.Euler(-86f, 0f, 0f);
            _rightCalf.localRotation = Quaternion.Euler(-86f, 0f, 0f);

            _leftFoot.localRotation = Quaternion.identity;
            _rightFoot.localRotation = Quaternion.identity;

            // 3. Hands placed naturally on workstation desk:
            // Left hand resting near keyboard (WASD), Right hand resting on mouse
            _leftUpperArm.localRotation = Quaternion.Euler(38f, 16f, -10f);
            _leftForearm.localRotation = Quaternion.Euler(52f, -12f, 0f);
            _leftHand.localRotation = Quaternion.Euler(0f, 0f, 15f);

            _rightUpperArm.localRotation = Quaternion.Euler(38f, -16f, 10f);
            _rightForearm.localRotation = Quaternion.Euler(52f, 12f, 0f);
            _rightHand.localRotation = Quaternion.Euler(0f, 0f, -15f);

            // 4. Head turns dynamically to track the player's camera look orientation across the monitors!
            Camera cam = _playerController != null ? _playerController.GetComponentInChildren<Camera>() : Camera.main;
            if (cam != null && _head != null)
            {
                Vector3 camForward = cam.transform.forward;
                Vector3 localLookDir = _avatarRoot.transform.InverseTransformDirection(camForward);
                float yaw = Mathf.Atan2(localLookDir.x, localLookDir.z) * Mathf.Rad2Deg;
                float pitch = -Mathf.Asin(Mathf.Clamp(localLookDir.y, -0.99f, 0.99f)) * Mathf.Rad2Deg;

                yaw = Mathf.Clamp(yaw, -75f, 75f);
                pitch = Mathf.Clamp(pitch, -45f, 45f);

                _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.Euler(pitch * 0.85f, yaw * 0.85f, 0f), Time.deltaTime * 12f);
            }
        }

        /// <summary>
        /// Locomotion animation while walking or sprinting in exploration mode.
        /// </summary>
        private void UpdateWalkingLocomotion()
        {
            _avatarRoot.transform.localPosition = Vector3.zero;
            _avatarRoot.transform.localRotation = Quaternion.identity;

            // Reset hips to standing height
            _hips.localPosition = new Vector3(0f, 0.90f, 0f);
            _hips.localRotation = Quaternion.identity;
            _spine.localRotation = Quaternion.identity;

            float speed = 0f;
            if (_characterController != null)
            {
                speed = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z).magnitude;
            }

            if (speed > 0.15f)
            {
                float cycleSpeed = speed > 4.2f ? 14f : 8.5f;
                _walkCycle += Time.deltaTime * cycleSpeed;

                float legStride = speed > 4.2f ? 34f : 24f;
                float armSwing = legStride * 0.9f;

                float sin = Mathf.Sin(_walkCycle);

                // Alternating leg stride
                _leftThigh.localRotation = Quaternion.Euler(sin * legStride, 0f, 0f);
                _rightThigh.localRotation = Quaternion.Euler(-sin * legStride, 0f, 0f);

                // Knee flex during back-swing
                _leftCalf.localRotation = Quaternion.Euler(sin < 0 ? Mathf.Abs(sin) * 45f : 0f, 0f, 0f);
                _rightCalf.localRotation = Quaternion.Euler(sin > 0 ? Mathf.Abs(sin) * 45f : 0f, 0f, 0f);

                // Opposite phase arm swing
                _leftUpperArm.localRotation = Quaternion.Euler(-sin * armSwing, 0f, -6f);
                _rightUpperArm.localRotation = Quaternion.Euler(sin * armSwing, 0f, 6f);

                _leftForearm.localRotation = Quaternion.Euler(15f + Mathf.Max(0f, -sin * 20f), 0f, 0f);
                _rightForearm.localRotation = Quaternion.Euler(15f + Mathf.Max(0f, sin * 20f), 0f, 0f);

                // Subtle torso bounce
                _hips.localPosition = new Vector3(0f, 0.90f + Mathf.Abs(sin) * 0.025f, 0f);
            }
            else
            {
                // Natural idle breathing posture
                float breathe = Mathf.Sin(Time.time * 1.8f) * 1.2f;

                _leftThigh.localRotation = Quaternion.Slerp(_leftThigh.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                _rightThigh.localRotation = Quaternion.Slerp(_rightThigh.localRotation, Quaternion.identity, Time.deltaTime * 8f);

                _leftCalf.localRotation = Quaternion.Slerp(_leftCalf.localRotation, Quaternion.identity, Time.deltaTime * 8f);
                _rightCalf.localRotation = Quaternion.Slerp(_rightCalf.localRotation, Quaternion.identity, Time.deltaTime * 8f);

                _leftUpperArm.localRotation = Quaternion.Slerp(_leftUpperArm.localRotation, Quaternion.Euler(breathe, 0f, -6f), Time.deltaTime * 8f);
                _rightUpperArm.localRotation = Quaternion.Slerp(_rightUpperArm.localRotation, Quaternion.Euler(breathe, 0f, 6f), Time.deltaTime * 8f);

                _leftForearm.localRotation = Quaternion.Slerp(_leftForearm.localRotation, Quaternion.Euler(10f, 0f, 0f), Time.deltaTime * 8f);
                _rightForearm.localRotation = Quaternion.Slerp(_rightForearm.localRotation, Quaternion.Euler(10f, 0f, 0f), Time.deltaTime * 8f);

                _head.localRotation = Quaternion.Slerp(_head.localRotation, Quaternion.identity, Time.deltaTime * 6f);
            }
        }
    }
}
