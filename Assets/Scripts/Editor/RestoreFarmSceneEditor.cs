#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace IrishFarmSim.Editor
{
    [InitializeOnLoad]
    public static class RestoreFarmSceneEditor
    {
        static RestoreFarmSceneEditor()
        {
            EditorApplication.delayCall += () =>
            {
                CheckAndAutoRestore();
            };
        }

        [MenuItem("Farm/Restore Farm Scene & Camera")]
        public static void MenuRestoreScene()
        {
            RestoreScene(true);
        }

        [MenuItem("Farm/Align Fences to Ground & Purge Discs")]
        public static void MenuAlignFencesAndPurgeDiscs()
        {
            FarmLightingOptimizer.CleanUpExtraneousGroundDiscs();
            int count = FenceGroundAligner.AlignAllInScene();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RestoreFarmSceneEditor] Ground-aligned {count} fence segments/pens and purged ground discs. Scene saved.");
        }

        public static void CheckAndAutoRestore()
        {
            if (Application.isPlaying) return;

            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.name.Equals("Farm", StringComparison.OrdinalIgnoreCase)) return;

            bool hadShowcase = GameObject.Find("=== FARM_ART_SHOWCASE ===") != null ||
                               GameObject.Find("=== FARM_ART_PREVIEW ===") != null ||
                               GameObject.Find("ShowcaseContainer") != null;
            if (hadShowcase)
            {
                AnimalManager.PurgeShowcaseAndSpriteArtifacts();
            }

            Camera existingCam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            GameObject farmRoot = GameObject.Find("Farm");
            GameObject cowPen = GameObject.Find("CowPen_Small") ?? GameObject.Find("CowPen_Large");
            GameObject chickenCoop = GameObject.Find("ChickenCoop_Small");
            GameObject pigPen = GameObject.Find("Purchased_PigPen");
            GameObject horseStable = GameObject.Find("Purchased_HorseStable");
            GameObject cropArea = GameObject.Find("CropArea");

            if (hadShowcase || existingCam == null || farmRoot == null || cowPen == null ||
                chickenCoop == null || pigPen == null || horseStable == null || cropArea == null)
            {
                Debug.Log("[RestoreFarmSceneEditor] Restoring clean Farm scene layout and purging showcase artifacts...");
                RestoreScene(false);
            }
            else
            {
                FarmSceneInitializer.OptimizeStoneWallColliders();
                FarmLightingOptimizer.CleanUpExtraneousGroundDiscs();
                FenceGroundAligner.AlignAllInScene();
            }
        }

        public static void RestoreScene(bool forceOpenScene)
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (forceOpenScene || !scene.name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene("Assets/Scenes/Farm.unity", OpenSceneMode.Single);
            }

            Debug.Log("[RestoreFarmSceneEditor] Restoring core prefabs, buildings, and fences in Farm scene...");

            // 1. Game Objects (Main Camera, Main Terrain, Player, Directional Light)
            GameObject gameObjectsRoot = GameObject.Find("Game Objects");
            if (gameObjectsRoot == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Farm/Game Objects.prefab");
                if (prefab != null)
                {
                    gameObjectsRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (gameObjectsRoot != null)
                    {
                        gameObjectsRoot.name = "Game Objects";
                        Debug.Log("[RestoreFarmSceneEditor] Restored 'Game Objects' prefab.");
                    }
                }
            }

            if (gameObjectsRoot != null)
            {
                gameObjectsRoot.transform.position = new Vector3(202.7074f, 20.94772f, 295.5883f);
                gameObjectsRoot.transform.rotation = Quaternion.identity;
                gameObjectsRoot.transform.localScale = Vector3.one;
            }

            // 2. Farm (Barn, Perimeter Fences, Garages, Tractor, Straw)
            GameObject farmRoot = GameObject.Find("Farm");
            if (farmRoot == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Farm/Farm.prefab");
                if (prefab != null)
                {
                    farmRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (farmRoot != null)
                    {
                        farmRoot.name = "Farm";
                        Debug.Log("[RestoreFarmSceneEditor] Restored 'Farm' prefab.");
                    }
                }
            }

            if (farmRoot != null)
            {
                farmRoot.transform.position = new Vector3(87.37000f, 24.12000f, 241.3800f);
                farmRoot.transform.rotation = new Quaternion(0f, 0.7162433f, 0f, 0.6978508f);
                farmRoot.transform.localScale = Vector3.one;

                // Reactivate Barn and all 21 perimeter fences
                int reactivatedFences = 0;
                for (int i = 0; i < farmRoot.transform.childCount; i++)
                {
                    Transform child = farmRoot.transform.GetChild(i);
                    if (child.name.StartsWith("Farm Fence") || child.name.StartsWith("Farm_fence"))
                    {
                        child.gameObject.SetActive(true);
                        reactivatedFences++;
                        for (int j = 0; j < child.childCount; j++)
                        {
                            child.GetChild(j).gameObject.SetActive(true);
                        }
                    }
                    else if (child.name == "Barn" || child.name.StartsWith("Garage") || child.name.StartsWith("Straw") ||
                             child.name.StartsWith("Tractor") || child.name == "Watertank" || child.name == "Walls")
                    {
                        child.gameObject.SetActive(true);
                    }
                }
                Debug.Log($"[RestoreFarmSceneEditor] Reactivated Barn and {reactivatedFences} perimeter fences under Farm.");
            }

            // 3. Environment (Trees, Rocks, Shrooms)
            GameObject envRoot = GameObject.Find("Environment");
            if (envRoot == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Farm/Environment.prefab");
                if (prefab != null)
                {
                    envRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (envRoot != null)
                    {
                        envRoot.name = "Environment";
                        Debug.Log("[RestoreFarmSceneEditor] Restored 'Environment' prefab.");
                    }
                }
            }

            if (envRoot != null)
            {
                envRoot.transform.position = new Vector3(151.5f, 23.58396f, 231.28f);
                envRoot.transform.rotation = Quaternion.identity;
                envRoot.transform.localScale = Vector3.one;
            }

            // 4. UI
            GameObject uiRoot = GameObject.Find("UI");
            if (uiRoot == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Farm/UI.prefab");
                if (prefab != null)
                {
                    uiRoot = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    if (uiRoot != null)
                    {
                        uiRoot.name = "UI";
                        Debug.Log("[RestoreFarmSceneEditor] Restored 'UI' prefab.");
                    }
                }
            }

            if (uiRoot != null)
            {
                uiRoot.transform.position = new Vector3(88.80164f, 27.87320f, 235.8676f);
                uiRoot.transform.rotation = Quaternion.identity;
            }

            // 5. Restore 5 Default Buildings in hierarchy
            // 5.1 Cow Pen (Chuồng bò)
            RestoreBuildingInScene("CowPen_Small", "cow_barn", "Chuồng bò", 3,
                new Vector3(61.59f, 23.11f, 251.21f), "Assets/Prefabs/Farm/CowPen_Small.prefab", farmRoot);

            // 5.2 Chicken Coop (Chuồng gà)
            RestoreBuildingInScene("ChickenCoop_Small", "chicken_coop", "Chuồng gà", 5,
                new Vector3(62.13f, 23.11f, 241.07f), "Assets/Prefabs/Farm/ChickenCoop_Small.prefab", farmRoot);

            // 5.3 Pig Pen (Chuồng heo)
            RestoreBuildingInScene("Purchased_PigPen", "pig_pen", "Chuồng heo", 4,
                new Vector3(62.43f, 23.11f, 261.56f), "Assets/Prefabs/Farm/PigPen_Small.prefab", farmRoot);

            // 5.4 Horse Stable (Chuồng ngựa)
            RestoreBuildingInScene("Purchased_HorseStable", "horse_stable", "Chuồng ngựa", 4,
                new Vector3(74.36f, 23.11f, 261.54f), "Assets/Prefabs/Farm/HorseStable_Small.prefab", farmRoot);

            // 5.5 Storage (Kho)
            RestoreBuildingInScene("Storage", "storage", "Kho", 250,
                new Vector3(85.00f, 23.11f, 240.00f), "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House1.prefab", farmRoot);

            // 6. Restore CropArea and 6 CropPlots in hierarchy
            RestoreCropAreaInScene();

            // 6.5 Purge any leftover showcase/preview objects
            AnimalManager.PurgeShowcaseAndSpriteArtifacts();

            // 7. Purge obsolete machine_shed if present
            FarmBuilding[] allFbs = UnityEngine.Object.FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var fb in allFbs)
            {
                if (fb != null && (fb.buildingId == "machine_shed" || fb.name.ToLower().Contains("machine_shed")))
                {
                    Debug.Log($"[RestoreFarmSceneEditor] Purging machine_shed: {fb.name}");
                    UnityEngine.Object.DestroyImmediate(fb.gameObject);
                }
            }

            // 8. Player & Camera positioning
            GameObject player = GameObject.Find("Player") ?? GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = new Vector3(98.37f, 23.11f, 243.20f);
            }

            Camera cam = UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                GameObject camGo = new GameObject("Main Camera");
                cam = camGo.AddComponent<Camera>();
                camGo.tag = "MainCamera";
            }

            cam.gameObject.SetActive(true);
            cam.enabled = true;
            cam.targetDisplay = 0;
            cam.cullingMask = -1;
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.Skybox;

            if (player != null)
            {
                cam.transform.position = player.transform.position + new Vector3(0f, 6.5f, -9.5f);
                cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);

                CameraController cc = cam.GetComponent<CameraController>() ?? cam.gameObject.AddComponent<CameraController>();
                cc.target = player;
                cc.followTarget = true;
                cc.difVec = new Vector3(0f, 6.5f, -9.5f);
                cc.enabled = true;

                Movement mov = player.GetComponent<Movement>();
                if (mov != null) mov.cameraController = cc;
            }

            FarmSceneInitializer.DeduplicateAudioListeners(cam.gameObject);
            FarmSceneInitializer.OptimizeStoneWallColliders(player);
            FarmLightingOptimizer.CleanUpExtraneousGroundDiscs();
            FenceGroundAligner.AlignAllInScene();

            // 9. Save and mark dirty
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            Debug.Log("[RestoreFarmSceneEditor] Farm scene restored and saved successfully with all 5 buildings, fences, and crop plots!");
        }

        private static GameObject RestoreBuildingInScene(string objName, string buildingId, string displayName, int capacity, Vector3 worldPos, string prefabPath, GameObject farmParent)
        {
            GameObject existing = GameObject.Find(objName);
            if (existing == null)
            {
                FarmBuilding[] fbs = UnityEngine.Object.FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var fb in fbs)
                {
                    if (fb != null && fb.buildingId.Equals(buildingId, StringComparison.OrdinalIgnoreCase))
                    {
                        existing = fb.gameObject;
                        break;
                    }
                }
            }

            if (existing == null)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab != null)
                {
                    existing = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                }
                if (existing == null)
                {
                    existing = new GameObject(objName);
                    FarmBuilding.CreateProceduralBuilding(existing, buildingId, displayName);
                }
                existing.name = objName;
                Debug.Log($"[RestoreFarmSceneEditor] Instantiated building: {objName} ({displayName}).");
            }

            existing.SetActive(true);
            existing.transform.position = worldPos;
            existing.transform.rotation = Quaternion.identity;

            if (farmParent != null && existing.transform.parent != farmParent.transform)
            {
                existing.transform.SetParent(farmParent.transform, true);
            }

            FarmBuilding comp = existing.GetComponent<FarmBuilding>() ?? existing.AddComponent<FarmBuilding>();
            comp.buildingId = buildingId;
            comp.displayName = displayName;
            comp.capacity = capacity;
            comp.isPurchased = true;
            comp.EnsureVisualStructure();

            if (buildingId != "storage")
            {
                AnimalPenBoundary boundary = existing.GetComponent<AnimalPenBoundary>() ?? existing.GetComponentInChildren<AnimalPenBoundary>(true);
                if (boundary == null)
                {
                    boundary = existing.AddComponent<AnimalPenBoundary>();
                    boundary.innerBoundsSize = (buildingId == "cow_barn" || buildingId == "horse_stable") ? new Vector3(11f, 3f, 8f) : new Vector3(8f, 3f, 6f);
                    boundary.penType = buildingId;
                }
            }

            return existing;
        }

        private static void RestoreCropAreaInScene()
        {
            GameObject cropArea = GameObject.Find("CropArea");
            if (cropArea == null)
            {
                cropArea = new GameObject("CropArea");
                cropArea.transform.position = Vector3.zero;
            }
            cropArea.SetActive(true);

            Vector3[] plotPositions = new Vector3[]
            {
                new Vector3(90f, 23.11f, 260f),
                new Vector3(93f, 23.11f, 260f),
                new Vector3(96f, 23.11f, 260f),
                new Vector3(90f, 23.11f, 263f),
                new Vector3(93f, 23.11f, 263f),
                new Vector3(96f, 23.11f, 263f)
            };

            for (int i = 0; i < 6; i++)
            {
                string plotName = $"CropPlot_0{i + 1}";
                Transform plotT = cropArea.transform.Find(plotName);
                GameObject plotGo = plotT != null ? plotT.gameObject : GameObject.Find(plotName);

                if (plotGo == null)
                {
                    plotGo = new GameObject(plotName);
                    plotGo.transform.SetParent(cropArea.transform, false);

                    GameObject soil = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    soil.name = "Soil";
                    soil.transform.SetParent(plotGo.transform, false);
                    soil.transform.localPosition = new Vector3(0f, 0.1f, 0f);
                    soil.transform.localScale = new Vector3(2.6f, 0.2f, 2.6f);
                    Renderer sr = soil.GetComponent<Renderer>();
                    if (sr != null) sr.material.color = new Color(0.38f, 0.24f, 0.14f);

                    BoxCollider bc = plotGo.AddComponent<BoxCollider>();
                    bc.size = new Vector3(2.6f, 0.5f, 2.6f);
                    bc.center = new Vector3(0f, 0.25f, 0f);
                    bc.isTrigger = true;

                    CropPlot cp = plotGo.AddComponent<CropPlot>();
                    cp.state = PlotState.Empty;

                    Debug.Log($"[RestoreFarmSceneEditor] Created {plotName}.");
                }

                plotGo.SetActive(true);
                plotGo.transform.position = plotPositions[i];
            }
        }
    }
}
#endif
