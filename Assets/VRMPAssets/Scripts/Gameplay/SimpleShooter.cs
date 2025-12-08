using UnityEngine;

namespace XRMultiplayer
{
    public class SimpleShooter : MonoBehaviour
    {
        [SerializeField] GameObject m_ProjectilePrefab;
        [SerializeField] Transform m_FirePoint;
        [SerializeField] float m_ShootForce = 20.0f;
        [SerializeField] Color m_PlayerColor = Color.cyan;
        [SerializeField] bool m_UseCameraForFirePoint = false;
        [SerializeField] float m_DesktopAimDownAngle = 5f; // Angle to aim down on desktop

        Camera m_Camera;

        void Awake()
        {
            if (m_FirePoint == null)
            {
                m_FirePoint = transform;
            }
            
            // Try to find camera if using camera for fire point
            if (m_UseCameraForFirePoint)
            {
                m_Camera = Camera.main;
                if (m_Camera == null)
                {
                    m_Camera = FindFirstObjectByType<Camera>();
                }
            }
        }

        public void SetProjectilePrefab(GameObject prefab)
        {
            m_ProjectilePrefab = prefab;
        }

        public void SetFirePoint(Transform firePoint)
        {
            m_FirePoint = firePoint;
        }

        public void SetUseCameraForFirePoint(bool useCamera)
        {
            m_UseCameraForFirePoint = useCamera;
            if (m_UseCameraForFirePoint && m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera == null)
                {
                    m_Camera = FindFirstObjectByType<Camera>();
                }
            }
        }

        public float GetShootForce()
        {
            return m_ShootForce;
        }

        public void Shoot()
        {
            // Check if we can shoot (ball count limit)
            if (BatteryGameManager.Instance != null && !BatteryGameManager.Instance.CanShoot())
            {
                return; // Out of balls or game is resetting
            }

            if (m_ProjectilePrefab == null)
            {
                // Fallback load if not assigned
                m_ProjectilePrefab = Resources.Load<GameObject>("SphereProjectile"); 
                // Note: User might not have it in Resources. 
                // I will try to find it in Start() or via Manager if this fails.
            }

            if (m_ProjectilePrefab != null)
            {
                // Determine fire point position and rotation
                Vector3 firePosition;
                Quaternion fireRotation;
                
                if (m_UseCameraForFirePoint && m_Camera != null)
                {
                    // Use camera position and forward direction for shooting
                    firePosition = m_Camera.transform.position;
                    // Apply downward angle offset for desktop to fix aim being too high
                    fireRotation = m_Camera.transform.rotation * Quaternion.Euler(-m_DesktopAimDownAngle, 0, 0);
                }
                else
                {
                    // Use the assigned fire point
                    firePosition = m_FirePoint.position;
                    fireRotation = m_FirePoint.rotation;
                }

                GameObject projObj = Instantiate(m_ProjectilePrefab, firePosition, fireRotation);
                
                Rigidbody rb = projObj.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = fireRotation * Vector3.forward * m_ShootForce;
                }

                Projectile projScript = projObj.GetComponent<Projectile>();
                if (projScript != null)
                {
                    // No pooling logic for simple shooter for now, just destroy handling in Projectile script
                    projScript.Setup(true, m_PlayerColor, null);
                }

                // Consume a ball
                if (BatteryGameManager.Instance != null)
                {
                    BatteryGameManager.Instance.TryShoot();
                }
            }
            else
            {
                Debug.LogWarning("SimpleShooter: No Projectile Prefab assigned!");
            }
        }
    }
}
