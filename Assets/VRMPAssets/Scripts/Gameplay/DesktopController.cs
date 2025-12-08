using UnityEngine;
using UnityEngine.InputSystem;

namespace XRMultiplayer
{
    public class DesktopController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] float m_MoveSpeed = 5.0f;
        [SerializeField] float m_LookSensitivity = 0.1f; // Sensitivity is often different in new input system

        [Header("References")]
        [SerializeField] Transform m_CameraTransform;

        float m_RotationX = 0;
        bool m_IsLocked = true;
        SimpleShooter m_Shooter;

        void Start()
        {
            if (m_CameraTransform == null)
                m_CameraTransform = GetComponentInChildren<Camera>()?.transform;

            LockCursor();
            m_Shooter = GetComponent<SimpleShooter>();
            
            // Configure shooter to use camera for fire point so shooting follows mouse look
            if (m_Shooter != null && m_CameraTransform != null)
            {
                m_Shooter.SetUseCameraForFirePoint(true);
            }
        }

        void Update()
        {
            HandleInput();
            HandleMovement();
            HandleLook();
            HandleInteraction();
        }

        void HandleInteraction()
        {
            if (Mouse.current == null) return;

            // Left click for shooting
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                // Prioritize shooting if available
                if (m_Shooter != null)
                {
                    m_Shooter.Shoot();
                }

                // Also do Raycast (for Moles or other click interactions that might trigger logic)
                // Note: Shooting might hit the mole via collision, but immediate click feedback is nice too.
                // We keep both. Projectile handles collision logic.
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
                    if (Physics.Raycast(ray, out RaycastHit hit, 100f))
                    {
                        var mole = hit.collider.GetComponent<Mole>();
                        if (mole != null)
                        {
                            mole.OnHit();
                        }
                    }
                }
            }

            // Trackpad support: Use middle mouse button or trackpad click for aiming
            // Right mouse button is already handled by AimingIndicator
            // Trackpad can be detected via mouse scroll delta when right button is held
            if (Mouse.current.rightButton.isPressed)
            {
                // Trackpad aiming: Use scroll delta to adjust aim
                Vector2 scrollDelta = Mouse.current.scroll.ReadValue();
                if (scrollDelta.magnitude > 0.1f)
                {
                    // Could adjust aim sensitivity based on scroll, but for now just enable aiming
                    // The AimingIndicator will handle the visual
                }
            }
        }

        void HandleInput()
        {
            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                m_IsLocked = !m_IsLocked;
                if (m_IsLocked)
                    LockCursor();
                else
                    UnlockCursor();
            }
        }

        void HandleMovement()
        {
            if (Keyboard.current == null) return;

            float moveX = 0;
            float moveZ = 0;

            if (Keyboard.current.aKey.isPressed) moveX -= 1;
            if (Keyboard.current.dKey.isPressed) moveX += 1;
            if (Keyboard.current.wKey.isPressed) moveZ += 1;
            if (Keyboard.current.sKey.isPressed) moveZ -= 1;

            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            move.y = 0; // Keep movement on the horizontal plane
            move.Normalize(); // Normalize to prevent faster diagonal movement

            transform.Translate(move * m_MoveSpeed * Time.deltaTime, Space.World);
        }

        void HandleLook()
        {
            if (!m_IsLocked || Mouse.current == null) return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            float mouseX = mouseDelta.x * m_LookSensitivity;
            float mouseY = mouseDelta.y * m_LookSensitivity;

            m_RotationX -= mouseY;
            m_RotationX = Mathf.Clamp(m_RotationX, -90f, 90f);

            if (m_CameraTransform != null)
                m_CameraTransform.localRotation = Quaternion.Euler(m_RotationX, 0f, 0f);

            transform.Rotate(Vector3.up * mouseX);
        }

        void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
