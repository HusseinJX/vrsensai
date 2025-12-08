using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Displays the ball count in the top left of the screen
    /// </summary>
    public class BatteryGameHUD : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] TMP_Text m_BallCountText;
        [SerializeField] string m_BallCountFormat = "Balls: {0}/{1}<br>Level: {2}"; // Combined format with level (using <br> for TextMeshPro)

        private BatteryGameManager m_GameManager;

        void Start()
        {
            // Try to find text component first
            if (m_BallCountText == null)
            {
                // Try to find by name
                Canvas canvas = FindFirstObjectByType<Canvas>();
                if (canvas != null)
                {
                    Transform ballText = canvas.transform.Find("BallCountText");
                    if (ballText != null)
                    {
                        m_BallCountText = ballText.GetComponent<TMP_Text>();
                    }
                }
                
                // Fallback: find any TMP_Text
                if (m_BallCountText == null)
                {
                    m_BallCountText = GetComponentInChildren<TMP_Text>();
                }
            }

            // Create UI if it doesn't exist
            if (m_BallCountText == null)
            {
                CreateHUDUI();
            }
            
            // Subscribe to events after UI is created
            m_GameManager = BatteryGameManager.Instance;
            
            if (m_GameManager != null)
            {
                m_GameManager.OnBallCountChanged += UpdateBallCount;
                m_GameManager.OnLevelChanged += UpdateBallCount; // Update ball count when level changes too
                // Update with current values
                int currentBalls = m_GameManager.GetCurrentBalls();
                int level = m_GameManager.GetCurrentLevel();
                if (m_BallCountText != null)
                {
                    m_BallCountText.text = string.Format(m_BallCountFormat, currentBalls, m_GameManager.GetMaxBalls(), level);
                    Debug.Log($"BatteryGameHUD: Initialized with Balls: {currentBalls}, Level: {level}");
                }
                UpdateBallCount(currentBalls);
            }
            else
            {
                Debug.LogWarning("BatteryGameHUD: BatteryGameManager not found!");
            }
        }

        void CreateHUDUI()
        {
            // Find or create Canvas
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("HUD Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            }

            // Create ball count text object in top left (will also show level)
            GameObject textObj = new GameObject("BallCountText");
            
            if (canvas != null && canvas.transform != null)
            {
                textObj.transform.SetParent(canvas.transform, false);
            }
            
            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -10);
            rectTransform.sizeDelta = new Vector2(200, 80); // Taller to fit both lines

            m_BallCountText = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            
            if (m_BallCountText != null)
            {
                // Set default font from TMP Settings
                if (TMPro.TMP_Settings.defaultFontAsset != null)
                {
                    m_BallCountText.font = TMPro.TMP_Settings.defaultFontAsset;
                }
                else
                {
                    // Fallback: try to find any TMP font asset
                    var fonts = Resources.FindObjectsOfTypeAll<TMPro.TMP_FontAsset>();
                    if (fonts != null && fonts.Length > 0)
                    {
                        m_BallCountText.font = fonts[0];
                    }
                }
                
                m_BallCountText.text = "Balls: 4/4<br>Level: 1";
                m_BallCountText.fontSize = 72;
                m_BallCountText.color = Color.black;
                m_BallCountText.alignment = TextAlignmentOptions.TopLeft;
                m_BallCountText.enableWordWrapping = false; // Don't wrap, allow explicit line breaks
                m_BallCountText.overflowMode = TextOverflowModes.Overflow; // Allow text to overflow if needed
            }
            else
            {
                Debug.LogError("BatteryGameHUD: Failed to add TMP_Text component! TextMeshPro might not be imported.");
                // Fallback to Unity Text component
                var unityText = textObj.AddComponent<UnityEngine.UI.Text>();
                if (unityText != null)
                {
                    unityText.text = "Balls: 4/4\nLevel: 1"; // Unity Text uses \n
                    unityText.fontSize = 72;
                    unityText.color = Color.black;
                    unityText.alignment = TextAnchor.UpperLeft;
                }
            }
        }

        void OnDestroy()
        {
            if (m_GameManager != null)
            {
                m_GameManager.OnBallCountChanged -= UpdateBallCount;
                m_GameManager.OnLevelChanged -= UpdateBallCount;
            }
        }

        void UpdateBallCount(int currentBalls)
        {
            if (m_BallCountText != null && m_GameManager != null)
            {
                int level = m_GameManager.GetCurrentLevel();
                string text = string.Format(m_BallCountFormat, currentBalls, m_GameManager.GetMaxBalls(), level);
                m_BallCountText.text = text;
                Debug.Log($"BatteryGameHUD: Updated text to '{text}' (Balls: {currentBalls}, Level: {level})");
            }
            else
            {
                Debug.LogWarning($"BatteryGameHUD: Cannot update - Text: {m_BallCountText != null}, Manager: {m_GameManager != null}");
            }
        }
    }
}

