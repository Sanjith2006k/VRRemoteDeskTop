using UnityEngine;

namespace VRRemoteDesktop.Settings
{
    [CreateAssetMenu(fileName = "NewUITheme", menuName = "VR Remote Desktop/UI Theme")]
    public class UITheme : ScriptableObject
    {
        public Color BackgroundColor = new Color(0.05f, 0.08f, 0.12f); // Dark Navy
        public Color PrimaryColor = new Color(0f, 1f, 1f); // Cyan
        public Color SecondaryColor = new Color(0.5f, 0f, 1f); // Violet
        public Color AccentColor = new Color(1f, 0f, 0.5f); // Magenta
        
        public Color SuccessColor = new Color(0.2f, 0.8f, 0.2f);
        public Color WarningColor = new Color(1f, 0.6f, 0f);
        public Color ErrorColor = new Color(1f, 0.2f, 0.2f);
        
        public Color TextPrimary = Color.white;
        public Color TextSecondary = new Color(0.7f, 0.7f, 0.7f);
    }
}
