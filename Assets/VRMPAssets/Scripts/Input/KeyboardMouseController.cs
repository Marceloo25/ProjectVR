using UnityEngine;
using UnityEngine.InputSystem;
using Unity.XR.CoreUtils;

namespace XRMultiplayer
{
    /// <summary>
    /// Provides keyboard and mouse controls for VR players when not using a headset.
    /// This allows for desktop players to join VR sessions or for easier testing.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class KeyboardMouseController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] float m_MoveSpeed = 5f;
        [SerializeField] float m_RunSpeed = 8f;
        [SerializeField] float m_JumpHeight = 2f;
        [SerializeField] float m_Gravity = -9.81f;
        
        [Header("Mouse Look Settings")]
        [SerializeField] float m_MouseSensitivity = 2f;
        [SerializeField] float m_VerticalLookLimit = 80f;
        [SerializeField] bool m_InvertY = false;
        
        [Header("References")]
        [SerializeField] Transform m_CameraTransform;
        [SerializeField] Transform m_XROriginTransform;
        
        [Header("Settings")]
        [SerializeField] bool m_EnableKeyboardMouse = false;
        [SerializeField] KeyCode m_ToggleKey = KeyCode.Tab;
        
        // Components
        CharacterController m_CharacterController;
        XROrigin m_XROrigin;
        
        // Input variables
        Vector2 m_MovementInput;
        Vector2 m_MouseInput;
        bool m_JumpInput;
        bool m_RunInput;
        
        // Camera rotation
        float m_VerticalRotation = 0f;
        
        // Movement
        Vector3 m_Velocity;
        bool m_IsGrounded;
        
        // State
        bool m_CursorLocked = false;
        //bool m_WasVREnabled = false;

        void Awake()
        {
            m_CharacterController = GetComponent<CharacterController>();
            
            // Try to find XR Origin if not assigned
            if (m_XROriginTransform == null)
            {
                m_XROrigin = FindFirstObjectByType<XROrigin>();
                if (m_XROrigin != null)
                {
                    m_XROriginTransform = m_XROrigin.transform;
                    m_CameraTransform = m_XROrigin.Camera.transform;
                }
            }
            
            // Set initial character controller position to match XR Origin
            if (m_XROriginTransform != null)
            {
                transform.position = m_XROriginTransform.position;
                transform.rotation = m_XROriginTransform.rotation;
            }
        }

        void Start()
        {
            // Check if we're in VR or not
            CheckVRStatus();
        }

        void Update()
        {
            HandleToggle();
            
            if (!m_EnableKeyboardMouse) return;
            
            HandleInput();
            HandleMovement();
            HandleMouseLook();
            UpdateXROrigin();
        }
        
        void HandleToggle()
        {
            if (Input.GetKeyDown(m_ToggleKey))
            {
                ToggleKeyboardMouse();
            }
        }
        
        public void ToggleKeyboardMouse()
        {
            m_EnableKeyboardMouse = !m_EnableKeyboardMouse;
            
            if (m_EnableKeyboardMouse)
            {
                EnableKeyboardMouse();
            }
            else
            {
                DisableKeyboardMouse();
            }
        }
        
        void EnableKeyboardMouse()
        {
            // Lock cursor for mouse look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            m_CursorLocked = true;
            
            // Sync character controller position with current XR Origin position
            if (m_XROriginTransform != null)
            {
                transform.position = m_XROriginTransform.position;
                transform.rotation = m_XROriginTransform.rotation;
                
                // Set initial camera rotation
                m_VerticalRotation = m_CameraTransform.localEulerAngles.x;
                if (m_VerticalRotation > 180f)
                    m_VerticalRotation -= 360f;
            }
            
            Debug.Log("Keyboard/Mouse controls enabled. Press Tab to toggle back to VR.");
        }
        
        void DisableKeyboardMouse()
        {
            // Unlock cursor
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            m_CursorLocked = false;
            
            Debug.Log("Keyboard/Mouse controls disabled. VR controls restored.");
        }
        
        void CheckVRStatus()
        {
            // Auto-enable keyboard mouse if no VR headset detected
            bool isVREnabled = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.loadedDeviceName != "None";
            
            // Also check for XR Input devices
            bool hasXRDevices = false;
            #if UNITY_XR_MANAGEMENT
            var xrDisplaySubsystems = new List<UnityEngine.XR.XRDisplaySubsystem>();
            UnityEngine.XR.SubsystemManager.GetInstances<UnityEngine.XR.XRDisplaySubsystem>(xrDisplaySubsystems);
            hasXRDevices = xrDisplaySubsystems.Count > 0 && xrDisplaySubsystems[0].running;
            #endif
            
            if (!isVREnabled && !hasXRDevices)
            {
                m_EnableKeyboardMouse = true;
                EnableKeyboardMouse();
                Debug.Log("No VR headset detected. Keyboard/Mouse controls auto-enabled.");
            }
            else if (isVREnabled || hasXRDevices)
            {
                Debug.Log("VR headset detected. Use Tab to toggle to Keyboard/Mouse controls if needed.");
            }
        }

        void HandleInput()
        {
            // Movement input
            float horizontal = Input.GetAxis("Horizontal"); // A/D keys
            float vertical = Input.GetAxis("Vertical");     // W/S keys
            m_MovementInput = new Vector2(horizontal, vertical);
            
            // Mouse input
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            m_MouseInput = new Vector2(mouseX, mouseY);
            
            // Jump input
            m_JumpInput = Input.GetButtonDown("Jump"); // Space key
            
            // Run input
            m_RunInput = Input.GetKey(KeyCode.LeftShift);
            
            // Escape to unlock cursor temporarily
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (m_CursorLocked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    m_CursorLocked = false;
                }
            }
            
            // Click to lock cursor again
            if (!m_CursorLocked && Input.GetMouseButtonDown(0))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                m_CursorLocked = true;
            }
        }

        void HandleMovement()
        {
            // Ground check
            m_IsGrounded = m_CharacterController.isGrounded;
            
            if (m_IsGrounded && m_Velocity.y < 0)
            {
                m_Velocity.y = -2f; // Keep grounded
            }

            // Movement calculation
            Vector3 move = transform.right * m_MovementInput.x + transform.forward * m_MovementInput.y;
            
            // Apply speed
            float currentSpeed = m_RunInput ? m_RunSpeed : m_MoveSpeed;
            move *= currentSpeed;

            // Apply movement
            m_CharacterController.Move(move * Time.deltaTime);

            // Jumping
            if (m_JumpInput && m_IsGrounded)
            {
                m_Velocity.y = Mathf.Sqrt(m_JumpHeight * -2f * m_Gravity);
            }

            // Apply gravity
            m_Velocity.y += m_Gravity * Time.deltaTime;
            m_CharacterController.Move(m_Velocity * Time.deltaTime);
        }

        void HandleMouseLook()
        {
            if (!m_CursorLocked) return;
            
            // Horizontal rotation (Y-axis)
            float mouseX = m_MouseInput.x * m_MouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation (X-axis)
            float mouseY = m_MouseInput.y * m_MouseSensitivity;
            if (m_InvertY) mouseY = -mouseY;
            
            m_VerticalRotation -= mouseY;
            m_VerticalRotation = Mathf.Clamp(m_VerticalRotation, -m_VerticalLookLimit, m_VerticalLookLimit);
        }
        
        void UpdateXROrigin()
        {
            if (m_XROriginTransform == null) return;
            
            // Update XR Origin position and rotation to match character controller
            m_XROriginTransform.position = transform.position;
            m_XROriginTransform.rotation = transform.rotation;
            
            // Update camera rotation for vertical look
            if (m_CameraTransform != null)
            {
                m_CameraTransform.localRotation = Quaternion.Euler(m_VerticalRotation, 0f, 0f);
            }
        }
        
        void OnEnable()
        {
            if (m_EnableKeyboardMouse)
            {
                EnableKeyboardMouse();
            }
        }
        
        void OnDisable()
        {
            DisableKeyboardMouse();
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && m_CursorLocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                m_CursorLocked = false;
            }
        }

        // Public methods for UI/external control
        public void SetKeyboardMouseEnabled(bool enabled)
        {
            if (enabled)
                EnableKeyboardMouse();
            else
                DisableKeyboardMouse();
        }
        
        public bool IsKeyboardMouseEnabled()
        {
            return m_EnableKeyboardMouse;
        }
        
        public void SetMovementSpeed(float speed)
        {
            m_MoveSpeed = speed;
        }
        
        public void SetRunSpeed(float speed)
        {
            m_RunSpeed = speed;
        }
        
        public void SetMouseSensitivity(float sensitivity)
        {
            m_MouseSensitivity = sensitivity;
        }
    }
}