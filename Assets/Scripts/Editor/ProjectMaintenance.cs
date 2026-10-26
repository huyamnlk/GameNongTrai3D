using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ProjectMaintenance
{
    private static readonly string[] RequiredScenes = new string[]
    {
        "Assets/Scenes/Main Menu.unity",
        "Assets/Scenes/Player Name.unity",
        "Assets/Scenes/Farm.unity",
        "Assets/Scenes/Mart.unity",
        "Assets/Scenes/Misc Menu.unity",
        "Assets/Scenes/Settings.unity"
    };

    [MenuItem("Tools/Project Fix/Execute All Fixes")]
    public static void ExecuteAllFixes()
    {
        Debug.Log("[ProjectMaintenance] Starting complete project fix & validation...");

        FixTerrainTreePrototypes();
        SetupBuildSettings();
        CheckAndFixScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[ProjectMaintenance] >>> ALL PROJECT FIXES COMPLETED SUCCESSFULLY <<<");
    }

    [MenuItem("Tools/Project Fix/Fix Terrain Tree Prototypes")]
    public static void FixTerrainTreePrototypes()
    {
        Debug.Log("[ProjectMaintenance] Checking TerrainData tree prototypes...");

        // Find fallback tree prefabs
        GameObject fallbackTree0 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3rd Party/Cartoon Town and Farm/Prefabs/Tree1.prefab");
        GameObject fallbackTree1 = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/3rd Party/Cartoon Town and Farm/Prefabs/Tree2.prefab");

        if (fallbackTree0 == null)
        {
            string[] guids = AssetDatabase.FindAssets("Tree1 t:Prefab");
            if (guids.Length > 0)
                fallbackTree0 = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }
        if (fallbackTree1 == null)
        {
            string[] guids = AssetDatabase.FindAssets("Tree2 t:Prefab");
            if (guids.Length > 0)
                fallbackTree1 = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        string[] terrainGuids = AssetDatabase.FindAssets("t:TerrainData");
        foreach (string guid in terrainGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
            if (terrainData == null) continue;

            TreePrototype[] prototypes = terrainData.treePrototypes;
            if (prototypes == null || prototypes.Length == 0) continue;

            bool modified = false;
            for (int i = 0; i < prototypes.Length; i++)
            {
                if (prototypes[i].prefab == null)
                {
                    Debug.LogWarning($"[ProjectMaintenance] TerrainData '{path}' has missing tree prefab at index {i}.");
                    GameObject replacement = (i % 2 == 0) ? fallbackTree0 : fallbackTree1;
                    if (replacement != null)
                    {
                        prototypes[i].prefab = replacement;
                        modified = true;
                        Debug.Log($"[ProjectMaintenance] Replaced tree prototype at index {i} with '{replacement.name}'.");
                    }
                }
            }

            if (modified)
            {
                terrainData.treePrototypes = prototypes;
                EditorUtility.SetDirty(terrainData);
                Debug.Log($"[ProjectMaintenance] Saved updated tree prototypes for '{path}'.");
            }
        }
    }

    [MenuItem("Tools/Project Fix/Setup Build Settings")]
    public static void SetupBuildSettings()
    {
        Debug.Log("[ProjectMaintenance] Setting up EditorBuildSettings scenes...");

        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();
        foreach (string scenePath in RequiredScenes)
        {
            if (File.Exists(scenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(scenePath, true));
                Debug.Log($"[ProjectMaintenance] Scene added to build settings: {scenePath}");
            }
            else
            {
                Debug.LogError($"[ProjectMaintenance] Scene NOT found: {scenePath}");
            }
        }

        EditorBuildSettings.scenes = buildScenes.ToArray();
        Debug.Log("[ProjectMaintenance] Build settings successfully configured.");
    }

    [MenuItem("Tools/Project Fix/Check and Save Scenes")]
    public static void CheckAndFixScenes()
    {
        Debug.Log("[ProjectMaintenance] Inspecting and verifying all scenes...");

        Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
        Lightmapping.Cancel();

        foreach (string scenePath in RequiredScenes)
        {
            if (!File.Exists(scenePath)) continue;

            Debug.Log($"[ProjectMaintenance] Opening scene: {scenePath}");
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Lightmapping.giWorkflowMode = Lightmapping.GIWorkflowMode.OnDemand;
            Lightmapping.bakedGI = false;
            Lightmapping.realtimeGI = false;
            Lightmapping.Cancel();

            GameObject[] roots = scene.GetRootGameObjects();
            int missingScriptsCount = 0;
            foreach (GameObject root in roots)
            {
                missingScriptsCount += CheckGameObjectRecursively(root);
            }

            if (missingScriptsCount > 0)
            {
                Debug.LogWarning($"[ProjectMaintenance] Scene '{scene.name}' contained {missingScriptsCount} missing script references.");
            }

            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[ProjectMaintenance] Scene '{scene.name}' verified and saved.");
        }

        // Leave Main Menu open as the active scene
        EditorSceneManager.OpenScene("Assets/Scenes/Main Menu.unity", OpenSceneMode.Single);
    }

    private static int CheckGameObjectRecursively(GameObject go)
    {
        int count = 0;
        Component[] components = go.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] == null)
            {
                count++;
                Debug.LogWarning($"[ProjectMaintenance] Missing component found on GameObject '{go.name}' (Path: {GetPath(go)})");
            }
        }

        for (int i = 0; i < go.transform.childCount; i++)
        {
            count += CheckGameObjectRecursively(go.transform.GetChild(i).gameObject);
        }

        return count;
    }

    private static string GetPath(GameObject go)
    {
        string path = go.name;
        Transform parent = go.transform.parent;
        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }
        return path;
    }
}
