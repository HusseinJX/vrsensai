using System.Collections.Generic;
using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Manages the battery shooting game mechanics:
    /// - Tracks ball count (4 max)
    /// - Monitors battery states
    /// - Resets batteries when all are knocked down or balls run out
    /// </summary>
    public class BatteryGameManager : MonoBehaviour
    {
        public static BatteryGameManager Instance { get; private set; }

        [Header("Game Settings")]
        [SerializeField] int m_MaxBalls = 4;
        [SerializeField] float m_BatteryKnockedDownThreshold = 0.5f; // Y position threshold to consider battery knocked down
        [SerializeField] float m_BatteryMovedThreshold = 0.2f; // Distance threshold to consider battery moved/disorganized
        [SerializeField] float m_BatteryRotationThreshold = 30f; // Rotation threshold (degrees) to consider battery disorganized
        [SerializeField] float m_ResetDelay = 2.0f; // Delay before resetting batteries
        [SerializeField] int m_BatteriesPerLevel = 2; // Additional batteries per level

        [Header("Battery References")]
        [SerializeField] Transform m_BatteryParent; // Parent object containing all batteries
        [SerializeField] string m_BatteryNamePrefix = "Battery Interactable"; // Name prefix to find batteries
        [SerializeField] GameObject m_BatteryPrefab; // Prefab to spawn new batteries (optional)

        private int m_CurrentBalls;
        private int m_CurrentLevel = 1;
        private int m_TargetBatteryCount = 0; // Batteries needed for current level
        private List<BatteryState> m_BatteryStates = new List<BatteryState>();
        private List<BatteryState> m_AllBatteries = new List<BatteryState>(); // All batteries found (including disabled)
        private bool m_IsResetting = false;
        private bool m_LevelCompleted = false; // Track if level was completed within ball limit

        // Events
        public System.Action<int> OnBallCountChanged;
        public System.Action OnBatteriesReset;
        public System.Action<int> OnLevelChanged;

        private class BatteryState
        {
            public Transform transform;
            public Vector3 initialPosition;
            public Quaternion initialRotation;
            public Vector3 initialScale;
            public Rigidbody rigidbody;
            public bool isKnockedDown;

            public BatteryState(Transform t)
            {
                transform = t;
                initialPosition = t.position;
                initialRotation = t.rotation;
                initialScale = t.localScale;
                rigidbody = t.GetComponentInChildren<Rigidbody>();
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Start()
        {
            m_CurrentBalls = m_MaxBalls;
            m_CurrentLevel = 1;
            FindAllBatteries();
            SetupLevel();
            OnBallCountChanged?.Invoke(m_CurrentBalls);
            OnLevelChanged?.Invoke(m_CurrentLevel);

            // Auto-add HUD if it doesn't exist
            if (FindFirstObjectByType<BatteryGameHUD>() == null)
            {
                gameObject.AddComponent<BatteryGameHUD>();
            }

            // Auto-add signboard if it doesn't exist
            if (FindFirstObjectByType<BatterySignboard>() == null)
            {
                gameObject.AddComponent<BatterySignboard>();
            }
        }

        void Update()
        {
            if (m_IsResetting) return;

            CheckBatteryStates();
            CheckGameEndConditions();
        }

        void FindAllBatteries()
        {
            m_AllBatteries.Clear();

            // Try to find battery parent if not assigned
            if (m_BatteryParent == null)
            {
                // Search for objects with "Battery" in the name
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains(m_BatteryNamePrefix))
                    {
                        if (m_BatteryParent == null)
                        {
                            // Use the first battery's parent as the parent
                            m_BatteryParent = obj.transform.parent;
                            break;
                        }
                    }
                }
            }

            // Find all batteries (including disabled ones)
            if (m_BatteryParent != null)
            {
                foreach (Transform child in m_BatteryParent)
                {
                    if (child.name.Contains(m_BatteryNamePrefix))
                    {
                        m_AllBatteries.Add(new BatteryState(child));
                    }
                }
            }
            else
            {
                // Fallback: search all objects in scene
                GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (GameObject obj in allObjects)
                {
                    if (obj.name.Contains(m_BatteryNamePrefix))
                    {
                        m_AllBatteries.Add(new BatteryState(obj.transform));
                    }
                }
            }

            Debug.Log($"BatteryGameManager: Found {m_AllBatteries.Count} total batteries.");
        }

        void SetupLevel()
        {
            // Calculate target battery count for this level
            m_TargetBatteryCount = m_CurrentLevel * m_BatteriesPerLevel;
            if (m_TargetBatteryCount > m_AllBatteries.Count)
            {
                m_TargetBatteryCount = m_AllBatteries.Count; // Don't exceed available batteries
            }

            // Activate only the batteries needed for this level
            m_BatteryStates.Clear();
            for (int i = 0; i < m_AllBatteries.Count; i++)
            {
                if (m_AllBatteries[i].transform == null) continue;
                
                if (i < m_TargetBatteryCount)
                {
                    // Activate this battery
                    m_AllBatteries[i].transform.gameObject.SetActive(true);
                    m_BatteryStates.Add(m_AllBatteries[i]);
                }
                else
                {
                    // Deactivate extra batteries
                    m_AllBatteries[i].transform.gameObject.SetActive(false);
                }
            }

            // Reset all active batteries to initial positions
            ResetBatteries();
            
            Debug.Log($"BatteryGameManager: Level {m_CurrentLevel} - {m_TargetBatteryCount} batteries active.");
        }

        void FindAndRegisterBatteries()
        {
            // Legacy method - now just updates active batteries
            m_BatteryStates.Clear();
            foreach (var battery in m_AllBatteries)
            {
                if (battery.transform != null && battery.transform.gameObject.activeSelf)
                {
                    m_BatteryStates.Add(battery);
                }
            }
        }

        void CheckBatteryStates()
        {
            foreach (var battery in m_BatteryStates)
            {
                if (battery.transform == null) continue;

                // Check if battery is knocked down (fallen below threshold or rotated significantly)
                bool wasKnockedDown = battery.isKnockedDown;
                battery.isKnockedDown = IsBatteryKnockedDown(battery);

                // If battery just got knocked down, we might want to do something
                if (battery.isKnockedDown && !wasKnockedDown)
                {
                    // Battery just fell
                }
            }
        }

        bool IsBatteryKnockedDown(BatteryState battery)
        {
            if (battery.transform == null) return false;

            // Check if position is significantly lower than initial (fallen off table)
            float heightDifference = battery.initialPosition.y - battery.transform.position.y;
            if (heightDifference > m_BatteryKnockedDownThreshold)
            {
                return true;
            }

            // Check if rotation is significantly different (fallen over)
            float angleDifference = Quaternion.Angle(battery.initialRotation, battery.transform.rotation);
            if (angleDifference > m_BatteryRotationThreshold) // More than threshold degrees rotated
            {
                return true;
            }

            // Check if battery has moved significantly from initial position (disorganized)
            float distanceFromInitial = Vector3.Distance(battery.transform.position, battery.initialPosition);
            if (distanceFromInitial > m_BatteryMovedThreshold)
            {
                return true;
            }

            return false;
        }

        void CheckGameEndConditions()
        {
            // Check if all batteries are knocked down or disorganized
            bool allKnockedDown = true;
            foreach (var battery in m_BatteryStates)
            {
                if (battery.transform == null || !battery.transform.gameObject.activeSelf) continue;
                if (!battery.isKnockedDown)
                {
                    allKnockedDown = false;
                    break;
                }
            }

            // If all batteries knocked down and still have balls, level complete!
            if (allKnockedDown && m_CurrentBalls > 0 && !m_IsResetting)
            {
                m_LevelCompleted = true;
                StartCoroutine(LevelCompleteCoroutine());
                return;
            }

            // Check if out of balls - reset when out of balls
            bool outOfBalls = m_CurrentBalls <= 0;
            
            if (outOfBalls && !m_IsResetting)
            {
                m_LevelCompleted = false;
                StartCoroutine(ResetBatteriesCoroutine());
            }
        }

        System.Collections.IEnumerator LevelCompleteCoroutine()
        {
            m_IsResetting = true;
            yield return new WaitForSeconds(m_ResetDelay);

            // Advance to next level
            m_CurrentLevel++;
            OnLevelChanged?.Invoke(m_CurrentLevel);
            
            // Setup new level with more batteries
            SetupLevel();
            
            // Reset ball count
            ResetBallCount();

            m_IsResetting = false;
            m_LevelCompleted = false;
            
            Debug.Log($"BatteryGameManager: Level {m_CurrentLevel - 1} complete! Starting level {m_CurrentLevel}.");
        }

        System.Collections.IEnumerator ResetBatteriesCoroutine()
        {
            m_IsResetting = true;
            yield return new WaitForSeconds(m_ResetDelay);

            // Reset to level 1 when player fails (runs out of balls without completing level)
            if (!m_LevelCompleted)
            {
                m_CurrentLevel = 1;
                OnLevelChanged?.Invoke(m_CurrentLevel);
                SetupLevel(); // Reset to level 1 with initial battery count
            }
            else
            {
                ResetBatteries();
            }
            
            // Wait a bit longer to ensure physics settles
            yield return new WaitForSeconds(0.1f);
            
            // Reset ball count
            ResetBallCount();

            m_IsResetting = false;
            m_LevelCompleted = false;
        }

        void ResetBatteries()
        {
            foreach (var battery in m_BatteryStates)
            {
                if (battery.transform == null) continue;

                // Make kinematic first to prevent physics interference during reset
                if (battery.rigidbody != null)
                {
                    // Stop all physics immediately
                    battery.rigidbody.isKinematic = true;
                    battery.rigidbody.linearVelocity = Vector3.zero;
                    battery.rigidbody.angularVelocity = Vector3.zero;
                    battery.rigidbody.Sleep(); // Put rigidbody to sleep
                }

                // Reset position, rotation, and scale using transform directly
                // This bypasses physics temporarily
                battery.transform.position = battery.initialPosition;
                battery.transform.rotation = battery.initialRotation;
                battery.transform.localScale = battery.initialScale;

                // Check if this is a NetworkPhysicsInteractable and use its reset method
                var networkPhysics = battery.transform.GetComponentInParent<NetworkPhysicsInteractable>();
                if (networkPhysics != null)
                {
                    networkPhysics.ResetObjectPhysics();
                }

                battery.isKnockedDown = false;
            }

            // Re-enable physics after a short delay to let everything settle
            StartCoroutine(ReEnablePhysicsAfterReset());
            
            OnBatteriesReset?.Invoke();
            Debug.Log("BatteryGameManager: Batteries reset to initial positions.");
        }

        System.Collections.IEnumerator ReEnablePhysicsAfterReset()
        {
            // Wait a frame to ensure transforms are set
            yield return null;
            
            // Wait a bit more for physics to settle
            yield return new WaitForSeconds(0.05f);

            foreach (var battery in m_BatteryStates)
            {
                if (battery.transform == null) continue;
                if (battery.rigidbody != null)
                {
                    // Wake up the rigidbody and re-enable physics
                    battery.rigidbody.WakeUp();
                    battery.rigidbody.isKinematic = false;
                }
            }
        }

        void ResetBallCount()
        {
            m_CurrentBalls = m_MaxBalls;
            OnBallCountChanged?.Invoke(m_CurrentBalls);
        }

        public bool CanShoot()
        {
            return m_CurrentBalls > 0 && !m_IsResetting;
        }

        public bool TryShoot()
        {
            if (!CanShoot()) return false;

            m_CurrentBalls--;
            OnBallCountChanged?.Invoke(m_CurrentBalls);
            return true;
        }

        public int GetCurrentBalls()
        {
            return m_CurrentBalls;
        }

        public int GetMaxBalls()
        {
            return m_MaxBalls;
        }

        public int GetCurrentLevel()
        {
            return m_CurrentLevel;
        }
    }
}

