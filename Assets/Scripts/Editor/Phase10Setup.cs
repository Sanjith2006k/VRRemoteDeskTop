using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using VRRemoteDesktop.Remote;
using VRRemoteDesktop.Audio;
using VRRemoteDesktop.UI;

namespace VRRemoteDesktop.Editor
{
    [InitializeOnLoad]
    public class Phase10Setup
    {
        private const string PrefKey = "VRRemoteDesktop_Phase10_SetupDone";

        static Phase10Setup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!SessionState.GetBool(PrefKey, false))
                {
                    SessionState.SetBool(PrefKey, true);
                    SetupPhase10And11();
                }
            };
        }

        [MenuItem("VR Remote Desktop/Setup Phase 10 & 11 (Networking, Audio, Debug Panel)")]
        public static void SetupPhase10And11()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.name != "Main")
            {
                if (File.Exists("Assets/Scenes/Main.unity"))
                {
                    scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
                }
            }

            Debug.Log("[Phase10Setup] Setting up Phase 10 & 11 Networking, Audio, and Debug systems...");

            // 1. Locate or create Systems container
            GameObject systemsRoot = GameObject.Find("CoreSystems");
            if (systemsRoot == null)
            {
                systemsRoot = new GameObject("CoreSystems");
            }

            // 2. Ensure RemoteDesktopManager has Screen and Input providers
            RemoteDesktopManager rdm = Object.FindAnyObjectByType<RemoteDesktopManager>();
            if (rdm == null)
            {
                GameObject rdmObj = new GameObject("RemoteDesktopManager");
                rdmObj.transform.SetParent(systemsRoot.transform, false);
                rdm = rdmObj.AddComponent<RemoteDesktopManager>();
            }

            if (rdm.GetComponent<MockRemoteScreenProvider>() == null)
            {
                rdm.gameObject.AddComponent<MockRemoteScreenProvider>();
            }
            if (rdm.GetComponent<MockRemoteInputProvider>() == null)
            {
                rdm.gameObject.AddComponent<MockRemoteInputProvider>();
            }
            if (rdm.GetComponent<VRRemoteDesktop.Networking.NetworkRemoteScreenProvider>() == null)
            {
                rdm.gameObject.AddComponent<VRRemoteDesktop.Networking.NetworkRemoteScreenProvider>();
            }
            if (rdm.GetComponent<VRRemoteDesktop.Networking.NetworkRemoteInputProvider>() == null)
            {
                rdm.gameObject.AddComponent<VRRemoteDesktop.Networking.NetworkRemoteInputProvider>();
            }

            // 3. Ensure AudioManager exists
            AudioManager audioMgr = Object.FindAnyObjectByType<AudioManager>();
            if (audioMgr == null)
            {
                GameObject audioObj = new GameObject("AudioManager");
                audioObj.transform.SetParent(systemsRoot.transform, false);
                audioMgr = audioObj.AddComponent<AudioManager>();
            }

            // 4. Ensure DesktopDebugPanel exists
            DesktopDebugPanel debugPanel = Object.FindAnyObjectByType<DesktopDebugPanel>();
            if (debugPanel == null)
            {
                GameObject debugObj = new GameObject("DesktopDebugPanel");
                debugObj.transform.SetParent(systemsRoot.transform, false);
                debugPanel = debugObj.AddComponent<DesktopDebugPanel>();
            }

            // 5. Ensure all cameras (including UICamera) are Perspective for XR compatibility
            Camera[] allCameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include);
            foreach (var cam in allCameras)
            {
                if (cam != null && cam.orthographic)
                {
                    cam.orthographic = false;
                    cam.fieldOfView = 60f;
                    Debug.Log($"[Phase10Setup] Converted camera '{cam.name}' to Perspective for XR compatibility.");
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Phase10Setup] Phase 10 & 11 setup complete and scene saved successfully!");
        }
    }
}
