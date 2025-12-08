using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Creates a world-space signboard showing the number of balls left
    /// </summary>
    public class BatterySignboard : MonoBehaviour
    {
        [Header("Signboard Settings")]
        [SerializeField] float m_SignboardHeight = 2.0f; // Height above battery table
        [SerializeField] float m_SignboardDistance = 1.5f; // Distance from battery table center
        [SerializeField] Vector2 m_SignboardSize = new Vector2(0.3f, 0.15f); // Made smaller
        [SerializeField] float m_FontSize = 0.2f;
        [SerializeField] Color m_TextColor = Color.black;
        [SerializeField] Color m_BackgroundColor = new Color(0, 0, 0, 0.7f);

        private GameObject m_SignboardObject;
        private TMP_Text m_TextComponent;
        private BatteryGameManager m_GameManager;

        void Start()
        {
            // Wait a frame to ensure BatteryGameManager is initialized
            StartCoroutine(InitializeSignboard());
        }

        System.Collections.IEnumerator InitializeSignboard()
        {
            yield return null; // Wait one frame
            
            m_GameManager = BatteryGameManager.Instance;
            
            if (m_GameManager != null)
            {
                m_GameManager.OnBallCountChanged += UpdateSignboard;
                CreateSignboard();
                
                // Update with current ball count
                int currentBalls = m_GameManager.GetCurrentBalls();
                UpdateSignboard(currentBalls);
                Debug.Log($"BatterySignboard: Initialized with {currentBalls} balls");
            }
            else
            {
                Debug.LogWarning("BatterySignboard: BatteryGameManager not found! Retrying...");
                // Retry after a short delay
                yield return new WaitForSeconds(0.5f);
                m_GameManager = BatteryGameManager.Instance;
                if (m_GameManager != null)
                {
                    m_GameManager.OnBallCountChanged += UpdateSignboard;
                    CreateSignboard();
                    UpdateSignboard(m_GameManager.GetCurrentBalls());
                }
            }
        }

        void OnDestroy()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnBallCountChanged -= UpdateSignboard;
            }
        }

        void CreateSignboard()
        {
            // Find battery parent to position signboard near it
            Transform batteryParent = null;
            
            // Search for battery parent
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                if (obj.name.Contains("Battery Interactable"))
                {
                    batteryParent = obj.transform.parent;
                    break;
                }
            }

            // Calculate position
            Vector3 signboardPosition;
            if (batteryParent != null)
            {
                // Position signboard next to the battery table
                Vector3 tableCenter = batteryParent.position;
                // Find average position of all batteries for better centering
                int batteryCount = 0;
                Vector3 avgPosition = Vector3.zero;
                foreach (Transform child in batteryParent)
                {
                    if (child.name.Contains("Battery Interactable"))
                    {
                        avgPosition += child.position;
                        batteryCount++;
                    }
                }
                if (batteryCount > 0)
                {
                    tableCenter = avgPosition / batteryCount;
                }

                // Position signboard to the side and above the table
                signboardPosition = tableCenter + Vector3.up * m_SignboardHeight + Vector3.right * m_SignboardDistance;
            }
            else
            {
                // Default position if we can't find batteries
                signboardPosition = new Vector3(0, 2, 0);
            }

            // Create canvas for world space UI
            GameObject canvasObj = new GameObject("BatterySignboardCanvas");
            canvasObj.transform.position = signboardPosition;
            
            // Face the camera if available, otherwise use default forward
            if (Camera.main != null)
            {
                canvasObj.transform.LookAt(Camera.main.transform);
                canvasObj.transform.Rotate(0, 180, 0); // Face the camera
            }
            else
            {
                // Default rotation if no camera
                canvasObj.transform.rotation = Quaternion.identity;
            }

            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            
            // Set world camera only if it exists
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            if (mainCamera != null)
            {
                canvas.worldCamera = mainCamera;
            }
            else
            {
                Debug.LogWarning("BatterySignboard: No camera found for world space canvas!");
            }
            
            RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
            canvasRect.sizeDelta = m_SignboardSize;
            // Set scale for world space - smaller scale for a smaller signboard
            // Flip X scale to fix mirroring
            canvasRect.localScale = new Vector3(-0.03f, 0.03f, 0.03f); // Negative X to fix mirroring

            // Add CanvasScaler for proper scaling (only needed for ScreenSpace canvases, but won't hurt)
            UnityEngine.UI.CanvasScaler scaler = canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1.0f;

            // Add GraphicRaycaster
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create background panel
            GameObject panelObj = new GameObject("Background");
            panelObj.transform.SetParent(canvasObj.transform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            panelRect.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
            panelImage.color = m_BackgroundColor;

            // Create text object
            GameObject textObj = new GameObject("BallCountText");
            textObj.transform.SetParent(canvasObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.anchoredPosition = Vector2.zero;

            // Use TextMeshProUGUI for UI text (works better than TMP_Text for canvas)
            m_TextComponent = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            
            if (m_TextComponent != null)
            {
                // Set default font from TMP Settings
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    m_TextComponent.font = TMPro.TMP_Settings.defaultFontAsset;
                    Debug.Log("BatterySignboard: Set default font from TMP_Settings");
                }
                else
                {
                    // Fallback: try to find any TMP font asset
                    var fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        m_TextComponent.font = fonts[0];
                        Debug.Log($"BatterySignboard: Set font from found assets: {fonts[0].name}");
                    }
                    else
                    {
                        Debug.LogWarning("BatterySignboard: No TMP font found! Text may not display.");
                    }
                }
                
                m_TextComponent.text = "Balls: 4/4";
                m_TextComponent.fontSize = 24; // Smaller font size
                m_TextComponent.color = m_TextColor;
                m_TextComponent.alignment = TextAlignmentOptions.Center;
                m_TextComponent.fontStyle = FontStyles.Bold;
                m_TextComponent.enableWordWrapping = false;
                
                Debug.Log($"BatterySignboard: Text component created successfully at position {signboardPosition}");
            }
            else
            {
                Debug.LogError("BatterySignboard: Failed to create TextMeshProUGUI component!");
            }

            m_SignboardObject = canvasObj;

            // Make signboard always face camera - Billboard component will handle this
            canvasObj.AddComponent<Billboard>();
        }

        void UpdateSignboard(int currentBalls)
        {
            // If text component doesn't exist yet, try to find it
            if (m_TextComponent == null && m_SignboardObject != null)
            {
                m_TextComponent = m_SignboardObject.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (m_TextComponent == null)
                {
                    // Also try TMP_Text as fallback
                    m_TextComponent = m_SignboardObject.GetComponentInChildren<TMP_Text>();
                }
                
                if (m_TextComponent != null)
                {
                    Debug.Log("BatterySignboard: Found text component in signboard object");
                }
            }
            
            if (m_TextComponent != null && m_GameManager != null)
            {
                string newText = $"Balls: {currentBalls}/{m_GameManager.GetMaxBalls()}";
                m_TextComponent.text = newText;
                Debug.Log($"BatterySignboard: Updated to {newText}");
            }
            else
            {
                // Don't log warning if signboard hasn't been created yet
                if (m_SignboardObject != null)
                {
                    Debug.LogWarning($"BatterySignboard: Cannot update - TextComponent: {m_TextComponent != null}, GameManager: {m_GameManager != null}, SignboardObject: {m_SignboardObject != null}");
                    // Try to recreate if component is missing
                    if (m_TextComponent == null)
                    {
                        Debug.LogWarning("BatterySignboard: Attempting to recreate text component...");
                        var textObj = m_SignboardObject.transform.Find("BallCountText");
                        if (textObj != null)
                        {
                            m_TextComponent = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                            if (m_TextComponent != null && m_GameManager != null)
                            {
                                string newText = $"Balls: {currentBalls}/{m_GameManager.GetMaxBalls()}";
                                m_TextComponent.text = newText;
                                Debug.Log($"BatterySignboard: Recovered and updated to {newText}");
                            }
                        }
                    }
                }
            }
        }
    }
}

