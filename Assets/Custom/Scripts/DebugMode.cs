using UnityEngine;

[ExecuteAlways]
public class DebugMode : MonoBehaviour
{
    [Header("Debug Drawer")]
    [Tooltip("When true the debug drawer runs and draws collider wireframes in the Scene/Game view using Gizmos.")]
    public bool Enabled = false;

    [Tooltip("Color used to draw collider wireframes.")]
    public Color gizmoColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Tooltip("Layer mask to filter which colliders are drawn.")]
    public LayerMask layerMask = 0;

    [Tooltip("Include inactive GameObjects when drawing colliders.")]
    public bool includeInactive = false;

    void OnValidate()
    {
        // If the user hasn't set a specific mask, try to enable common layers if they exist: "Resource" and "Tool".
        if (layerMask == 0)
        {
            int mask = 0;
            int res = LayerMask.NameToLayer("Resource");
            int tool = LayerMask.NameToLayer("Tool");
            if (res >= 0) mask |= (1 << res);
            if (tool >= 0) mask |= (1 << tool);
            if (mask != 0) layerMask = mask;
        }
    }

    // Draw collider shapes per-type for accurate debugging.
    void OnDrawGizmos()
    {
        if (!Enabled) return;

        Gizmos.color = gizmoColor;

        Collider[] cols = null;

#if UNITY_2022_2_OR_NEWER
        cols = UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
#else
        cols = FindObjectsOfType<Collider>(true);
#endif
        if (cols == null) return;

        Matrix4x4 oldMat = Gizmos.matrix;

        foreach (var c in cols)
        {
            if (c == null) continue;
            var go = c.gameObject;
            if (go == null) continue;
            if (!includeInactive && !go.activeInHierarchy) continue;
            if ((layerMask.value & (1 << go.layer)) == 0) continue;

            if (c is BoxCollider box)
            {
                // Draw rotated box matching the collider's center and size
                Gizmos.matrix = box.transform.localToWorldMatrix;
                Gizmos.DrawWireCube(box.center, box.size);
            }
            else if (c is SphereCollider sph)
            {
                Vector3 worldCenter = sph.transform.TransformPoint(sph.center);
                float maxScale = Mathf.Max(sph.transform.lossyScale.x, Mathf.Max(sph.transform.lossyScale.y, sph.transform.lossyScale.z));
                float worldRadius = sph.radius * maxScale;
                Gizmos.DrawWireSphere(worldCenter, worldRadius);
            }
            else if (c is CapsuleCollider cap)
            {
                // Approximate capsule with two spheres and a connecting line
                Transform t = cap.transform;
                Vector3 center = cap.center;
                int dir = cap.direction; // 0=x,1=y,2=z
                Vector3 axis = dir == 0 ? Vector3.right : (dir == 1 ? Vector3.up : Vector3.forward);
                float maxScale = Mathf.Max(t.lossyScale.x, Mathf.Max(t.lossyScale.y, t.lossyScale.z));
                float radius = cap.radius * maxScale;
                float height = Mathf.Max(0f, cap.height * maxScale - 2f * radius);

                Vector3 localOffset = axis * (height * 0.5f);
                Vector3 pa = t.TransformPoint(center + localOffset);
                Vector3 pb = t.TransformPoint(center - localOffset);

                Gizmos.DrawWireSphere(pa, radius);
                Gizmos.DrawWireSphere(pb, radius);
                Gizmos.DrawLine(pa, pb);
            }
            else if (c is MeshCollider meshCol && meshCol.sharedMesh != null)
            {
                var mesh = meshCol.sharedMesh;
                Gizmos.matrix = meshCol.transform.localToWorldMatrix;
                Gizmos.DrawWireMesh(mesh);
            }
            else
            {
                // Fallback: draw axis-aligned bounds
                var b = c.bounds;
                Gizmos.matrix = Matrix4x4.identity;
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }

        Gizmos.matrix = oldMat;
    }
}
