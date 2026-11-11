using UnityEngine;

namespace VRRemoteDesktop.Computer
{
    /// <summary>
    /// Represents the physical virtual monitor screen in VR/Desktop mode.
    /// This component abstracts the rendering of the screen so that it can display:
    /// - Local UI (RenderTexture)
    /// - Test textures / images
    /// - Future remote streaming video frames
    /// Also manages screen properties: resolution, aspect ratio, brightness, curvature, and scaling.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    public class RemoteDesktopScreen : MonoBehaviour
    {
        [Header("Stream / Display Source")]
        [Tooltip("Default texture to display when no active stream is connected.")]
        [SerializeField] private Texture _defaultTexture;

        [Header("Display Properties")]
        [Tooltip("Simulated native screen resolution.")]
        [SerializeField] private Vector2Int _resolution = new Vector2Int(1920, 1080);

        [Tooltip("Screen brightness multiplier (0.0 - 2.0).")]
        [Range(0.0f, 2.0f)]
        [SerializeField] private float _brightness = 1.0f;

        [Tooltip("Screen aspect ratio width divided by height (e.g. 1.777 for 16:9).")]
        [SerializeField] private float _aspectRatio = 16f / 9f;

        [Tooltip("Screen scale multiplier for width and height.")]
        [SerializeField] private Vector2 _screenScale = Vector2.one;

        [Tooltip("Target refresh rate in Hz.")]
        [SerializeField] private int _targetRefreshRate = 60;

        [Tooltip("Screen curvature factor (0.0 = flat, 0.1 - 0.5 = curved).")]
        [Range(0.0f, 0.5f)]
        [SerializeField] private float _curvature = 0.0f;

        private Renderer _screenRenderer;
        private MaterialPropertyBlock _propBlock;
        private Texture _currentTexture;

        public Vector2Int Resolution => _resolution;
        public float AspectRatio => _aspectRatio;
        public float Brightness => _brightness;
        public float Curvature => _curvature;
        public Vector2 ScreenScale => _screenScale;
        public int TargetRefreshRate => _targetRefreshRate;
        public Texture CurrentTexture => _currentTexture;

        private void Awake()
        {
            _screenRenderer = GetComponent<Renderer>();
            _propBlock = new MaterialPropertyBlock();

            if (_defaultTexture != null)
            {
                SetTexture(_defaultTexture);
            }
            else
            {
                ApplyDisplayProperties();
            }
        }

        /// <summary>
        /// Feeds a new frame/texture into the virtual screen (supports Texture2D, RenderTexture, or WebCamTexture).
        /// </summary>
        public void SetTexture(Texture texture)
        {
            _currentTexture = texture;
            if (_screenRenderer == null) _screenRenderer = GetComponent<Renderer>();
            if (_screenRenderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _screenRenderer.GetPropertyBlock(_propBlock);

            Texture texToSet = texture != null ? texture : Texture2D.blackTexture;
            _propBlock.SetTexture("_BaseMap", texToSet);
            _propBlock.SetTexture("_EmissionMap", texToSet);

            Color emissionColor = Color.white * _brightness;
            _propBlock.SetColor("_EmissionColor", emissionColor);

            _screenRenderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Adjusts the screen brightness in real-time.
        /// </summary>
        public void SetBrightness(float brightness)
        {
            _brightness = Mathf.Clamp(brightness, 0.0f, 2.0f);
            if (_currentTexture != null)
            {
                SetTexture(_currentTexture);
            }
        }

        /// <summary>
        /// Updates the reported screen resolution.
        /// </summary>
        public void SetResolution(int width, int height)
        {
            if (width <= 0 || height <= 0) return;
            _resolution = new Vector2Int(width, height);
            _aspectRatio = (float)width / height;
        }

        /// <summary>
        /// Sets screen curvature factor (for future curved shader / mesh warping).
        /// </summary>
        public void SetCurvature(float curvature)
        {
            _curvature = Mathf.Clamp(curvature, 0.0f, 0.5f);
        }

        /// <summary>
        /// Sets physical screen scale on the frame/quad.
        /// </summary>
        public void SetScreenScale(Vector2 scale)
        {
            _screenScale = scale;
            transform.localScale = new Vector3(scale.x, scale.y, 1f);
        }

        /// <summary>
        /// Refreshes all display properties on the material block.
        /// </summary>
        public void ApplyDisplayProperties()
        {
            if (_screenRenderer == null) _screenRenderer = GetComponent<Renderer>();
            if (_screenRenderer == null) return;
            if (_propBlock == null) _propBlock = new MaterialPropertyBlock();

            _screenRenderer.GetPropertyBlock(_propBlock);
            Color emissionColor = Color.white * _brightness;
            _propBlock.SetColor("_EmissionColor", emissionColor);
            _screenRenderer.SetPropertyBlock(_propBlock);
        }

        /// <summary>
        /// Clears the screen (displays pure black).
        /// </summary>
        public void ClearScreen()
        {
            _currentTexture = null;
            if (_screenRenderer == null) return;

            _screenRenderer.GetPropertyBlock(_propBlock);
            _propBlock.SetTexture("_BaseMap", Texture2D.blackTexture);
            _propBlock.SetTexture("_EmissionMap", Texture2D.blackTexture);
            _screenRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
