using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    public enum BuildingType
    {
        CowBarn,
        ChickenCoop,
        PigPen,
        HorseStable,
        Storage,
        MachineShed,
        Other
    }

    /// <summary>
    /// Represents a functional farm building (Barn, Coop, Pen, Stable, Storage).
    /// Handles physical placement, click interactions, animal occupancy, and status visualization.
    /// Includes IsValidBuilding helper to prevent MissingReferenceException.
    /// </summary>
    public class FarmBuilding : MonoBehaviour
    {
        [Header("Building Data")]
        public string buildingId = "";
        public string displayName = "";
        public int capacity = 3;
        public bool isPurchased = false;
        public string buildingType = "";
        public BuildingType buildingTypeEnum = BuildingType.Other;
        public AnimalType allowedAnimalType = AnimalType.Cow;

        public static BuildingType ParseBuildingType(string id)
        {
            if (string.IsNullOrEmpty(id)) return BuildingType.Other;
            string lower = id.ToLower();
            if (lower.Contains("cow")) return BuildingType.CowBarn;
            if (lower.Contains("chicken") || lower.Contains("coop")) return BuildingType.ChickenCoop;
            if (lower.Contains("pig")) return BuildingType.PigPen;
            if (lower.Contains("horse") || lower.Contains("stable")) return BuildingType.HorseStable;
            if (lower.Contains("storage")) return BuildingType.Storage;
            if (lower.Contains("machine") || lower.Contains("shed")) return BuildingType.MachineShed;
            return BuildingType.Other;
        }

        public void EnforceTypeAndCapacity()
        {
            if (string.IsNullOrEmpty(buildingId) || (buildingId == "cow_barn" && (gameObject.name.ToLower().Contains("pig") || gameObject.name.ToLower().Contains("chicken") || gameObject.name.ToLower().Contains("horse") || gameObject.name.ToLower().Contains("machine") || gameObject.name.ToLower().Contains("shed") || gameObject.name.ToLower().Contains("storage"))))
            {
                string n = gameObject.name.ToLower();
                buildingId = n.Contains("horse") ? "horse_stable" :
                             (n.Contains("pig") ? "pig_pen" :
                             (n.Contains("chicken") ? "chicken_coop" :
                             (n.Contains("machine") || n.Contains("shed") ? "machine_shed" :
                             (n.Contains("storage") ? "storage" : "cow_barn"))));
            }

            buildingTypeEnum = ParseBuildingType(buildingId);
            if (buildingTypeEnum == BuildingType.Other) buildingTypeEnum = ParseBuildingType(buildingType);
            if (buildingTypeEnum == BuildingType.Other) buildingTypeEnum = ParseBuildingType(displayName);
            if (buildingTypeEnum == BuildingType.Other) buildingTypeEnum = ParseBuildingType(gameObject.name);

            if (buildingTypeEnum == BuildingType.CowBarn)
            {
                capacity = 3;
                allowedAnimalType = AnimalType.Cow;
                displayName = "Cow Barn / Chuồng bò";
                buildingType = "cow_barn";
                buildingId = "cow_barn";
            }
            else if (buildingTypeEnum == BuildingType.ChickenCoop)
            {
                capacity = 5;
                allowedAnimalType = AnimalType.Chicken;
                displayName = "Chicken Coop / Chuồng gà";
                buildingType = "chicken_coop";
                buildingId = "chicken_coop";
            }
            else if (buildingTypeEnum == BuildingType.PigPen)
            {
                capacity = 4;
                allowedAnimalType = AnimalType.Pig;
                displayName = "Pig Pen / Chuồng lợn";
                buildingType = "pig_pen";
                buildingId = "pig_pen";
            }
            else if (buildingTypeEnum == BuildingType.HorseStable)
            {
                capacity = 4;
                allowedAnimalType = AnimalType.Horse;
                displayName = "Horse Stable / Chuồng ngựa";
                buildingType = "horse_stable";
                buildingId = "horse_stable";
            }
            else if (buildingTypeEnum == BuildingType.Storage)
            {
                capacity = 50;
                allowedAnimalType = AnimalType.Cow;
                displayName = "Kho mở rộng (Storage Building)";
                buildingType = "storage";
                buildingId = "storage";
            }
        }

        public List<AnimalCareController> occupants = new List<AnimalCareController>();

        private GameObject visualRoot;
        private TextMesh statusBillboard;
        private Transform animalsContainer;

        /// <summary>
        /// Safety check helper to verify if a FarmBuilding reference is valid, active, and not destroyed.
        /// </summary>
        public static bool IsValidBuilding(FarmBuilding building)
        {
            if (building == null) return false;
            if (!building) return false;
            try
            {
                if (building.gameObject == null) return false;
                if (!building.gameObject) return false;
                if (!building.gameObject.activeInHierarchy) return false;
                if (string.IsNullOrEmpty(building.buildingId)) return false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public Transform AnimalsRoot
        {
            get
            {
                if (animalsContainer == null)
                {
                    Transform found = transform.Find("AnimalsContainer");
                    if (found != null)
                    {
                        animalsContainer = found;
                    }
                    else
                    {
                        GameObject go = new GameObject("AnimalsContainer");
                        go.transform.SetParent(transform, false);
                        animalsContainer = go.transform;
                    }
                }
                return animalsContainer;
            }
        }

        public bool IsGhostOrPreview()
        {
            if (gameObject.layer == LayerMask.NameToLayer("PlacementGhost")) return true;
            if (name.IndexOf("Ghost", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (name.IndexOf("Preview", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (GetComponent<BuildingPreview>() != null) return true;
            return false;
        }

        void Awake()
        {
            if (IsGhostOrPreview()) return;
            if (buildingType == "machine_shed" || buildingId == "machine_shed" || buildingTypeEnum == BuildingType.MachineShed ||
                gameObject.name.ToLower().Contains("machine_shed") || gameObject.name.ToLower().Contains("machineshed"))
            {
                Debug.Log($"[FarmBuilding] Purging obsolete machine_shed instance: {gameObject.name}");
                SafeDestroy(gameObject);
                return;
            }
            EnforceTypeAndCapacity();
            EnsureVisualStructure();
            SetupInteractionCollider();
            SetupBillboard();
        }

        void Start()
        {
            if (IsGhostOrPreview()) return;
            EnforceTypeAndCapacity();
            if (transform.position.y == 0f)
            {
                GroundSnapUtility.SnapBuildingToGround(gameObject);
            }
            ValidateRenderersAndMaterials(gameObject);
            RefreshVisuals();
        }

        public void SetupBuilding(BuildingData data, bool purchased)
        {
            if (data != null)
            {
                buildingId = data.buildingId;
                displayName = data.displayName;
                capacity = data.capacity;
                buildingType = data.buildingType;
            }

            EnforceTypeAndCapacity();
            isPurchased = purchased;

            if (IsGhostOrPreview()) return;

            EnsureVisualStructure();
            SetupInteractionCollider();
            SetupBillboard();
            if (transform.position.y == 0f)
            {
                GroundSnapUtility.SnapBuildingToGround(gameObject);
            }
            ValidateRenderersAndMaterials(gameObject);
            RefreshVisuals();
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
            {
                Destroy(obj);
            }
            else
            {
                DestroyImmediate(obj);
            }
        }

        public void EnsureVisualStructure()
        {
            if (IsGhostOrPreview()) return;

            buildingTypeEnum = ParseBuildingType(buildingId);
            Transform existingProc = transform.Find("ProceduralModel");
            if (existingProc != null)
            {
                Transform bedding = existingProc.Find("PenBedding") ?? existingProc.Find("StrawPatch_1");
                bool procOversized = (bedding != null && bedding.localScale.x > 15f);

                // Detect model mismatch: Chicken Coop, Cow Barn, Pig Pen, Horse Stable, Machine Shed props
                bool hasFeedingTrough = existingProc.Find("FeedingTrough") != null;
                bool hasNestingRoost = existingProc.Find("NestingRoost") != null;
                bool hasMudWallow = existingProc.Find("MudWallow") != null;
                bool hasHorseStall = existingProc.Find("HorseStallShed") != null;
                bool hasWorkshopBody = existingProc.Find("WorkshopBody") != null;
                bool isMismatched = false;
                if (buildingTypeEnum == BuildingType.ChickenCoop && (hasFeedingTrough || hasMudWallow || hasHorseStall || hasWorkshopBody || !hasNestingRoost)) isMismatched = true;
                if (buildingTypeEnum == BuildingType.CowBarn && (hasNestingRoost || hasMudWallow || hasHorseStall || hasWorkshopBody || !hasFeedingTrough)) isMismatched = true;
                if (buildingTypeEnum == BuildingType.PigPen && (hasNestingRoost || hasFeedingTrough || hasHorseStall || hasWorkshopBody || !hasMudWallow)) isMismatched = true;
                if (buildingTypeEnum == BuildingType.HorseStable && (hasNestingRoost || hasFeedingTrough || hasMudWallow || hasWorkshopBody || !hasHorseStall)) isMismatched = true;
                if (buildingTypeEnum == BuildingType.MachineShed && (hasFeedingTrough || hasNestingRoost || hasMudWallow || hasHorseStall || bedding != null || !hasWorkshopBody)) isMismatched = true;
                if (buildingTypeEnum == BuildingType.Storage && (hasFeedingTrough || hasNestingRoost || hasMudWallow || hasHorseStall || bedding != null || hasWorkshopBody)) isMismatched = true;

                if (procOversized || isMismatched)
                {
                    Debug.Log($"[FarmBuilding] Rebuilding ProceduralModel on {name} (Mismatched={isMismatched}, Oversized={procOversized}).");
                    existingProc.SetParent(null);
                    SafeDestroy(existingProc.gameObject);
                    existingProc = null;

                    string normId = (buildingTypeEnum == BuildingType.ChickenCoop) ? "chicken_coop" :
                                    (buildingTypeEnum == BuildingType.PigPen ? "pig_pen" :
                                    (buildingTypeEnum == BuildingType.HorseStable ? "horse_stable" :
                                    (buildingTypeEnum == BuildingType.CowBarn ? "cow_barn" :
                                    (buildingTypeEnum == BuildingType.MachineShed ? "machine_shed" :
                                    (buildingTypeEnum == BuildingType.Storage ? "storage" : buildingId)))));
                    CreateProceduralBuilding(gameObject, normId, displayName);
                    ValidateRenderersAndMaterials(gameObject);
                    return;
                }
                else
                {
                    Transform gateSign = existingProc.Find("GateSign");
                    if (gateSign != null)
                    {
                        TextMesh tm = gateSign.GetComponent<TextMesh>();
                        if (tm != null) tm.text = displayName.ToUpper();
                    }
                    ValidateRenderersAndMaterials(gameObject);
                    return;
                }
            }

            // Check if this object already has visible 3D renderers (excluding Billboard)
            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            bool hasValidVisibleMesh = false;
            bool isOversized = false;
            foreach (var r in rends)
            {
                if (r != null && r.gameObject.name != "Billboard")
                {
                    if (r.bounds.size.x > 15f || r.bounds.size.z > 15f)
                    {
                        isOversized = true;
                    }

                    MeshFilter mf = r.GetComponent<MeshFilter>();
                    if (mf != null && mf.sharedMesh != null && mf.sharedMesh.vertexCount > 0 && r.bounds.size.magnitude > 0.5f)
                    {
                        hasValidVisibleMesh = true;
                    }
                }
            }

            bool isProtectedPrefab = name.StartsWith("CowPen", StringComparison.OrdinalIgnoreCase) ||
                                     name.StartsWith("ChickenCoop", StringComparison.OrdinalIgnoreCase) ||
                                     name.StartsWith("PigPen", StringComparison.OrdinalIgnoreCase) ||
                                     name.StartsWith("HorseStable", StringComparison.OrdinalIgnoreCase) ||
                                     name.StartsWith("Purchased_", StringComparison.OrdinalIgnoreCase) ||
                                     name.IndexOf("Storage", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                     name.IndexOf("House1", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isOversized && !isProtectedPrefab)
            {
                Debug.Log($"[FarmBuilding] Detected oversized renderers on legacy object {name}. Rebuilding with compact dimensions.");
                for (int i = transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name != "Billboard" && child.name != "AnimalsContainer" && child.name != "CowPenBounds" && child.name != "ChickenCoopBounds" && child.name != "PigPenBounds" && child.name != "HorseStableBounds")
                    {
                        SafeDestroy(child.gameObject);
                    }
                }
                hasValidVisibleMesh = false;
            }

            if (!hasValidVisibleMesh && !isProtectedPrefab)
            {
                CreateProceduralBuilding(gameObject, buildingId, displayName);
            }
            ValidateRenderersAndMaterials(gameObject);
        }

        public static Shader GetSafeShader()
        {
            // Built-in Render Pipeline will fail and render magenta if URP shader is requested.
            Shader s = null;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null)
            {
                s = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (s == null) s = Shader.Find("Standard");
            if (s == null) s = Shader.Find("Legacy Shaders/Diffuse");
            if (s == null) s = Shader.Find("Mobile/Diffuse");
            if (s == null) s = Shader.Find("Diffuse");
            return s;
        }

        public static Material CreateBuildingMaterial(string matName, Color color)
        {
            Material mat = new Material(GetSafeShader());
            mat.name = matName;
            mat.color = color;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            return mat;
        }

        public static void CreateProceduralBuilding(GameObject root, string bId, string dName)
        {
            Transform existing = root.transform.Find("ProceduralModel");
            if (existing != null) return;

            BuildingType bType = ParseBuildingType(bId);
            if (bType == BuildingType.CowBarn || bType == BuildingType.ChickenCoop || bType == BuildingType.PigPen || bType == BuildingType.HorseStable)
            {
                string normId = (bType == BuildingType.ChickenCoop) ? "chicken_coop" :
                                (bType == BuildingType.PigPen ? "pig_pen" :
                                (bType == BuildingType.HorseStable ? "horse_stable" : "cow_barn"));
                CreateEnclosedFencePen(root, normId, dName);
                return;
            }

            if (bType == BuildingType.MachineShed || bId.ToLower().Contains("machine") || bId.ToLower().Contains("shed"))
            {
                CreateProceduralMachineShed(root, dName);
                return;
            }

            GameObject model = new GameObject("ProceduralModel");
            model.transform.SetParent(root.transform, false);

            Color wallColor = new Color(0.35f, 0.45f, 0.55f);
            Color roofColor = new Color(0.2f, 0.3f, 0.45f);
            if (bId == "horse_stable")
            {
                wallColor = new Color(0.48f, 0.3f, 0.15f);
                roofColor = new Color(0.35f, 0.2f, 0.12f);
            }

            // 1. House Main Body
            GameObject barnBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barnBody.name = "BarnBody";
            barnBody.transform.SetParent(model.transform, false);
            barnBody.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            barnBody.transform.localScale = new Vector3(5.0f, 3.0f, 5.0f);
            Collider cb = barnBody.GetComponent<Collider>();
            if (cb != null) SafeDestroy(cb);

            Renderer rBody = barnBody.GetComponent<Renderer>();
            if (rBody != null)
            {
                rBody.material = CreateBuildingMaterial("BarnBodyMat", wallColor);
            }

            // 2. House Roof
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "BarnRoof";
            roof.transform.SetParent(model.transform, false);
            roof.transform.localPosition = new Vector3(0f, 3.5f, 0f);
            roof.transform.localScale = new Vector3(5.6f, 1.0f, 5.6f);
            Collider cr = roof.GetComponent<Collider>();
            if (cr != null) SafeDestroy(cr);

            Renderer rRoof = roof.GetComponent<Renderer>();
            if (rRoof != null)
            {
                rRoof.material = CreateBuildingMaterial("BarnRoofMat", roofColor);
            }

            // 3. House Door Frame
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "BarnDoor";
            door.transform.SetParent(model.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.1f, 2.52f);
            door.transform.localScale = new Vector3(1.8f, 2.2f, 0.1f);
            Collider cd = door.GetComponent<Collider>();
            if (cd != null) SafeDestroy(cd);

            Renderer rDoor = door.GetComponent<Renderer>();
            if (rDoor != null)
            {
                rDoor.material = CreateBuildingMaterial("BarnDoorMat", new Color(0.92f, 0.9f, 0.85f));
            }

            if (!root.name.StartsWith("PlacementGhost") && root.layer != LayerMask.NameToLayer("PlacementGhost") && root.transform.position.y == 0f)
            {
                GroundSnapUtility.SnapBuildingToGround(root);
            }
        }

        /// <summary>
        /// Creates a procedural processing machine workshop (Xưởng chế biến)
        /// with foundation, workshop hall, industrial roof with exhaust vent, chimney pipe,
        /// wide roll-up/double doors, exterior workbench with anvil/tools, crates/barrels,
        /// brass gear emblem, lamp, and gate sign.
        /// </summary>
        public static void CreateProceduralMachineShed(GameObject root, string dName)
        {
            Transform existing = root.transform.Find("ProceduralModel");
            if (existing != null)
            {
                if (existing.Find("WorkshopBody") != null) return;
                existing.SetParent(null);
                SafeDestroy(existing.gameObject);
            }

            GameObject model = new GameObject("ProceduralModel");
            model.transform.SetParent(root.transform, false);

            Color foundationColor = new Color(0.38f, 0.40f, 0.42f); // Sturdy concrete slab
            Color wallColor = new Color(0.55f, 0.32f, 0.22f);       // Warm workshop timber / brick
            Color roofColor = new Color(0.22f, 0.32f, 0.42f);       // Slate / dark teal-grey
            Color ventColor = new Color(0.28f, 0.38f, 0.48f);
            Color ironColor = new Color(0.25f, 0.26f, 0.28f);       // Heavy dark iron
            Color doorColor = new Color(0.32f, 0.22f, 0.15f);       // Dark wood
            Color doorTrim = new Color(0.42f, 0.32f, 0.22f);
            Color woodBench = new Color(0.58f, 0.42f, 0.25f);
            Color brassColor = new Color(0.85f, 0.65f, 0.20f);      // Brass / Gold accent
            Color lampGlow = new Color(1.0f, 0.90f, 0.35f);

            // 1. Workshop Foundation (Concrete / stone base slab: 8.6m x 0.3m x 6.6m)
            GameObject foundation = GameObject.CreatePrimitive(PrimitiveType.Cube);
            foundation.name = "WorkshopFoundation";
            foundation.transform.SetParent(model.transform, false);
            foundation.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            foundation.transform.localScale = new Vector3(8.6f, 0.3f, 6.6f);
            Collider cf = foundation.GetComponent<Collider>();
            if (cf != null) SafeDestroy(cf);
            Renderer rf = foundation.GetComponent<Renderer>();
            if (rf != null) rf.material = CreateBuildingMaterial("WorkshopFoundationMat", foundationColor);

            // 2. Main Workshop Hall (WorkshopBody: 7.6m x 3.4m x 5.6m)
            GameObject workshopBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
            workshopBody.name = "WorkshopBody";
            workshopBody.transform.SetParent(model.transform, false);
            workshopBody.transform.localPosition = new Vector3(0f, 2.0f, 0f);
            workshopBody.transform.localScale = new Vector3(7.6f, 3.4f, 5.6f);
            Collider cb = workshopBody.GetComponent<Collider>();
            if (cb != null) SafeDestroy(cb);
            Renderer rb = workshopBody.GetComponent<Renderer>();
            if (rb != null) rb.material = CreateBuildingMaterial("WorkshopBodyMat", wallColor);

            // 3. Workshop Slanted Industrial Roof (WorkshopRoof: 8.2m x 0.9m x 6.2m)
            GameObject roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.name = "WorkshopRoof";
            roof.transform.SetParent(model.transform, false);
            roof.transform.localPosition = new Vector3(0f, 4.0f, 0f);
            roof.transform.localScale = new Vector3(8.2f, 0.9f, 6.2f);
            Collider cr = roof.GetComponent<Collider>();
            if (cr != null) SafeDestroy(cr);
            Renderer rr = roof.GetComponent<Renderer>();
            if (rr != null) rr.material = CreateBuildingMaterial("WorkshopRoofMat", roofColor);

            // 4. Roof Monitor / Ventilation Ridge
            GameObject vent = GameObject.CreatePrimitive(PrimitiveType.Cube);
            vent.name = "WorkshopRoofVent";
            vent.transform.SetParent(model.transform, false);
            vent.transform.localPosition = new Vector3(0f, 4.65f, 0f);
            vent.transform.localScale = new Vector3(3.4f, 0.55f, 2.2f);
            Collider cv = vent.GetComponent<Collider>();
            if (cv != null) SafeDestroy(cv);
            Renderer rv = vent.GetComponent<Renderer>();
            if (rv != null) rv.material = CreateBuildingMaterial("WorkshopVentMat", ventColor);

            // 5. Heavy Industrial Chimney / Exhaust Smoke Stack
            GameObject chimney = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimney.name = "WorkshopChimney";
            chimney.transform.SetParent(model.transform, false);
            chimney.transform.localPosition = new Vector3(2.4f, 4.8f, 1.4f);
            chimney.transform.localScale = new Vector3(0.65f, 1.4f, 0.65f);
            Collider cc = chimney.GetComponent<Collider>();
            if (cc != null) SafeDestroy(cc);
            Renderer rc = chimney.GetComponent<Renderer>();
            if (rc != null) rc.material = CreateBuildingMaterial("WorkshopChimneyMat", ironColor);

            // Chimney Brass Ring
            GameObject chimneyCap = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            chimneyCap.name = "WorkshopChimneyCap";
            chimneyCap.transform.SetParent(model.transform, false);
            chimneyCap.transform.localPosition = new Vector3(2.4f, 6.1f, 1.4f);
            chimneyCap.transform.localScale = new Vector3(0.75f, 0.15f, 0.75f);
            Collider ccc = chimneyCap.GetComponent<Collider>();
            if (ccc != null) SafeDestroy(ccc);
            Renderer rcc = chimneyCap.GetComponent<Renderer>();
            if (rcc != null) rcc.material = CreateBuildingMaterial("WorkshopBrassMat", brassColor);

            // 6. Wide Industrial Double Doors
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = "WorkshopDoor";
            door.transform.SetParent(model.transform, false);
            door.transform.localPosition = new Vector3(0f, 1.35f, -2.83f);
            door.transform.localScale = new Vector3(2.6f, 2.4f, 0.12f);
            Collider cd = door.GetComponent<Collider>();
            if (cd != null) SafeDestroy(cd);
            Renderer rd = door.GetComponent<Renderer>();
            if (rd != null) rd.material = CreateBuildingMaterial("WorkshopDoorMat", doorColor);

            // Door Cross Planks
            for (int i = 0; i < 2; i++)
            {
                float dx = (i == 0) ? -0.65f : 0.65f;
                GameObject cross = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cross.name = $"WorkshopDoorCross_{i + 1}";
                cross.transform.SetParent(model.transform, false);
                cross.transform.localPosition = new Vector3(dx, 1.35f, -2.91f);
                cross.transform.localRotation = Quaternion.Euler(0f, 0f, (i == 0 ? 30f : -30f));
                cross.transform.localScale = new Vector3(0.18f, 2.3f, 0.06f);
                Collider ccr = cross.GetComponent<Collider>();
                if (ccr != null) SafeDestroy(ccr);
                Renderer rcr = cross.GetComponent<Renderer>();
                if (rcr != null) rcr.material = CreateBuildingMaterial("WorkshopDoorTrimMat", doorTrim);
            }

            // 7. Exterior Machinist / Carpenter Workbench
            GameObject bench = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bench.name = "WorkshopBench";
            bench.transform.SetParent(model.transform, false);
            bench.transform.localPosition = new Vector3(-2.4f, 0.55f, -2.75f);
            bench.transform.localScale = new Vector3(1.8f, 0.8f, 0.8f);
            Collider cbe = bench.GetComponent<Collider>();
            if (cbe != null) SafeDestroy(cbe);
            Renderer rbe = bench.GetComponent<Renderer>();
            if (rbe != null) rbe.material = CreateBuildingMaterial("WorkshopWoodMat", woodBench);

            // Cast Iron Anvil on Bench
            GameObject anvil = GameObject.CreatePrimitive(PrimitiveType.Cube);
            anvil.name = "WorkshopAnvil";
            anvil.transform.SetParent(model.transform, false);
            anvil.transform.localPosition = new Vector3(-2.4f, 1.05f, -2.75f);
            anvil.transform.localScale = new Vector3(0.55f, 0.35f, 0.4f);
            Collider ca = anvil.GetComponent<Collider>();
            if (ca != null) SafeDestroy(ca);
            Renderer ra = anvil.GetComponent<Renderer>();
            if (ra != null) ra.material = CreateBuildingMaterial("WorkshopIronMat", ironColor);

            // 8. Storage Crate & Supply Barrel
            GameObject crate = GameObject.CreatePrimitive(PrimitiveType.Cube);
            crate.name = "WorkshopCrate";
            crate.transform.SetParent(model.transform, false);
            crate.transform.localPosition = new Vector3(2.3f, 0.45f, -2.75f);
            crate.transform.localScale = new Vector3(0.85f, 0.85f, 0.85f);
            Collider ccr1 = crate.GetComponent<Collider>();
            if (ccr1 != null) SafeDestroy(ccr1);
            Renderer rcr1 = crate.GetComponent<Renderer>();
            if (rcr1 != null) rcr1.material = CreateBuildingMaterial("WorkshopCrateMat", new Color(0.62f, 0.46f, 0.28f));

            GameObject barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "WorkshopBarrel";
            barrel.transform.SetParent(model.transform, false);
            barrel.transform.localPosition = new Vector3(3.0f, 0.55f, -2.65f);
            barrel.transform.localScale = new Vector3(0.7f, 0.9f, 0.7f);
            Collider cbar = barrel.GetComponent<Collider>();
            if (cbar != null) SafeDestroy(cbar);
            Renderer rbar = barrel.GetComponent<Renderer>();
            if (rbar != null) rbar.material = CreateBuildingMaterial("WorkshopBarrelMat", new Color(0.48f, 0.30f, 0.18f));

            // 9. Decorative Brass Cog / Gear above Doorway
            GameObject gear = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gear.name = "WorkshopGear";
            gear.transform.SetParent(model.transform, false);
            gear.transform.localPosition = new Vector3(0f, 2.95f, -2.85f);
            gear.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            gear.transform.localScale = new Vector3(0.9f, 0.08f, 0.9f);
            Collider cg = gear.GetComponent<Collider>();
            if (cg != null) SafeDestroy(cg);
            Renderer rg = gear.GetComponent<Renderer>();
            if (rg != null) rg.material = CreateBuildingMaterial("WorkshopBrassMat", brassColor);

            // 10. Illuminated Workshop Lantern above Entrance
            GameObject lamp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lamp.name = "WorkshopLamp";
            lamp.transform.SetParent(model.transform, false);
            lamp.transform.localPosition = new Vector3(0f, 3.55f, -2.95f);
            lamp.transform.localScale = new Vector3(0.35f, 0.35f, 0.35f);
            Collider cl = lamp.GetComponent<Collider>();
            if (cl != null) SafeDestroy(cl);
            Renderer rl = lamp.GetComponent<Renderer>();
            if (rl != null) rl.material = CreateBuildingMaterial("WorkshopLampMat", lampGlow);

            // 11. Workshop Gate Signboard
            GameObject signGo = new GameObject("GateSign");
            signGo.transform.SetParent(model.transform, false);
            signGo.transform.localPosition = new Vector3(0f, 4.35f, -3.15f);
            TextMesh signText = signGo.AddComponent<TextMesh>();
            signText.text = string.IsNullOrEmpty(dName) ? "XƯỞNG CHẾ BIẾN" : dName.ToUpper();
            signText.font = FarmUITheme.BoldFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signText.fontSize = 44;
            signText.characterSize = 0.065f;
            signText.fontStyle = FontStyle.Bold;
            signText.alignment = TextAlignment.Center;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.color = new Color(1f, 0.92f, 0.40f);
            MeshRenderer smr = signGo.GetComponent<MeshRenderer>();
            if (smr != null && signText.font != null)
            {
                smr.material = signText.font.material;
            }
            signGo.AddComponent<BillboardFaceCamera>();

            // Snap the entire building flush to ground (only during gameplay when position.y is 0)
            if (Application.isPlaying && !root.name.StartsWith("PlacementGhost") && root.layer != LayerMask.NameToLayer("PlacementGhost") && root.transform.position.y == 0f)
            {
                GroundSnapUtility.SnapBuildingToGround(root);
            }
            ValidateRenderersAndMaterials(root);
        }

        /// <summary>
        /// Creates a compact enclosed wooden fence pen with realistic post spacing,
        /// gate opening, ground bedding, props, and boundary.
        /// CowPen: 11.0m x 8.0m, Fence Height 1.7m (range 10-12 x 7-9 x 1.5-2).
        /// ChickenCoop: 8.0m x 6.0m, Fence Height 1.5m (range 7-9 x 5-7 x 1.2-1.8).
        /// </summary>
        public static void CreateEnclosedFencePen(GameObject root, string bId, string dName)
        {
            BuildingType bType = ParseBuildingType(bId);
            bool isChicken = (bType == BuildingType.ChickenCoop || bId == "chicken_coop");
            bool isPig = (bType == BuildingType.PigPen || bId == "pig_pen");
            bool isHorse = (bType == BuildingType.HorseStable || bId == "horse_stable");

            Transform existing = root.transform.Find("ProceduralModel");
            if (existing != null)
            {
                bool hasRoost = existing.Find("NestingRoost") != null;
                bool hasTrough = existing.Find("FeedingTrough") != null;
                bool hasMudWallow = existing.Find("MudWallow") != null;
                bool hasHorseStall = existing.Find("HorseStallShed") != null;
                if ((isChicken && !hasRoost) || (isPig && !hasMudWallow) || (isHorse && !hasHorseStall) || (!isChicken && !isPig && !isHorse && !hasTrough))
                {
                    existing.SetParent(null);
                    SafeDestroy(existing.gameObject);
                }
                else
                {
                    return;
                }
            }

            GameObject model = new GameObject("ProceduralModel");
            model.transform.SetParent(root.transform, false);

            Color woodDark = new Color(0.66f, 0.48f, 0.30f); // Warm Cedar Post color
            Color woodLight = new Color(0.84f, 0.68f, 0.46f); // Bright Golden Pine Rail color
            Color strawColor = new Color(0.96f, 0.88f, 0.50f); // Bright Straw bedding
            Color soilColor = new Color(0.58f, 0.46f, 0.32f); // Warm Meadow Loam
            Color paddockSoil = new Color(0.62f, 0.50f, 0.36f); // Warm Horse paddock ground
            Color mudColor = new Color(0.50f, 0.38f, 0.26f); // Soft Clay Mud wallow
            Color roofRed = new Color(0.88f, 0.32f, 0.24f); // Cheerful Terracotta Red
            Color roofWood = new Color(0.68f, 0.48f, 0.30f); // Warm Shingle Wood

            // Dimensions:
            // Cow Pen: Length (X) = 11.0m, Width (Z) = 8.0m, Height = 1.7m
            // Chicken Coop: Length (X) = 8.0m, Width (Z) = 6.0m, Height = 1.5m
            // Pig Pen: Length (X) = 9.0m, Width (Z) = 7.0m, Height = 1.4m
            // Horse Stable: Length (X) = 12.0m, Width (Z) = 9.0m, Height = 1.85m
            float penLenX = isChicken ? 8.0f : (isPig ? 9.0f : (isHorse ? 12.0f : 11.0f));
            float penWidZ = isChicken ? 6.0f : (isPig ? 7.0f : (isHorse ? 9.0f : 8.0f));
            float halfX = penLenX * 0.5f;
            float halfZ = penWidZ * 0.5f;
            float postH = isChicken ? 1.5f : (isPig ? 1.4f : (isHorse ? 1.85f : 1.7f));
            float postSize = isChicken ? 0.18f : (isPig ? 0.20f : (isHorse ? 0.24f : 0.22f));
            float topRailY = postH * 0.85f;
            float midRailY = postH * 0.52f;
            float botRailY = postH * 0.25f;
            float gateHalfWidth = isChicken ? 1.1f : (isPig ? 1.3f : (isHorse ? 1.6f : 1.45f));

            // 1. Bedding Ground
            GameObject bedding = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bedding.name = "PenBedding";
            bedding.transform.SetParent(model.transform, false);
            bedding.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            bedding.transform.localScale = new Vector3(penLenX, 0.04f, penWidZ);
            Collider cb = bedding.GetComponent<Collider>();
            if (cb != null) SafeDestroy(cb);
            Renderer rb = bedding.GetComponent<Renderer>();
            if (rb != null)
            {
                rb.material = CreateBuildingMaterial("PenBeddingMat", isChicken ? strawColor : soilColor);
            }

            // Mud wallow for Pig Pen
            if (isPig)
            {
                GameObject mudPatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                mudPatch.name = "MudWallow";
                mudPatch.transform.SetParent(model.transform, false);
                mudPatch.transform.localPosition = new Vector3(-halfX + 2.5f, 0.03f, 0.3f);
                mudPatch.transform.localScale = new Vector3(3.6f, 0.03f, 3.2f);
                Collider cmp = mudPatch.GetComponent<Collider>();
                if (cmp != null) SafeDestroy(cmp);
                Renderer rmp = mudPatch.GetComponent<Renderer>();
                if (rmp != null)
                {
                    rmp.material = CreateBuildingMaterial("MudWallowMat", mudColor);
                }
            }
            // Straw scatter patches for Cow Pen
            else if (!isChicken)
            {
                for (int i = 0; i < 2; i++)
                {
                    GameObject strawPatch = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    strawPatch.name = $"StrawPatch_{i + 1}";
                    strawPatch.transform.SetParent(model.transform, false);
                    float px = (i == 0) ? -2.5f : 2.5f;
                    float pz = (i == 0) ? 1.5f : -1.0f;
                    strawPatch.transform.localPosition = new Vector3(px, 0.03f, pz);
                    strawPatch.transform.localScale = new Vector3(3.5f, 0.03f, 2.5f);
                    Collider csp = strawPatch.GetComponent<Collider>();
                    if (csp != null) SafeDestroy(csp);
                    Renderer rsp = strawPatch.GetComponent<Renderer>();
                    if (rsp != null)
                    {
                        rsp.material = CreateBuildingMaterial("StrawPatchMat", new Color(0.78f, 0.68f, 0.32f));
                    }
                }
            }

            // Helper to create a wooden post
            void CreatePost(string name, Vector3 pos, float height = -1f, float size = -1f)
            {
                float h = height > 0 ? height : postH;
                float s = size > 0 ? size : postSize;
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                post.name = name;
                post.transform.SetParent(model.transform, false);
                post.transform.localPosition = new Vector3(pos.x, h * 0.5f, pos.z);
                post.transform.localScale = new Vector3(s, h, s);
                Collider cp = post.GetComponent<Collider>();
                if (cp != null) SafeDestroy(cp);
                Renderer rp = post.GetComponent<Renderer>();
                if (rp != null)
                {
                    rp.material = CreateBuildingMaterial("PostMat", woodDark);
                }
            }

            // Helper to create a horizontal fence rail
            void CreateRail(string name, Vector3 pos, Vector3 scale)
            {
                GameObject rail = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rail.name = name;
                rail.transform.SetParent(model.transform, false);
                rail.transform.localPosition = pos;
                rail.transform.localScale = scale;
                Collider cr = rail.GetComponent<Collider>();
                if (cr != null) SafeDestroy(cr);
                Renderer rr = rail.GetComponent<Renderer>();
                if (rr != null)
                {
                    rr.material = CreateBuildingMaterial("RailMat", woodLight);
                }
            }

            // 2. Posts along Perimeter with ~2.0m - 2.5m spacing
            // Back Wall (Z = halfZ, from -halfX to halfX)
            int backPostCount = Mathf.RoundToInt(penLenX / 2.2f) + 1;
            for (int i = 0; i < backPostCount; i++)
            {
                float t = (float)i / (backPostCount - 1);
                float x = Mathf.Lerp(-halfX, halfX, t);
                CreatePost($"Post_Back_{i}", new Vector3(x, 0f, halfZ));
            }

            // Left Wall (X = -halfX, from -halfZ to halfZ)
            int sidePostCount = Mathf.RoundToInt(penWidZ / 2.2f) + 1;
            for (int i = 1; i < sidePostCount - 1; i++)
            {
                float t = (float)i / (sidePostCount - 1);
                float z = Mathf.Lerp(-halfZ, halfZ, t);
                CreatePost($"Post_Left_{i}", new Vector3(-halfX, 0f, z));
            }

            // Right Wall (X = halfX, from -halfZ to halfZ)
            for (int i = 1; i < sidePostCount - 1; i++)
            {
                float t = (float)i / (sidePostCount - 1);
                float z = Mathf.Lerp(-halfZ, halfZ, t);
                CreatePost($"Post_Right_{i}", new Vector3(halfX, 0f, z));
            }

            // Front Wall (Z = -halfZ)
            // Gate Entrance Posts (taller with header beam across entrance)
            float gatePostH = postH * 1.35f;
            CreatePost("GatePost_Left", new Vector3(-gateHalfWidth, 0f, -halfZ), gatePostH, postSize * 1.2f);
            CreatePost("GatePost_Right", new Vector3(gateHalfWidth, 0f, -halfZ), gatePostH, postSize * 1.2f);

            // Gate Arch / Header Beam across top of entrance
            CreateRail("GateHeaderBeam", new Vector3(0f, gatePostH - 0.05f, -halfZ), new Vector3(gateHalfWidth * 2f + 0.3f, 0.15f, 0.18f));

            // Left Wing posts (from -halfX to -gateHalfWidth)
            float wingLen = halfX - gateHalfWidth;
            int wingPostCount = Mathf.Max(2, Mathf.RoundToInt(wingLen / 2.2f) + 1);
            for (int i = 0; i < wingPostCount; i++)
            {
                float t = (float)i / wingPostCount;
                float x = Mathf.Lerp(-halfX, -gateHalfWidth, t);
                CreatePost($"Post_FrontL_{i}", new Vector3(x, 0f, -halfZ));
            }

            // Right Wing posts (from gateHalfWidth to halfX)
            for (int i = 1; i <= wingPostCount; i++)
            {
                float t = (float)i / wingPostCount;
                float x = Mathf.Lerp(gateHalfWidth, halfX, t);
                CreatePost($"Post_FrontR_{i}", new Vector3(x, 0f, -halfZ));
            }

            // 3. Horizontal Fence Rails
            float frontWingMidL = -halfX + wingLen * 0.5f;
            float frontWingMidR = halfX - wingLen * 0.5f;

            // Back Wall Rails
            CreateRail("Rail_Back_Top", new Vector3(0f, topRailY, halfZ), new Vector3(penLenX, 0.10f, 0.08f));
            CreateRail("Rail_Back_Mid", new Vector3(0f, midRailY, halfZ), new Vector3(penLenX, 0.10f, 0.08f));
            CreateRail("Rail_Back_Bot", new Vector3(0f, botRailY, halfZ), new Vector3(penLenX, 0.10f, 0.08f));

            // Left Wall Rails
            CreateRail("Rail_Left_Top", new Vector3(-halfX, topRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));
            CreateRail("Rail_Left_Mid", new Vector3(-halfX, midRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));
            CreateRail("Rail_Left_Bot", new Vector3(-halfX, botRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));

            // Right Wall Rails
            CreateRail("Rail_Right_Top", new Vector3(halfX, topRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));
            CreateRail("Rail_Right_Mid", new Vector3(halfX, midRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));
            CreateRail("Rail_Right_Bot", new Vector3(halfX, botRailY, 0f), new Vector3(0.08f, 0.10f, penWidZ));

            // Front Wall - Left Wing
            CreateRail("Rail_FrontLeft_Top", new Vector3(frontWingMidL, topRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));
            CreateRail("Rail_FrontLeft_Mid", new Vector3(frontWingMidL, midRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));
            CreateRail("Rail_FrontLeft_Bot", new Vector3(frontWingMidL, botRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));

            // Front Wall - Right Wing
            CreateRail("Rail_FrontRight_Top", new Vector3(frontWingMidR, topRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));
            CreateRail("Rail_FrontRight_Mid", new Vector3(frontWingMidR, midRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));
            CreateRail("Rail_FrontRight_Bot", new Vector3(frontWingMidR, botRailY, -halfZ), new Vector3(wingLen, 0.10f, 0.08f));

            // 4. Fence Wall BoxColliders with margin before gate entrance to prevent pinching
            float gateClearanceMargin = 0.20f;
            float wingColLen = Mathf.Max(0.5f, wingLen - gateClearanceMargin);
            float colMidL = -halfX + wingColLen * 0.5f;
            float colMidR = halfX - wingColLen * 0.5f;

            void AddWallCollider(string colName, Vector3 center, Vector3 size)
            {
                GameObject colGo = new GameObject(colName);
                colGo.transform.SetParent(model.transform, false);
                colGo.transform.localPosition = center;
                BoxCollider col = colGo.AddComponent<BoxCollider>();
                col.size = size;
            }

            AddWallCollider("Collider_Wall_Back", new Vector3(0f, postH * 0.5f, halfZ), new Vector3(penLenX, postH, 0.25f));
            AddWallCollider("Collider_Wall_Left", new Vector3(-halfX, postH * 0.5f, 0f), new Vector3(0.25f, postH, penWidZ));
            AddWallCollider("Collider_Wall_Right", new Vector3(halfX, postH * 0.5f, 0f), new Vector3(0.25f, postH, penWidZ));
            AddWallCollider("Collider_Wall_FrontLeft", new Vector3(colMidL, postH * 0.5f, -halfZ), new Vector3(wingColLen, postH, 0.25f));
            AddWallCollider("Collider_Wall_FrontRight", new Vector3(colMidR, postH * 0.5f, -halfZ), new Vector3(wingColLen, postH, 0.25f));

            // 5. Interior Farm Props
            if (isChicken)
            {
                // Chicken Coop Nesting Roost Hen House in back corner
                float hx = halfX - 1.8f;
                float hz = halfZ - 1.5f;

                GameObject coopBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                coopBox.name = "NestingRoost";
                coopBox.transform.SetParent(model.transform, false);
                coopBox.transform.localPosition = new Vector3(hx, 0.6f, hz);
                coopBox.transform.localScale = new Vector3(1.8f, 1.2f, 1.4f);
                Collider ccb = coopBox.GetComponent<Collider>();
                if (ccb != null) SafeDestroy(ccb);
                Renderer rcb = coopBox.GetComponent<Renderer>();
                if (rcb != null)
                {
                    rcb.material = CreateBuildingMaterial("CoopBoxMat", new Color(0.72f, 0.52f, 0.32f));
                }

                // Roost Slanted Roof
                GameObject coopRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                coopRoof.name = "RoostRoof";
                coopRoof.transform.SetParent(model.transform, false);
                coopRoof.transform.localPosition = new Vector3(hx, 1.3f, hz);
                coopRoof.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                coopRoof.transform.localScale = new Vector3(2.0f, 0.2f, 1.6f);
                Collider ccr = coopRoof.GetComponent<Collider>();
                if (ccr != null) SafeDestroy(ccr);
                Renderer rcr = coopRoof.GetComponent<Renderer>();
                if (rcr != null)
                {
                    rcr.material = CreateBuildingMaterial("RoostRoofMat", roofRed);
                }

                // Grain Dish
                GameObject dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                dish.name = "GrainDish";
                dish.transform.SetParent(model.transform, false);
                dish.transform.localPosition = new Vector3(-1.5f, 0.08f, 0f);
                dish.transform.localScale = new Vector3(0.8f, 0.12f, 0.8f);
                Collider cd = dish.GetComponent<Collider>();
                if (cd != null) SafeDestroy(cd);
                Renderer rd = dish.GetComponent<Renderer>();
                if (rd != null)
                {
                    rd.material = CreateBuildingMaterial("GrainDishMat", new Color(0.92f, 0.82f, 0.35f));
                }
            }
            else if (isPig)
            {
                // Pig Pen Props:
                // 1. Pig Feeding Trough with slop / vegetables
                GameObject trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trough.name = "PigFeedTrough";
                trough.transform.SetParent(model.transform, false);
                trough.transform.localPosition = new Vector3(1.0f, 0.20f, halfZ - 0.9f);
                trough.transform.localScale = new Vector3(2.8f, 0.40f, 0.8f);
                Collider ct = trough.GetComponent<Collider>();
                if (ct != null) SafeDestroy(ct);
                Renderer rt = trough.GetComponent<Renderer>();
                if (rt != null) rt.material = CreateBuildingMaterial("TroughMat", new Color(0.40f, 0.26f, 0.15f));

                GameObject slop = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slop.name = "PigSlop";
                slop.transform.SetParent(model.transform, false);
                slop.transform.localPosition = new Vector3(1.0f, 0.35f, halfZ - 0.9f);
                slop.transform.localScale = new Vector3(2.6f, 0.15f, 0.65f);
                Collider cs = slop.GetComponent<Collider>();
                if (cs != null) SafeDestroy(cs);
                Renderer rs = slop.GetComponent<Renderer>();
                if (rs != null) rs.material = CreateBuildingMaterial("PigSlopMat", new Color(0.65f, 0.55f, 0.28f));

                // 2. Pig Shelter Shed (wooden lean-to in rear corner)
                float sx = halfX - 1.8f;
                float sz = halfZ - 1.6f;

                GameObject shelterRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelterRoof.name = "PigShelterRoof";
                shelterRoof.transform.SetParent(model.transform, false);
                shelterRoof.transform.localPosition = new Vector3(sx, 1.25f, sz);
                shelterRoof.transform.localRotation = Quaternion.Euler(15f, 0f, 0f);
                shelterRoof.transform.localScale = new Vector3(2.4f, 0.15f, 2.2f);
                Collider csr = shelterRoof.GetComponent<Collider>();
                if (csr != null) SafeDestroy(csr);
                Renderer rsr = shelterRoof.GetComponent<Renderer>();
                if (rsr != null) rsr.material = CreateBuildingMaterial("PigShelterMat", roofWood);

                // Straw Bedding under shelter
                GameObject shelterStraw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                shelterStraw.name = "PigShelterStraw";
                shelterStraw.transform.SetParent(model.transform, false);
                shelterStraw.transform.localPosition = new Vector3(sx, 0.035f, sz);
                shelterStraw.transform.localScale = new Vector3(2.2f, 0.03f, 2.0f);
                Collider css = shelterStraw.GetComponent<Collider>();
                if (css != null) SafeDestroy(css);
                Renderer rss = shelterStraw.GetComponent<Renderer>();
                if (rss != null) rss.material = CreateBuildingMaterial("StrawPatchMat", new Color(0.78f, 0.68f, 0.32f));

                // 3. Water Tub
                GameObject waterTub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                waterTub.name = "WaterTub";
                waterTub.transform.SetParent(model.transform, false);
                waterTub.transform.localPosition = new Vector3(-halfX + 1.2f, 0.16f, -halfZ + 1.4f);
                waterTub.transform.localScale = new Vector3(1.1f, 0.32f, 1.1f);
                Collider cwt = waterTub.GetComponent<Collider>();
                if (cwt != null) SafeDestroy(cwt);
                Renderer rwt = waterTub.GetComponent<Renderer>();
                if (rwt != null) rwt.material = CreateBuildingMaterial("WaterTubMat", woodDark);

                GameObject waterSurf = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                waterSurf.name = "WaterTubSurface";
                waterSurf.transform.SetParent(model.transform, false);
                waterSurf.transform.localPosition = new Vector3(-halfX + 1.2f, 0.28f, -halfZ + 1.4f);
                waterSurf.transform.localScale = new Vector3(0.95f, 0.04f, 0.95f);
                Collider cws = waterSurf.GetComponent<Collider>();
                if (cws != null) SafeDestroy(cws);
                Renderer rws = waterSurf.GetComponent<Renderer>();
                if (rws != null) rws.material = CreateBuildingMaterial("WaterMat", new Color(0.20f, 0.50f, 0.75f));
            }
            else if (isHorse)
            {
                // Horse Stable Props:
                // 1. Horse Stall Shed (Covered wooden horse shelter in rear-right corner)
                float hx = halfX - 2.2f;
                float hz = halfZ - 1.8f;

                GameObject stallRoot = new GameObject("HorseStallShed");
                stallRoot.transform.SetParent(model.transform, false);
                stallRoot.transform.localPosition = new Vector3(hx, 0f, hz);

                // Stall Back Wall
                GameObject backWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                backWall.name = "HorseStallWall";
                backWall.transform.SetParent(stallRoot.transform, false);
                backWall.transform.localPosition = new Vector3(0f, 1.25f, 1.2f);
                backWall.transform.localScale = new Vector3(3.8f, 2.5f, 0.2f);
                Collider cbw = backWall.GetComponent<Collider>();
                if (cbw != null) SafeDestroy(cbw);
                Renderer rbw = backWall.GetComponent<Renderer>();
                if (rbw != null) rbw.material = CreateBuildingMaterial("StallWoodMat", woodDark);

                // Stall Side Wall (Right)
                GameObject sideWall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sideWall.name = "HorseStallWall";
                sideWall.transform.SetParent(stallRoot.transform, false);
                sideWall.transform.localPosition = new Vector3(1.8f, 1.25f, 0f);
                sideWall.transform.localScale = new Vector3(0.2f, 2.5f, 2.4f);
                Collider csw = sideWall.GetComponent<Collider>();
                if (csw != null) SafeDestroy(csw);
                Renderer rsw = sideWall.GetComponent<Renderer>();
                if (rsw != null) rsw.material = CreateBuildingMaterial("StallWoodMat", woodDark);

                // Stall Slanted Timber Roof
                GameObject stallRoof = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stallRoof.name = "HorseStallRoof";
                stallRoof.transform.SetParent(stallRoot.transform, false);
                stallRoof.transform.localPosition = new Vector3(0f, 2.65f, 0.1f);
                stallRoof.transform.localRotation = Quaternion.Euler(14f, 0f, 0f);
                stallRoof.transform.localScale = new Vector3(4.2f, 0.18f, 2.8f);
                Collider csr = stallRoof.GetComponent<Collider>();
                if (csr != null) SafeDestroy(csr);
                Renderer rsr = stallRoof.GetComponent<Renderer>();
                if (rsr != null) rsr.material = CreateBuildingMaterial("StallRoofMat", roofWood);

                // Stall Soft Golden Straw Bedding
                GameObject stallStraw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stallStraw.name = "HorseStallStraw";
                stallStraw.transform.SetParent(stallRoot.transform, false);
                stallStraw.transform.localPosition = new Vector3(0f, 0.04f, 0f);
                stallStraw.transform.localScale = new Vector3(3.6f, 0.05f, 2.2f);
                Collider css = stallStraw.GetComponent<Collider>();
                if (css != null) SafeDestroy(css);
                Renderer rss = stallStraw.GetComponent<Renderer>();
                if (rss != null) rss.material = CreateBuildingMaterial("StrawPatchMat", new Color(0.85f, 0.75f, 0.32f));

                // 2. Elevated Hay Rack / Manger along rear-left fence
                GameObject hayRack = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hayRack.name = "HorseHayRack";
                hayRack.transform.SetParent(model.transform, false);
                hayRack.transform.localPosition = new Vector3(-2.2f, 1.0f, halfZ - 0.6f);
                hayRack.transform.localScale = new Vector3(2.6f, 0.65f, 0.75f);
                Collider chr = hayRack.GetComponent<Collider>();
                if (chr != null) SafeDestroy(chr);
                Renderer rhr = hayRack.GetComponent<Renderer>();
                if (rhr != null) rhr.material = CreateBuildingMaterial("TroughMat", woodDark);

                GameObject hayFill = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hayFill.name = "HorseMangerHay";
                hayFill.transform.SetParent(model.transform, false);
                hayFill.transform.localPosition = new Vector3(-2.2f, 1.25f, halfZ - 0.6f);
                hayFill.transform.localScale = new Vector3(2.4f, 0.35f, 0.65f);
                Collider chf = hayFill.GetComponent<Collider>();
                if (chf != null) SafeDestroy(chf);
                Renderer rhf = hayFill.GetComponent<Renderer>();
                if (rhf != null) rhf.material = CreateBuildingMaterial("HayMat", new Color(0.72f, 0.78f, 0.26f));

                // 3. Sturdy Wooden Water Trough in rear-left corner
                GameObject waterTrough = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterTrough.name = "HorseWaterTrough";
                waterTrough.transform.SetParent(model.transform, false);
                waterTrough.transform.localPosition = new Vector3(-halfX + 1.6f, 0.35f, halfZ - 1.4f);
                waterTrough.transform.localScale = new Vector3(2.2f, 0.65f, 1.2f);
                Collider cwt = waterTrough.GetComponent<Collider>();
                if (cwt != null) SafeDestroy(cwt);
                Renderer rwt = waterTrough.GetComponent<Renderer>();
                if (rwt != null) rwt.material = CreateBuildingMaterial("WaterTroughMat", new Color(0.40f, 0.26f, 0.15f));

                GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterPlane.name = "HorseWaterSurface";
                waterPlane.transform.SetParent(model.transform, false);
                waterPlane.transform.localPosition = new Vector3(-halfX + 1.6f, 0.58f, halfZ - 1.4f);
                waterPlane.transform.localScale = new Vector3(2.0f, 0.04f, 1.0f);
                Collider cwp = waterPlane.GetComponent<Collider>();
                if (cwp != null) SafeDestroy(cwp);
                Renderer rwp = waterPlane.GetComponent<Renderer>();
                if (rwp != null) rwp.material = CreateBuildingMaterial("WaterMat", new Color(0.20f, 0.50f, 0.75f));

                // 4. Mineral Salt Lick Post in front-right corner
                GameObject saltPost = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                saltPost.name = "HorseSaltLickPost";
                saltPost.transform.SetParent(model.transform, false);
                saltPost.transform.localPosition = new Vector3(halfX - 1.3f, 0.55f, -halfZ + 1.8f);
                saltPost.transform.localScale = new Vector3(0.18f, 0.55f, 0.18f);
                Collider csp = saltPost.GetComponent<Collider>();
                if (csp != null) SafeDestroy(csp);
                Renderer rsp = saltPost.GetComponent<Renderer>();
                if (rsp != null) rsp.material = CreateBuildingMaterial("PostMat", woodDark);

                GameObject saltBlock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                saltBlock.name = "HorseSaltLick";
                saltBlock.transform.SetParent(model.transform, false);
                saltBlock.transform.localPosition = new Vector3(halfX - 1.3f, 1.18f, -halfZ + 1.8f);
                saltBlock.transform.localScale = new Vector3(0.36f, 0.36f, 0.36f);
                Collider csb = saltBlock.GetComponent<Collider>();
                if (csb != null) SafeDestroy(csb);
                Renderer rsb = saltBlock.GetComponent<Renderer>();
                if (rsb != null) rsb.material = CreateBuildingMaterial("SaltLickMat", new Color(0.92f, 0.76f, 0.72f)); // Mineral salt block
            }
            else
            {
                // Cow Pen Props: Feeding Trough along back wall
                GameObject trough = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trough.name = "FeedingTrough";
                trough.transform.SetParent(model.transform, false);
                trough.transform.localPosition = new Vector3(1.5f, 0.25f, halfZ - 1.0f);
                trough.transform.localScale = new Vector3(3.2f, 0.50f, 0.9f);
                Collider ct = trough.GetComponent<Collider>();
                if (ct != null) SafeDestroy(ct);
                Renderer rt = trough.GetComponent<Renderer>();
                if (rt != null)
                {
                    rt.material = CreateBuildingMaterial("TroughMat", new Color(0.40f, 0.26f, 0.15f));
                }

                // Hay inside trough
                GameObject hay = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hay.name = "TroughHay";
                hay.transform.SetParent(model.transform, false);
                hay.transform.localPosition = new Vector3(1.5f, 0.45f, halfZ - 1.0f);
                hay.transform.localScale = new Vector3(3.0f, 0.2f, 0.75f);
                Collider ch = hay.GetComponent<Collider>();
                if (ch != null) SafeDestroy(ch);
                Renderer rh = hay.GetComponent<Renderer>();
                if (rh != null)
                {
                    rh.material = CreateBuildingMaterial("HayMat", new Color(0.70f, 0.75f, 0.25f));
                }

                // Water trough in back-left corner
                GameObject waterTrough = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterTrough.name = "WaterTrough";
                waterTrough.transform.SetParent(model.transform, false);
                waterTrough.transform.localPosition = new Vector3(-halfX + 1.6f, 0.25f, halfZ - 1.4f);
                waterTrough.transform.localScale = new Vector3(1.8f, 0.50f, 1.2f);
                Collider cwt = waterTrough.GetComponent<Collider>();
                if (cwt != null) SafeDestroy(cwt);
                Renderer rwt = waterTrough.GetComponent<Renderer>();
                if (rwt != null)
                {
                    rwt.material = CreateBuildingMaterial("WaterTroughMat", new Color(0.40f, 0.26f, 0.15f));
                }

                // Water surface
                GameObject waterPlane = GameObject.CreatePrimitive(PrimitiveType.Cube);
                waterPlane.name = "WaterSurface";
                waterPlane.transform.SetParent(model.transform, false);
                waterPlane.transform.localPosition = new Vector3(-halfX + 1.6f, 0.42f, halfZ - 1.4f);
                waterPlane.transform.localScale = new Vector3(1.6f, 0.04f, 1.0f);
                Collider cwp = waterPlane.GetComponent<Collider>();
                if (cwp != null) SafeDestroy(cwp);
                Renderer rwp = waterPlane.GetComponent<Renderer>();
                if (rwp != null)
                {
                    rwp.material = CreateBuildingMaterial("WaterMat", new Color(0.20f, 0.50f, 0.75f));
                }

                // Hay Bales in corner
                for (int b = 0; b < 2; b++)
                {
                    GameObject bale = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    bale.name = $"HayBale_{b + 1}";
                    bale.transform.SetParent(model.transform, false);
                    float bx = halfX - 1.4f;
                    float bz = -halfZ + 1.4f + (b * 1.1f);
                    bale.transform.localPosition = new Vector3(bx, 0.35f, bz);
                    bale.transform.localScale = new Vector3(1.2f, 0.6f, 0.8f);
                    Collider cba = bale.GetComponent<Collider>();
                    if (cba != null) SafeDestroy(cba);
                    Renderer rba = bale.GetComponent<Renderer>();
                    if (rba != null)
                    {
                        rba.material = CreateBuildingMaterial("HayBaleMat", new Color(0.78f, 0.72f, 0.28f));
                    }
                }
            }

            // 6. Gate Signboard
            GameObject signGo = new GameObject("GateSign");
            signGo.transform.SetParent(model.transform, false);
            signGo.transform.localPosition = new Vector3(0f, gatePostH + 0.35f, -halfZ);
            TextMesh signText = signGo.AddComponent<TextMesh>();
            signText.text = dName.ToUpper();
            signText.font = FarmUITheme.BoldFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            signText.fontSize = 48;
            signText.characterSize = 0.065f;
            signText.fontStyle = FontStyle.Bold;
            signText.alignment = TextAlignment.Center;
            signText.anchor = TextAnchor.MiddleCenter;
            signText.color = new Color(1f, 0.95f, 0.45f);
            MeshRenderer smr = signGo.GetComponent<MeshRenderer>();
            if (smr != null && signText.font != null)
            {
                smr.material = signText.font.material;
            }
            signGo.AddComponent<BillboardFaceCamera>();

            // 7. Phase 6: Create dedicated CowPenBounds, ChickenCoopBounds, PigPenBounds, or HorseStableBounds child object
            string boundsName = isChicken ? "ChickenCoopBounds" : (isPig ? "PigPenBounds" : (isHorse ? "HorseStableBounds" : "CowPenBounds"));
            Transform boundsTrans = root.transform.Find(boundsName);
            if (boundsTrans == null)
            {
                GameObject boundsGo = new GameObject(boundsName);
                boundsGo.transform.SetParent(root.transform, false);
                boundsGo.transform.localPosition = Vector3.zero;
            }

            // Configure AnimalPenBoundary on root
            AnimalPenBoundary boundary = root.GetComponent<AnimalPenBoundary>();
            if (boundary == null) boundary = root.AddComponent<AnimalPenBoundary>();
            boundary.penType = bId;
            boundary.innerBoundsSize = new Vector3(penLenX - 1.2f, 2.5f, penWidZ - 1.2f);
            boundary.centerOffset = Vector3.zero;
            boundary.defaultMargin = 0.4f;

            // Snap the entire building flush to ground (only during gameplay, when position.y is 0)
            if (Application.isPlaying && !root.name.StartsWith("PlacementGhost") && root.layer != LayerMask.NameToLayer("PlacementGhost") && root.transform.position.y == 0f)
            {
                GroundSnapUtility.SnapBuildingToGround(root);
            }
            ValidateRenderersAndMaterials(root);
        }

        public void SetupInteractionCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }

            bool isPen = (buildingType == "cow_barn" || buildingType == "chicken_coop" || buildingType == "pig_pen" || buildingType == "horse_stable" ||
                          buildingId == "cow_barn" || buildingId == "chicken_coop" || buildingId == "pig_pen" || buildingId == "horse_stable" ||
                          buildingTypeEnum == BuildingType.CowBarn || buildingTypeEnum == BuildingType.ChickenCoop || buildingTypeEnum == BuildingType.PigPen || buildingTypeEnum == BuildingType.HorseStable);

            // Animal pens MUST be trigger colliders so the player can walk through the entrance gate freely!
            // Physical perimeter fencing is provided by the individual fence wall box colliders.
            col.isTrigger = isPen;

            if (buildingType == "cow_barn" || buildingId == "cow_barn" || buildingTypeEnum == BuildingType.CowBarn)
            {
                col.size = new Vector3(11.5f, 2.5f, 8.5f);
                col.center = new Vector3(0f, 1.25f, 0f);
            }
            else if (buildingType == "chicken_coop" || buildingId == "chicken_coop" || buildingTypeEnum == BuildingType.ChickenCoop)
            {
                col.size = new Vector3(8.5f, 2.2f, 6.5f);
                col.center = new Vector3(0f, 1.1f, 0f);
            }
            else if (buildingType == "pig_pen" || buildingId == "pig_pen" || buildingTypeEnum == BuildingType.PigPen)
            {
                col.size = new Vector3(9.5f, 2.2f, 7.5f);
                col.center = new Vector3(0f, 1.1f, 0f);
            }
            else if (buildingType == "horse_stable" || buildingId == "horse_stable" || buildingTypeEnum == BuildingType.HorseStable)
            {
                col.size = new Vector3(12.5f, 2.8f, 9.5f);
                col.center = new Vector3(0f, 1.4f, 0f);
            }
            else if (buildingType == "machine_shed" || buildingId == "machine_shed" || buildingTypeEnum == BuildingType.MachineShed)
            {
                col.size = new Vector3(8.5f, 4.5f, 7.0f);
                col.center = new Vector3(0f, 2.25f, 0f);
                col.isTrigger = false;
            }
            else if (buildingType == "storage" || buildingId == "storage" || buildingTypeEnum == BuildingType.Storage)
            {
                // Snug fit around House1 model so path in front remains clear and player is not blocked
                col.size = new Vector3(6.5f, 4.5f, 6.5f);
                col.center = new Vector3(0f, 2.25f, 0f);
                col.isTrigger = false;
            }
            else
            {
                col.size = new Vector3(6.5f, 4.5f, 6.5f);
                col.center = new Vector3(0f, 2.25f, 0f);
                col.isTrigger = false;
            }

            CleanRedundantPenColliders();
        }

        public void CleanRedundantPenColliders()
        {
            bool isPen = (buildingType == "cow_barn" || buildingType == "chicken_coop" || buildingType == "pig_pen" || buildingType == "horse_stable" ||
                          buildingId == "cow_barn" || buildingId == "chicken_coop" || buildingId == "pig_pen" || buildingId == "horse_stable" ||
                          buildingTypeEnum == BuildingType.CowBarn || buildingTypeEnum == BuildingType.ChickenCoop || buildingTypeEnum == BuildingType.PigPen || buildingTypeEnum == BuildingType.HorseStable);
            if (!isPen) return;

            Collider[] allCols = GetComponentsInChildren<Collider>(true);
            foreach (var c in allCols)
            {
                if (c == null) continue;
                string cn = c.gameObject.name.ToLower();

                // 1. Pen root collider must ALWAYS be a trigger so player can enter
                if (c.gameObject == this.gameObject)
                {
                    c.isTrigger = true;
                    continue;
                }

                // 2. Main perimeter wall colliders provide physical fence boundaries
                if (cn.StartsWith("collider_wall_"))
                {
                    c.isTrigger = false;
                    continue;
                }

                // 3. Troughs and Hay/Straw props: trigger for Edit Mode selection without blocking movement
                if (cn.Contains("trough") || cn.Contains("well") || cn.Contains("manger") || cn.Contains("haybale") || cn.Contains("hayrack"))
                {
                    c.isTrigger = true;
                    MovableFarmObject m = c.GetComponent<MovableFarmObject>() ?? c.gameObject.AddComponent<MovableFarmObject>();
                    if (string.IsNullOrEmpty(m.objectId)) m.objectId = $"{name}/{c.gameObject.name}";
                    m.AutoDetectProperties();
                    continue;
                }

                // 4. All other primitive props (posts, rails, gate headers, bedding, roof, signs) must not have physical colliders
                if (cn.Contains("post") || cn.Contains("rail") || cn.Contains("header") || cn.Contains("bedding") || cn.Contains("strawpatch") || cn.Contains("mudwallow") || cn.Contains("roof") || cn.Contains("hay") || cn.Contains("water") || cn.Contains("slop") || cn.Contains("sign"))
                {
                    SafeDestroy(c);
                }
            }
        }

        public void SetupBillboard()
        {
            Transform existing = transform.Find("Billboard");
            if (existing != null)
            {
                statusBillboard = existing.GetComponent<TextMesh>();
                return;
            }

            GameObject bbGo = new GameObject("Billboard");
            bbGo.transform.SetParent(transform, false);
            bool isSmallPen = (buildingType == "cow_barn" || buildingType == "chicken_coop" || buildingType == "pig_pen" || buildingType == "horse_stable" ||
                               buildingId == "cow_barn" || buildingId == "chicken_coop" || buildingId == "pig_pen" || buildingId == "horse_stable" ||
                               buildingTypeEnum == BuildingType.CowBarn || buildingTypeEnum == BuildingType.ChickenCoop || buildingTypeEnum == BuildingType.PigPen || buildingTypeEnum == BuildingType.HorseStable);
            float billboardY = isSmallPen ? 2.5f : ((buildingType == "machine_shed" || buildingId == "machine_shed" || buildingTypeEnum == BuildingType.MachineShed) ? 5.5f : 6.8f);
            bbGo.transform.localPosition = new Vector3(0f, billboardY, 0f);

            statusBillboard = bbGo.AddComponent<TextMesh>();
            statusBillboard.fontSize = 50;
            statusBillboard.characterSize = 0.08f;
            statusBillboard.fontStyle = FontStyle.Bold;
            statusBillboard.alignment = TextAlignment.Center;
            statusBillboard.anchor = TextAnchor.MiddleCenter;
            statusBillboard.color = Color.white;
            statusBillboard.font = FarmUITheme.BoldFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            MeshRenderer mr = bbGo.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = statusBillboard.font.material;
            }

            bbGo.AddComponent<BillboardFaceCamera>();
        }

        public void ClearHighlight()
        {
            var lrs = GetComponentsInChildren<LineRenderer>(true);
            foreach (var lr in lrs) SafeDestroy(lr);

            var allComps = GetComponentsInChildren<Component>(true);
            foreach (var c in allComps)
            {
                if (c == null) continue;
                string cName = c.GetType().Name.ToLower();
                if (cName.Contains("outline") || cName.Contains("selection") || cName.Contains("highlight"))
                {
                    SafeDestroy(c);
                }
            }

            RestoreOriginalMaterials();
        }

        public void RestoreOriginalMaterials()
        {
            if (IsGhostOrPreview()) return;

            Color woodDark = new Color(0.66f, 0.48f, 0.30f); // Warm Cedar Post color
            Color woodLight = new Color(0.84f, 0.68f, 0.46f); // Bright Golden Pine Rail color
            Color strawColor = new Color(0.96f, 0.88f, 0.50f); // Bright Straw bedding
            Color soilColor = new Color(0.58f, 0.46f, 0.32f); // Warm Meadow Loam
            Color mudColor = new Color(0.50f, 0.38f, 0.26f); // Soft Clay Mud wallow
            Color roofRed = new Color(0.88f, 0.32f, 0.24f); // Cheerful Terracotta Red
            Color roofWood = new Color(0.68f, 0.48f, 0.30f); // Warm Shingle Wood
            bool isChicken = (buildingTypeEnum == BuildingType.ChickenCoop || buildingId == "chicken_coop");

            Renderer[] rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null || r.gameObject.name == "Billboard" || r.gameObject.name == "GateSign") continue;

                string n = r.gameObject.name;
                if (n == "PenBedding")
                {
                    r.material = CreateBuildingMaterial("PenBeddingMat", isChicken ? strawColor : soilColor);
                }
                else if (n == "MudWallow")
                {
                    r.material = CreateBuildingMaterial("MudWallowMat", mudColor);
                }
                else if (n.StartsWith("StrawPatch") || n == "PigShelterStraw")
                {
                    r.material = CreateBuildingMaterial("StrawPatchMat", new Color(0.94f, 0.85f, 0.48f));
                }
                else if (n.StartsWith("Post_") || n == "WaterTub")
                {
                    r.material = CreateBuildingMaterial("PostMat", woodDark);
                }
                else if (n.StartsWith("Rail_"))
                {
                    r.material = CreateBuildingMaterial("RailMat", woodLight);
                }
                else if (n == "NestingRoost")
                {
                    r.material = CreateBuildingMaterial("CoopBoxMat", new Color(0.82f, 0.64f, 0.44f));
                }
                else if (n == "RoostRoof")
                {
                    r.material = CreateBuildingMaterial("RoostRoofMat", roofRed);
                }
                else if (n == "PigShelterRoof")
                {
                    r.material = CreateBuildingMaterial("PigShelterMat", roofWood);
                }
                else if (n == "GrainDish")
                {
                    r.material = CreateBuildingMaterial("GrainDishMat", new Color(0.95f, 0.85f, 0.38f));
                }
                else if (n == "FeedingTrough" || n == "PigFeedTrough")
                {
                    r.material = CreateBuildingMaterial("TroughMat", new Color(0.64f, 0.48f, 0.32f));
                }
                else if (n == "PigSlop")
                {
                    r.material = CreateBuildingMaterial("PigSlopMat", new Color(0.72f, 0.62f, 0.35f));
                }
                else if (n == "TroughHay")
                {
                    r.material = CreateBuildingMaterial("HayMat", new Color(0.85f, 0.82f, 0.35f));
                }
                else if (n == "WaterTrough")
                {
                    r.material = CreateBuildingMaterial("WaterTroughMat", new Color(0.64f, 0.48f, 0.32f));
                }
                else if (n == "WaterSurface" || n == "WaterTubSurface")
                {
                    r.material = CreateBuildingMaterial("WaterMat", new Color(0.35f, 0.75f, 0.95f));
                }
                else if (n.StartsWith("HayBale"))
                {
                    r.material = CreateBuildingMaterial("HayBaleMat", new Color(0.92f, 0.84f, 0.40f));
                }
                else if (n == "HorseStallWall" || n == "HorseHayRack" || n == "HorseSaltLickPost")
                {
                    r.material = CreateBuildingMaterial("PostMat", woodDark);
                }
                else if (n == "HorseStallRoof")
                {
                    r.material = CreateBuildingMaterial("StallRoofMat", roofWood);
                }
                else if (n == "HorseStallStraw")
                {
                    r.material = CreateBuildingMaterial("StrawPatchMat", new Color(0.85f, 0.75f, 0.32f));
                }
                else if (n == "HorseMangerHay")
                {
                    r.material = CreateBuildingMaterial("HayMat", new Color(0.72f, 0.78f, 0.26f));
                }
                else if (n == "HorseWaterTrough")
                {
                    r.material = CreateBuildingMaterial("WaterTroughMat", new Color(0.40f, 0.26f, 0.15f));
                }
                else if (n == "HorseWaterSurface")
                {
                    r.material = CreateBuildingMaterial("WaterMat", new Color(0.20f, 0.50f, 0.75f));
                }
                else if (n == "HorseSaltLick")
                {
                    r.material = CreateBuildingMaterial("SaltLickMat", new Color(0.92f, 0.76f, 0.72f));
                }
                else if (n == "BarnBody")
                {
                    r.material = CreateBuildingMaterial("BarnBodyMat", new Color(0.35f, 0.45f, 0.55f));
                }
                else if (n == "BarnRoof")
                {
                    r.material = CreateBuildingMaterial("BarnRoofMat", new Color(0.2f, 0.3f, 0.45f));
                }
                else if (n == "BarnDoor")
                {
                    r.material = CreateBuildingMaterial("BarnDoorMat", new Color(0.92f, 0.9f, 0.85f));
                }
                else if (n == "WorkshopFoundation")
                {
                    r.material = CreateBuildingMaterial("WorkshopFoundationMat", new Color(0.38f, 0.40f, 0.42f));
                }
                else if (n == "WorkshopBody")
                {
                    r.material = CreateBuildingMaterial("WorkshopBodyMat", new Color(0.55f, 0.32f, 0.22f));
                }
                else if (n == "WorkshopRoof")
                {
                    r.material = CreateBuildingMaterial("WorkshopRoofMat", new Color(0.22f, 0.32f, 0.42f));
                }
                else if (n == "WorkshopRoofVent")
                {
                    r.material = CreateBuildingMaterial("WorkshopVentMat", new Color(0.28f, 0.38f, 0.48f));
                }
                else if (n == "WorkshopChimney")
                {
                    r.material = CreateBuildingMaterial("WorkshopChimneyMat", new Color(0.25f, 0.26f, 0.28f));
                }
                else if (n == "WorkshopChimneyCap" || n == "WorkshopGear")
                {
                    r.material = CreateBuildingMaterial("WorkshopBrassMat", new Color(0.85f, 0.65f, 0.20f));
                }
                else if (n == "WorkshopDoor")
                {
                    r.material = CreateBuildingMaterial("WorkshopDoorMat", new Color(0.32f, 0.22f, 0.15f));
                }
                else if (n.StartsWith("WorkshopDoorCross"))
                {
                    r.material = CreateBuildingMaterial("WorkshopDoorTrimMat", new Color(0.42f, 0.32f, 0.22f));
                }
                else if (n == "WorkshopBench")
                {
                    r.material = CreateBuildingMaterial("WorkshopWoodMat", new Color(0.58f, 0.42f, 0.25f));
                }
                else if (n == "WorkshopAnvil")
                {
                    r.material = CreateBuildingMaterial("WorkshopIronMat", new Color(0.25f, 0.26f, 0.28f));
                }
                else if (n == "WorkshopCrate")
                {
                    r.material = CreateBuildingMaterial("WorkshopCrateMat", new Color(0.62f, 0.46f, 0.28f));
                }
                else if (n == "WorkshopBarrel")
                {
                    r.material = CreateBuildingMaterial("WorkshopBarrelMat", new Color(0.48f, 0.30f, 0.18f));
                }
                else if (n == "WorkshopLamp")
                {
                    r.material = CreateBuildingMaterial("WorkshopLampMat", new Color(1.0f, 0.90f, 0.35f));
                }
                else
                {
                    if (r.sharedMaterial == null || r.sharedMaterial.name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        (r.sharedMaterial.HasProperty("_Color") && r.sharedMaterial.color.g > 0.8f && r.sharedMaterial.color.r < 0.4f))
                    {
                        r.material = CreateBuildingMaterial("FallbackWoodMat", woodDark);
                    }
                }
            }
        }

        public static void ValidateRenderersAndMaterials(GameObject root)
        {
            if (root == null) return;
            Material fallbackMat = CreateBuildingMaterial("FenceFallbackBrown", new Color(0.55f, 0.38f, 0.22f));
            Renderer[] rends = root.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null) continue;
                r.enabled = true;
                if (r.sharedMaterial == null || r.sharedMaterial.shader == null || r.sharedMaterial.shader.name == "Hidden/InternalErrorShader" ||
                    r.sharedMaterial.name.IndexOf("preview", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    r.material = fallbackMat;
                }
            }
        }

        public static bool ShowDebugBuildingBillboards = false;

        public void RefreshVisuals()
        {
            if (visualRoot != null)
            {
                visualRoot.SetActive(isPurchased);
            }

            if (statusBillboard != null)
            {
                bool isPen = (buildingType == "cow_barn" || buildingType == "chicken_coop" || buildingType == "pig_pen" || buildingType == "horse_stable" ||
                              buildingId == "cow_barn" || buildingId == "chicken_coop" || buildingId == "pig_pen" || buildingId == "horse_stable");
                if (isPen && isPurchased && !ShowDebugBuildingBillboards)
                {
                    // Pens already have the 3D wooden gate sign, so hide floating status text to prevent visual clutter
                    statusBillboard.gameObject.SetActive(false);
                    return;
                }

                statusBillboard.gameObject.SetActive(true);

                if (!isPurchased)
                {
                    statusBillboard.text = $"{displayName}\n[CHƯA XÂY - MUA TRONG SHOP]";
                    statusBillboard.color = new Color(1f, 0.8f, 0.2f);
                }
                else
                {
                    if (buildingType == "storage")
                    {
                        int totalItems = InventorySystem.Instance != null ? InventorySystem.Instance.GetTotalItemCount() : 0;
                        int cap = InventorySystem.Instance != null ? InventorySystem.Instance.StorageCapacity : 50;
                        statusBillboard.text = $"{displayName}\n[{totalItems}/{cap} Vật phẩm]";
                        statusBillboard.color = new Color(0.4f, 0.85f, 1f);
                    }
                    else
                    {
                        int occ = GetOccupantCount();
                        string animalWord = (allowedAnimalType == AnimalType.Chicken || buildingTypeEnum == BuildingType.ChickenCoop) ? "Gà" :
                                            (allowedAnimalType == AnimalType.Pig || buildingTypeEnum == BuildingType.PigPen) ? "Lợn" :
                                            (allowedAnimalType == AnimalType.Horse || buildingTypeEnum == BuildingType.HorseStable) ? "Ngựa" : "Bò";
                        statusBillboard.text = $"{displayName}\n[{occ}/{capacity} {animalWord}]";
                        statusBillboard.color = Color.white;
                    }
                }
            }
        }

        public int GetOccupantCount()
        {
            occupants.RemoveAll(a => a == null || !a || !a.gameObject);
            return occupants.Count;
        }

        public bool CanAddAnimal()
        {
            if (buildingType == "storage" || buildingId == "storage" || buildingTypeEnum == BuildingType.Storage)
            {
                return false;
            }
            return isPurchased && GetOccupantCount() < capacity;
        }

        public bool AddAnimal(AnimalCareController animal)
        {
            if (buildingType == "storage" || buildingTypeEnum == BuildingType.Storage)
            {
                return false;
            }
            if (animal == null || !animal) return false;
            if (animal.animalType != allowedAnimalType)
            {
                Debug.LogWarning($"[FarmBuilding] Loại vật nuôi không hợp lệ: {animal.animalType} không thể vào {displayName} (chỉ cho phép {allowedAnimalType})!");
                return false;
            }
            if (GetOccupantCount() >= capacity)
            {
                Debug.LogWarning($"[FarmBuilding] Chuồng đã đầy: {GetOccupantCount()}/{capacity}!");
                return false;
            }
            if (!occupants.Contains(animal))
            {
                occupants.Add(animal);
                animal.transform.SetParent(AnimalsRoot, true);
                RefreshVisuals();
                return true;
            }
            return false;
        }

        public bool RemoveAnimal(AnimalCareController animal)
        {
            if (animal == null) return false;
            bool removed = occupants.Remove(animal);
            RefreshVisuals();
            return removed;
        }

        public void OnClickBuilding()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            if (!isPurchased)
            {
                CropSelectionUI.ShowToast($"{displayName}: Chưa mua! Hãy mở Shop để xây chuồng.");
                ShopUI.OpenCategory(ShopCategory.Buildings);
                return;
            }

            if (buildingType == "storage")
            {
                StorageUI ui = StorageUI.EnsureUI();
                if (ui != null)
                {
                    ui.selectedStorage = this;
                    ui.Open();
                }
                return;
            }

            if (BuildingPlacementController.Instance != null)
            {
                BuildingPlacementController.Instance.selectedBuilding = this;
            }

            // Open Building Management UI
            BuildingUI.OpenBuilding(this);
        }

        void OnMouseDown()
        {
            if (FarmEditModeController.IsEditModeActive) return;
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            OnClickBuilding();
        }
    }
}
