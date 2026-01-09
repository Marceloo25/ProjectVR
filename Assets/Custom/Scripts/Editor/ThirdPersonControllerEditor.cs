using UnityEditor;
using UnityEngine;
using StarterAssets;

[CustomEditor(typeof(ThirdPersonController))]
public class ThirdPersonControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var controller = (ThirdPersonController)target;
        if (controller == null)
            return;

        GUILayout.Space(8);

        var ragdoll = controller.GetComponent<ThirdPersonRagdoll>();
        if (ragdoll == null)
        {
            EditorGUILayout.HelpBox("Add ThirdPersonRagdoll to enable ragdoll toggling for this character.", MessageType.Info);
            if (GUILayout.Button("Add ThirdPersonRagdoll"))
            {
                Undo.AddComponent<ThirdPersonRagdoll>(controller.gameObject);
                EditorUtility.SetDirty(controller);
            }
            return;
        }

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            if (GUILayout.Button("Enable Ragdoll"))
            {
                ragdoll.SetRagdollActive(true);
            }

            if (GUILayout.Button("Disable Ragdoll"))
            {
                ragdoll.SetRagdollActive(false);
            }
        }

        EditorGUILayout.EndHorizontal();

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Ragdoll toggling buttons are available in Play Mode.", MessageType.None);
        }
    }
}
