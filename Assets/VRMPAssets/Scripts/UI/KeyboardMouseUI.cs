using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace XRMultiplayer
{
    /// <summary>
    /// UI Controller for managing keyboard and mouse input settings.
    /// </summary>
    public class KeyboardMouseUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] Toggle m_EnableToggle;
        [SerializeField] Slider m_MovementSpeedSlider;
        [SerializeField] Slider m_RunSpeedSlider;
        [SerializeField] Slider m_MouseSensitivitySlider;
        [SerializeField] Slider m_JumpHeightSlider;
        [SerializeField] Toggle m_InvertYToggle;
        
        [Header("Labels")]
        [SerializeField] TextMeshProUGUI m_MovementSpeedLabel;
        [SerializeField] TextMeshProUGUI m_RunSpeedLabel;
        [SerializeField] TextMeshProUGUI m_MouseSensitivityLabel;
        [SerializeField] TextMeshProUGUI m_JumpHeightLabel;
        [SerializeField] TextMeshProUGUI m_StatusLabel;
        
        [Header("Help Panel")]
        [SerializeField] GameObject m_HelpPanel;
        [SerializeField] TextMeshProUGUI m_HelpText;
        
        KeyboardMouseController m_KeyboardMouseController;
        
        const string HELP_TEXT = 
            "<b>Keyboard & Mouse Controls:</b>\n\n" +
            "<b>Movement:</b>\n" +
            "• WASD - Move forward/back/left/right\n" +
            "• Left Shift - Run\n" +
            "• Space - Jump\n\n" +
            "<b>Camera:</b>\n" +
            "• Mouse - Look around\n" +
            "• Escape - Unlock cursor\n" +
            "• Left Click - Lock cursor again\n\n" +
            "<b>Toggle:</b>\n" +
            "• Tab - Switch between VR and Keyboard/Mouse\n\n" +
            "<color=yellow>Note: These controls work alongside VR controls and are useful for testing or non-VR users joining VR sessions.</color>";

        void Start()
        {
            // Find the KeyboardMouseController
            m_KeyboardMouseController = FindFirstObjectByType<KeyboardMouseController>();
            
            if (m_KeyboardMouseController == null)
            {
                Debug.LogWarning("KeyboardMouseUI: No KeyboardMouseController found in scene.");
                if (m_StatusLabel != null)
                    m_StatusLabel.text = "Keyboard/Mouse Controller not found";
                return;
            }
            
            SetupUI();
            UpdateUI();
        }
        
        void SetupUI()
        {
            // Setup toggle
            if (m_EnableToggle != null)
            {
                m_EnableToggle.onValueChanged.AddListener(OnEnableToggleChanged);
                m_EnableToggle.isOn = m_KeyboardMouseController.IsKeyboardMouseEnabled();
            }
            
            // Setup sliders
            if (m_MovementSpeedSlider != null)
            {
                m_MovementSpeedSlider.minValue = 1f;
                m_MovementSpeedSlider.maxValue = 10f;
                m_MovementSpeedSlider.value = 5f;
                m_MovementSpeedSlider.onValueChanged.AddListener(OnMovementSpeedChanged);
            }
            
            if (m_RunSpeedSlider != null)
            {
                m_RunSpeedSlider.minValue = 2f;
                m_RunSpeedSlider.maxValue = 15f;
                m_RunSpeedSlider.value = 8f;
                m_RunSpeedSlider.onValueChanged.AddListener(OnRunSpeedChanged);
            }
            
            if (m_MouseSensitivitySlider != null)
            {
                m_MouseSensitivitySlider.minValue = 0.5f;
                m_MouseSensitivitySlider.maxValue = 5f;
                m_MouseSensitivitySlider.value = 2f;
                m_MouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
            }
            
            if (m_JumpHeightSlider != null)
            {
                m_JumpHeightSlider.minValue = 1f;
                m_JumpHeightSlider.maxValue = 5f;
                m_JumpHeightSlider.value = 2f;
            }
            
            // Setup help text
            if (m_HelpText != null)
            {
                m_HelpText.text = HELP_TEXT;
            }
            
            // Hide help panel initially
            if (m_HelpPanel != null)
            {
                m_HelpPanel.SetActive(false);
            }
        }
        
        void Update()
        {
            UpdateStatusLabel();
        }
        
        void UpdateUI()
        {
            if (m_KeyboardMouseController == null) return;
            
            bool isEnabled = m_KeyboardMouseController.IsKeyboardMouseEnabled();
            
            // Update toggle without triggering callback
            if (m_EnableToggle != null && m_EnableToggle.isOn != isEnabled)
            {
                m_EnableToggle.SetIsOnWithoutNotify(isEnabled);
            }
            
            UpdateLabels();
        }
        
        void UpdateLabels()
        {
            if (m_MovementSpeedLabel != null && m_MovementSpeedSlider != null)
            {
                m_MovementSpeedLabel.text = $"Movement Speed: {m_MovementSpeedSlider.value:F1}";
            }
            
            if (m_RunSpeedLabel != null && m_RunSpeedSlider != null)
            {
                m_RunSpeedLabel.text = $"Run Speed: {m_RunSpeedSlider.value:F1}";
            }
            
            if (m_MouseSensitivityLabel != null && m_MouseSensitivitySlider != null)
            {
                m_MouseSensitivityLabel.text = $"Mouse Sensitivity: {m_MouseSensitivitySlider.value:F1}";
            }
            
            if (m_JumpHeightLabel != null && m_JumpHeightSlider != null)
            {
                m_JumpHeightLabel.text = $"Jump Height: {m_JumpHeightSlider.value:F1}";
            }
        }
        
        void UpdateStatusLabel()
        {
            if (m_StatusLabel == null || m_KeyboardMouseController == null) return;
            
            bool isEnabled = m_KeyboardMouseController.IsKeyboardMouseEnabled();
            bool hasVR = UnityEngine.XR.XRSettings.enabled && UnityEngine.XR.XRSettings.loadedDeviceName != "None";
            
            if (isEnabled)
            {
                m_StatusLabel.text = "<color=green>Keyboard/Mouse Active</color>";
                m_StatusLabel.text += "\nPress Tab to toggle, Esc to unlock cursor";
            }
            else
            {
                if (hasVR)
                {
                    m_StatusLabel.text = "<color=blue>VR Mode Active</color>";
                    m_StatusLabel.text += "\nPress Tab to switch to Keyboard/Mouse";
                }
                else
                {
                    m_StatusLabel.text = "<color=orange>No VR Detected</color>";
                    m_StatusLabel.text += "\nEnable Keyboard/Mouse to move";
                }
            }
        }
        
        // UI Callbacks
        void OnEnableToggleChanged(bool value)
        {
            if (m_KeyboardMouseController != null)
            {
                m_KeyboardMouseController.SetKeyboardMouseEnabled(value);
            }
        }
        
        void OnMovementSpeedChanged(float value)
        {
            if (m_KeyboardMouseController != null)
            {
                m_KeyboardMouseController.SetMovementSpeed(value);
            }
            UpdateLabels();
        }
        
        void OnRunSpeedChanged(float value)
        {
            if (m_KeyboardMouseController != null)
            {
                m_KeyboardMouseController.SetRunSpeed(value);
            }
            UpdateLabels();
        }
        
        void OnMouseSensitivityChanged(float value)
        {
            if (m_KeyboardMouseController != null)
            {
                m_KeyboardMouseController.SetMouseSensitivity(value);
            }
            UpdateLabels();
        }
        
        // Public methods for buttons
        public void ToggleKeyboardMouse()
        {
            if (m_KeyboardMouseController != null)
            {
                m_KeyboardMouseController.ToggleKeyboardMouse();
                UpdateUI();
            }
        }
        
        public void ShowHelpPanel()
        {
            if (m_HelpPanel != null)
            {
                m_HelpPanel.SetActive(true);
            }
        }
        
        public void HideHelpPanel()
        {
            if (m_HelpPanel != null)
            {
                m_HelpPanel.SetActive(false);
            }
        }
        
        public void ResetToDefaults()
        {
            if (m_MovementSpeedSlider != null)
                m_MovementSpeedSlider.value = 5f;
            
            if (m_RunSpeedSlider != null)
                m_RunSpeedSlider.value = 8f;
            
            if (m_MouseSensitivitySlider != null)
                m_MouseSensitivitySlider.value = 2f;
            
            if (m_JumpHeightSlider != null)
                m_JumpHeightSlider.value = 2f;
        }
    }
}