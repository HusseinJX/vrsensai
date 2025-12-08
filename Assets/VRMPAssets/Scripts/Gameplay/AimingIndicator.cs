using UnityEngine;

namespace XRMultiplayer
{
    /// <summary>
    /// Shows an aiming indicator (line/arrow) showing the initial direction and force of the shot
    /// </summary>
    public class AimingIndicator : MonoBehaviour
    {
        [Header("Visual Settings")]
        [SerializeField] LineRenderer m_LineRenderer;
        [SerializeField] float m_MaxLineLength = 10f;
        [SerializeField] int m_LineSegments = 20;
        [SerializeField] float m_LineWidth = 0.01f; // Made thinner
        [SerializeField] Color m_LineColor = Color.yellow;
        [SerializeField] bool m_ShowTrajectory = true;
        
        [Header("Aim Offset (Desktop)")]
        [SerializeField] float m_DesktopAimDownAngle = 5f; // Angle to aim down on desktop

        [Header("References")]
        [SerializeField] Transform m_AimPoint; // Where to aim from (camera or controller)
        [SerializeField] SimpleShooter m_Shooter;

        private Camera m_Camera;
        private bool m_IsActive = false;
        private UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor m_VRInteractor;

        void Start()
        {
            // Try to find camera if not assigned
            if (m_Camera == null)
            {
                m_Camera = Camera.main;
                if (m_Camera == null)
                {
                    m_Camera = FindFirstObjectByType<Camera>();
                }
            }

            // Try to find shooter if not assigned
            if (m_Shooter == null)
            {
                m_Shooter = GetComponent<SimpleShooter>();
                if (m_Shooter == null)
                {
                    m_Shooter = FindFirstObjectByType<SimpleShooter>();
                }
            }

            // Try to find VR interactor if not assigned
            if (m_VRInteractor == null)
            {
                m_VRInteractor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
            }

            // Create line renderer if not assigned
            if (m_LineRenderer == null)
            {
                GameObject lineObj = new GameObject("AimingLine");
                lineObj.transform.SetParent(transform);
                m_LineRenderer = lineObj.AddComponent<LineRenderer>();
                m_LineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                m_LineRenderer.startWidth = m_LineWidth;
                m_LineRenderer.endWidth = m_LineWidth * 0.3f; // Even thinner at end
                m_LineRenderer.useWorldSpace = true;
                m_LineRenderer.enabled = false;
            }

            // Set up line renderer
            m_LineRenderer.positionCount = m_LineSegments;
            m_LineRenderer.startColor = m_LineColor;
            m_LineRenderer.endColor = m_LineColor;
        }

        void Update()
        {
            // Show indicator when aiming (mouse button held or VR trigger held)
            bool isAiming = IsAiming();
            
            if (isAiming && !m_IsActive)
            {
                m_IsActive = true;
                m_LineRenderer.enabled = true;
            }
            else if (!isAiming && m_IsActive)
            {
                m_IsActive = false;
                m_LineRenderer.enabled = false;
            }

            if (m_IsActive)
            {
                UpdateAimingLine();
            }
        }

        bool IsAiming()
        {
            // Check mouse button (for desktop) - right click for aiming
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                // Right mouse button for aiming (left for shooting)
                if (UnityEngine.InputSystem.Mouse.current.rightButton.isPressed)
                {
                    return true;
                }
            }

            // Check VR trigger or grip (for Quest controllers)
            // Use cached interactor if available
            var interactor = m_VRInteractor;
            if (interactor == null)
            {
                interactor = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor>();
            }

            if (interactor != null)
            {
                // Check activate input (trigger) - partial press for aiming
                var activateInput = interactor.activateInput;
                if (activateInput != null)
                {
                    // Check if trigger is partially pressed (for aiming) or fully pressed
                    if (activateInput.inputActionReferencePerformed != null)
                    {
                        var action = activateInput.inputActionReferencePerformed.action;
                        if (action != null)
                        {
                            // Use ReadValue to detect partial press (0.1 threshold for aiming)
                            float value = action.ReadValue<float>();
                            if (value > 0.1f && value < 0.9f) // Partial press = aiming, full press = shooting
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        void UpdateAimingLine()
        {
            if (m_LineRenderer == null) return;

            Vector3 startPosition;
            Vector3 direction;
            float force = 20f; // Default force, should get from shooter

            // Determine aim point and direction
            // Priority: VR interactor > Camera > AimPoint > Transform
            if (m_VRInteractor != null && m_VRInteractor.transform != null)
            {
                // Use VR controller position and forward
                startPosition = m_VRInteractor.transform.position;
                direction = m_VRInteractor.transform.forward;
            }
            else if (m_Camera != null)
            {
                startPosition = m_Camera.transform.position;
                // Apply downward angle offset for desktop to fix aim being too high
                direction = Quaternion.Euler(-m_DesktopAimDownAngle, 0, 0) * m_Camera.transform.forward;
            }
            else if (m_AimPoint != null)
            {
                startPosition = m_AimPoint.position;
                direction = m_AimPoint.forward;
            }
            else
            {
                startPosition = transform.position;
                direction = transform.forward;
            }

            // Get force from shooter if available
            if (m_Shooter != null)
            {
                force = m_Shooter.GetShootForce();
            }

            if (m_ShowTrajectory)
            {
                // Draw trajectory arc
                DrawTrajectory(startPosition, direction, force);
            }
            else
            {
                // Draw simple line
                DrawSimpleLine(startPosition, direction);
            }
        }

        void DrawSimpleLine(Vector3 start, Vector3 direction)
        {
            Vector3 end = start + direction * m_MaxLineLength;
            m_LineRenderer.positionCount = 2;
            m_LineRenderer.SetPosition(0, start);
            m_LineRenderer.SetPosition(1, end);
        }

        void DrawTrajectory(Vector3 start, Vector3 direction, float force)
        {
            Vector3 velocity = direction * force;
            float timeStep = m_MaxLineLength / (m_LineSegments * force);
            Vector3 gravity = Physics.gravity;

            for (int i = 0; i < m_LineSegments; i++)
            {
                float t = i * timeStep;
                Vector3 position = start + velocity * t + 0.5f * gravity * t * t;
                m_LineRenderer.SetPosition(i, position);

                // Stop if we hit something
                if (i > 0)
                {
                    Vector3 prevPos = m_LineRenderer.GetPosition(i - 1);
                    if (Physics.Linecast(prevPos, position, out RaycastHit hit))
                    {
                        // Adjust last point to hit position
                        m_LineRenderer.positionCount = i + 1;
                        m_LineRenderer.SetPosition(i, hit.point);
                        break;
                    }
                }
            }
        }
    }
}

