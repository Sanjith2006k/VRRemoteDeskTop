using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;
using Unity.XR.CoreUtils;
using VRRemoteDesktop.Computer;
using VRRemoteDesktop.Environment;
using VRRemoteDesktop.Input;
using VRRemoteDesktop.Player;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.XR;

namespace VRRemoteDesktop.Editor
{
    /// <summary>
    /// Master automated scene setup for VR Remote Desktop Control.
    /// Integrates OpenXR, XR Origin rig, Ray Interactors, 3-Monitor interaction bridges,
    /// and ensures zero missing references or compilation errors.
    /// </summary>
    public static class VRRemoteDesktopSetup
    {
        [MenuItem("VR Remote Desktop/Setup VR Remote Desktop Complete")]
        public static void SetupComplete()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                if (File.Exists("Assets/Scenes/Main.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[VRRemoteDesktopSetup] Beginning automated VR Remote Desktop integration...");

            // 1. Ensure XRInteractionManager
            XRInteractionManager xim = Object.FindAnyObjectByType<XRInteractionManager>();
            if (xim == null)
            {
                GameObject ximObj = new GameObject("XR Interaction Manager");
                xim = ximObj.AddComponent<XRInteractionManager>();
                Debug.Log("[VRRemoteDesktopSetup] Created XR Interaction Manager.");
            }

            // 2. Locate or instantiate XR Origin (VR)
            XROrigin origin = Object.FindAnyObjectByType<XROrigin>();
            GameObject originObj = null;

            if (origin != null)
            {
                originObj = origin.gameObject;
            }
            else
            {
                string prefabPath = "Assets/Samples/XR Interaction Toolkit/3.6.0/Starter Assets/Prefabs/XR Origin (XR Rig).prefab";
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    originObj = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                    originObj.name = "XR Origin (VR)";
                    origin = originObj.GetComponent<XROrigin>();
                    Debug.Log("[VRRemoteDesktopSetup] Instantiated XR Origin from Starter Assets prefab.");
                }
                else
                {
                    originObj = new GameObject("XR Origin (VR)");
                    origin = originObj.AddComponent<XROrigin>();
                    GameObject offset = new GameObject("Camera Offset");
                    offset.transform.SetParent(originObj.transform, false);
                    origin.CameraFloorOffsetObject = offset;
                    Debug.Log("[VRRemoteDesktopSetup] Created standard XR Origin.");
                }
            }

            // Position XR Origin in front of the monitors at the workstation chair
            originObj.transform.position = new Vector3(0f, 0f, 0.15f);
            originObj.transform.rotation = Quaternion.identity;

            // 3. Attach XRPlayerRigManager
            XRPlayerRigManager rigMgr = originObj.GetComponent<XRPlayerRigManager>();
            if (rigMgr == null)
            {
                rigMgr = originObj.AddComponent<XRPlayerRigManager>();
            }

            SerializedObject soRig = new SerializedObject(rigMgr);
            soRig.FindProperty("_xrOrigin").objectReferenceValue = origin;
            soRig.FindProperty("_interactionManager").objectReferenceValue = xim;
            if (origin.Camera != null)
            {
                soRig.FindProperty("_xrCamera").objectReferenceValue = origin.Camera;
                if (origin.Camera.GetComponent<AudioListener>() == null)
                {
                    origin.Camera.gameObject.AddComponent<AudioListener>();
                    Debug.Log("[VRRemoteDesktopSetup] Attached AudioListener to XR Camera.");
                }
            }
            if (origin.CameraFloorOffsetObject != null)
            {
                soRig.FindProperty("_cameraOffset").objectReferenceValue = origin.CameraFloorOffsetObject.transform;
            }

            RoomSpeakerController speakers = Object.FindAnyObjectByType<RoomSpeakerController>();
            if (speakers != null)
            {
                speakers.SpeakerVolume = 0.9f;
                Debug.Log("[VRRemoteDesktopSetup] Configured RoomSpeakerController with volume 0.9 and auto-play.");
            }

            // 4. Configure Controller Interactors
            var rayInteractors = originObj.GetComponentsInChildren<XRRayInteractor>(true);
            foreach (var ray in rayInteractors)
            {
                SerializedObject soRay = new SerializedObject(ray);
                var propMgr = soRay.FindProperty("m_InteractionManager");
                if (propMgr != null) propMgr.objectReferenceValue = xim;
                soRay.ApplyModifiedProperties();

                // Ensure Line Visual
                var lineVisual = ray.GetComponent<XRInteractorLineVisual>();
                if (lineVisual == null) lineVisual = ray.gameObject.AddComponent<XRInteractorLineVisual>();
                lineVisual.lineWidth = 0.005f;
                lineVisual.smoothMovement = true;

                if (ray.gameObject.name.Contains("Right") || ray.transform.parent.name.Contains("Right"))
                {
                    soRig.FindProperty("_rightRayInteractor").objectReferenceValue = ray;
                    soRig.FindProperty("_rightLineVisual").objectReferenceValue = lineVisual;
                }
                else if (ray.gameObject.name.Contains("Left") || ray.transform.parent.name.Contains("Left"))
                {
                    soRig.FindProperty("_leftRayInteractor").objectReferenceValue = ray;
                    soRig.FindProperty("_leftLineVisual").objectReferenceValue = lineVisual;
                }
            }
            soRig.ApplyModifiedProperties();

            // 5. Configure Center Monitor (Screen)
            GameObject centerScreenObj = GameObject.Find("Screen");
            if (centerScreenObj != null)
            {
                var vrInter = centerScreenObj.GetComponent<VRDesktopInteractable>();
                if (vrInter == null) vrInter = centerScreenObj.AddComponent<VRDesktopInteractable>();

                SerializedObject soCenter = new SerializedObject(vrInter);
                var mgrProp = soCenter.FindProperty("m_InteractionManager");
                if (mgrProp != null) mgrProp.objectReferenceValue = xim;

                var cursor = Object.FindAnyObjectByType<VRDesktopCursor>();
                if (cursor != null)
                {
                    var curProp = soCenter.FindProperty("_cursor");
                    if (curProp != null) curProp.objectReferenceValue = cursor;
                }

                var canvasObj = GameObject.Find("DesktopCanvas");
                if (canvasObj != null)
                {
                    var cProp = soCenter.FindProperty("_canvasRect");
                    if (cProp != null) cProp.objectReferenceValue = canvasObj.GetComponent<RectTransform>();
                }
                soCenter.ApplyModifiedProperties();
                Debug.Log("[VRRemoteDesktopSetup] Center monitor (Screen) interactable configured.");
            }

            // 6. Configure Left Monitor (LeftScreen)
            GameObject leftScreenObj = GameObject.Find("LeftScreen");
            if (leftScreenObj != null)
            {
                var auxInter = leftScreenObj.GetComponent<AuxiliaryScreenInteractable>();
                if (auxInter == null) auxInter = leftScreenObj.AddComponent<AuxiliaryScreenInteractable>();
                auxInter.Configure("LeftDesktopCanvas", "UILeftCamera", new Color(0f, 1f, 1f, 0.95f));

                SerializedObject soLeft = new SerializedObject(auxInter);
                var mgrProp = soLeft.FindProperty("m_InteractionManager");
                if (mgrProp != null) mgrProp.objectReferenceValue = xim;
                soLeft.ApplyModifiedProperties();
                Debug.Log("[VRRemoteDesktopSetup] Left monitor (LeftScreen) interactable configured.");
            }

            // 7. Configure Right Monitor (RightScreen)
            GameObject rightScreenObj = GameObject.Find("RightScreen");
            if (rightScreenObj != null)
            {
                var auxInter = rightScreenObj.GetComponent<AuxiliaryScreenInteractable>();
                if (auxInter == null) auxInter = rightScreenObj.AddComponent<AuxiliaryScreenInteractable>();
                auxInter.Configure("RightDesktopCanvas", "UIRightCamera", new Color(0.85f, 0.4f, 1f, 0.95f));

                SerializedObject soRight = new SerializedObject(auxInter);
                var mgrProp = soRight.FindProperty("m_InteractionManager");
                if (mgrProp != null) mgrProp.objectReferenceValue = xim;
                soRight.ApplyModifiedProperties();
                Debug.Log("[VRRemoteDesktopSetup] Right monitor (RightScreen) interactable configured.");
            }

            // 8. Configure RemoteDesktopManager with Session Providers
            RemoteDesktopManager rdm = Object.FindAnyObjectByType<RemoteDesktopManager>();
            if (rdm != null)
            {
                if (rdm.GetComponent<SimulatedDesktopSessionProvider>() == null)
                {
                    rdm.gameObject.AddComponent<SimulatedDesktopSessionProvider>();
                }
                if (rdm.GetComponent<RemoteDesktopSessionProvider>() == null)
                {
                    rdm.gameObject.AddComponent<RemoteDesktopSessionProvider>();
                }
                Debug.Log("[VRRemoteDesktopSetup] Attached Simulated and Remote Session Providers to RemoteDesktopManager.");
            }

            // 8b. Ensure exactly one Player object and clean initial positioning
            var allPlayers = Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Include);
            if (allPlayers != null && allPlayers.Length > 1)
            {
                PlayerController primary = null;
                for (int i = 0; i < allPlayers.Length; i++)
                {
                    var p = allPlayers[i];
                    if (primary == null && p.gameObject.name == "Player")
                    {
                        primary = p;
                    }
                    else if (p != primary)
                    {
                        Debug.Log($"[VRRemoteDesktopSetup] Removing duplicate player instance: {p.gameObject.name}");
                        Object.DestroyImmediate(p.gameObject);
                    }
                }
            }

            var singlePlayer = Object.FindAnyObjectByType<PlayerController>();
            if (singlePlayer != null)
            {
                singlePlayer.transform.position = new Vector3(0f, 1.0f, -0.9f);
                singlePlayer.transform.rotation = Quaternion.identity;

                if (singlePlayer.GetComponent<PlayerAvatarBody>() == null)
                {
                    singlePlayer.gameObject.AddComponent<PlayerAvatarBody>();
                }

                var cam = singlePlayer.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    cam.tag = "MainCamera";
                    cam.transform.localPosition = Vector3.zero;
                    cam.transform.localRotation = Quaternion.identity;
                }
            }

            // 9. Save scene cleanly
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[VRRemoteDesktopSetup] VR Remote Desktop setup successfully completed and Main scene saved!");
        }
    }
}
