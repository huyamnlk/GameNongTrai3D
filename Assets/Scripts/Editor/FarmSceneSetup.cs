using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using IrishFarmSim;

namespace IrishFarmSim.Editor
{
    [InitializeOnLoad]
    public static class FarmSceneSetup
    {
        static FarmSceneSetup()
        {
            EditorApplication.delayCall += OnEditorLoaded;
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnEditorLoaded()
        {
            UnityEngine.SceneManagement.Scene active = SceneManager.GetActiveScene();
            if (active.IsValid() && active.name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            {
                BakeCropAreaIntoCurrentScene(active);
            }
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            if (scene.IsValid() && scene.name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            {
                BakeCropAreaIntoCurrentScene(scene);
            }
        }

        [MenuItem("Tools/Farm/Bake 6 Crop Plots Into Farm Scene")]
        public static void BakeCropAreaCommand()
        {
            string scenePath = "Assets/Scenes/Farm.unity";
            UnityEngine.SceneManagement.Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            {
                scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            }

            BakeCropAreaIntoCurrentScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("[FarmSceneSetup] >>> Baked 6 CropPlots into Farm.unity and saved scene successfully! <<<");
        }

        public static void BakeCropAreaIntoCurrentScene(UnityEngine.SceneManagement.Scene scene)
        {
            if (Application.isPlaying) return;

            bool hadShowcase = GameObject.Find("=== FARM_ART_SHOWCASE ===") != null ||
                               GameObject.Find("=== FARM_ART_PREVIEW ===") != null ||
                               GameObject.Find("ShowcaseContainer") != null;
            AnimalManager.PurgeShowcaseAndSpriteArtifacts();

            GameObject container = GameObject.Find("CropArea");
            if (container == null)
            {
                container = new GameObject("CropArea");
                container.transform.position = Vector3.zero;
                Undo.RegisterCreatedObjectUndo(container, "Create CropArea");
            }

            GameObject player = GameObject.Find("Player");
            if (player == null) player = GameObject.FindGameObjectWithTag("Player");

            Vector3 centerPos = new Vector3(80f, 0f, 250f);
            Vector3 fwd = Vector3.forward;
            Vector3 right = Vector3.right;

            if (player != null)
            {
                fwd = player.transform.forward;
                if (fwd.sqrMagnitude < 0.01f) fwd = Vector3.forward;
                fwd.y = 0;
                fwd.Normalize();

                right = player.transform.right;
                if (right.sqrMagnitude < 0.01f) right = Vector3.right;
                right.y = 0;
                right.Normalize();

                centerPos = player.transform.position + fwd * 6.5f + right * 1.5f;
            }

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                centerPos.y = terrain.SampleHeight(centerPos) + terrain.transform.position.y;
            }

            string[] plotNames = new string[]
            {
                "CropPlot_01", "CropPlot_02", "CropPlot_03",
                "CropPlot_04", "CropPlot_05", "CropPlot_06"
            };

            float colSpacing = 3.2f;
            float rowSpacing = 3.2f;
            Vector2[] gridOffsets = new Vector2[]
            {
                new Vector2(-colSpacing, -rowSpacing * 0.5f),
                new Vector2( 0f,         -rowSpacing * 0.5f),
                new Vector2( colSpacing, -rowSpacing * 0.5f),
                new Vector2(-colSpacing,  rowSpacing * 0.5f),
                new Vector2( 0f,          rowSpacing * 0.5f),
                new Vector2( colSpacing,  rowSpacing * 0.5f)
            };

            for (int i = 0; i < 6; i++)
            {
                string pName = plotNames[i];
                Transform existing = container.transform.Find(pName);
                GameObject plotGo;
                if (existing != null)
                {
                    plotGo = existing.gameObject;
                }
                else
                {
                    Vector3 plotPos = centerPos + right * gridOffsets[i].x + fwd * gridOffsets[i].y;
                    if (terrain != null)
                    {
                        plotPos.y = terrain.SampleHeight(plotPos) + terrain.transform.position.y + 0.06f;
                    }

                    plotGo = new GameObject(pName);
                    plotGo.transform.SetParent(container.transform);
                    plotGo.transform.position = plotPos;
                    Undo.RegisterCreatedObjectUndo(plotGo, "Create " + pName);
                }

                CropPlot plot = plotGo.GetComponent<CropPlot>();
                if (plot == null) plot = plotGo.AddComponent<CropPlot>();

                plot.SetupVisualComponents();
                plot.UpdateVisuals();
            }

            if (hadShowcase && !Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log($"[FarmSceneSetup] Configured 6 CropPlots under CropArea near Player in {scene.name}.");
        }
    }
}
