using UnityEngine;
using Unity.XR.CoreUtils;

namespace XRMultiplayer
{
    /// <summary>
    /// Automatically sets up keyboard and mouse controls for the local player.
    /// Add this to your XR Interaction Setup or player GameObject.
    /// </summary>
    public class KeyboardMouseSetup : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] bool m_AutoSetupForLocalPlayer = true;
        [SerializeField] bool m_OnlyIfNoVRDetected = false;
        
        [Header("Character Controller Settings")]
        [SerializeField] float m_CharacterControllerRadius = 0.5f;
        [SerializeField] float m_CharacterControllerHeight = 1.8f;
        [SerializeField] Vector3 m_CharacterControllerCenter = new Vector3(0, 0.9f, 0);
        
        KeyboardMouseController m_KeyboardMouseController;
        CharacterController m_CharacterController;
        XROrigin m_XROrigin;

        void Start()
        {
            if (m_AutoSetupForLocalPlayer)
            {
                SetupKeyboardMouse();
            }
        }
        
        public void SetupKeyboardMouse()
        {
            // Only set up for local player in multiplayer
            XRINetworkPlayer networkPlayer = GetComponent<XRINetworkPlayer>();
            if (networkPlayer != null && !networkPlayer.IsLocalPlayer)
            {
                return; // Don't set up for remote players
            }
            
            // Check if we should only setup when no VR is detected
            if (m_OnlyIfNoVRDetected)
            {
                bool hasVR = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.loadedDeviceName != "None";
                if (hasVR)
                {
                    Debug.Log("VR detected, skipping keyboard/mouse setup. Use Tab to toggle if needed.");
                    return;
                }
            }
            
            // Find or create XR Origin
            m_XROrigin = FindFirstObjectByType<XROrigin>();
            if (m_XROrigin == null)
            {
                Debug.LogWarning("No XR Origin found. Keyboard/Mouse controls may not work properly.");
                return;
            }
            
            // Create CharacterController if it doesn't exist
            m_CharacterController = GetComponent<CharacterController>();
            if (m_CharacterController == null)
            {
                m_CharacterController = gameObject.AddComponent<CharacterController>();
                ConfigureCharacterController();
            }
            
            // Create KeyboardMouseController if it doesn't exist
            m_KeyboardMouseController = GetComponent<KeyboardMouseController>();
            if (m_KeyboardMouseController == null)
            {
                m_KeyboardMouseController = gameObject.AddComponent<KeyboardMouseController>();
                Debug.Log("Keyboard/Mouse controller added. Press Tab to toggle between VR and keyboard/mouse controls.");
            }
        }
        
        void ConfigureCharacterController()
        {
            if (m_CharacterController == null) return;
            
            m_CharacterController.radius = m_CharacterControllerRadius;
            m_CharacterController.height = m_CharacterControllerHeight;
            m_CharacterController.center = m_CharacterControllerCenter;
            
            // Set the character controller to the XR Origin position if available
            if (m_XROrigin != null)
            {
                transform.position = m_XROrigin.transform.position;
            }
        }
        
        // Public methods for manual setup
        public void EnableKeyboardMouse()
        {
            if (m_KeyboardMouseController == null)
                SetupKeyboardMouse();
                
            if (m_KeyboardMouseController != null)
                m_KeyboardMouseController.SetKeyboardMouseEnabled(true);
        }
        
        public void DisableKeyboardMouse()
        {
            if (m_KeyboardMouseController != null)
                m_KeyboardMouseController.SetKeyboardMouseEnabled(false);
        }
        
        public void ToggleKeyboardMouse()
        {
            if (m_KeyboardMouseController == null)
                SetupKeyboardMouse();
                
            if (m_KeyboardMouseController != null)
                m_KeyboardMouseController.ToggleKeyboardMouse();
        }
        
        public bool IsKeyboardMouseActive()
        {
            return m_KeyboardMouseController != null && m_KeyboardMouseController.IsKeyboardMouseEnabled();
        }
    }
}