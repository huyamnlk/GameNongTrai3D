using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    public enum MachineState
    {
        Idle,
        Producing,
        Completed
    }

    /// <summary>
    /// Represents a physical production machine (Mill, Bakery, Dairy, Feed Mixer, Jam Maker) on the farm.
    /// Handles ingredient deduction, timers, 3D billboard progress bar, and output collection.
    /// </summary>
    public class ProductionMachine : MonoBehaviour
    {
        /// <summary>Raised after a finished batch was moved into the inventory (machine, itemId, amount).</summary>
        public static event Action<ProductionMachine, string, int> OnProductCollected;

        [Header("Machine Configuration")]
        public string machineId = "mill";
        public string displayName = "Cối xay bột (Mill)";
        public bool isPurchased = false;

        [Header("Recipe")]
        public ProductionRecipeData currentRecipe;
        public List<ProductionRecipeData> availableRecipes = new List<ProductionRecipeData>();

        [Header("Runtime State")]
        public MachineState state = MachineState.Idle;
        public float currentTimer = 0f;
        public float totalDuration = 10f;
        public int pendingOutputAmount = 0;

        private GameObject visualRoot;
        private TextMesh statusBillboard;
        private float billboardRefreshTimer;

        void Awake()
        {
            SetupInteractionCollider();
            SetupBillboard();
            MachineWorkAnimator.Ensure(this);
        }

        void Update()
        {
            if (!isPurchased) return;

            if (state == MachineState.Producing)
            {
                if (currentRecipe == null)
                {
                    // Corrupt/unknown recipe state: fall back to a safe idle machine instead of throwing.
                    state = MachineState.Idle;
                    currentTimer = 0f;
                    UpdateBillboard();
                    return;
                }

                currentTimer += Time.deltaTime;
                if (currentTimer >= totalDuration)
                {
                    currentTimer = totalDuration;
                    state = MachineState.Completed;
                    pendingOutputAmount = currentRecipe.outputAmount;
                    Debug.Log($"[ProductionMachine] {displayName}: Đã hoàn thành sản xuất '{currentRecipe.displayName}'!");
                    UpdateBillboard();
                    CropSelectionUI.ShowToast($"{displayName}: đã xong {ItemName(currentRecipe.outputItemId)}!\nChạm vào máy để thu hoạch.");
                    return;
                }

                billboardRefreshTimer -= Time.deltaTime;
                if (billboardRefreshTimer <= 0f)
                {
                    billboardRefreshTimer = 0.2f;
                    UpdateBillboard();
                }
            }
        }

        public void SetupMachine(MachineData data, bool purchased)
        {
            machineId = data.machineId;
            displayName = data.displayName;

            availableRecipes = new List<ProductionRecipeData>(data.recipes);
            if (availableRecipes.Count == 0 && data.defaultRecipe != null) availableRecipes.Add(data.defaultRecipe);

            currentRecipe = data.defaultRecipe;
            totalDuration = currentRecipe != null ? currentRecipe.productionTime : 10f;
            isPurchased = purchased;
            if (transform.position == Vector3.zero && data != null)
            {
                transform.position = data.defaultPosition;
            }

            Build3DVisual();
            SetupInteractionCollider();
            GroundSnapUtility.SnapObjectToGround(gameObject);

            MovableFarmObject m = GetComponent<MovableFarmObject>() ?? gameObject.AddComponent<MovableFarmObject>();
            m.isDeleted = false;
            m.AutoDetectProperties();
            m.RecordCurrentTransform();

            UpdateBillboard();
        }

        private static string ItemName(string itemId)
        {
            return InventorySystem.GetDefaultItemName(InventorySystem.NormalizeId(itemId));
        }

        public void SetupInteractionCollider()
        {
            BoxCollider col = GetComponent<BoxCollider>();
            if (col == null)
            {
                col = gameObject.AddComponent<BoxCollider>();
            }

            // Size collider appropriately to the machine base footprint
            // Rather than encompassing wide spinning sails or high chimneys
            col.center = new Vector3(0f, 1.8f, 0f);
            col.size = new Vector3(3.5f, 3.6f, 3.5f);
        }

        private void SetupBillboard()
        {
            Transform existing = transform.Find("Billboard");
            if (existing != null)
            {
                statusBillboard = existing.GetComponent<TextMesh>();
                return;
            }

            GameObject bbGo = new GameObject("Billboard");
            bbGo.transform.SetParent(transform, false);
            bbGo.transform.localPosition = new Vector3(0f, 6.5f, 0f);

            statusBillboard = bbGo.AddComponent<TextMesh>();
            statusBillboard.fontSize = 28;
            statusBillboard.characterSize = 0.2f;
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

        public void Build3DVisual()
        {
            if (visualRoot != null) SafeDestroy(visualRoot);

            // Clean up any loose ProceduralModel attached directly to this machine root
            Transform oldProc = transform.Find(MachineVisualFactory.ModelRootName);
            if (oldProc != null) SafeDestroy(oldProc.gameObject);

            visualRoot = new GameObject("VisualModel");
            visualRoot.transform.SetParent(transform, false);

            string prefabPath = "";
            if (machineId == "mill") prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Windmill1.prefab";
            else if (machineId == "bakery") prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House3.prefab";
            else if (machineId == "dairy") prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House4.prefab";
            else if (machineId == "feed_mixer") prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/House5.prefab";

            GameObject loadedPrefab = null;
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(prefabPath))
            {
                loadedPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }
#endif
            if (loadedPrefab != null)
            {
                GameObject model = Instantiate(loadedPrefab, visualRoot.transform);
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one;
            }
            else
            {
                // Build the procedural model directly under visualRoot so toggling visualRoot toggles the whole model
                MachineVisualFactory.Build(visualRoot, machineId);
            }

            visualRoot.SetActive(isPurchased);

            MachineWorkAnimator animator = MachineWorkAnimator.Ensure(this);
            if (animator != null) animator.Rebuild();
        }

        public void UpdateBillboard()
        {
            if (visualRoot != null) visualRoot.SetActive(isPurchased);

            if (statusBillboard == null) return;

            if (!isPurchased)
            {
                statusBillboard.text = $"{displayName}\n[CHƯA MUA - XEM TRONG SHOP]";
                statusBillboard.color = new Color(1f, 0.8f, 0.2f);
            }
            else if (state == MachineState.Idle)
            {
                statusBillboard.text = $"{displayName}\n[SẴN SÀNG SẢN XUẤT]";
                statusBillboard.color = Color.green;
            }
            else if (state == MachineState.Producing)
            {
                float dur = Mathf.Max(0.01f, totalDuration);
                int percent = (int)((currentTimer / dur) * 100f);
                int secLeft = Mathf.CeilToInt(totalDuration - currentTimer);
                string recipeName = currentRecipe != null ? currentRecipe.displayName : "";
                statusBillboard.text = $"{displayName}\n{recipeName}\nĐang chạy: {percent}% ({secLeft}s)";
                statusBillboard.color = Color.yellow;
            }
            else if (state == MachineState.Completed)
            {
                statusBillboard.text = $"{displayName}\n[HOÀN THÀNH - CHẠM ĐỂ THU HOẠCH]";
                statusBillboard.color = new Color(0.2f, 1f, 0.4f);
            }
        }

        // ------------------------------------------------------------------ recipes

        /// <summary>Recipes can only be switched while the machine is idle.</summary>
        public bool CanChangeRecipe => state == MachineState.Idle && availableRecipes != null && availableRecipes.Count > 1;

        public int CurrentRecipeIndex
        {
            get
            {
                if (availableRecipes == null || currentRecipe == null) return 0;
                int idx = availableRecipes.FindIndex(r => r != null && r.recipeId == currentRecipe.recipeId);
                return idx < 0 ? 0 : idx;
            }
        }

        public bool SelectRecipe(string recipeId)
        {
            if (state != MachineState.Idle || availableRecipes == null) return false;
            ProductionRecipeData found = availableRecipes.Find(r => r != null && string.Equals(r.recipeId, recipeId, StringComparison.OrdinalIgnoreCase));
            if (found == null) return false;
            currentRecipe = found;
            totalDuration = found.productionTime;
            UpdateBillboard();
            return true;
        }

        public bool CycleRecipe(int direction)
        {
            if (!CanChangeRecipe) return false;
            int count = availableRecipes.Count;
            int next = ((CurrentRecipeIndex + direction) % count + count) % count;
            return SelectRecipe(availableRecipes[next].recipeId);
        }

        /// <summary>Returns the first missing ingredient description, or empty when everything is available.</summary>
        public string GetMissingIngredientText(ProductionRecipeData recipe = null)
        {
            recipe = recipe ?? currentRecipe;
            if (recipe == null) return "Chưa có công thức";
            if (InventorySystem.Instance == null) return "Chưa có kho";

            foreach (var ing in recipe.GetIngredients())
            {
                int have = InventorySystem.Instance.GetItemCount(ing.Key);
                if (have < ing.Value)
                {
                    return $"{ItemName(ing.Key)} ({have}/{ing.Value})";
                }
            }
            return "";
        }

        public bool HasAllIngredients(ProductionRecipeData recipe = null)
        {
            return string.IsNullOrEmpty(GetMissingIngredientText(recipe));
        }

        /// <summary>
        /// Applies persisted state (used by the save system) and keeps timers consistent with the recipe.
        /// </summary>
        public void ApplySavedState(string recipeId, MachineState savedState, float savedTimer, int savedPending)
        {
            if (!string.IsNullOrEmpty(recipeId) && availableRecipes != null)
            {
                ProductionRecipeData found = availableRecipes.Find(r => r != null && string.Equals(r.recipeId, recipeId, StringComparison.OrdinalIgnoreCase));
                if (found != null) currentRecipe = found;
            }

            if (currentRecipe != null) totalDuration = Mathf.Max(0.1f, currentRecipe.productionTime);

            state = savedState;
            currentTimer = Mathf.Clamp(savedTimer, 0f, totalDuration);
            pendingOutputAmount = Mathf.Max(0, savedPending);

            if (state == MachineState.Producing && currentTimer >= totalDuration)
            {
                state = MachineState.Completed;
            }
            if (state == MachineState.Completed)
            {
                currentTimer = totalDuration;
                if (pendingOutputAmount <= 0 && currentRecipe != null) pendingOutputAmount = currentRecipe.outputAmount;
            }
            if (state == MachineState.Idle)
            {
                currentTimer = 0f;
                pendingOutputAmount = 0;
            }
            if (state == MachineState.Producing && currentRecipe == null) state = MachineState.Idle;

            UpdateBillboard();
        }

        // --------------------------------------------------------------- production

        public bool StartProduction()
        {
            if (!isPurchased || state != MachineState.Idle || currentRecipe == null)
            {
                return false;
            }

            if (InventorySystem.Instance == null)
            {
                CropSelectionUI.ShowToast("Không tìm thấy kho nông trại!");
                return false;
            }

            // Check every ingredient before consuming anything
            string missing = GetMissingIngredientText();
            if (!string.IsNullOrEmpty(missing))
            {
                CropSelectionUI.ShowToast($"Thiếu nguyên liệu: {missing}!");
                return false;
            }

            // Deduct ingredients
            var ingredients = currentRecipe.GetIngredients();
            foreach (var ing in ingredients)
            {
                InventorySystem.Instance.RemoveItem(ing.Key, ing.Value);
            }

            currentTimer = 0f;
            totalDuration = currentRecipe.productionTime;
            state = MachineState.Producing;
            UpdateBillboard();

            CropSelectionUI.ShowToast($"Đã bắt đầu sản xuất {currentRecipe.displayName} ({totalDuration}s)!");
            Debug.Log($"[ProductionMachine] Started {currentRecipe.displayName} on {displayName}");
            SaveLoadSystem.SaveGame();
            return true;
        }

        public bool CollectProduct()
        {
            if (state != MachineState.Completed || currentRecipe == null)
            {
                return false;
            }

            if (pendingOutputAmount <= 0) pendingOutputAmount = currentRecipe.outputAmount;

            // Check Storage capacity
            if (InventorySystem.Instance == null || InventorySystem.Instance.IsFull(pendingOutputAmount))
            {
                CropSelectionUI.ShowToast("Storage Full! Không thể nhận sản phẩm, hãy nâng cấp kho.");
                Debug.LogWarning("[ProductionMachine] Cannot collect: Storage Full!");
                return false;
            }

            // Add product to inventory
            string outId = currentRecipe.outputItemId;
            int amount = pendingOutputAmount;
            if (!InventorySystem.Instance.AddItem(outId, amount))
            {
                CropSelectionUI.ShowToast("Storage Full! Không thể nhận sản phẩm, hãy nâng cấp kho.");
                return false;
            }

            // Add XP
            PlayerProgression.AddXP(currentRecipe.productionXP, $"Sản xuất {currentRecipe.displayName}");

            CropSelectionUI.ShowToast($"Đã thu hoạch {ItemName(outId)} +{amount}!\n+{currentRecipe.productionXP} XP");
            Debug.Log($"[ProductionMachine] Collected {amount} {outId} from {displayName}");

            state = MachineState.Idle;
            currentTimer = 0f;
            pendingOutputAmount = 0;
            UpdateBillboard();

            OnProductCollected?.Invoke(this, outId, amount);
            SaveLoadSystem.SaveGame();
            return true;
        }

        public void CancelProduction()
        {
            if (state == MachineState.Producing)
            {
                // Refund ingredients (only when the storage can hold them, otherwise they would be lost)
                if (currentRecipe != null)
                {
                    if (InventorySystem.Instance == null)
                    {
                        return;
                    }

                    int refundTotal = 0;
                    foreach (var ing in currentRecipe.GetIngredients()) refundTotal += ing.Value;
                    if (InventorySystem.Instance.IsFull(refundTotal))
                    {
                        CropSelectionUI.ShowToast("Kho đã đầy! Hãy dọn kho trước khi hủy để nhận lại nguyên liệu.");
                        return;
                    }

                    foreach (var ing in currentRecipe.GetIngredients())
                    {
                        InventorySystem.Instance.AddItem(ing.Key, ing.Value);
                    }
                }

                state = MachineState.Idle;
                currentTimer = 0f;
                UpdateBillboard();
                CropSelectionUI.ShowToast($"Đã hủy sản xuất trên {displayName}. Hoàn lại nguyên liệu.");
                SaveLoadSystem.SaveGame();
            }
        }

        public void ForceComplete()
        {
            if (state == MachineState.Producing && currentRecipe != null)
            {
                currentTimer = totalDuration;
                state = MachineState.Completed;
                pendingOutputAmount = currentRecipe.outputAmount;
                UpdateBillboard();
            }
        }

        public void OnClickMachine()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            if (!isPurchased)
            {
                CropSelectionUI.ShowToast($"{displayName}: Chưa mua! Hãy mở Shop để mua máy.");
                ShopUI.OpenMachineShop();
                return;
            }

            if (state == MachineState.Completed)
            {
                CollectProduct();
                return;
            }

            // Open Machine Production UI
            MachineUI.OpenMachine(this);
        }

        public static void SafeDestroy(UnityEngine.Object obj)
        {
            if (obj == null) return;
            if (Application.isPlaying)
                Destroy(obj);
            else
                DestroyImmediate(obj);
        }
    }
}
