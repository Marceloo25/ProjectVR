using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Farming))]
public class FarmingEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        Farming farm = (Farming)target;
        GUILayout.Space(8);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Tilt"))
        {
            farm.Tilt();
        }

        if (GUILayout.Button("Water"))
        {
            farm.Water();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Grow"))
        {
            farm.Grow();
        }

        if (GUILayout.Button("Harvest"))
        {
            farm.Harvest();
            EditorUtility.SetDirty(farm);
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Refresh Spheres"))
        {
            farm.Refresh();
        }
    }
}
