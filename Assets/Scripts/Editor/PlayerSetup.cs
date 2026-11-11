using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.InputSystem;
using VRRemoteDesktop.Player;

namespace VRRemoteDesktop.Editor
{
    public static class PlayerSetup
    {
        [MenuItem("VR Remote Desktop/Setup Phase 2 Player")]
        public static void SetupPlayerMenu()
        {
            SetupPlayer();
        }

        public static void SetupPlayer()
        {
            var currentScene = EditorSceneManager.GetActiveScene();
            if (currentScene.name != "Main")
            {
                if (System.IO.File.Exists("Assets/Scenes/Main.unity"))
                {
                    currentScene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[PlayerSetup] Setting up Phase 2 Player in scene: " + currentScene.name);

            // 1. Clean up unused/broken XR Origin (VR) if present
            GameObject xrOrigin = GameObject.Find("XR Origin (VR)");
            if (xrOrigin != null)
            {
                Debug.Log("[PlayerSetup] Removing redundant XR Origin (VR)...");
                Object.DestroyImmediate(xrOrigin);
            }

            // 2. Ensure UICamera is independent at root level (isolated offscreen)
            GameObject uiCamObj = GameObject.Find("UICamera");
            if (uiCamObj != null)
            {
                uiCamObj.transform.SetParent(null);
                uiCamObj.transform.position = new Vector3(0f, 1000f, 0f);
                uiCamObj.transform.rotation = Quaternion.identity;
                Debug.Log("[PlayerSetup] UICamera restored to root at (0, 1000, 0).");
            }

            // 3. Find the 3D scene camera (NOT UICamera!)
            GameObject sceneCamObj = null;
            Camera[] allCams = Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude);
            foreach (var cam in allCams)
            {
                if (cam.gameObject.name != "UICamera" && cam.targetTexture == null)
                {
                    sceneCamObj = cam.gameObject;
                    break;
                }
            }

            if (sceneCamObj == null)
            {
                sceneCamObj = GameObject.Find("Camera");
            }

            if (sceneCamObj == null)
            {
                sceneCamObj = new GameObject("Camera");
                sceneCamObj.AddComponent<Camera>();
            }

            // 4. Find or create Player root object
            GameObject playerObj = GameObject.Find("Player");
            if (playerObj == null)
            {
                playerObj = new GameObject("Player");
            }

            // Ensure player is at root level
            playerObj.transform.SetParent(null);
            // Position player on floor (y=0) at z=-1.5, facing the desk at z=0.6
            playerObj.transform.position = new Vector3(0f, 0f, -1.5f);
            playerObj.transform.rotation = Quaternion.identity;

            // 5. Configure CharacterController
            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc == null) cc = playerObj.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 45f;
            cc.minMoveDistance = 0f;

            // 6. Configure PlayerController
            PlayerController playerCtrl = playerObj.GetComponent<PlayerController>();
            if (playerCtrl == null) playerCtrl = playerObj.AddComponent<PlayerController>();
            playerCtrl.WalkSpeed = 4.0f;
            playerCtrl.SprintSpeed = 6.5f;

            // 7. Setup CameraOffset hierarchy
            Transform cameraOffset = playerObj.transform.Find("CameraOffset");
            if (cameraOffset == null)
            {
                GameObject offsetObj = new GameObject("CameraOffset");
                offsetObj.transform.SetParent(playerObj.transform);
                cameraOffset = offsetObj.transform;
            }
            // Human eye height: 1.65 meters
            cameraOffset.localPosition = new Vector3(0f, 1.65f, 0f);
            cameraOffset.localRotation = Quaternion.identity;

            // 8. Re-parent scene camera under CameraOffset
            sceneCamObj.transform.SetParent(cameraOffset);
            sceneCamObj.transform.localPosition = Vector3.zero;
            sceneCamObj.transform.localRotation = Quaternion.identity;
            sceneCamObj.tag = "MainCamera";
            sceneCamObj.name = "Camera";

            // Camera settings for first-person perspective
            Camera camComp = sceneCamObj.GetComponent<Camera>();
            if (camComp != null)
            {
                camComp.orthographic = false;
                camComp.fieldOfView = 65f;
                camComp.nearClipPlane = 0.1f;
                camComp.farClipPlane = 100f;
                // Exclude UI layer and PlayerAvatar layers (8, 9) from the main first-person camera
                int uiLayer = LayerMask.NameToLayer("UI");
                int avatarLayer = 8;
                int avatarHeadLayer = 9;
                int mask = ~0;
                if (uiLayer >= 0) mask &= ~(1 << uiLayer);
                mask &= ~(1 << avatarLayer);
                mask &= ~(1 << avatarHeadLayer);
                camComp.cullingMask = mask;
            }

            // Ensure AudioListener on MainCamera
            if (sceneCamObj.GetComponent<AudioListener>() == null)
            {
                sceneCamObj.AddComponent<AudioListener>();
            }

            // 9. Setup MouseLook on Camera
            MouseLook mouseLook = sceneCamObj.GetComponent<MouseLook>();
            if (mouseLook == null) mouseLook = sceneCamObj.AddComponent<MouseLook>();

            SerializedObject mlSo = new SerializedObject(mouseLook);
            mlSo.FindProperty("_playerBody").objectReferenceValue = playerObj.transform;
            mlSo.FindProperty("_cameraTransform").objectReferenceValue = sceneCamObj.transform;
            mlSo.FindProperty("_sensitivity").floatValue = 1.0f;
            mlSo.FindProperty("_minPitch").floatValue = -85f;
            mlSo.FindProperty("_maxPitch").floatValue = 85f;
            mlSo.FindProperty("_lockCursorOnStart").boolValue = true;
            mlSo.FindProperty("_unlockOnEscape").boolValue = true;
            mlSo.ApplyModifiedProperties();

            // 10. Bind Input Actions from InputSystem_Actions asset
            InputActionAsset actionsAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
            if (actionsAsset != null)
            {
                var moveAction = actionsAsset.FindAction("Player/Move");
                var lookAction = actionsAsset.FindAction("Player/Look");
                var sprintAction = actionsAsset.FindAction("Player/Sprint");
                var cancelAction = actionsAsset.FindAction("Player/Cancel");

                SerializedObject pcSo = new SerializedObject(playerCtrl);
                if (moveAction != null)
                {
                    pcSo.FindProperty("_moveAction").objectReferenceValue = InputActionReference.Create(moveAction);
                }
                if (sprintAction != null)
                {
                    pcSo.FindProperty("_sprintAction").objectReferenceValue = InputActionReference.Create(sprintAction);
                }
                pcSo.ApplyModifiedProperties();

                mlSo.Update();
                if (lookAction != null)
                {
                    mlSo.FindProperty("_lookAction").objectReferenceValue = InputActionReference.Create(lookAction);
                }
                if (cancelAction != null)
                {
                    mlSo.FindProperty("_cancelAction").objectReferenceValue = InputActionReference.Create(cancelAction);
                }
                mlSo.ApplyModifiedProperties();
            }

            // 11. Create Prefabs/Player folder and save prefab
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Player"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                {
                    AssetDatabase.CreateFolder("Assets", "Prefabs");
                }
                AssetDatabase.CreateFolder("Assets/Prefabs", "Player");
            }

            string prefabPath = "Assets/Prefabs/Player/Player.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(playerObj, prefabPath, InteractionMode.AutomatedAction);
            Debug.Log("[PlayerSetup] Saved clean Player Prefab to: " + prefabPath);

            // 12. Save scene
            EditorSceneManager.MarkSceneDirty(currentScene);
            EditorSceneManager.SaveScene(currentScene);

            Debug.Log("[PlayerSetup] Phase 2 Player setup successfully completed and Main scene saved!");
        }
    }
}
