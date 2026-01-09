using System;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    [RequireComponent(typeof(XRINetworkPlayer))]
    public class XRAvatarVisuals : MonoBehaviour
    {
        /// <summary>
        /// Head Renderers to change rendering mode for local players.
        /// </summary>
        [Header("Renderer References"), SerializeField, Tooltip("Head Renderers to change rendering mode for local players.")]
        protected Renderer[] m_HeadRends;

        /// <summary>
        /// Head Renderer to control the blend shape for mouth movement. Also updates shirt color based on <see cref="playerColor"/>.
        /// </summary>
        [SerializeField, Tooltip("Head Renderer to drive mouth movement blendshape and player shirt color.")]
        protected SkinnedMeshRenderer m_headRend;

        /// <summary>
        /// Updates shirt color based on <see cref="playerColor"/>.
        /// </summary>
        [SerializeField, Tooltip("Body Renderer for player shirt color.")]
        protected MeshRenderer m_bodyRend;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [Header("Host Visuals"), SerializeField, Tooltip("GameObject that gets enabled for the Host only.")]
        protected GameObject m_HostVisuals;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [SerializeField, Tooltip("Show Host Visuals.")]
        protected bool m_ShowHostVisuals = true;

        /// <summary>
        /// Materials to swap for the local player.
        /// </summary>
        [Header("Local Player Material Swap"), SerializeField]
        protected LocalPlayerMaterialSwap m_LocalPlayerMaterialSwap;

        /// <summary>
        /// Reference to the attached XRINetworkPlayerAvatar component.
        /// </summary>
        protected XRINetworkPlayer m_NetworkPlayer;

        public virtual void Awake()
        {
            if (!TryGetComponent(out m_NetworkPlayer))
            {
                Utils.LogError("XRAvatarVisuals requires a XRINetworkPlayerAvatar component to be attached to the same GameObject. Disabling this component now.");
                enabled = false;
                return;
            }

            m_NetworkPlayer.onSpawnedLocal += PlayerSpawnedLocal;
            m_NetworkPlayer.onSpawnedAll += PlayerSpawnedAll;
            m_NetworkPlayer.onColorUpdated += SetPlayerColor;
            XRINetworkGameManager.Instance.OnSessionOwnerPromoted += HostUpdated;
        }

        public virtual void OnDestroy()
        {
            m_NetworkPlayer.onSpawnedLocal -= PlayerSpawnedLocal;
            m_NetworkPlayer.onSpawnedAll -= PlayerSpawnedAll;
            m_NetworkPlayer.onColorUpdated -= SetPlayerColor;

            XRINetworkGameManager.Instance.OnSessionOwnerPromoted -= HostUpdated;
        }

        public virtual void Update()
        {
            UpdateMouth();
        }

        public virtual void UpdateMouth()
        {
            if (m_headRend != null)
                m_headRend.SetBlendShapeWeight(0, 100 - (m_NetworkPlayer.playerVoiceAmp * 100));
        }

        public virtual void PlayerSpawnedLocal()
        {
            m_LocalPlayerMaterialSwap.SwapMaterials();
            int layer = LayerMask.NameToLayer("Mirror");
            foreach (var r in m_HeadRends)
            {
                r.gameObject.layer = layer;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        public virtual void PlayerSpawnedAll()
        {
            m_HostVisuals.SetActive(m_ShowHostVisuals && m_NetworkPlayer.NetworkObject.OwnerClientId == NetworkManager.Singleton.CurrentSessionOwner);
        }

        public virtual void SetPlayerColor(Color newColor)
        {
            // Safely update head material (work with material arrays and shader property differences)
            if (m_headRend != null)
            {
                var headMats = m_headRend.materials;
                if (headMats != null && headMats.Length > 2 && headMats[2] != null)
                {
                    var headMat = headMats[2];
                    if (headMat.HasProperty("_BaseColor"))
                        headMat.SetColor("_BaseColor", newColor);
                    else if (headMat.HasProperty("_Color"))
                        headMat.SetColor("_Color", newColor);
                    else
                        headMat.color = newColor;

                    headMats[2] = headMat;
                    m_headRend.materials = headMats;
                }
            }

            // Update body renderer if provided: create/assign an instance material so we don't modify shared assets
            if (m_bodyRend != null)
            {
                var bodyMats = m_bodyRend.materials;
                if (bodyMats != null && bodyMats.Length > 0)
                {
                    // Prefer cloning the head material if available so visual matches; otherwise clone existing body mat.
                    Material newBodyMat = null;
                    if (m_headRend != null && m_headRend.materials.Length > 2 && m_headRend.materials[2] != null)
                        newBodyMat = new Material(m_headRend.materials[2]);
                    else
                        newBodyMat = new Material(bodyMats[0]);

                    if (newBodyMat.HasProperty("_BaseColor"))
                        newBodyMat.SetColor("_BaseColor", newColor);
                    else if (newBodyMat.HasProperty("_Color"))
                        newBodyMat.SetColor("_Color", newColor);
                    else
                        newBodyMat.color = newColor;

                    bodyMats[0] = newBodyMat;
                    m_bodyRend.materials = bodyMats;
                }
            }
        }

        public virtual void HostUpdated(ulong newHostId)
        {
            m_HostVisuals.SetActive(m_ShowHostVisuals && m_NetworkPlayer.NetworkObject.OwnerClientId == newHostId);
        }
    }
}

[Serializable]

/// <summary>
/// Helper class for swapping the local player to standard materials from the dithering materials.
/// </summary>
public class LocalPlayerMaterialSwap
{
    public Renderer headRend;
    public Renderer hmdRend;
    public Renderer hostRend;
    public Renderer[] hands;
    public Material[] headMaterials;
    public Material[] hmdMaterials;
    public Material hostMaterial;
    public Material handMaterial;


    public void SwapMaterials()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            hands[i].material = handMaterial;
        }

        hmdRend.materials = hmdMaterials;
        headRend.materials = headMaterials;
        hostRend.material = hostMaterial;
    }
}
