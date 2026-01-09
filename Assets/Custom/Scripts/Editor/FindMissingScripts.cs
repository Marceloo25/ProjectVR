using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

public static class FindMissingScripts
{
    [MenuItem("Tools/Find Missing Scripts/In Open Scene")]
    static void FindInOpenScene()
    {
        Debug.Log("Searching for missing scripts in current scene...");
        int count = FindInGameObjects(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None));
        if (count == 0)
            Debug.Log("No missing scripts found in current scene.");
    }

    [MenuItem("Tools/Find Missing Scripts/In All Scenes In Build")]
    static void FindInAllScenesInBuild()
    {
        Debug.Log("Searching for missing scripts in all scenes in Build Settings...");
        var sceneGuids = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        int total = 0;
        foreach (var scenePath in sceneGuids)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log($"Scanning scene: {scenePath}");
            total += FindInGameObjects(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None));
        }

        if (total == 0)
            Debug.Log("No missing scripts found in any build scenes.");
    }

    [MenuItem("Tools/Find Missing Scripts/In All Scenes In Project")]
    static void FindInAllScenesInProject()
    {
        Debug.Log("Searching for missing scripts in all scenes in the project...");
        // Only search scenes under Assets (skip package scenes/tests)
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        int total = 0;

        foreach (string guid in sceneGuids)
        {
            string scenePath = AssetDatabase.GUIDToAssetPath(guid);
            try
            {
                var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                Debug.Log($"Scanning scene: {scenePath}");
                total += FindInGameObjects(Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Skipping scene '{scenePath}': {e.Message}");
            }
        }

        if (total == 0)
            Debug.Log("No missing scripts found in any project scenes.");
        else
            Debug.Log($"Finished scanning project scenes. Total missing scripts found: {total}");
    }

    [MenuItem("Tools/Find Missing Scripts/In All Prefabs")]
    static void FindInAllPrefabs()
    {
        Debug.Log("Searching for missing scripts in all prefabs...");
        // Restrict search to project Assets (skip packages)
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
        int total = 0;

        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var gos = prefab.GetComponentsInChildren<Transform>(true)
                            .Select(t => t.gameObject)
                            .ToArray();

            total += FindInGameObjects(gos, prefix: $"[Prefab: {path}] ");
        }

        if (total == 0)
            Debug.Log("No missing scripts found in any prefabs.");
        else
            Debug.Log($"Finished scanning prefabs. Total missing scripts found: {total}");
    }

    static int FindInGameObjects(GameObject[] gos, string prefix = "")
    {
        int missingCount = 0;
        foreach (var go in gos)
        {
            var comps = go.GetComponents<MonoBehaviour>();
            for (int i = 0; i < comps.Length; i++)
            {
                if (comps[i] == null)
                {
                    missingCount++;
                    Debug.LogWarning($"{prefix}Missing script on {go.name}", go);
                }
            }
        }
        return missingCount;
    }
}