using UnityEngine;
using UnityEngine.XR.Templates.VRMultiplayer;

namespace XRMultiplayer
{
    /// <summary>
    /// Shows a red dot crosshair for aiming
    /// - PC: Center of screen
    /// - VR: Where controller is pointing
    /// </summary>
    public class AimDot : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] float m_DotSize = 0.01f; // Size in world space for VR
        [SerializeField] float m_ScreenDotSize = 10f; // Size in pixels for screen space
        [SerializeField] Color m_DotColor = Color.red;
        
        [Header("References")]
        [SerializeField] Transform m_AimPoint; // Controller or camera
        [SerializeField] Camera m_Camera;
        
        private GameObject m_DotObject;
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor m_VRInteractor;
        private bool m_IsDesktop = false;
        private UnityEngine.UI.Image m_ScreenDot; // For screen space UI
        private RectTransform m_ScreenDotRect;

        void Start()
        {
            // Detect platform
            m_IsDesktop = XRPlatformUnderstanding.CurrentPlatform == XRPlatformType.Desktop;
            
            if (m_IsDesktop)
            {
                CreateScreenSpaceDot();
            }
            else
            {
                CreateWorldSpaceDot();
            }
        }

        void CreateScreenSpaceDot()
        {
            // Create canvas for screen space dot
            GameObject canvasObj = new GameObject("AimDotCanvas");
            canvasObj.transform.SetParent(transform);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // On top of everything
            
            // Create dot image
            GameObject dotObj = new GameObject("AimDot");
            dotObj.transform.SetParent(canvasObj.transform, false);
            
            m_ScreenDotRect = dotObj.AddComponent<RectTransform>();
            m_ScreenDotRect.anchorMin = new Vector2(0.5f, 0.5f);
            m_ScreenDotRect.anchorMax = new Vector2(0.5f, 0.5f);
            m_ScreenDotRect.pivot = new Vector2(0.5f, 0.5f);
            m_ScreenDotRect.anchoredPosition = Vector2.zero;
            m_ScreenDotRect.sizeDelta = new Vector2(m_ScreenDotSize, m_ScreenDotSize);
            
            m_ScreenDot = dotObj.AddComponent<UnityEngine.UI.Image>();
            m_ScreenDot.color = m_DotColor;
            
            // Create circular sprite for the dot
            Texture2D tex = new Texture2D(64, 64);
            Color[] colors = new Color[64 * 64];
            Vector2 center = new Vector2(32, 32);
            float radius = 30f;
            
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), center);
                    colors[y * 64 + x] = dist < radius ? m_DotColor : Color.clear;
                }
            }
            tex.SetPixels(colors);
            tex.Apply();
            
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f));
            m_ScreenDot.sprite = sprite;
        }

        void CreateWorldSpaceDot()
        {
            // Create a small sphere for VR world space dot
            m_DotObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            m_DotObject.name = "AimDot";
            m_DotObject.transform.SetParent(transform);
            m_DotObject.transform.localScale = Vector3.one * m_DotSize;
            
            // Remove collider
            Collider col = m_DotObject.GetComponent<Collider>();
            if (col != null)
            {
                Destroy(col);
            }
            
            // Set material/color
            Renderer renderer = m_DotObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = m_DotColor;
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_Glossiness", 0f);
                mat.SetFloat("_Mode", 3); // Transparent mode
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_ALPHABLEND_ON");
                mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                mat.renderQueue = 3000;
                renderer.material = mat;
            }
            
            m_DotObject.SetActive(false);
            
            // Find VR interactor
            m_VRInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
        }

        void Update()
        {
            if (m_IsDesktop)
            {
                UpdateScreenSpaceDot();
            }
            else
            {
                UpdateWorldSpaceDot();
            }
        }

        void UpdateScreenSpaceDot()
        {
            // Screen space dot is always visible at center
            if (m_ScreenDot != null)
            {
                m_ScreenDot.enabled = true;
            }
        }

        void UpdateWorldSpaceDot()
        {
            if (m_DotObject == null) return;

            // Find where controller is pointing
            Vector3 aimPosition = Vector3.zero;
            bool shouldShow = false;

            // Try to get aim point from VR interactor
            if (m_VRInteractor != null && m_VRInteractor.transform != null)
            {
                Vector3 startPos = m_VRInteractor.transform.position;
                Vector3 direction = m_VRInteractor.transform.forward;
                
                // Raycast to find where controller is pointing
                if (Physics.Raycast(startPos, direction, out RaycastHit hit, 100f))
                {
                    aimPosition = hit.point;
                    shouldShow = true;
                }
                else
                {
                    // Show dot at max distance if nothing hit
                    aimPosition = startPos + direction * 10f;
                    shouldShow = true;
                }
            }
            else if (m_AimPoint != null)
            {
                Vector3 startPos = m_AimPoint.position;
                Vector3 direction = m_AimPoint.forward;
                
                if (Physics.Raycast(startPos, direction, out RaycastHit hit, 100f))
                {
                    aimPosition = hit.point;
                    shouldShow = true;
                }
                else
                {
                    aimPosition = startPos + direction * 10f;
                    shouldShow = true;
                }
            }

            if (shouldShow)
            {
                m_DotObject.SetActive(true);
                m_DotObject.transform.position = aimPosition;
                
                // Make dot face camera
                if (m_Camera == null)
                {
                    m_Camera = Camera.main;
                }
                if (m_Camera != null)
                {
                    m_DotObject.transform.LookAt(m_Camera.transform);
                }
            }
            else
            {
                m_DotObject.SetActive(false);
            }
        }

        public void SetAimPoint(Transform aimPoint)
        {
            m_AimPoint = aimPoint;
        }

        public void SetCamera(Camera camera)
        {
            m_Camera = camera;
        }
    }
}


