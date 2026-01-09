using TMPro;
using UnityEngine;

namespace XRMultiplayer
{
    public class VersionText : MonoBehaviour
    {
        [SerializeField] TMP_Text[] m_VersionTextComponents;

        // Start is called before the first frame update
        void Start()
        {
            SetText();
        }

        private void OnValidate()
        {
            SetText();
        }

        void SetText()
        {
            if (m_VersionTextComponents != null)
            {
                foreach (TMP_Text t in m_VersionTextComponents)
                {
                }
            }
            else
            {
                Utils.Log("Missing Text component on VersionText script", 2);
            }
        }
    }
}
