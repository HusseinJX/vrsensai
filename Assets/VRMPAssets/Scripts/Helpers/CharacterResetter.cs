using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace XRMultiplayer
{
    public class CharacterResetter : MonoBehaviour
    {
        [SerializeField] Vector2 m_MinMaxHeight = new Vector2(-2.5f, 25.0f);
        [SerializeField] float m_ResetDistance = 75.0f;
        [SerializeField] Vector3 offlinePosition = new Vector3(0, 0f, 8.0f); // Slightly less forward
        [SerializeField] Vector3 onlinePosition = new Vector3(0, 0f, 8.0f); // Slightly less forward
        [SerializeField] bool m_PreserveYPosition = true; // Don't change Y axis, preserve current height
        TeleportationProvider m_TeleportationProvider;
        Vector3 m_ResetPosition;
        private bool m_HasSetInitialPosition = false;
        
        private void Awake()
        {
            // Don't set position in Awake - let XRINetworkGameManager handle initial position
            m_ResetPosition = offlinePosition;
        }
        
        private void Start()
        {
            XRINetworkGameManager.Connected.Subscribe(UpdateResetPosition);
            m_TeleportationProvider = GetComponentInChildren<TeleportationProvider>();

            m_ResetPosition = offlinePosition;
            
            // Only set position if we haven't already set it, and wait a bit for other components
            StartCoroutine(DelayedInitialCheck());
        }
        
        System.Collections.IEnumerator DelayedInitialCheck()
        {
            // Wait for all components to initialize (signboard, HUD, etc.)
            yield return new WaitForSeconds(0.6f);
            
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                Vector3 currentPos = xrOrigin.transform.position;
                // Only reset if we're too far back (outside the door area)
                if (currentPos.z < 5.0f && !m_HasSetInitialPosition)
                {
                    SetPositionDirectly(offlinePosition);
                    m_HasSetInitialPosition = true;
                    Debug.Log($"CharacterResetter: Set initial forward position after component initialization");
                }
            }
        }
        
        void SetPositionDirectly(Vector3 position)
        {
            // Try multiple ways to find and set the XR Origin position
            var xrOrigin = FindFirstObjectByType<Unity.XR.CoreUtils.XROrigin>();
            if (xrOrigin != null)
            {
                // If preserving Y, only change X and Z
                if (m_PreserveYPosition)
                {
                    Vector3 currentPos = xrOrigin.transform.position;
                    position = new Vector3(position.x, currentPos.y, position.z);
                }
                
                xrOrigin.transform.position = position;
                // Don't modify CameraFloorOffsetObject position as it's relative to the origin
                Debug.Log($"CharacterResetter: Set XR Origin position directly to {position}");
            }
            else
            {
                // Fallback: set this object's position if it's the XR Origin
                if (GetComponent<Unity.XR.CoreUtils.XROrigin>() != null)
                {
                    if (m_PreserveYPosition)
                    {
                        Vector3 currentPos = transform.position;
                        position = new Vector3(position.x, currentPos.y, position.z);
                    }
                    transform.position = position;
                    Debug.Log($"CharacterResetter: Set own position (XR Origin) to {position}");
                }
                else
                {
                    Debug.LogWarning("CharacterResetter: Could not find XR Origin to set position!");
                }
            }
        }

        void UpdateResetPosition(bool connected)
        {
            if (connected)
            {
                m_ResetPosition = onlinePosition;
            }
            else
            {
                m_ResetPosition = offlinePosition;
                ResetPlayer();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (transform.position.y < m_MinMaxHeight.x)
            {
                ResetPlayer();
            }
            else if (transform.position.y > m_MinMaxHeight.y)
            {
                ResetPlayer();
            }
            if (Mathf.Abs(transform.position.x) > m_ResetDistance || Mathf.Abs(transform.position.z) > m_ResetDistance)
            {
                ResetPlayer();
            }
        }

        public void ResetPlayer()
        {
            ResetPlayer(m_ResetPosition);
        }

        void ResetPlayer(Vector3 destination)
        {
            // First, set position directly to ensure it happens
            SetPositionDirectly(destination);
            
            // Then try teleportation if provider is available
            if (m_TeleportationProvider != null)
            {
                TeleportRequest teleportRequest = new()
                {
                    destinationPosition = destination,
                    destinationRotation = Quaternion.identity
                };

                if (!m_TeleportationProvider.QueueTeleportRequest(teleportRequest))
                {
                    Utils.LogWarning("Failed to queue teleport request, but position was set directly");
                }
            }
        }

        [ContextMenu("Set Player To Online Position")]
        void SetPlayerToOnlinePosition()
        {
            ResetPlayer(onlinePosition);
        }

        [ContextMenu("Set Player To Offline Position")]
        void SetPlayerToOfflinePosition()
        {
            ResetPlayer(offlinePosition);
        }
    }
}
