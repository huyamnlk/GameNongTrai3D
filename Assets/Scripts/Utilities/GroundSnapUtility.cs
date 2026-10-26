using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Utility for snapping buildings and structures precisely to the terrain/ground surface.
    /// Calculates the total combined bounds of all child renderers (bottomY = bounds.min.y)
    /// and aligns transform.position.y so the bottom of the structure sits flush on the ground.
    /// </summary>
    public static class GroundSnapUtility
    {
        /// <summary>
        /// Precision Ground Snapping taking renderer and collider bounds into account:
        /// 1. Finds the lowest point of all active Renderers and Colliders (excluding billboards, indicators).
        /// 2. Calculates bottomToPivotOffset = transform.position.y - lowestY.
        /// 3. Queries Terrain and raycasts down to Ground surface.
        /// 4. Sets transform.position.y = groundY + bottomToPivotOffset so wheels/base sit flush on ground.
        /// </summary>
        public static float SnapToGroundExact(GameObject obj)
        {
            return SnapObjectToGround(obj);
        }

        /// <summary>
        /// Precision Ground Snapping for all map objects (Straw, Wood/Lumber, Ladder/Stairs, Decor, etc.):
        /// 1. Calculates the true bottom bounds (bounds.min.y) across all active Renderers and Colliders in the hierarchy.
        /// 2. Raycasts downward from above the object to find the valid Terrain/Ground elevation (ignoring fences, walls, other props).
        /// 3. Adjusts transform.position.y so the true bottom of the object touches the ground with a tiny 0.01f offset to avoid clipping/z-fighting.
        /// 4. Preserves X, Z and rotation Y. Refreshes colliders afterwards.
        /// </summary>
        public static float SnapObjectToGround(GameObject obj)
        {
            if (obj == null) return 0f;

            Vector3 pos = obj.transform.position;

            // Animals are constructed with root pivot at y=0 flush with hoof/foot soles
            if (obj.GetComponent<HorseWanderer>() != null ||
                obj.GetComponent<PigWanderer>() != null ||
                obj.GetComponent<ChickenWanderer>() != null ||
                obj.GetComponent<CowController>() != null ||
                obj.GetComponent<AnimalCareController>() != null)
            {
                float aGroundY = GetGroundY(pos);
                obj.transform.position = new Vector3(pos.x, aGroundY, pos.z);
                return aGroundY;
            }

            // Pens & wall/fence segments: every post/rail/collider gets its own ground height
            if (FenceGroundAligner.AlignObject(obj))
            {
                return obj.transform.position.y;
            }

            // Special tractor handler with wheel prioritization
            string objName = obj.name.ToLower();
            if (objName.Contains("tractor") || objName.Contains("vehicle"))
            {
                return SnapTractorToGroundExact(obj);
            }

            float finalPosY = CalculateGroundedPivotY(obj, pos, obj.transform.rotation);
            obj.transform.position = new Vector3(pos.x, finalPosY, pos.z);

            // Sync physics so colliders immediately reflect updated world transform
            Physics.SyncTransforms();

            // Refresh colliders and update MovableFarmObject original transform tracking
            MovableFarmObject m = obj.GetComponent<MovableFarmObject>();
            if (m != null)
            {
                m.EnsureCollider();
            }

            return finalPosY;
        }

        /// <summary>
        /// Calculates the exact pivot Y so that the lowest contact point of all renderers
        /// sits flush on the terrain/ground surface at the specified horizontal position and rotation.
        /// Evaluates local mesh bounds directly so calculations are instantaneous, deterministic,
        /// and immune to cached world bounds or distant terrain slope distortion.
        /// </summary>
        public static float CalculateGroundedPivotY(GameObject obj, Vector3 worldPos, Quaternion worldRot)
        {
            if (obj == null) return worldPos.y;

            float groundY = SampleGroundElevation(worldPos.x, worldPos.z, obj.transform);
            float bottomOffset = GetObjectBottomOffset(obj, worldRot);

            return groundY + bottomOffset;
        }

        /// <summary>
        /// Computes the exact vertical distance between the object's pivot (transform.position.y)
        /// and the lowest contact point of its active visual meshes/renderers.
        /// </summary>
        public static float GetObjectBottomOffset(GameObject obj, Quaternion worldRot = default)
        {
            if (obj == null) return 0f;

            Quaternion savedRot = obj.transform.rotation;
            if (worldRot != default && worldRot != obj.transform.rotation)
            {
                obj.transform.rotation = worldRot;
            }

            float minLocalY = float.MaxValue;
            bool foundAny = false;

            // 1. MeshFilters: evaluate all 8 corners of each mesh in root object space
            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>(true);
            foreach (var mf in meshFilters)
            {
                if (mf == null || mf.sharedMesh == null || !mf.gameObject.activeInHierarchy) continue;
                string mName = mf.gameObject.name.ToLower();
                if (mName.Contains("billboard") || mName.Contains("blobshadow") ||
                    mName.Contains("indicator") || mName.Contains("gatesign") ||
                    mName.Contains("statustext")) continue;

                Bounds mb = mf.sharedMesh.bounds;
                Vector3[] corners = new Vector3[]
                {
                    mb.min,
                    new Vector3(mb.min.x, mb.min.y, mb.max.z),
                    new Vector3(mb.min.x, mb.max.y, mb.min.z),
                    new Vector3(mb.min.x, mb.max.y, mb.max.z),
                    new Vector3(mb.max.x, mb.min.y, mb.min.z),
                    new Vector3(mb.max.x, mb.min.y, mb.max.z),
                    new Vector3(mb.max.x, mb.max.y, mb.min.z),
                    mb.max
                };

                foreach (var c in corners)
                {
                    Vector3 worldPt = mf.transform.TransformPoint(c);
                    Vector3 rootLocalPt = obj.transform.InverseTransformPoint(worldPt);
                    if (rootLocalPt.y < minLocalY)
                    {
                        minLocalY = rootLocalPt.y;
                        foundAny = true;
                    }
                }
            }

            // 2. Also check renderers (SkinnedMeshRenderer or non-mesh renderers)
            Renderer[] rends = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer) continue;
                string rName = r.gameObject.name.ToLower();
                if (rName.Contains("billboard") || rName.Contains("blobshadow") ||
                    rName.Contains("indicator") || rName.Contains("gatesign") ||
                    rName.Contains("statustext")) continue;

                Vector3 minLocal = obj.transform.InverseTransformPoint(r.bounds.min);
                if (minLocal.y < minLocalY)
                {
                    minLocalY = minLocal.y;
                    foundAny = true;
                }
            }

            if (worldRot != default && worldRot != savedRot)
            {
                obj.transform.rotation = savedRot;
            }

            if (!foundAny || float.IsNaN(minLocalY) || float.IsInfinity(minLocalY))
            {
                return 0f;
            }

            float scaleY = obj.transform.lossyScale.y;
            if (scaleY <= 0.001f) scaleY = 1f;

            float bottomOffset = -minLocalY * scaleY;
            return bottomOffset;
        }

        /// <summary>
        /// Precision ground sampling at a horizontal (x, z) coordinate, querying Terrain and downward raycasts.
        /// </summary>
        public static float SampleGroundElevation(float x, float z, Transform ignoreRoot = null)
        {
            Vector3 testPos = new Vector3(x, 0f, z);
            float groundY = 0f;
            bool detected = false;

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                groundY = terrain.SampleHeight(testPos) + terrain.transform.position.y;
                detected = true;
            }

            // Raycast downward from high up to capture custom ground mesh elevation if any
            float rayStartY = (detected ? Mathf.Max(testPos.y, groundY) : 30f) + 30f;
            Ray ray = new Ray(new Vector3(x, rayStartY, z), Vector3.down);
            int mask = ~((1 << 2) | (1 << 5)); // Exclude IgnoreRaycast and UI
            RaycastHit[] hits = Physics.RaycastAll(ray, 80f, mask, QueryTriggerInteraction.Ignore);
            if (hits != null && hits.Length > 0)
            {
                System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                foreach (var h in hits)
                {
                    if (ignoreRoot != null && (h.transform.root == ignoreRoot.root || h.transform.IsChildOf(ignoreRoot))) continue;
                    if (h.collider is TerrainCollider || h.collider.CompareTag("Terrain") ||
                        (h.collider.gameObject != null && h.collider.gameObject.name.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        groundY = h.point.y;
                        detected = true;
                        break;
                    }
                }
            }

            return groundY;
        }

        /// <summary>
        /// Precision Ground Snapping specifically optimized for Tractors & Vehicles:
        /// 1. Finds the lowest wheel renderer or lowest chassis renderer/collider point.
        /// 2. If tractor has multiple wheels (e.g. wheels1, wheels2), takes the lowest wheel point.
        /// 3. Raycasts down to terrain/ground to detect the exact surface elevation.
        /// 4. Adjusts transform.position.y so tires sit flush on the ground without floating or sinking.
        /// </summary>
        public static float SnapTractorToGroundExact(GameObject tractor)
        {
            if (tractor == null) return 0f;

            Vector3 pos = tractor.transform.position;
            float lowestY = float.MaxValue;
            bool foundBounds = false;

            // Check wheel renderers first to prioritize tire contact
            Renderer[] renderers = tractor.GetComponentsInChildren<Renderer>(true);
            float lowestWheelY = float.MaxValue;
            bool foundWheels = false;

            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer) continue;

                string rName = r.gameObject.name.ToLower();
                if (rName.Contains("wheel") || rName.Contains("tire") || rName.Contains("banh"))
                {
                    if (r.bounds.min.y < lowestWheelY)
                    {
                        lowestWheelY = r.bounds.min.y;
                        foundWheels = true;
                    }
                }

                if (r.bounds.min.y < lowestY)
                {
                    lowestY = r.bounds.min.y;
                    foundBounds = true;
                }
            }

            // Also check colliders
            Collider[] colliders = tractor.GetComponentsInChildren<Collider>(true);
            foreach (var c in colliders)
            {
                if (c == null || !c.enabled || !c.gameObject.activeInHierarchy || c.isTrigger) continue;
                if (c.bounds.min.y < lowestY)
                {
                    lowestY = c.bounds.min.y;
                    foundBounds = true;
                }
            }

            // If wheel renderers exist, prioritize lowest wheel point
            float effectiveLowestY = foundWheels ? lowestWheelY : lowestY;
            float bottomOffset = (foundWheels || foundBounds) ? (pos.y - effectiveLowestY) : 0f;

            if (float.IsNaN(bottomOffset) || float.IsInfinity(bottomOffset) || Mathf.Abs(bottomOffset) > 20f)
            {
                bottomOffset = 0f;
            }

            // Determine ground height via terrain and raycast
            float groundY = pos.y;
            bool groundDetected = false;

            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                groundY = terrain.SampleHeight(pos) + terrain.transform.position.y;
                groundDetected = true;
            }

            // Raycast downward from above tractor to detect exact surface
            float rayStartY = (groundDetected ? Mathf.Max(pos.y, groundY) : pos.y) + 20f;
            Ray ray = new Ray(new Vector3(pos.x, rayStartY, pos.z), Vector3.down);
            int mask = ~((1 << 2) | (1 << 5)); // Exclude IgnoreRaycast and UI
            RaycastHit[] hits = Physics.RaycastAll(ray, 45f, mask);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var h in hits)
            {
                if (h.transform.root == tractor.transform.root || h.transform.IsChildOf(tractor.transform)) continue;
                if (h.collider is TerrainCollider || h.collider.CompareTag("Terrain") || h.normal.y > 0.4f)
                {
                    groundY = h.point.y;
                    groundDetected = true;
                    break;
                }
            }

            float finalPosY = groundY + bottomOffset;
            tractor.transform.position = new Vector3(pos.x, finalPosY, pos.z);
            return finalPosY;
        }

        public static void SnapBuildingToGround(GameObject building)
        {
            SnapToGroundExact(building);
        }

        /// <summary>
        /// Legacy alias for SnapBuildingToGround.
        /// </summary>
        public static void SnapToGround(GameObject building)
        {
            SnapBuildingToGround(building);
        }

        /// <summary>
        /// Legacy overload with LayerMask.
        /// </summary>
        public static void SnapToGround(GameObject building, LayerMask groundMask)
        {
            SnapBuildingToGround(building);
        }

        /// <summary>
        /// Gets the vertical offset between transform.position.y and the lowest point of all renderers (bounds.min.y).
        /// Useful for positioning previews without repeating bounds scans every frame.
        /// </summary>
        public static float GetRendererBottomOffset(GameObject building)
        {
            if (building == null) return 0f;
            Bounds bounds;
            if (TryGetTotalRendererBounds(building, out bounds))
            {
                return building.transform.position.y - bounds.min.y;
            }
            return 0f;
        }

        /// <summary>
        /// Calculates the total encapsulated world Bounds of all active Renderers within the GameObject hierarchy.
        /// Excludes Billboard and floating UI texts.
        /// </summary>
        public static bool TryGetTotalRendererBounds(GameObject obj, out Bounds totalBounds)
        {
            totalBounds = new Bounds(Vector3.zero, Vector3.zero);
            if (obj == null) return false;

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(true);
            bool hasValidBounds = false;

            foreach (var r in renderers)
            {
                if (r == null || !r.enabled || !r.gameObject.activeInHierarchy) continue;
                if (r is ParticleSystemRenderer) continue;

                // Exclude floating billboards or floating UI texts
                if (r.gameObject.name.IndexOf("Billboard", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.gameObject.name.IndexOf("GateSign", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.gameObject.name.IndexOf("StatusText", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.GetComponent<TextMesh>() != null)
                {
                    continue;
                }

                if (!hasValidBounds)
                {
                    totalBounds = r.bounds;
                    hasValidBounds = true;
                }
                else
                {
                    totalBounds.Encapsulate(r.bounds);
                }
            }

            return hasValidBounds;
        }

        /// <summary>
        /// Queries the terrain/ground surface height at a specific world coordinate.
        /// </summary>
        public static float GetGroundHeightAt(Vector3 position, LayerMask groundMask)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain != null)
            {
                return terrain.SampleHeight(position) + terrain.transform.position.y;
            }

            // Raycast downward from high up (ignoring triggers)
            Vector3 rayStart = new Vector3(position.x, position.y + 100f, position.z);
            if (groundMask.value != 0 && Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 300f, groundMask, QueryTriggerInteraction.Ignore))
            {
                return hit.point.y;
            }

            RaycastHit[] hits = Physics.RaycastAll(rayStart, Vector3.down, 300f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider.isTrigger) continue;
                if (h.collider is TerrainCollider ||
                    (h.collider.gameObject != null && h.collider.gameObject.name.IndexOf("Terrain", System.StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    return h.point.y;
                }
            }

            return position.y;
        }

        public static float GetGroundY(Vector3 position)
        {
            return GetGroundHeightAt(position, 0);
        }

        public static float SampleGroundY(Vector3 position)
        {
            return GetGroundHeightAt(position, 0);
        }

        public static void SnapToGround(GameObject obj, bool alignRotation)
        {
            SnapToGroundExact(obj);
        }
    }
}
