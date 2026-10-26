using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    [Serializable]
    public class DecorationItemData
    {
        public string id;
        public string displayName;
        public string category; // "Hàng rào", "Đèn", "Ghế", "Bảng hiệu", "Cây xanh", "Máng ăn"
        public int priceGold;
        public Vector3 footprint;
        public string prefabPath;
    }

    /// <summary>
    /// Manages spawning, cataloging, and placement of farm decorations:
    /// Fences, Lamps, Benches, Signboards, Deco Trees, and Troughs.
    /// Fully integrated with FarmEditModeController and SaveLoadSystem.
    /// </summary>
    public class FarmDecorationManager : MonoBehaviour
    {
        public static FarmDecorationManager Instance { get; private set; }

        public static readonly List<DecorationItemData> AvailableDecorations = new List<DecorationItemData>
        {
            new DecorationItemData
            {
                id = "deco_fence",
                displayName = "Hàng rào gỗ (Fence)",
                category = "Hàng rào",
                priceGold = 25,
                footprint = new Vector3(2.5f, 1.2f, 0.6f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Fence1x2.prefab"
            },
            new DecorationItemData
            {
                id = "deco_lamp",
                displayName = "Đèn cổ điển (Lamp)",
                category = "Đèn",
                priceGold = 50,
                footprint = new Vector3(1.2f, 3.0f, 1.2f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Streetlight1A.prefab"
            },
            new DecorationItemData
            {
                id = "deco_bench",
                displayName = "Ghế gỗ nghỉ chân (Bench)",
                category = "Ghế",
                priceGold = 35,
                footprint = new Vector3(1.8f, 1.0f, 1.0f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Bench1.prefab"
            },
            new DecorationItemData
            {
                id = "deco_sign",
                displayName = "Bảng hiệu nông trại (Sign)",
                category = "Bảng hiệu",
                priceGold = 30,
                footprint = new Vector3(1.2f, 1.8f, 0.6f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Signboard1.prefab"
            },
            new DecorationItemData
            {
                id = "deco_tree",
                displayName = "Cây xanh bóng mát (Tree)",
                category = "Cây xanh",
                priceGold = 60,
                footprint = new Vector3(2.2f, 3.5f, 2.2f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Tree1.prefab"
            },
            new DecorationItemData
            {
                id = "deco_trough",
                displayName = "Máng ăn chăn nuôi (Trough)",
                category = "Máng ăn",
                priceGold = 45,
                footprint = new Vector3(2.0f, 0.8f, 1.2f),
                prefabPath = "Assets/3rd Party/Cartoon Town and Farm/Prefabs/Trough.prefab"
            }
        };

        public List<DecorationItemData> decorationCatalog => AvailableDecorations;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            EnsureInstance();
        }

        public static FarmDecorationManager EnsureInstance()
        {
            if (Instance != null) return Instance;

            FarmDecorationManager found = FindAnyObjectByType<FarmDecorationManager>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            GameObject go = new GameObject("FarmDecorationManager");
            Instance = go.AddComponent<FarmDecorationManager>();
            DontDestroyOnLoad(go);
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public static GameObject LoadPrefab(string path)
        {
            GameObject loaded = null;
#if UNITY_EDITOR
            loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#endif
            if (loaded == null)
            {
                string resName = System.IO.Path.GetFileNameWithoutExtension(path);
                loaded = Resources.Load<GameObject>(resName) ?? Resources.Load<GameObject>("Decorations/" + resName);
            }
            return loaded;
        }

        public GameObject SpawnDecoration(string decoId, Vector3 position, Quaternion rotation, bool recordUndo = true)
        {
            DecorationItemData data = AvailableDecorations.Find(d => d.id == decoId);
            if (data == null)
            {
                Debug.LogWarning($"[FarmDecorationManager] Unknown decoration ID: {decoId}");
                return null;
            }

            GameObject prefab = LoadPrefab(data.prefabPath);
            GameObject spawned;

            if (prefab != null)
            {
                spawned = Instantiate(prefab, position, rotation);
            }
            else
            {
                // Procedural fallback
                spawned = CreateProceduralDecoration(data, position, rotation);
            }

            string uniqueId = "Deco_" + decoId + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            spawned.name = uniqueId;

            // Ensure collider
            if (spawned.GetComponent<Collider>() == null)
            {
                BoxCollider box = spawned.AddComponent<BoxCollider>();
                box.size = data.footprint;
                box.center = new Vector3(0f, data.footprint.y * 0.5f, 0f);
            }

            // Snap to ground
            GroundSnapUtility.SnapToGroundExact(spawned);

            // Attach and configure MovableFarmObject
            MovableFarmObject mfo = spawned.GetComponent<MovableFarmObject>();
            if (mfo == null) mfo = spawned.AddComponent<MovableFarmObject>();
            mfo.objectId = uniqueId;
            mfo.displayName = data.displayName;
            mfo.categoryGroupName = "TRANG TRÍ";
            mfo.objectType = MovableType.Decoration;
            mfo.footprintSize = data.footprint;
            mfo.RecordCurrentTransform();

            AchievementSystem.Instance?.NotifyDecorationPlacedOrMoved();
            Debug.Log($"[FarmDecorationManager] Spawned {data.displayName} at {position}.");

            return spawned;
        }

        private GameObject CreateProceduralDecoration(DecorationItemData data, Vector3 pos, Quaternion rot)
        {
            GameObject go = new GameObject("Procedural_" + data.id);
            go.transform.position = pos;
            go.transform.rotation = rot;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = "Visual";
            visual.transform.SetParent(go.transform, false);
            visual.transform.localScale = data.footprint;
            visual.transform.localPosition = new Vector3(0f, data.footprint.y * 0.5f, 0f);

            Renderer r = visual.GetComponent<Renderer>();
            if (r != null)
            {
                Color c = Color.white;
                if (data.id.Contains("fence") || data.id.Contains("bench")) c = new Color(0.65f, 0.45f, 0.25f);
                else if (data.id.Contains("lamp")) c = new Color(0.25f, 0.25f, 0.28f);
                else if (data.id.Contains("tree")) c = new Color(0.25f, 0.65f, 0.35f);
                else if (data.id.Contains("trough")) c = new Color(0.55f, 0.35f, 0.15f);
                r.material.color = c;
            }

            return go;
        }
    }
}
