using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.InputSystem;

namespace XRMultiplayer
{
    [RequireComponent(typeof(SimpleShooter))]
    public class VRShooterInput : MonoBehaviour
    {
        SimpleShooter m_Shooter;
        // In XR Interaction Toolkit 3.0+, use XRBaseControllerInteractor instead of ActionBasedController
        XRBaseInputInteractor m_ControllerInteractor;
        
        // Input Action Reference for Activate/Trigger (fallback if interactor doesn't have one)
        [SerializeField] InputActionReference m_ActivateAction;
        
        // Input Action Reference for Secondary Button (B button on Quest)
        [SerializeField] InputActionReference m_SecondaryButtonAction;

        bool m_WasActivatePressed = false;
        bool m_WasSecondaryPressed = false;

        void Start()
        {
            m_Shooter = GetComponent<SimpleShooter>();
            // Try to find XRBaseControllerInteractor (XRRayInteractor or XRDirectInteractor)
            m_ControllerInteractor = GetComponent<XRBaseInputInteractor>();
        }

        void Update()
        {
            bool shouldShoot = false;
            
            // Check activate/trigger input
            InputAction activateAction = null;
            if (m_ControllerInteractor != null)
            {
                // In XR Interaction Toolkit 3.0+, activateInput has an inputActionReferencePerformed property
                var activateInput = m_ControllerInteractor.activateInput;
                if (activateInput != null && activateInput.inputActionReferencePerformed != null)
                {
                    activateAction = activateInput.inputActionReferencePerformed.action;
                }
            }
            
            // Fallback to serialized field if interactor doesn't have one
            if (activateAction == null && m_ActivateAction != null)
            {
                activateAction = m_ActivateAction.action;
            }
            
            if (activateAction != null)
            {
                bool isPressed = activateAction.IsPressed();
                if (isPressed && !m_WasActivatePressed)
                {
                    shouldShoot = true;
                }
                m_WasActivatePressed = isPressed;
            }
            
            // Check secondary button (B button on Quest controllers)
            // Use the serialized InputActionReference which should be set in the inspector
            // to reference the "Secondary Button" action from XRI Default Input Actions
            if (m_SecondaryButtonAction != null && m_SecondaryButtonAction.action != null)
            {
                bool isSecondaryPressed = m_SecondaryButtonAction.action.IsPressed();
                if (isSecondaryPressed && !m_WasSecondaryPressed)
                {
                    shouldShoot = true;
                }
                m_WasSecondaryPressed = isSecondaryPressed;
            }
            
            if (shouldShoot)
            {
                m_Shooter.Shoot();
            }
        }
    }
}
