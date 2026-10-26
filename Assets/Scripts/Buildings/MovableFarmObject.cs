using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    public enum MovableType
    {
        Building,
        Pen,
        CropPlot,
        Machine,
        Fence,
        Decoration,
        Straw,
        Lumber,
        Ladder,
        Trough
    }

    /// <summary>
    /// Component attached dynamically or statically to farm objects that can be repositioned in Farm Edit Mode.
    /// Manages original transforms for reverting, footprint dimensions, and visual selection highlights.
    /// Supports Straw, Lumber, and Ladder editing, deletion, and categorization.
    /// </summary>
    [SelectionBase]
    public class MovableFarmObject : MonoBehaviour
    {
        [Header("Movable Details")]
        public MovableType objectType = MovableType.Building;
        public string objectId = "";
        public string displayName = "Vật thể";
        public string categoryGroupName = "CÔNG TRÌNH";
        public Vector3 footprintSize = new Vector3(4f, 2f, 4f);
        public bool isDeleted = false;

        [Header("Transform Tracking")]
        [HideInInspector] public Vector3 originalPosition;
        [HideInInspector] public Quaternion originalRotation;

        private GameObject selectionIndicator;
        private GameObject hoverIndicator;

        void Awake()
        {
            AutoDetectProperties();
        }

        public static string GenerateStableId(GameObject go)
        {
            if (go == null) return "";
            if (go.transform.parent != null &&
                go.transform.parent.name != "Farm" &&
                go.transform.parent.name != "Farm_Layout" &&
                go.transform.parent.name != "Environment")
            {
                return go.transform.parent.name + "/" + go.name;
            }
            return go.name;
        }

        public void AutoDetectProperties()
        {
            string objName = gameObject.name.ToLower();

            // 0. Water & Feed Troughs / Wells / Mangers (MÁNG NƯỚC / MÁNG ĂN)
            if (objName.Contains("trough") || objName.Contains("well") || objName.Contains("manger") || objName.Contains("hayrack"))
            {
                objectType = MovableType.Trough;
                categoryGroupName = "MÁNG NƯỚC";
                displayName = "Máng nước/ăn (" + gameObject.name + ")";
                if (string.IsNullOrEmpty(objectId)) objectId = GenerateStableId(gameObject);
                CalculateFootprintFromRenderers(new Vector3(2.0f, 1.0f, 1.2f));
                EnsureCollider();
                return;
            }

            // 1. Straw Bales / Rolls (RƠM)
            if (objName.Contains("straw") || objName.Contains("hay"))
            {
                objectType = MovableType.Straw;
                categoryGroupName = "RƠM";
                displayName = "Cục rơm (" + gameObject.name + ")";
                if (string.IsNullOrEmpty(objectId)) objectId = GenerateStableId(gameObject);
                CalculateFootprintFromRenderers(new Vector3(1.6f, 1.2f, 1.6f));
                EnsureCollider();
                return;
            }

            // 2. Lumber / Wood / Logs (GỖ)
            if (objName.Contains("lumber") || objName.Contains("wood") || (objName.StartsWith("log") && !objName.Contains("logo")))
            {
                objectType = MovableType.Lumber;
                categoryGroupName = "GỖ";
                displayName = "Khúc gỗ (" + gameObject.name + ")";
                if (string.IsNullOrEmpty(objectId)) objectId = GenerateStableId(gameObject);
                CalculateFootprintFromRenderers(new Vector3(2.5f, 1.2f, 2.5f));
                EnsureCollider();
                return;
            }

            // 3. Ladder / Stairs / Steps (CẦU THANG)
            if (objName.Contains("ladder") || objName.Contains("stair") || objName.Contains("step"))
            {
                objectType = MovableType.Ladder;
                categoryGroupName = "CẦU THANG";
                displayName = "Cầu thang (" + gameObject.name + ")";
                if (string.IsNullOrEmpty(objectId)) objectId = GenerateStableId(gameObject);
                CalculateFootprintFromRenderers(new Vector3(1.2f, 2.2f, 2.2f));
                EnsureCollider();
                return;
            }

            if (string.IsNullOrEmpty(objectId))
            {
                objectId = GenerateStableId(gameObject);
            }

            // 4. Farm Building (Pen, Barn, Storage)
            FarmBuilding fb = GetComponent<FarmBuilding>() ?? GetComponentInParent<FarmBuilding>();
            if (fb != null)
            {
                objectId = !string.IsNullOrEmpty(fb.buildingId) ? fb.buildingId : fb.gameObject.name;
                displayName = !string.IsNullOrEmpty(fb.displayName) ? fb.displayName : fb.gameObject.name;
                if (fb.allowedAnimalType == AnimalType.Cow || fb.buildingId == "cow_barn" || fb.buildingType == "cow_barn")
                {
                    objectType = MovableType.Pen;
                    categoryGroupName = "CHUỒNG VẬT NUÔI";
                    displayName = "Chuồng bò";
                    footprintSize = new Vector3(10.5f, 2.5f, 7.5f);
                }
                else if (fb.allowedAnimalType == AnimalType.Chicken || fb.buildingId == "chicken_coop" || fb.buildingType == "chicken_coop")
                {
                    objectType = MovableType.Pen;
                    categoryGroupName = "CHUỒNG VẬT NUÔI";
                    displayName = "Chuồng gà";
                    footprintSize = new Vector3(7.5f, 2.0f, 5.5f);
                }
                else if (fb.allowedAnimalType == AnimalType.Pig || fb.buildingId == "pig_pen" || fb.buildingType == "pig_pen")
                {
                    objectType = MovableType.Pen;
                    categoryGroupName = "CHUỒNG VẬT NUÔI";
                    displayName = "Chuồng heo";
                    footprintSize = new Vector3(8.5f, 2.2f, 6.5f);
                }
                else if (fb.allowedAnimalType == AnimalType.Horse || fb.buildingId == "horse_stable" || fb.buildingType == "horse_stable")
                {
                    objectType = MovableType.Pen;
                    categoryGroupName = "CHUỒNG VẬT NUÔI";
                    displayName = "Chuồng ngựa";
                    footprintSize = new Vector3(11.5f, 2.8f, 8.5f);
                }
                else if (fb.buildingId == "storage" || fb.buildingType == "storage")
                {
                    objectType = MovableType.Building;
                    categoryGroupName = "CÔNG TRÌNH";
                    displayName = "Kho nông trại";
                    footprintSize = new Vector3(6.5f, 4.5f, 6.5f);
                }
                else
                {
                    objectType = MovableType.Building;
                    categoryGroupName = "CÔNG TRÌNH";
                    footprintSize = new Vector3(6.0f, 3.5f, 6.0f);
                }
                return;
            }

            // 5. Crop Plot
            CropPlot plot = GetComponent<CropPlot>() ?? GetComponentInParent<CropPlot>();
            if (plot != null)
            {
                objectId = plot.gameObject.name;
                objectType = MovableType.CropPlot;
                categoryGroupName = "Ô ĐẤT";
                displayName = "Ô đất trồng (" + plot.gameObject.name + ")";
                footprintSize = new Vector3(2.4f, 0.6f, 2.4f);
                return;
            }

            // 6. Production Machine
            ProductionMachine pm = GetComponent<ProductionMachine>() ?? GetComponentInParent<ProductionMachine>();
            if (pm != null)
            {
                objectId = !string.IsNullOrEmpty(pm.machineId) ? pm.machineId : pm.gameObject.name;
                objectType = MovableType.Machine;
                categoryGroupName = "MÁY MÓC";
                displayName = !string.IsNullOrEmpty(pm.displayName) ? pm.displayName : pm.gameObject.name;
                footprintSize = new Vector3(3.2f, 2.5f, 3.2f);
                return;
            }

            // 7. Fence (HÀNG RÀO)
            if (objName.Contains("fence"))
            {
                objectType = MovableType.Fence;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Hàng rào (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(3.0f, 1.2f, 0.6f));
                EnsureCollider();
                return;
            }

            // 8. Farm Props & Environment
            if (objName.Contains("barn") || objName.Contains("garage") || objName.Contains("house"))
            {
                objectType = MovableType.Building;
                categoryGroupName = "CÔNG TRÌNH";
                displayName = "Công trình (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(6f, 4f, 6f));
                EnsureCollider();
                return;
            }

            // 9. Specific Decorations (ĐÈN, GHẾ, BẢNG HIỆU, MÁNG ĂN, CÂY XANH)
            if (objName.Contains("lamp") || objName.Contains("light") || objName.Contains("street"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Đèn trang trí (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(1.2f, 3.0f, 1.2f));
                EnsureCollider();
                return;
            }

            if (objName.Contains("bench"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Ghế gỗ (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(1.8f, 1.0f, 1.0f));
                EnsureCollider();
                return;
            }

            if (objName.Contains("sign"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Bảng hiệu (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(1.2f, 1.8f, 0.6f));
                EnsureCollider();
                return;
            }

            if (objName.Contains("trough") || objName.Contains("well"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Máng ăn / Máng nước (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(2.0f, 1.0f, 1.2f));
                EnsureCollider();
                return;
            }

            if (objName.Contains("tree"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Cây xanh (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(2.2f, 3.5f, 2.2f));
                EnsureCollider();
                return;
            }

            if (objName.Contains("tractor") || objName.Contains("tank") ||
                objName.Contains("rock") || objName.Contains("shroom") || objName.Contains("wall") || objName.Contains("deco"))
            {
                objectType = MovableType.Decoration;
                categoryGroupName = "TRANG TRÍ";
                displayName = "Vật trang trí (" + gameObject.name + ")";
                CalculateFootprintFromRenderers(new Vector3(2.0f, 1.5f, 2.0f));
                EnsureCollider();
                return;
            }

            // Default fallback
            objectType = MovableType.Decoration;
            categoryGroupName = "TRANG TRÍ";
            displayName = gameObject.name;
            CalculateFootprintFromRenderers(new Vector3(2.0f, 1.5f, 2.0f));
            EnsureCollider();
        }

        public void CalculateFootprintFromRenderers(Vector3 defaultSize)
        {
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            if (rends != null && rends.Length > 0)
            {
                Bounds b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++)
                {
                    if (rends[i] != null && rends[i].enabled)
                    {
                        string rn = rends[i].gameObject.name.ToLower();
                        if (rn.Contains("indicator") || rn.Contains("shadow")) continue;
                        b.Encapsulate(rends[i].bounds);
                    }
                }
                footprintSize = new Vector3(
                    Mathf.Max(0.8f, Mathf.Round(b.size.x * 2f) / 2f),
                    Mathf.Max(0.5f, Mathf.Round(b.size.y * 2f) / 2f),
                    Mathf.Max(0.8f, Mathf.Round(b.size.z * 2f) / 2f)
                );
            }
            else
            {
                footprintSize = defaultSize;
            }
        }

        public void EnsureCollider()
        {
            // Guarantee this object and its children are on a raycastable layer (default layer 0)
            if (gameObject.layer == 2) gameObject.layer = 0; // Layer 2 is Ignore Raycast
            foreach (Transform child in transform)
            {
                if (child.gameObject.layer == 2) child.gameObject.layer = 0;
            }

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            bool hasValidCol = false;
            foreach (var col in cols)
            {
                if (col != null)
                {
                    string colName = col.gameObject.name.ToLower();
                    if (colName.Contains("indicator")) continue;
                    col.enabled = true;
                    if (objectType == MovableType.Trough || objectType == MovableType.Straw)
                    {
                        col.isTrigger = true;
                    }
                    hasValidCol = true;
                }
            }

            if (!hasValidCol)
            {
                BoxCollider bc = gameObject.AddComponent<BoxCollider>();
                bc.size = footprintSize;
                bc.center = new Vector3(0f, footprintSize.y * 0.5f, 0f);
                if (objectType == MovableType.Trough || objectType == MovableType.Straw)
                {
                    bc.isTrigger = true;
                }
            }
        }

        public void RecordCurrentTransform()
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
        }

        public void RevertToOriginal()
        {
            transform.position = originalPosition;
            transform.rotation = originalRotation;

            GroundSnapUtility.SnapBuildingToGround(gameObject);

            AnimalPenBoundary pen = GetComponentInChildren<AnimalPenBoundary>(true);
            if (pen != null)
            {
                pen.NotifyPenMoved(Vector3.zero, Quaternion.identity);
            }
        }

        public void SetSelected(bool selected)
        {
            if (selected)
            {
                ClearHoverIndicator();
                CreateSelectionIndicator();
            }
            else
            {
                ClearSelectionIndicator();
            }
        }

        public void SetHovered(bool hovered)
        {
            if (hovered && selectionIndicator == null)
            {
                CreateHoverIndicator();
            }
            else
            {
                ClearHoverIndicator();
            }
        }

        private void CreateHoverIndicator()
        {
            ClearHoverIndicator();

            hoverIndicator = new GameObject("HoverIndicator_Temp");
            hoverIndicator.transform.SetParent(transform, false);
            hoverIndicator.transform.localPosition = new Vector3(0f, 0.06f, 0f);

            LineRenderer lr = hoverIndicator.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.12f;
            lr.endWidth = 0.12f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lr.startColor = new Color(1f, 0.88f, 0.2f, 0.85f);
            lr.endColor = new Color(1f, 0.88f, 0.2f, 0.85f);

            float hx = footprintSize.x * 0.5f;
            float hz = footprintSize.z * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-hx, 0f, -hz),
                new Vector3(-hx, 0f,  hz),
                new Vector3( hx, 0f,  hz),
                new Vector3( hx, 0f, -hz)
            };
            lr.positionCount = 4;
            lr.SetPositions(corners);
        }

        public void ClearHoverIndicator()
        {
            if (hoverIndicator != null)
            {
                hoverIndicator.SetActive(false);
                DestroyImmediate(hoverIndicator);
                hoverIndicator = null;
            }

            Transform existing = transform.Find("HoverIndicator_Temp");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                DestroyImmediate(existing.gameObject);
            }
        }

        private void CreateSelectionIndicator()
        {
            ClearSelectionIndicator();

            selectionIndicator = new GameObject("SelectionIndicator_Temp");
            selectionIndicator.transform.SetParent(transform, false);
            selectionIndicator.transform.localPosition = new Vector3(0f, 0.08f, 0f);

            LineRenderer lr = selectionIndicator.AddComponent<LineRenderer>();
            lr.useWorldSpace = false;
            lr.loop = true;
            lr.startWidth = 0.15f;
            lr.endWidth = 0.15f;
            lr.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            lr.startColor = new Color(0.2f, 0.85f, 1f, 0.95f);
            lr.endColor = new Color(0.2f, 0.85f, 1f, 0.95f);

            float hx = footprintSize.x * 0.5f;
            float hz = footprintSize.z * 0.5f;
            Vector3[] corners = new Vector3[]
            {
                new Vector3(-hx, 0f, -hz),
                new Vector3(-hx, 0f,  hz),
                new Vector3( hx, 0f,  hz),
                new Vector3( hx, 0f, -hz)
            };
            lr.positionCount = 4;
            lr.SetPositions(corners);
        }

        public void ClearSelectionIndicator()
        {
            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(false);
                DestroyImmediate(selectionIndicator);
                selectionIndicator = null;
            }

            Transform existing = transform.Find("SelectionIndicator_Temp");
            if (existing != null)
            {
                existing.gameObject.SetActive(false);
                DestroyImmediate(existing.gameObject);
            }
        }

        void OnDestroy()
        {
            ClearSelectionIndicator();
            ClearHoverIndicator();
        }

        /// <summary>
        /// Scans the scene to ensure all Straw, Lumber, Ladder, Troughs, and editable Farm Props
        /// have MovableFarmObject components with active colliders for Edit Mode.
        /// </summary>
        public static List<MovableFarmObject> ScanAndRegisterAllMapObjects()
        {
            List<MovableFarmObject> list = new List<MovableFarmObject>();
            Transform[] allScene = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            foreach (var t in allScene)
            {
                if (t == null) continue;
                string n = t.name.ToLower();

                // Skip UI, Cameras, Player, Indicators, Systems
                if (t.GetComponentInParent<Canvas>() != null) continue;
                if (n.Contains("canvas") || n.Contains("camera") || n.Contains("player") || n.Contains("indicator") || n.Contains("eventsystem") || n.Contains("light"))
                    continue;

                bool isStraw = n.Contains("straw") || n.Contains("hay");
                bool isLumber = n.Contains("lumber") || n.Contains("wood") || (n.StartsWith("log") && !n.Contains("logo"));
                bool isLadder = n.Contains("ladder") || n.Contains("stair") || n.Contains("step");
                bool isTrough = n.Contains("trough") || n.Contains("well") || n.Contains("manger") || n.Contains("hayrack");
                bool isDecor = n.Contains("bench") || n.Contains("sign") || n.Contains("lamp") || n.Contains("barrel") || n.Contains("box") || n.Contains("cart");

                if (isStraw || isLumber || isLadder || isTrough || isDecor)
                {
                    // Don't register sub-parts of a composite prop (e.g. TroughHay child inside FeedingTrough)
                    Transform parentCheck = t.parent;
                    bool hasDirectPropParent = false;
                    while (parentCheck != null)
                    {
                        string pn = parentCheck.name.ToLower();
                        if (pn.Contains("trough") || pn.Contains("ladder") || pn.Contains("stair") || pn.Contains("straw") || pn.Contains("hay"))
                        {
                            hasDirectPropParent = true;
                            break;
                        }
                        if (parentCheck.name == "Farm" || parentCheck.name == "Farm_Layout" || parentCheck.name == "Environment")
                        {
                            break;
                        }
                        parentCheck = parentCheck.parent;
                    }
                    if (hasDirectPropParent) continue;

                    t.gameObject.isStatic = false;
                    foreach (Transform child in t)
                    {
                        child.gameObject.isStatic = false;
                    }

                    MovableFarmObject m = t.GetComponent<MovableFarmObject>();
                    if (m == null)
                    {
                        m = t.gameObject.AddComponent<MovableFarmObject>();
                    }
                    if (string.IsNullOrEmpty(m.objectId))
                    {
                        m.objectId = GenerateStableId(t.gameObject);
                    }
                    m.AutoDetectProperties();
                    m.EnsureCollider();
                    m.RecordCurrentTransform();
                    if (!list.Contains(m)) list.Add(m);
                }
            }
            return list;
        }

        public static MovableFarmObject ResolveMovable(GameObject candidate)
        {
            if (candidate == null) return null;

            // Reject Player, Camera, Terrain, UI Canvas, System Objects, Pets
            if (candidate.CompareTag("Player") || candidate.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0) return null;
            if (candidate.CompareTag("MainCamera") || candidate.GetComponent<Camera>() != null || candidate.name.IndexOf("Camera", StringComparison.OrdinalIgnoreCase) >= 0) return null;
            if (candidate.GetComponent<Terrain>() != null || candidate.GetComponent<TerrainCollider>() != null || candidate.CompareTag("Terrain")) return null;
            if (candidate.GetComponentInParent<Canvas>() != null) return null;
            if (candidate.GetComponentInParent<PetWanderer>() != null) return null;
            if (candidate.name == "EventSystem" || candidate.name == "Directional Light" || candidate.name == "LightingOptimizer" || candidate.name == "GroundDebugReporter") return null;

            // 1. High-priority check: Is candidate or an ancestor up to pen/building a specific Prop (Trough, Ladder, Straw, Lumber)?
            Transform curr = candidate.transform;
            while (curr != null)
            {
                if (curr.GetComponent<FarmBuilding>() != null || curr.GetComponent<CropPlot>() != null)
                {
                    break;
                }

                string cName = curr.name.ToLower();
                bool isProp = cName.Contains("trough") || cName.Contains("well") || cName.Contains("manger") || cName.Contains("hayrack") ||
                              cName.Contains("straw") || cName.Contains("hay") ||
                              cName.Contains("lumber") || cName.Contains("ladder") ||
                              cName.Contains("stair") || (cName.StartsWith("log") && !cName.Contains("logo")) ||
                              cName.Contains("bench") || cName.Contains("sign");

                if (isProp)
                {
                    MovableFarmObject propM = curr.GetComponent<MovableFarmObject>() ?? curr.gameObject.AddComponent<MovableFarmObject>();
                    propM.AutoDetectProperties();
                    propM.EnsureCollider();
                    return propM;
                }

                if (curr.parent == null || curr.parent.name == "Farm" || curr.parent.name == "Farm_Layout" || curr.parent.name == "Environment")
                {
                    break;
                }
                curr = curr.parent;
            }

            // 2. Direct MovableFarmObject component check in candidate or any parent
            MovableFarmObject direct = candidate.GetComponentInParent<MovableFarmObject>();
            if (direct != null)
            {
                direct.AutoDetectProperties();
                return direct;
            }

            // 3. Direct FarmBuilding
            FarmBuilding fb = candidate.GetComponentInParent<FarmBuilding>();
            if (fb != null)
            {
                MovableFarmObject m = fb.GetComponent<MovableFarmObject>() ?? fb.gameObject.AddComponent<MovableFarmObject>();
                m.AutoDetectProperties();
                return m;
            }

            // 4. Direct CropPlot
            CropPlot plot = candidate.GetComponentInParent<CropPlot>();
            if (plot != null)
            {
                MovableFarmObject m = plot.GetComponent<MovableFarmObject>() ?? plot.gameObject.AddComponent<MovableFarmObject>();
                m.AutoDetectProperties();
                return m;
            }

            // 5. Direct ProductionMachine
            ProductionMachine pm = candidate.GetComponentInParent<ProductionMachine>();
            if (pm != null)
            {
                MovableFarmObject m = pm.GetComponent<MovableFarmObject>() ?? pm.gameObject.AddComponent<MovableFarmObject>();
                m.AutoDetectProperties();
                return m;
            }

            // 6. Animal clicked -> resolve to parent pen
            AnimalCareController acc = candidate.GetComponentInParent<AnimalCareController>();
            if (acc != null)
            {
                FarmBuilding penBarn = acc.GetComponentInParent<FarmBuilding>();
                if (penBarn != null)
                {
                    MovableFarmObject m = penBarn.GetComponent<MovableFarmObject>() ?? penBarn.gameObject.AddComponent<MovableFarmObject>();
                    m.AutoDetectProperties();
                    return m;
                }
            }

            // 7. Check if under Farm, Farm_Layout, Environment, Decorations (Fences, Barn, Garages, Tractor, Trees, Rocks)
            Transform t = candidate.transform;
            while (t != null && t.parent != null)
            {
                if (t.parent.name == "Farm" || t.parent.name == "Environment" || t.parent.name == "Decorations" || t.parent.name.Contains("Farm_Layout"))
                {
                    MovableFarmObject m = t.GetComponent<MovableFarmObject>() ?? t.gameObject.AddComponent<MovableFarmObject>();
                    m.AutoDetectProperties();
                    return m;
                }
                t = t.parent;
            }

            return null;
        }
    }
}
