using System.Collections.Generic;
using UnityEngine;

public class Farming : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Parent transform that contains the Sphere children (e.g. Farm > Land)")]
    [SerializeField]
    Transform m_LandRoot;

    [Header("Watering")]
    [Tooltip("Material to apply when watering a plant")]
    [SerializeField]
    Material m_WateredMaterial;

    [Header("Tilting")]
    [Tooltip("Material to apply when tilting a plant")]
    [SerializeField]
    Material m_TiltedMaterial;

    List<GameObject> m_Spheres = new List<GameObject>();
    int m_TiltIndex = 0;
    int m_WaterIndex = 0;

    void Start()
    {
        RefreshSpheres();
    }

    void RefreshSpheres()
    {
        m_Spheres.Clear();
        if (m_LandRoot == null)
        {
            var t = transform.Find("Land");
            if (t != null) m_LandRoot = t;
        }

        if (m_LandRoot == null) return;

        foreach (Transform child in m_LandRoot)
        {
            m_Spheres.Add(child.gameObject);
        }
    }

    // Tilt: activate one sphere at a time (cycles through children on each call)
    [ContextMenu("Tilt")]
    public void Tilt()
    {
        if (m_Spheres.Count == 0) RefreshSpheres();
        if (m_Spheres.Count == 0) return;
        if (m_TiltedMaterial == null) return;


        // Activate the current index
        m_Spheres[m_TiltIndex].SetActive(true);
        // Apply watered material to the current sphere's primary renderer only
        var renderers = m_Spheres[m_WaterIndex].GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.materials = new Material[] { m_TiltedMaterial };
        }

        // advance index
        m_TiltIndex = (m_TiltIndex + 1) % m_Spheres.Count;
    }

    // Watering: change materials of sphere children to the watered variant
    [ContextMenu("Water")]
    public void Water()
    {
        if (m_Spheres.Count == 0) RefreshSpheres();
        if (m_Spheres.Count == 0) return;
        if (m_WateredMaterial == null) return;

        // Apply watered material to the current sphere's primary renderer only
        var renderers = m_Spheres[m_WaterIndex].GetComponentsInChildren<Renderer>();
        foreach (var rend in renderers)
        {
            rend.materials = new Material[] { m_WateredMaterial };
        }

        // advance index to match Tilt behavior
        m_WaterIndex = (m_WaterIndex + 1) % m_Spheres.Count;
    }

    // Growing: activate Cylinder child on each sphere
    [ContextMenu("Grow")]
    public void Grow()
    {
        if (m_Spheres.Count == 0) RefreshSpheres();

        foreach (var sphere in m_Spheres)
        {
            var cyl = sphere.transform.Find("Cylinder");
            if (cyl != null)
                cyl.gameObject.SetActive(true);
        }
    }

    // Harvest: reset everything — hide spheres and cylinders and restore materials
    [ContextMenu("Harvest")]
    public void Harvest()
    {
        if (m_Spheres.Count == 0) RefreshSpheres();

        foreach (var sphere in m_Spheres)
        {
            sphere.SetActive(false);

            var cyl = sphere.transform.Find("Cylinder");
            if (cyl != null)
                cyl.gameObject.SetActive(false);

            var renderers = sphere.GetComponentsInChildren<Renderer>();
            foreach (var rend in renderers)
            {
                rend.materials = new Material[] { m_TiltedMaterial };
            }
        }

        // reset tilt index
        m_TiltIndex = 0;
        m_WaterIndex = 0;
    }

    // Optional helper to expose in inspector for manual refresh
    [ContextMenu("Refresh Spheres")]
    public void Refresh()
    {
        RefreshSpheres();
    }
}
