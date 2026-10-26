using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Enforces boundary containment for farm animals inside an enclosed pen (Cow Pen, Chicken Coop).
    /// Ensures animals always spawn, wander, and stay strictly inside the fenced area.
    /// </summary>
    [SelectionBase]
    public class AnimalPenBoundary : MonoBehaviour
    {
        [Header("Pen Boundary Settings")]
        [Tooltip("Identifier matching FarmBuilding buildingId (cow_barn, chicken_coop).")]
        public string penType = "cow_barn";

        [Tooltip("Center offset in local coordinates.")]
        public Vector3 centerOffset = Vector3.zero;

        [Tooltip("Total dimensions of the inner pen wandering volume (X=Width, Y=Height, Z=Length).")]
        public Vector3 innerBoundsSize = new Vector3(4.6f, 2.0f, 4.6f);

        [Header("Registered Animals")]
        public List<GameObject> registeredAnimals = new List<GameObject>();

        [Header("Boundary Clamping")]
        [Tooltip("Safety margin from the fence inner perimeter in meters.")]
        public float defaultMargin = 0.4f;

        /// <summary>
        /// World space axis-aligned bounding box of the pen.
        /// </summary>
        public Bounds WorldBounds
        {
            get
            {
                Vector3 center = transform.position + transform.TransformVector(centerOffset);
                return new Bounds(center, innerBoundsSize);
            }
        }

        void Awake()
        {
            ScanAndRegisterExistingAnimals();
        }

        void Start()
        {
            ScanAndRegisterExistingAnimals();
        }

        /// <summary>
        /// Discovers any animals parented under this pen and registers them.
        /// </summary>
        public void ScanAndRegisterExistingAnimals()
        {
            AnimalCareController[] accs = GetComponentsInChildren<AnimalCareController>(true);
            foreach (var a in accs)
            {
                if (a != null && a.gameObject != gameObject && !registeredAnimals.Contains(a.gameObject))
                {
                    RegisterAnimal(a.gameObject);
                }
            }

            // Also check for CowControllers
            CowController[] cows = GetComponentsInChildren<CowController>(true);
            foreach (var c in cows)
            {
                if (c != null && !registeredAnimals.Contains(c.gameObject))
                {
                    RegisterAnimal(c.gameObject);
                }
            }
        }

        /// <summary>
        /// Registers an animal to be constrained within this pen.
        /// </summary>
        public void RegisterAnimal(GameObject animalGo)
        {
            if (animalGo == null) return;
            if (!registeredAnimals.Contains(animalGo))
            {
                registeredAnimals.Add(animalGo);
            }

            // Link CowController if present
            CowController cowCtrl = animalGo.GetComponent<CowController>();
            if (cowCtrl != null)
            {
                cowCtrl.penBoundary = this;
            }

            // Link ChickenWanderer if present
            ChickenWanderer chkWander = animalGo.GetComponent<ChickenWanderer>();
            if (chkWander != null)
            {
                chkWander.penBoundary = this;
            }

            // Link PigWanderer if present
            PigWanderer pigWander = animalGo.GetComponent<PigWanderer>();
            if (pigWander != null)
            {
                pigWander.penBoundary = this;
            }

            // Link HorseWanderer if present
            HorseWanderer horseWander = animalGo.GetComponent<HorseWanderer>();
            if (horseWander != null)
            {
                horseWander.penBoundary = this;
            }
        }

        /// <summary>
        /// Unregisters an animal from this pen.
        /// </summary>
        public void UnregisterAnimal(GameObject animalGo)
        {
            if (animalGo != null)
            {
                registeredAnimals.Remove(animalGo);
            }
        }

        /// <summary>
        /// Generates a random world position strictly inside the pen boundary,
        /// maintaining separation from other animals in the pen so they never stack or line up.
        /// Samples the active terrain height to place the animal flush on the ground.
        /// </summary>
        public Vector3 GetRandomPositionInside(float margin = 0.5f)
        {
            float effectiveMargin = Mathf.Max(margin, defaultMargin);
            float halfX = Mathf.Max(0.5f, (innerBoundsSize.x * 0.5f) - effectiveMargin);
            float halfZ = Mathf.Max(0.5f, (innerBoundsSize.z * 0.5f) - effectiveMargin);

            float minSep = (penType == "cow_barn" || penType == "horse_stable") ? 1.5f :
                           (penType == "pig_pen" ? 1.1f : 0.75f);

            Vector3 bestWorldPos = transform.TransformPoint(centerOffset);
            float bestMinDist = -1f;

            for (int attempt = 0; attempt < 12; attempt++)
            {
                float localX = Random.Range(-halfX, halfX);
                float localZ = Random.Range(-halfZ, halfZ);

                Vector3 localPos = centerOffset + new Vector3(localX, 0f, localZ);
                Vector3 candidateWorld = transform.TransformPoint(localPos);

                float nearestDist = float.MaxValue;
                for (int i = 0; i < registeredAnimals.Count; i++)
                {
                    GameObject other = registeredAnimals[i];
                    if (other == null) continue;
                    Vector2 d = new Vector2(candidateWorld.x - other.transform.position.x, candidateWorld.z - other.transform.position.z);
                    float dist = d.magnitude;
                    if (dist < nearestDist) nearestDist = dist;
                }

                if (nearestDist > bestMinDist)
                {
                    bestMinDist = nearestDist;
                    bestWorldPos = candidateWorld;
                }

                if (nearestDist >= minSep)
                {
                    break;
                }
            }

            // Ground alignment
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                bestWorldPos.y = terrain.SampleHeight(bestWorldPos) + terrain.transform.position.y;
            }
            else
            {
                bestWorldPos.y = transform.position.y;
            }

            return bestWorldPos;
        }

        /// <summary>
        /// Clamps a world position to ensure it remains strictly inside the pen boundary.
        /// </summary>
        public Vector3 ClampPosition(Vector3 worldPos, float margin = 0.35f)
        {
            float effectiveMargin = Mathf.Max(margin, defaultMargin);
            Vector3 localPos = transform.InverseTransformPoint(worldPos) - centerOffset;

            float halfX = Mathf.Max(0.5f, (innerBoundsSize.x * 0.5f) - effectiveMargin);
            float halfZ = Mathf.Max(0.5f, (innerBoundsSize.z * 0.5f) - effectiveMargin);

            localPos.x = Mathf.Clamp(localPos.x, -halfX, halfX);
            localPos.z = Mathf.Clamp(localPos.z, -halfZ, halfZ);

            Vector3 clampedWorld = transform.TransformPoint(localPos + centerOffset);

            // Maintain terrain ground height
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                clampedWorld.y = terrain.SampleHeight(clampedWorld) + terrain.transform.position.y;
            }
            else
            {
                clampedWorld.y = worldPos.y;
            }

            return clampedWorld;
        }

        /// <summary>
        /// Checks whether a given world position is inside the pen bounds.
        /// </summary>
        public bool IsInside(Vector3 worldPos, float margin = 0f)
        {
            Vector3 localPos = transform.InverseTransformPoint(worldPos) - centerOffset;
            float halfX = (innerBoundsSize.x * 0.5f) - margin;
            float halfZ = (innerBoundsSize.z * 0.5f) - margin;

            return localPos.x >= -halfX && localPos.x <= halfX &&
                   localPos.z >= -halfZ && localPos.z <= halfZ;
        }

        void LateUpdate()
        {
            // Clean destroyed references
            registeredAnimals.RemoveAll(a => a == null || !a);

            if (registeredAnimals.Count == 0 && transform.childCount > 0)
            {
                ScanAndRegisterExistingAnimals();
            }

            float minSep = (penType == "cow_barn" || penType == "horse_stable") ? 1.35f :
                           (penType == "pig_pen" ? 0.95f : 0.65f);

            // Gentle separation pass so animals never stack on top of each other or form a dense line
            for (int i = 0; i < registeredAnimals.Count; i++)
            {
                GameObject a = registeredAnimals[i];
                if (a == null) continue;

                for (int j = i + 1; j < registeredAnimals.Count; j++)
                {
                    GameObject b = registeredAnimals[j];
                    if (b == null) continue;

                    Vector3 diff = a.transform.position - b.transform.position;
                    diff.y = 0f;
                    float dist = diff.magnitude;

                    if (dist < minSep)
                    {
                        Vector3 pushDir;
                        if (dist < 0.05f)
                        {
                            float angle = (i * 1.7f + j * 2.3f);
                            pushDir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                        }
                        else
                        {
                            pushDir = diff / dist;
                        }

                        float pushAmount = (minSep - dist) * 0.5f;
                        a.transform.position += pushDir * pushAmount;
                        b.transform.position -= pushDir * pushAmount;
                    }
                }
            }

            for (int i = 0; i < registeredAnimals.Count; i++)
            {
                GameObject animal = registeredAnimals[i];
                if (animal == null) continue;

                Vector3 currentPos = animal.transform.position;
                Vector3 localPos = transform.InverseTransformPoint(currentPos) - centerOffset;

                float halfX = Mathf.Max(0.5f, (innerBoundsSize.x * 0.5f) - defaultMargin);
                float halfZ = Mathf.Max(0.5f, (innerBoundsSize.z * 0.5f) - defaultMargin);

                // If severely outside (> 4m), teleport back inside pen
                if (Mathf.Abs(localPos.x) > halfX + 4f || Mathf.Abs(localPos.z) > halfZ + 4f)
                {
                    Vector3 safePos = GetRandomPositionInside(defaultMargin + 0.2f);
                    animal.transform.position = safePos;
                    continue;
                }

                // Always clamp and snap Y to terrain inside pen
                animal.transform.position = ClampPosition(currentPos, defaultMargin);
            }
        }

        /// <summary>
        /// Called when the pen building is moved or rotated in Farm Edit Mode.
        /// Shifts all registered animals with the pen and re-clamps them cleanly on terrain.
        /// </summary>
        public void NotifyPenMoved(Vector3 deltaPosition, Quaternion deltaRotation)
        {
            ScanAndRegisterExistingAnimals();

            for (int i = 0; i < registeredAnimals.Count; i++)
            {
                GameObject animal = registeredAnimals[i];
                if (animal == null) continue;

                // If not already a child of pen, shift position
                if (animal.transform.parent != transform)
                {
                    animal.transform.position += deltaPosition;
                }

                // Place safely inside new boundary and snap to terrain
                Vector3 safePos = ClampPosition(animal.transform.position, defaultMargin);
                if (Terrain.activeTerrain != null)
                {
                    safePos.y = Terrain.activeTerrain.SampleHeight(safePos) + Terrain.activeTerrain.transform.position.y;
                }
                animal.transform.position = safePos;

                ChickenWanderer cw = animal.GetComponent<ChickenWanderer>();
                if (cw != null)
                {
                    cw.SendMessage("StartIdle", SendMessageOptions.DontRequireReceiver);
                }

                PigWanderer pw = animal.GetComponent<PigWanderer>();
                if (pw != null)
                {
                    pw.SendMessage("StartIdle", SendMessageOptions.DontRequireReceiver);
                }

                HorseWanderer hw = animal.GetComponent<HorseWanderer>();
                if (hw != null)
                {
                    hw.SendMessage("StartIdle", SendMessageOptions.DontRequireReceiver);
                }

                CowController cc = animal.GetComponent<CowController>();
                if (cc != null)
                {
                    cc.SendMessage("StopMoving", SendMessageOptions.DontRequireReceiver);
                }
            }
        }

        public void SnapPenAndAnimalsToGround()
        {
            GroundSnapUtility.SnapBuildingToGround(gameObject);
            NotifyPenMoved(Vector3.zero, Quaternion.identity);
        }


        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.1f, 1f, 0.2f, 0.95f);
            Vector3 center = transform.position + transform.TransformVector(centerOffset);
            Gizmos.DrawWireCube(center, innerBoundsSize);

            // Draw gate opening indicator
            Vector3 gateCenter = transform.position + transform.TransformVector(centerOffset + new Vector3(0f, 0.5f, -innerBoundsSize.z * 0.5f));
            Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            Gizmos.DrawWireCube(gateCenter, new Vector3(1.6f, 1.2f, 0.3f));
        }
    }
}
