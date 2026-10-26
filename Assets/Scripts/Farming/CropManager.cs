using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Manages the farm area and all 6 crop plots in Farm.unity.
    /// Ensures CropArea and CropPlot_01..06 exist, are visible in Game view,
    /// and are positioned on the ground near the Player.
    /// </summary>
    public class CropManager : MonoBehaviour
    {
        public static CropManager Instance { get; private set; }

        public List<CropPlot> plots = new List<CropPlot>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnSceneLoaded()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name.Equals("Farm", System.StringComparison.OrdinalIgnoreCase))
            {
                EnsureInstance();
            }
        }

        public static CropManager EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.FindOrCreatePlots();
                return Instance;
            }

            CropManager found = FindAnyObjectByType<CropManager>();
            if (found != null)
            {
                Instance = found;
                found.FindOrCreatePlots();
                return found;
            }

            GameObject go = new GameObject("CropManager");
            Instance = go.AddComponent<CropManager>();
            Instance.FindOrCreatePlots();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        void Start()
        {
            FindOrCreatePlots();
            CropDebugController.EnsureInstance();
        }

        public void FindOrCreatePlots()
        {
            plots.Clear();
            plots.AddRange(FindObjectsByType<CropPlot>(FindObjectsInactive.Include, FindObjectsSortMode.None));

            // Purge any plots that are not unlocked
            for (int i = plots.Count - 1; i >= 0; i--)
            {
                CropPlot p = plots[i];
                if (p == null)
                {
                    plots.RemoveAt(i);
                    continue;
                }
                string pId = string.IsNullOrEmpty(p.plotId) ? p.gameObject.name : p.plotId;
                bool isUnlocked = false;
                if (FarmDatabase.Plots.TryGetValue(pId, out var pData))
                {
                    isUnlocked = pData.isUnlockedByDefault;
                }
                if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(pId))
                {
                    isUnlocked = true;
                }

                if (!isUnlocked)
                {
                    Debug.Log($"[CropManager] Purging unpurchased plot from scene: {pId}");
                    plots.RemoveAt(i);
                    if (Application.isPlaying) Destroy(p.gameObject);
                    else DestroyImmediate(p.gameObject);
                }
            }

            CreateDefaultFarmPatch();
            SyncPlotProgression();
        }

        public void SyncPlotProgression()
        {
            foreach (var p in plots)
            {
                if (p == null) continue;
                string pId = string.IsNullOrEmpty(p.plotId) ? p.gameObject.name : p.plotId;
                if (FarmDatabase.Plots.TryGetValue(pId, out var pData))
                {
                    p.plotId = pId;
                    p.unlockLevel = pData.unlockLevel;
                    p.unlockCost = pData.unlockCost;
                    bool isUnlocked = pData.isUnlockedByDefault;
                    if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(pId))
                    {
                        isUnlocked = true;
                    }
                    p.isUnlocked = isUnlocked;
                    if (p.state == PlotState.Locked && isUnlocked)
                    {
                        p.state = PlotState.Empty;
                    }
                    p.UpdateVisuals();
                }
            }
        }

        public Vector3 GetFarmCropAreaAnchor()
        {
            // Find CropPlot_01 or calculate anchor from existing plots
            Transform cp1 = transform.Find("CropPlot_01");
            if (cp1 == null)
            {
                GameObject cp1Go = GameObject.Find("CropPlot_01");
                if (cp1Go != null) cp1 = cp1Go.transform;
            }

            if (cp1 != null)
            {
                float colSp = 3.2f;
                float rowSp = 3.2f;
                return cp1.position - new Vector3(-colSp, 0f, -rowSp * 0.5f);
            }

            return new Vector3(92f, 23.1f, 268f);
        }

        public void CreateDefaultFarmPatch()
        {
            GameObject container = GameObject.Find("CropArea");
            if (container == null)
            {
                container = new GameObject("CropArea");
                container.transform.position = Vector3.zero;
            }

            Vector3 centerPos = GetFarmCropAreaAnchor();
            Terrain terrain = Terrain.activeTerrain;

            // Only spawn plots that are unlocked (Default 6 plots + any unlocked in PlayerProgression)
            foreach (var kvp in FarmDatabase.Plots)
            {
                string pName = kvp.Key;
                PlotData pData = kvp.Value;

                bool shouldExist = pData.isUnlockedByDefault;
                if (PlayerProgression.Instance != null && PlayerProgression.Instance.IsPlotUnlocked(pName))
                {
                    shouldExist = true;
                }

                Transform existing = container.transform.Find(pName);
                if (!shouldExist)
                {
                    if (existing != null)
                    {
                        if (Application.isPlaying) Destroy(existing.gameObject);
                        else DestroyImmediate(existing.gameObject);
                    }
                    continue;
                }

                CropPlot plot = null;
                if (existing != null)
                {
                    plot = existing.GetComponent<CropPlot>();
                }
                else
                {
                    // Compute world position based on grid offset aligned with farm field
                    Vector3 plotPos = centerPos + new Vector3(pData.gridOffset.x, 0f, pData.gridOffset.y);
                    plotPos.y = GroundSnapUtility.SampleGroundElevation(plotPos.x, plotPos.z) + 0.07f;

                    GameObject plotGo = new GameObject(pName);
                    plotGo.transform.SetParent(container.transform);
                    plotGo.transform.position = plotPos;

                    plot = plotGo.AddComponent<CropPlot>();
                }

                if (plot != null)
                {
                    plot.plotId = pName;
                    plot.unlockLevel = pData.unlockLevel;
                    plot.unlockCost = pData.unlockCost;
                    plot.isUnlocked = true;
                    if (plot.state == PlotState.Locked)
                    {
                        plot.state = PlotState.Empty;
                    }
                    plot.SetupVisualComponents();
                    plot.UpdateVisuals();

                    MovableFarmObject m = plot.GetComponent<MovableFarmObject>();
                    if (m == null) m = plot.gameObject.AddComponent<MovableFarmObject>();
                    m.objectId = pName;
                    m.displayName = pData.displayName;
                    m.footprintSize = new Vector3(3f, 1f, 3f);
                    m.originalPosition = plot.transform.position;
                    m.originalRotation = plot.transform.rotation;

                    if (!plots.Contains(plot)) plots.Add(plot);
                }
            }

            Debug.Log($"[CropManager] CropArea successfully verified with {plots.Count} unlocked plots.");
        }

        /// <summary>
        /// Spawns a crop plot at an exact position and rotation (e.g. from placement mode or load).
        /// </summary>
        public CropPlot SpawnPlotAtPosition(string plotId, Vector3 position, Quaternion rotation)
        {
            if (string.IsNullOrEmpty(plotId)) return null;

            PlotData pData = null;
            if (FarmDatabase.Plots != null) FarmDatabase.Plots.TryGetValue(plotId, out pData);

            GameObject container = GameObject.Find("CropArea");
            if (container == null)
            {
                container = new GameObject("CropArea");
                container.transform.position = Vector3.zero;
            }

            Transform existing = container.transform.Find(plotId);
            if (existing == null)
            {
                GameObject rootGo = GameObject.Find(plotId);
                if (rootGo != null) existing = rootGo.transform;
            }

            GameObject plotGo;
            CropPlot plot;
            if (existing != null)
            {
                plotGo = existing.gameObject;
                plotGo.transform.SetParent(container.transform);
                plotGo.transform.position = position;
                plotGo.transform.rotation = rotation;
                plot = plotGo.GetComponent<CropPlot>();
                if (plot == null) plot = plotGo.AddComponent<CropPlot>();
            }
            else
            {
                plotGo = new GameObject(plotId);
                plotGo.transform.SetParent(container.transform);
                plotGo.transform.position = position;
                plotGo.transform.rotation = rotation;
                plot = plotGo.AddComponent<CropPlot>();
            }

            plot.plotId = plotId;
            plot.unlockLevel = pData != null ? pData.unlockLevel : 1;
            plot.unlockCost = pData != null ? pData.unlockCost : 0;
            plot.isUnlocked = true;
            if (plot.state == PlotState.Locked) plot.state = PlotState.Empty;

            plot.SetupVisualComponents();
            plot.UpdateVisuals();

            // Ensure MovableFarmObject is present for farm edit mode & interaction
            MovableFarmObject m = plotGo.GetComponent<MovableFarmObject>();
            if (m == null) m = plotGo.AddComponent<MovableFarmObject>();
            m.objectId = plotId;
            m.displayName = pData != null ? pData.displayName : "Ô đất trồng";
            m.footprintSize = new Vector3(3f, 1f, 3f);
            m.originalPosition = position;
            m.originalRotation = rotation;

            GroundSnapUtility.SnapObjectToGround(plotGo);

            if (!plots.Contains(plot)) plots.Add(plot);

            Debug.Log($"[CropManager] Successfully spawned plot at position: {plotId} at {plotGo.transform.position}");
            return plot;
        }

        /// <summary>
        /// Spawns a single plot in the farm grid when purchased from Shop or unlocked.
        /// </summary>
        public CropPlot SpawnSinglePlot(string plotId)
        {
            if (string.IsNullOrEmpty(plotId)) return null;
            if (!FarmDatabase.Plots.TryGetValue(plotId, out PlotData pData))
            {
                Debug.LogError($"[CropManager] Plot data not found for ID: {plotId}");
                return null;
            }

            Vector3 centerPos = GetFarmCropAreaAnchor();
            Vector3 plotPos = centerPos + new Vector3(pData.gridOffset.x, 0f, pData.gridOffset.y);
            plotPos.y = GroundSnapUtility.SampleGroundElevation(plotPos.x, plotPos.z) + 0.07f;

            return SpawnPlotAtPosition(plotId, plotPos, Quaternion.identity);
        }

        public void FastForwardAllCrops()
        {
            if (CropDebugController.Instance != null)
            {
                CropDebugController.Instance.TriggerF1ForceMatureAll();
            }
            else
            {
                foreach (var p in plots)
                {
                    if (p != null) p.ForceMature();
                }
            }
        }
    }
}
