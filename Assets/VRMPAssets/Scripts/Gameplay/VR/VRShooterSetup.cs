using UnityEngine;
using UnityEngine.XR.Templates.VRMultiplayer;
using Unity.XR.CoreUtils;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace XRMultiplayer
{
    public class VRShooterSetup : MonoBehaviour
    {
        void Start()
        {
            // Do not run if in Desktop mode
            if (XRPlatformUnderstanding.CurrentPlatform == XRPlatformType.Desktop)
                return;

            SetupControllers();
        }

        void SetupControllers()
        {
            XROrigin xrOrigin = FindFirstObjectByType<XROrigin>();
            if (xrOrigin == null) return;

            // Find controllers
            // In XR Interaction Toolkit 3.0+, ActionBasedController is deprecated
            // Use XRRayInteractor or XRDirectInteractor instead
            var rayInteractors = xrOrigin.GetComponentsInChildren<XRRayInteractor>(true);
            var directInteractors = xrOrigin.GetComponentsInChildren<XRDirectInteractor>(true);
            
            var projectilePrefab = Resources.Load<GameObject>("SphereProjectile");
            int configuredCount = 0;

            // Configure ray interactors (typically used for distant interaction)
            foreach (var interactor in rayInteractors)
            {
                // Attach SimpleShooter
                var shooter = interactor.gameObject.AddComponent<SimpleShooter>();
                if (projectilePrefab != null)
                {
                    shooter.SetProjectilePrefab(projectilePrefab);
                }

                // Attach VR Input
                var input = interactor.gameObject.AddComponent<VRShooterInput>();
                
                // Add aiming indicator to VR controllers
                var aimingIndicator = interactor.gameObject.AddComponent<AimingIndicator>();
                
                // Attach AimDot to show where controller is pointing
                var aimDot = interactor.gameObject.AddComponent<AimDot>();
                aimDot.SetAimPoint(interactor.transform);
                
                // input autodetects shooter and interactor
                configuredCount++;
            }

            // Configure direct interactors (typically used for near interaction)
            foreach (var interactor in directInteractors)
            {
                // Attach SimpleShooter
                var shooter = interactor.gameObject.AddComponent<SimpleShooter>();
                if (projectilePrefab != null)
                {
                    shooter.SetProjectilePrefab(projectilePrefab);
                }

                // Attach VR Input
                var input = interactor.gameObject.AddComponent<VRShooterInput>();
                
                // Attach AimDot to show where controller is pointing
                var aimDot = interactor.gameObject.AddComponent<AimDot>();
                aimDot.SetAimPoint(interactor.transform);
                
                // input autodetects shooter and interactor
                configuredCount++;
            }
            
            Debug.Log($"VRShooterSetup: Configured {configuredCount} interactors for shooting.");
        }
    }
}
