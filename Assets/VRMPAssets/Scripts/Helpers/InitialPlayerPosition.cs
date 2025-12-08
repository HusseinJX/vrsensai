using UnityEngine;
using Unity.XR.CoreUtils;

namespace XRMultiplayer
{
    /// <summary>
    /// Sets the initial player position very early in the game startup
    /// </summary>
    public class InitialPlayerPosition : MonoBehaviour
    {
        [SerializeField] Vector3 m_InitialPosition = new Vector3(0, 0f, 10.0f);
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void SetInitialPosition()
        {
            // This runs before the scene loads, so we need to do it in Start instead
        }
        
        void Awake()
        {
            SetPosition();
        }
        
        void Start()
        {
            SetPosition();
            // Also set it after a short delay to ensure XR Origin is ready
            Invoke(nameof(SetPosition), 0.1f);
        }
        
        void SetPosition()
        {
            var xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin != null)
            {
                xrOrigin.transform.position = m_InitialPosition;
                Debug.Log($"InitialPlayerPosition: Set XR Origin to {m_InitialPosition}");
            }
        }
    }
}


