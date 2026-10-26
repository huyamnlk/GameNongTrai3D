using System;
using UnityEngine;

namespace IrishFarmSim
{
    public enum AnimalType
    {
        Cow,
        Chicken,
        Pig,
        Horse
    }

    /// <summary>
    /// Handles animal stats (Hunger, Happiness), production timer (Milk, Eggs, Bacon, Leather),
    /// feeding with inventory deduction, product harvesting, and XP rewards.
    /// </summary>
    public class AnimalCareController : MonoBehaviour
    {
        public static event Action<AnimalCareController> OnProductReady;
        public static event Action<AnimalCareController, string> OnAnimalFed;
        public static event Action<AnimalCareController, string, int> OnProductCollected;
        public static event Action<AnimalCareController> OnAnimalPetted;

        public static AnimalCareController SelectedAnimal { get; private set; }
        public static bool ShowDebugAnimalLabels = false;

        [Header("Selection State")]
        public bool isSelected = false;

        [Header("Animal Information")]
        public AnimalType animalType = AnimalType.Cow;
        public string animalName = "Bò sữa";
        public string breed = "Holstein";

        [Header("Stats (0 - 100)")]
        [Range(0, 100)] public float hunger = 60f;     // Higher = fuller
        [Range(0, 100)] public float happiness = 50f;  // Higher = happier
        [Range(0, 100)] public float affection = 50f;  // Higher = more affectionate

        [Header("Production Settings")]
        public string productItemId = "milk";
        public string productName = "Sữa bò tươi (Milk)";
        public int productAmount = 2;
        public float productionDuration = 25f; // Seconds
        public float productTimer = 0f;
        public bool isProductReady = false;

        [Header("XP Rewards")]
        public int feedXP = 3;
        public int collectXP = 15;

        private TextMesh statusLabel;

        void Awake()
        {
            SetupStatusLabel();
        }

        void Start()
        {
            // Sync initial state
            if (animalType == AnimalType.Cow)
            {
                productItemId = "milk";
                productName = "Sữa bò tươi (Milk)";
                CowController cc = GetComponent<CowController>();
                if (cc != null && cc.cow != null)
                {
                    if (!string.IsNullOrEmpty(cc.cow.breed))
                    {
                        breed = cc.cow.breed;
                    }
                    if (string.IsNullOrEmpty(animalName) || animalName.Trim() == "Bò" || animalName == "Bò sữa")
                    {
                        if (!string.IsNullOrEmpty(cc.cow.name) && cc.cow.name.Trim() != "Bò")
                            animalName = cc.cow.name;
                        else if (!string.IsNullOrEmpty(cc.cow.breed))
                            animalName = "Bò " + cc.cow.breed;
                        else
                            animalName = "Bò sữa (Cow)";
                    }
                    if (cc.cow.happiness > 0 && Mathf.Approximately(happiness, 50f))
                    {
                        happiness = cc.cow.happiness * 10f;
                    }
                }
            }
            else if (animalType == AnimalType.Chicken)
            {
                productItemId = "egg";
                productName = "Trứng gà tươi (Egg)";
                productAmount = 3;
                productionDuration = 15f;
            }
            else if (animalType == AnimalType.Pig)
            {
                productItemId = "bacon";
                productName = "Thịt xông khói (Bacon)";
                productAmount = 2;
                productionDuration = 30f;
            }
            else if (animalType == AnimalType.Horse)
            {
                productItemId = "leather";
                productName = "Da thuộc (Leather)";
                productAmount = 1;
                productionDuration = 35f;
            }
        }

        void Update()
        {
            // Hunger slowly decreases over time
            if (hunger > 0f)
            {
                hunger = Mathf.Max(0f, hunger - Time.deltaTime * 0.4f);
            }

            // Production timer runs when fed (hunger > 20)
            if (!isProductReady)
            {
                if (hunger > 15f)
                {
                    // Happiness bonus multiplier
                    float speedMultiplier = 1f + (happiness / 100f) * 0.5f;
                    productTimer += Time.deltaTime * speedMultiplier;

                    if (productTimer >= productionDuration)
                    {
                        productTimer = productionDuration;
                        isProductReady = true;
                        OnProductReady?.Invoke(this);
                        Debug.Log($"[AnimalCare] {animalName} đã tạo xong {productName}!");
                    }
                }
            }

            // Editor Debug Shortcut 'F': Instant product ready
            if (Application.isEditor && Input.GetKeyDown(KeyCode.F))
            {
                InstantProduce();
            }

            UpdateStatusLabel();
        }

        public void InstantProduce()
        {
            productTimer = productionDuration;
            isProductReady = true;
            UpdateStatusLabel();
        }

        public bool Feed(string feedItemId)
        {
            string resolvedId = feedItemId;
            if (InventorySystem.Instance != null)
            {
                if (!InventorySystem.Instance.HasItem(resolvedId, 1))
                {
                    if (feedItemId == "feed_hay" && InventorySystem.Instance.HasItem("hay", 1)) resolvedId = "hay";
                    else if (feedItemId == "hay" && InventorySystem.Instance.HasItem("feed_hay", 1)) resolvedId = "feed_hay";
                    else if (feedItemId == "feed_grain" && InventorySystem.Instance.HasItem("grain", 1)) resolvedId = "grain";
                    else if (feedItemId == "grain" && InventorySystem.Instance.HasItem("feed_grain", 1)) resolvedId = "feed_grain";
                    else if (feedItemId == "feed_pellets" && InventorySystem.Instance.HasItem("pellet", 1)) resolvedId = "pellet";
                    else if (feedItemId == "pellet" && InventorySystem.Instance.HasItem("feed_pellets", 1)) resolvedId = "feed_pellets";
                }
            }

            if (InventorySystem.Instance == null || !InventorySystem.Instance.HasItem(resolvedId, 1))
            {
                Debug.LogWarning($"[AnimalCare] Không đủ '{resolvedId}' trong kho để cho ăn!");
                return false;
            }

            // Deduct feed item from inventory
            InventorySystem.Instance.RemoveItem(resolvedId, 1);

            // Increase stats
            hunger = Mathf.Min(100f, hunger + 35f);
            happiness = Mathf.Min(100f, happiness + 25f);

            // Speed up production timer
            productTimer = Mathf.Min(productionDuration, productTimer + 5f);

            // Grant XP
            string feedName = (resolvedId == "hay" || resolvedId == "feed_hay") ? "Cỏ khô (Hay)" :
                             ((resolvedId == "grain" || resolvedId == "feed_grain") ? "Ngô hạt (Grain)" : "Thức ăn viên (Pellets)");
            PlayerProgression.AddXP(feedXP, $"Cho {animalName} ăn {feedName}");

            Debug.Log($"[AnimalCare] Đã cho {animalName} ăn {feedName}. No: {(int)hunger}%, Vui vẻ: {(int)happiness}%");
            OnAnimalFed?.Invoke(this, resolvedId);
            return true;
        }

        public bool Pet()
        {
            affection = Mathf.Min(100f, affection + 15f);
            happiness = Mathf.Min(100f, happiness + 10f);
            PlayerProgression.AddXP(2, $"Vuốt ve {animalName}");
            CropSelectionUI.ShowToast($"❤️ Đã vuốt ve {animalName}!\nTình cảm: {(int)affection}%, Vui vẻ: {(int)happiness}%");
            OnAnimalPetted?.Invoke(this);
            return true;
        }

        public bool CollectProduct()
        {
            if (!isProductReady)
            {
                Debug.LogWarning($"[AnimalCare] Sản phẩm của {animalName} chưa sẵn sàng!");
                return false;
            }

            if (InventorySystem.Instance != null && InventorySystem.Instance.IsFull(productAmount))
            {
                Debug.LogWarning("[AnimalCare] Không thể thu hoạch: Kho đầy (Storage Full)!");
                return false;
            }

            // Add product to inventory
            if (InventorySystem.Instance != null)
            {
                InventorySystem.Instance.AddItem(productItemId, productAmount);
            }

            // Grant XP
            PlayerProgression.AddXP(collectXP, $"Thu hoạch {productName}");

            // Reset production
            isProductReady = false;
            productTimer = 0f;
            happiness = Mathf.Min(100f, happiness + 10f);
            affection = Mathf.Min(100f, affection + 5f);

            Debug.Log($"[AnimalCare] Thu hoạch thành công: +{productAmount} {productName} vào kho!");
            OnProductCollected?.Invoke(this, productItemId, productAmount);
            return true;
        }

        public void OpenAnimalCareUI()
        {
            if (FarmEditModeController.IsEditModeActive) return;

            Select();
            AnimalUI.OpenForAnimal(this);
        }

        public void Select()
        {
            if (SelectedAnimal != null && SelectedAnimal != this)
            {
                SelectedAnimal.Deselect();
            }
            SelectedAnimal = this;
            isSelected = true;
            UpdateStatusLabel();
        }

        public void Deselect()
        {
            isSelected = false;
            if (SelectedAnimal == this) SelectedAnimal = null;
            UpdateStatusLabel();
        }

        public static void DeselectAll()
        {
            if (SelectedAnimal != null)
            {
                SelectedAnimal.Deselect();
            }
        }

        private void SetupStatusLabel()
        {
            Transform existing = transform.Find("AnimalStatusLabel");
            if (existing != null)
            {
                statusLabel = existing.GetComponent<TextMesh>();
                return;
            }

            GameObject labelGo = new GameObject("AnimalStatusLabel");
            labelGo.transform.SetParent(transform, false);

            float labelY = (animalType == AnimalType.Chicken) ? 0.65f :
                           (animalType == AnimalType.Pig ? 0.95f :
                           (animalType == AnimalType.Horse ? 1.85f : 1.35f));
            labelGo.transform.localPosition = new Vector3(0f, labelY, 0f);

            statusLabel = labelGo.AddComponent<TextMesh>();
            statusLabel.font = FarmUITheme.BoldFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statusLabel.fontSize = 48;
            statusLabel.characterSize = 0.07f;
            statusLabel.fontStyle = FontStyle.Bold;
            statusLabel.alignment = TextAlignment.Center;
            statusLabel.anchor = TextAnchor.MiddleCenter;
            statusLabel.color = Color.white;

            MeshRenderer mr = labelGo.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = statusLabel.font.material;

            labelGo.AddComponent<BillboardFaceCamera>();
            labelGo.SetActive(false);
        }

        private void UpdateStatusLabel()
        {
            if (statusLabel == null) return;

            // Debug shortcut 'H' in editor to toggle debug overlay for all animals
            if (Application.isEditor && Input.GetKeyDown(KeyCode.H))
            {
                ShowDebugAnimalLabels = !ShowDebugAnimalLabels;
            }

            // 1. If currently selected, show detailed care stats
            if (isSelected)
            {
                statusLabel.gameObject.SetActive(true);
                if (isProductReady)
                {
                    statusLabel.text = $"★ {animalName} ★\n[SẴN SÀNG THU HOẠCH]";
                    statusLabel.color = new Color(1f, 0.9f, 0.2f);
                }
                else
                {
                    int p = (int)(Mathf.Clamp01(productTimer / productionDuration) * 100f);
                    int secLeft = Mathf.CeilToInt(productionDuration - productTimer);
                    statusLabel.text = $"{animalName}\nNo: {(int)hunger}% | {p}% ({secLeft}s)";
                    statusLabel.color = hunger < 30f ? new Color(1f, 0.4f, 0.4f) : Color.white;
                }
                return;
            }

            // 2. If debug key is active
            if (ShowDebugAnimalLabels)
            {
                statusLabel.gameObject.SetActive(true);
                int p = (int)(Mathf.Clamp01(productTimer / productionDuration) * 100f);
                statusLabel.text = $"{animalName}\n[Debug: {p}%]";
                statusLabel.color = Color.cyan;
                return;
            }

            // 3. If product is ready, show a small tidy label
            if (isProductReady)
            {
                statusLabel.gameObject.SetActive(true);
                statusLabel.text = "★ Thu hoạch";
                statusLabel.color = new Color(1f, 0.88f, 0.2f);
                return;
            }

            // 4. Default: Hidden to eliminate overlapping text
            statusLabel.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (statusLabel != null && statusLabel.gameObject.activeSelf && Camera.main != null)
            {
                statusLabel.transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}
