using UnityEngine;
[ExecuteAlways]
public class LightingManager : MonoBehaviour
{
    [SerializeField] private Light DirectionalLight;
    [SerializeField] private LightingPreset Preset;
    [SerializeField, Range(0, 24)] private float TimeOfDay;

    private void Update()
    {
        if (Preset == null)
            return;

        if (Application.isPlaying)
        {
            TimeOfDay += Time.deltaTime / 240f; // Speed up time for demonstration
            TimeOfDay %= 24; // Loop back to 0 after 24
        }

        float timePercent = TimeOfDay / 24f;
        UpdateLighting(timePercent);
    }

    private void UpdateLighting(float timePercent)
    {
        if (Preset == null)
            return;

        //DirectionalLight.color = Preset.DirectionalColor.Evaluate(timePercent);
        //RenderSettings.ambientLight = Preset.AmbientColor.Evaluate(timePercent);
        //RenderSettings.fogColor = Preset.FogColor.Evaluate(timePercent);
        //RenderSettings.fogDensity = Preset.FogDensity.Evaluate(timePercent);
        
        // Update skybox exposure
        //if (RenderSettings.skybox != null)
        //{
        //    float exposure = Preset.SkyboxExposure.Evaluate(timePercent);
        //    RenderSettings.skybox.SetFloat("_Exposure", exposure);
        //}

        if (DirectionalLight != null)
        {
            float lightAngle = (timePercent * 360f) - 90f;
            DirectionalLight.transform.localRotation = Quaternion.Euler(new Vector3(lightAngle, 170f, 0));
        }
    }

    private void OnValidate()
    {
        if (DirectionalLight != null)
        {
            return;
        }
        if (RenderSettings.sun != null)
        {
            DirectionalLight = RenderSettings.sun;
        }
        else
        {
            Light[] lights = GameObject.FindObjectsByType<Light>(FindObjectsSortMode.None);
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    DirectionalLight = light;
                    return;
                }
            }
        }
    }

}
