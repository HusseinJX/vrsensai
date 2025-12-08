using UnityEngine;
using UnityEngine.XR.Templates.VRMultiplayer;
using Unity.XR.CoreUtils;
using UnityEngine.InputSystem.XR;

namespace XRMultiplayer
{
    public class DesktopModeManager : MonoBehaviour
    {
        [SerializeField] bool m_ForceDesktopMode = false;

        void Awake()
        {
            // Wait for next frame or check immediately? 
            // Better to check in Start to allow PlatformUnderstanding to init if needed, though it lazy loads.
        }

        void Start()
        {
            if (XRPlatformUnderstanding.CurrentPlatform == XRPlatformType.Desktop || m_ForceDesktopMode)
            {
                InitializeDesktopMode();
            }
        }

        void InitializeDesktopMode()
        {
            Debug.Log("Initializing Desktop Mode...");

            XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null)
            {
                Debug.LogError("DesktopModeManager: No XROrigin found!");
                return;
            }

            // Disable Tracked Pose Driver on Camera so mouse look works
            var trackedPoseDriver = xrOrigin.Camera.GetComponent<TrackedPoseDriver>();
            if (trackedPoseDriver != null)
            {
                trackedPoseDriver.enabled = false;
            }

            // Disable XR Controllers (Hands) to prevent ghost interactions/errors
            var controllers = xrOrigin.GetComponentsInChildren<UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInteractor>(true);
            foreach (var controller in controllers)
            {
                controller.gameObject.SetActive(false);
            }
            
            // Disable all LineRenderers from ray interactors to remove beams
            var lineRenderers = xrOrigin.GetComponentsInChildren<LineRenderer>(true);
            foreach (var lineRenderer in lineRenderers)
            {
                lineRenderer.enabled = false;
            }
            
            // Also try to find standard controller GameObjects if they don't have Interactor components (unlikely but safe)
            Transform cameraOffset = xrOrigin.CameraFloorOffsetObject.transform;
            foreach(Transform child in cameraOffset)
            {
                if (child.name.Contains("Controller") || child.name.Contains("Hand"))
                {
                    child.gameObject.SetActive(false);
                }
            }

            // Attach Desktop Controller to the XR Origin (representing the player body)
            var desktopController = xrOrigin.gameObject.AddComponent<DesktopController>();
            
            // Attach Shooter
            var shooter = xrOrigin.gameObject.AddComponent<SimpleShooter>();
            // Load projectile
            var projectilePrefab = Resources.Load<GameObject>("SphereProjectile");
            if (projectilePrefab != null)
            {
                shooter.SetProjectilePrefab(projectilePrefab);
            }
            else
            {
                Debug.LogWarning("DesktopModeManager: SphereProjectile not found in Resources!");
            }

            // Add aiming indicator to camera
            if (xrOrigin.Camera != null)
            {
                var aimingIndicator = xrOrigin.Camera.gameObject.AddComponent<AimingIndicator>();
                // The aiming indicator will find the shooter and camera automatically
                
                // Add aim dot to camera for center screen crosshair
                var aimDot = xrOrigin.Camera.gameObject.AddComponent<AimDot>();
                aimDot.SetCamera(xrOrigin.Camera);
            }

            // Use reflection or Find to set camera if needed, but it does it in Start()

            // Optional: Disable Hands visuals if they get in the way, or just leave them idle.
            // For now, we leave them as they might just hang at 0,0,0 relative to parent.
            // If they are annoying, we can find them and disable them.
            
            Debug.Log("Desktop Mode Initialized.");
        }
    }
}
