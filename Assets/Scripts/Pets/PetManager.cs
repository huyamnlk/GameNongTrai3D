using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IrishFarmSim
{
    /// <summary>
    /// Manages farm pets (Dogs and Cats): spawning, lifecycle, save/load,
    /// and ensuring pets naturally roam around the farm without duplicating or interfering with gameplay.
    /// </summary>
    public class PetManager : MonoBehaviour
    {
        public static PetManager Instance { get; private set; }

        public List<PetWanderer> activePets = new List<PetWanderer>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInit()
        {
            if (SceneManager.GetActiveScene().name.Equals("Farm", StringComparison.OrdinalIgnoreCase))
            {
                EnsureInstance();
            }
        }

        public static PetManager EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.PruneAndScanPets();
                return Instance;
            }

            PetManager found = FindAnyObjectByType<PetManager>();
            if (found != null)
            {
                Instance = found;
                Instance.PruneAndScanPets();
                return found;
            }

            GameObject go = new GameObject("PetManager");
            Instance = go.AddComponent<PetManager>();
            Instance.PruneAndScanPets();
            return Instance;
        }

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            PruneAndScanPets();
        }

        void Start()
        {
            PruneAndScanPets();
            EnsureDefaultPets();
        }

        public void EnsureDefaultPets()
        {
            PruneAndScanPets();
            if (activePets.Count == 0) SpawnDefaultPets();
        }

        public void PruneAndScanPets()
        {
            activePets.RemoveAll(p => p == null || !p.gameObject);
            PetWanderer[] found = FindObjectsByType<PetWanderer>(FindObjectsSortMode.None);
            foreach (var pet in found)
            {
                if (pet != null && !activePets.Contains(pet))
                {
                    activePets.Add(pet);
                }
            }
        }

        public void SpawnDefaultPets()
        {
            PruneAndScanPets();
            if (activePets.Count > 0) return;

            // Exactly 3 Dogs with distinct appearances
            // 1. Golden Retriever Dog
            SpawnPet("dog_golden", PetKind.Dog, "golden", "Chó Vàng (Lucky)", new Vector3(82f, 0f, 252f));

            // 2. Spotted Dalmatian Dog
            SpawnPet("dog_spotted", PetKind.Dog, "spotted", "Chó Đốm (Spot)", new Vector3(68f, 0f, 242f));

            // 3. Husky Dog
            SpawnPet("dog_husky", PetKind.Dog, "husky", "Chó Husky (Rocky)", new Vector3(76f, 0f, 230f));

            // Exactly 3 Cats with distinct appearances
            // 4. Orange Tabby Cat
            SpawnPet("cat_orange", PetKind.Cat, "orange", "Mèo Mướp (Miu)", new Vector3(92f, 0f, 236f));

            // 5. Gray Cat
            SpawnPet("cat_gray", PetKind.Cat, "gray", "Mèo Xám (Luna)", new Vector3(106f, 0f, 238f));

            // 6. Calico Cat
            SpawnPet("cat_calico", PetKind.Cat, "calico", "Mèo Tam Thể (Bông)", new Vector3(98f, 0f, 246f));

            Debug.Log($"[PetManager] Spawned {activePets.Count} default farm pets (3 Dogs & 3 Cats).");
        }

        public PetWanderer SpawnPet(string petId, PetKind kind, string coatId, string displayName, Vector3 spawnPos)
        {
            // Ground snap
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                spawnPos.y = terrain.SampleHeight(spawnPos) + terrain.transform.position.y;
            }

            GameObject petGo = new GameObject("Pet_" + petId);
            petGo.transform.position = spawnPos;
            petGo.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);

            PetWanderer wanderer = petGo.AddComponent<PetWanderer>();
            wanderer.SetupPet(petId, kind, coatId, displayName);

            activePets.Add(wanderer);
            return wanderer;
        }

        public List<PetSaveData> CaptureSaveData()
        {
            PruneAndScanPets();
            List<PetSaveData> list = new List<PetSaveData>();
            foreach (var pet in activePets)
            {
                if (pet == null) continue;
                Vector3 p = pet.transform.position;
                list.Add(new PetSaveData
                {
                    petId = pet.petId,
                    petType = pet.kind.ToString(),
                    coatId = pet.coatId,
                    posX = p.x,
                    posY = p.y,
                    posZ = p.z,
                    rotY = pet.transform.eulerAngles.y
                });
            }
            return list;
        }

        public void RestoreFromSaveData(List<PetSaveData> list)
        {
            PruneAndScanPets();

            if (list == null || list.Count == 0)
            {
                EnsureDefaultPets();
                return;
            }

            Dictionary<string, PetWanderer> map = new Dictionary<string, PetWanderer>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in activePets)
            {
                if (p != null) map[p.petId] = p;
            }

            foreach (var s in list)
            {
                if (s == null || string.IsNullOrEmpty(s.petId)) continue;

                Vector3 targetPos = new Vector3(s.posX, s.posY, s.posZ);
                if (Terrain.activeTerrain != null && targetPos.sqrMagnitude > 0.01f)
                {
                    targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos) + Terrain.activeTerrain.transform.position.y;
                }

                if (map.TryGetValue(s.petId, out PetWanderer existing) && existing != null)
                {
                    if (targetPos.sqrMagnitude > 0.01f)
                    {
                        existing.transform.position = targetPos;
                        existing.transform.rotation = Quaternion.Euler(0f, s.rotY, 0f);
                    }
                }
                else
                {
                    PetKind k = PetKind.Dog;
                    Enum.TryParse(s.petType, true, out k);
                    string coat = s.coatId;
                    string name;
                    if (k == PetKind.Dog)
                    {
                        name = coat == "spotted" ? "Chó Đốm (Spot)" : (coat == "husky" ? "Chó Husky (Rocky)" : "Chó Vàng (Lucky)");
                    }
                    else
                    {
                        name = coat == "gray" ? "Mèo Xám (Luna)" : (coat == "calico" ? "Mèo Tam Thể (Bông)" : "Mèo Mướp (Miu)");
                    }

                    SpawnPet(s.petId, k, coat, name, targetPos);
                }
            }

            PruneAndScanPets();
            Debug.Log($"[PetManager] Restored {activePets.Count} pets from save data.");
        }
    }
}
