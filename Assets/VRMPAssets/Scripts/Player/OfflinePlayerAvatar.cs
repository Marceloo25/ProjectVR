using Unity.XR.CoreUtils;
using Unity.XR.CoreUtils.Bindings.Variables;
using UnityEngine;
using UnityEngine.Android;

namespace XRMultiplayer
{
    /// <summary>
    /// Represents the offline player avatar.
    /// </summary>
    public class OfflinePlayerAvatar : MonoBehaviour
    {
        public static BindableVariable<float> voiceAmp = new BindableVariable<float>();

        [Header("Avatar Variant")]
        [Tooltip("Child GameObject for the VR avatar variant (usually named 'Avatar_Head'). If left null, it will be found by name.")]
        [SerializeField] GameObject m_VrAvatarRoot;
        [SerializeField] GameObject VRSetup;

        [Tooltip("Child GameObject for the PC/mirror avatar variant (usually named 'Avatar_Robot'). If left null, it will be found by name.")]
        [SerializeField] GameObject m_PcAvatarRoot;
        [SerializeField] GameObject PCSetup;


        public bool useVrAvatar = true;

        //ShaderVariantCollection 

        /// <summary>
        /// Toggle between the VR avatar (Avatar_Head) and the PC/mirror avatar (Avatar_Robot).
        /// Intended to be called by the OfflineMenu toggle.
        /// </summary>
        /// <param name="useVrAvatar">True enables Avatar_Head and disables Avatar_Robot. False does the opposite.</param>
        public void ToggleVrAvatar()
        {
            if(useVrAvatar) useVrAvatar = false;
            else useVrAvatar = true;
            m_VrAvatarRoot.SetActive(useVrAvatar);
            m_PcAvatarRoot.SetActive(!useVrAvatar);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the player is muted.
        /// </summary>
        public static bool muted
        {
            get => s_Muted;
            set
            {
                if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
                    s_Muted = value;
            }
        }

        /// <summary>
        /// A value indicating whether the player is muted.
        /// </summary>
        static bool s_Muted;

        /// <summary>
        /// The head transform.
        /// </summary>
        [SerializeField] Transform m_HeadTransform;

        /// <summary>
        /// The head renderer.
        /// </summary>
        [SerializeField] SkinnedMeshRenderer m_HeadRend;

        /// <summary>
        /// The body renderer.
        /// </summary>
        [SerializeField] MeshRenderer m_BodyRend;

        /// <summary>
        /// The voice amplitude curve.
        /// </summary>
        [SerializeField] AnimationCurve m_VoiceCurve;

        /// <summary>
        /// The head origin.
        /// </summary>
        Transform m_HeadOrigin;

        /// <summary>
        /// The mouth blend smoothing.
        /// </summary>
        [SerializeField] float m_MouthBlendSmoothing = 5.0f;

        /// <summary>
        /// The microphone loudness.
        /// </summary>
        float m_MicLoudness;

        /// <summary>
        /// The microphone device name.
        /// </summary>
        string m_Device;

        /// <summary>
        /// The sample window.
        /// </summary>
        int m_SampleWindow = 128;

        /// <summary>
        /// The clip record.
        /// </summary>
        AudioClip m_ClipRecord;

        /// <summary>
        /// The voice destination volume.
        /// </summary>
        float m_VoiceDestinationVolume;

        bool m_MicInitialized = false;

        /// <inheritdoc/>
        void Start()
        {
            XROrigin rig = FindFirstObjectByType<XROrigin>();
            m_HeadOrigin = rig.Camera.transform;

        }

        void OnEnable()
        {
            XRINetworkGameManager.LocalPlayerColor.Subscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
            XRINetworkGameManager.Connected.Subscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        void OnDisable()
        {
            XRINetworkGameManager.LocalPlayerColor.Unsubscribe(UpdatePlayerColor);
            VoiceChatManager.s_HasMicrophonePermission.Subscribe(MicrophonePermissionGranted);
            StopMicrophone();
            if (useVrAvatar)
            {
                VRSetup.SetActive(true);
                PCSetup.SetActive(false);
            }
            else
            {
                VRSetup.SetActive(false);
                PCSetup.SetActive(true); //Needs to Spawn not SetActive;
            }
            XRINetworkGameManager.Connected.Unsubscribe(connected =>
            {
                gameObject.SetActive(!connected);
            });
        }

        /// <inheritdoc/>
        private void LateUpdate()
        {
            m_HeadTransform.SetPositionAndRotation(m_HeadOrigin.position, m_HeadOrigin.rotation);
        }

        /// <inheritdoc/>
        void Update()
        {
            if (!s_Muted)
            {
                m_MicLoudness = LevelMax();

                m_VoiceDestinationVolume = Mathf.Clamp01(Mathf.Lerp(m_VoiceDestinationVolume, m_MicLoudness, Time.deltaTime * m_MouthBlendSmoothing));

                float appliedCurve = m_VoiceCurve.Evaluate(m_VoiceDestinationVolume);
                voiceAmp.Value = appliedCurve;
                m_HeadRend.SetBlendShapeWeight(0, 100 - appliedCurve * 100);
            }
            else
            {
                voiceAmp.Value = 0.0f;
            }
        }

        void MicrophonePermissionGranted(bool granted)
        {
            if (granted)
            {
                InitMic();
            }
        }

        void UpdatePlayerColor(Color color)
        {
            // Defensive: update head material safely (work with material arrays and property names)
            if (m_HeadRend != null)
            {
                var headMats = m_HeadRend.materials;
                if (headMats != null && headMats.Length > 2 && headMats[2] != null)
                {
                    var headMat = headMats[2];
                    if (headMat.HasProperty("_BaseColor"))
                        headMat.SetColor("_BaseColor", color);
                    else if (headMat.HasProperty("_Color"))
                        headMat.SetColor("_Color", color);
                    else
                        headMat.color = color;

                    // assign back in case renderer expects the array to be replaced
                    headMats[2] = headMat;
                    m_HeadRend.materials = headMats;
                }
            }

            // Update body renderer: create/assign an instance material and set color on that material.
            if (m_BodyRend != null)
            {
                var bodyMats = m_BodyRend.materials;
                if (bodyMats != null && bodyMats.Length > 0)
                {
                    // Prefer cloning the head material if available so visual matches; otherwise clone existing body mat.
                    Material newBodyMat = null;
                    if (m_HeadRend != null && m_HeadRend.materials.Length > 2 && m_HeadRend.materials[2] != null)
                        newBodyMat = new Material(m_HeadRend.materials[2]);
                    else
                        newBodyMat = new Material(bodyMats[0]);

                    if (newBodyMat.HasProperty("_BaseColor"))
                        newBodyMat.SetColor("_BaseColor", color);
                    else if (newBodyMat.HasProperty("_Color"))
                        newBodyMat.SetColor("_Color", color);
                    else
                        newBodyMat.color = color;

                    bodyMats[0] = newBodyMat;
                    m_BodyRend.materials = bodyMats;
                }
            }

        }

        /// <summary>
        /// Initializes the microphone, called from <see cref="VoiceChatManager.s_HasMicrophonePermission" callback/>.
        /// </summary>
        void InitMic()
        {
            m_MicInitialized = true;
            m_Device ??= Microphone.devices[0];
            m_ClipRecord = Microphone.Start(m_Device, true, 999, 44100);
        }

        /// <summary>
        /// Stops the microphone.
        /// </summary>
        void StopMicrophone()
        {
            m_MicInitialized = false;
            if (Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Microphone.End(m_Device);
            }
            else
            {
                s_Muted = true;
            }
        }

        /// <summary>
        /// Gets the maximum level of the microphone input.
        /// </summary>
        /// <returns>The maximum level of the microphone input.</returns>
        float LevelMax()
        {
            if (!m_MicInitialized) return 0;
            float levelMax = 0;
            float[] waveData = new float[m_SampleWindow];
            int micPosition = Microphone.GetPosition(null) - (m_SampleWindow + 1); // null means the first microphone
            if (micPosition < 0) return 0;
            m_ClipRecord.GetData(waveData, micPosition);
            // Getting a peak on the last 128 samples
            for (int i = 0; i < m_SampleWindow; i++)
            {
                float wavePeak = waveData[i] * waveData[i];
                if (levelMax < wavePeak)
                {
                    levelMax = wavePeak;
                }
            }
            return levelMax;
        }
    }
}
