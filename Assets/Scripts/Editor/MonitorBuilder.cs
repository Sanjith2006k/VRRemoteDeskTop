using UnityEngine;
using UnityEditor;
using VRRemoteDesktop.Computer;

public class MonitorBuilder : EditorWindow
{
    [MenuItem("VR Remote Desktop/Build Phase 3 Monitor")]
    public static void BuildMonitor()
    {
        CreateScreenMaterial();
        GameObject monitorPrefab = BuildMonitorHierarchy();
        
        // Save as prefab
        if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Computer"))
        {
            AssetDatabase.CreateFolder("Assets/Prefabs", "Computer");
        }
        
        string prefabPath = "Assets/Prefabs/Computer/VirtualMonitor.prefab";
        PrefabUtility.SaveAsPrefabAssetAndConnect(monitorPrefab, prefabPath, InteractionMode.UserAction);
        
        Debug.Log("Phase 3 Monitor Built and saved to " + prefabPath);
    }

    private static void CreateScreenMaterial()
    {
        string path = "Assets/Materials/Mat_VirtualScreen.mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) == null)
        {
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            mat.SetColor("_BaseColor", Color.white); // The texture will drive the color
            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
        }
    }

    private static GameObject BuildMonitorHierarchy()
    {
        Material matCharcoal = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_DarkCharcoal.mat");
        Material matScreen = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/Mat_VirtualScreen.mat");

        // Try to place it on the desk if it exists, otherwise origin
        Vector3 spawnPos = new Vector3(0, 1.2f, 0.6f); 
        GameObject desk = GameObject.Find("ComputerDesk");
        if (desk != null) spawnPos = desk.transform.position + new Vector3(0, 0.8f, 0);

        GameObject monitorRoot = new GameObject("VirtualMonitor");
        monitorRoot.transform.position = spawnPos;

        // Monitor Stand Base
        GameObject standBase = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standBase.name = "MonitorStand_Base";
        standBase.transform.SetParent(monitorRoot.transform);
        standBase.transform.localPosition = new Vector3(0, 0.01f, 0);
        standBase.transform.localScale = new Vector3(0.4f, 0.02f, 0.2f);
        if (matCharcoal != null) standBase.GetComponent<Renderer>().sharedMaterial = matCharcoal;

        // Monitor Stand Neck
        GameObject standNeck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        standNeck.name = "MonitorStand_Neck";
        standNeck.transform.SetParent(monitorRoot.transform);
        standNeck.transform.localPosition = new Vector3(0, 0.15f, 0.08f); // Moved back to prevent clipping
        standNeck.transform.localRotation = Quaternion.Euler(15f, 0, 0);
        standNeck.transform.localScale = new Vector3(0.08f, 0.3f, 0.04f);
        if (matCharcoal != null) standNeck.GetComponent<Renderer>().sharedMaterial = matCharcoal;

        // Monitor Frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = "MonitorFrame";
        frame.transform.SetParent(monitorRoot.transform);
        frame.transform.localPosition = new Vector3(0, 0.3f, 0.02f);
        frame.transform.localScale = new Vector3(1.2f, 0.7f, 0.05f);
        if (matCharcoal != null) frame.GetComponent<Renderer>().sharedMaterial = matCharcoal;

        // Screen display surface
        GameObject screen = GameObject.CreatePrimitive(PrimitiveType.Quad);
        screen.name = "Screen";
        screen.transform.SetParent(frame.transform);
        // Positioned slightly in front of the frame
        screen.transform.localPosition = new Vector3(0, 0, -0.51f);
        // Scale relative to the frame (which is 1.2 x 0.7)
        // Quad default size is 1x1, so we want it to almost cover the frame minus a bezel.
        // Bezel = 0.05 on all sides.
        float bezel = 0.02f;
        screen.transform.localScale = new Vector3(1f - bezel, 1f - bezel, 1f);
        if (matScreen != null) screen.GetComponent<Renderer>().sharedMaterial = matScreen;

        // Add the RemoteDesktopScreen script
        screen.AddComponent<RemoteDesktopScreen>();
        
        // Phase 5: Add XR Interaction script
        screen.AddComponent<VRRemoteDesktop.XR.VRDesktopInteractable>();

        // Destroy colliders on visual parts except screen if needed (screen needs it for raycasts later)
        DestroyImmediate(standBase.GetComponent<Collider>());
        DestroyImmediate(standNeck.GetComponent<Collider>());
        // Keep frame collider for physical presence
        
        // Put the monitor in the Workstation hierarchy if it exists
        GameObject workParent = GameObject.Find("Workstation");
        if (workParent != null) monitorRoot.transform.SetParent(workParent.transform);

        return monitorRoot;
    }
}
