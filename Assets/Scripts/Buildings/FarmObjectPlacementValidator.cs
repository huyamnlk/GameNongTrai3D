using System;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Placement validator for farm buildings, animal pens, crop plots, and production machines.
    /// Checks terrain bounds, slope, obstacle avoidance, and non-overlapping with other farm objects.
    /// </summary>
    public static class FarmObjectPlacementValidator
    {
        // Typical playable farm bounds
        private static readonly Rect FarmGroundBounds = new Rect(20f, 160f, 240f, 200f);

        /// <summary>
        /// Validates whether an object can be placed at the target position and rotation.
        /// </summary>
        public static bool ValidatePlacement(GameObject movingRoot, Vector3 targetPos, Quaternion targetRot, Vector3 footprintSize, out string failureReason)
        {
            failureReason = "";

            // 1. Terrain presence check
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
            {
                failureReason = "Không tìm thấy mặt đất nông trại (Terrain)!";
                return false;
            }

            // 2. Farm boundary check
            Vector3 tPos = terrain.transform.position;
            Vector3 tSize = terrain.terrainData.size;
            float minX = tPos.x + 15f;
            float maxX = tPos.x + tSize.x - 15f;
            float minZ = tPos.z + 15f;
            float maxZ = tPos.z + tSize.z - 15f;

            float hx = footprintSize.x * 0.5f;
            float hz = footprintSize.z * 0.5f;

            if (targetPos.x - hx < minX || targetPos.x + hx > maxX ||
                targetPos.z - hz < minZ || targetPos.z + hz > maxZ)
            {
                failureReason = "Vị trí nằm ngoài khuôn viên nông trại!";
                return false;
            }

            // 3. Terrain Slope check
            float normX = (targetPos.x - tPos.x) / tSize.x;
            float normZ = (targetPos.z - tPos.z) / tSize.z;
            if (normX >= 0f && normX <= 1f && normZ >= 0f && normZ <= 1f)
            {
                float steepness = terrain.terrainData.GetSteepness(normX, normZ);
                if (steepness > 35f)
                {
                    failureReason = "Mặt đất quá dốc, không thể đặt tại đây!";
                    return false;
                }
            }

            // 4. Overlap Check with other Farm Buildings, Pens, Plots, and Machines
            Vector3 center = targetPos + new Vector3(0f, footprintSize.y * 0.5f + 0.1f, 0f);
            Vector3 halfExtents = new Vector3(Mathf.Max(0.5f, hx - 0.2f), footprintSize.y * 0.5f, Mathf.Max(0.5f, hz - 0.2f));

            int layerMask = ~((1 << 2) | (1 << LayerMask.NameToLayer("Ignore Raycast")));
            Collider[] colliders = Physics.OverlapBox(center, halfExtents, targetRot, layerMask, QueryTriggerInteraction.Ignore);

            foreach (var col in colliders)
            {
                if (col == null) continue;
                if (movingRoot != null && (col.gameObject == movingRoot || col.transform.IsChildOf(movingRoot.transform)))
                {
                    continue; // Skip self and children
                }

                // Skip terrain
                if (col is TerrainCollider || col.CompareTag("Terrain") || col.name.IndexOf("Terrain", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                // Strictly check Player overlap (Requirement B.7)
                if (col.CompareTag("Player") || col.name.IndexOf("Player", StringComparison.OrdinalIgnoreCase) >= 0 || col.GetComponent<Movement>() != null)
                {
                    failureReason = "Không thể đặt đè lên nhân vật!";
                    return false;
                }

                // Check other Movable Farm Objects (Fences, Decor, Buildings, Pens, Machines)
                MovableFarmObject otherMovable = col.GetComponentInParent<MovableFarmObject>();
                if (otherMovable != null && (movingRoot == null || otherMovable.gameObject != movingRoot))
                {
                    if (otherMovable.isDeleted || !otherMovable.gameObject.activeInHierarchy)
                    {
                        continue;
                    }
                    failureReason = $"Vị trí bị chồng lên '{otherMovable.displayName}'!";
                    return false;
                }

                // Check other CropPlots
                CropPlot otherPlot = col.GetComponentInParent<CropPlot>();
                if (otherPlot != null && (movingRoot == null || otherPlot.gameObject != movingRoot))
                {
                    failureReason = $"Vị trí bị chồng lên ô đất trồng '{otherPlot.gameObject.name}'!";
                    return false;
                }

                // Check other FarmBuildings
                FarmBuilding otherBldg = col.GetComponentInParent<FarmBuilding>();
                if (otherBldg != null && (movingRoot == null || otherBldg.gameObject != movingRoot))
                {
                    failureReason = $"Vị trí bị chồng lên công trình '{otherBldg.displayName}'!";
                    return false;
                }

                // Check other ProductionMachines
                ProductionMachine otherMachine = col.GetComponentInParent<ProductionMachine>();
                if (otherMachine != null && (movingRoot == null || otherMachine.gameObject != movingRoot))
                {
                    failureReason = $"Vị trí bị chồng lên máy móc '{otherMachine.displayName}'!";
                    return false;
                }

                // Check Environment Obstacles / Walls / Water
                if (col.name.IndexOf("Wall", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.name.IndexOf("Water", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.name.IndexOf("Border", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    col.name.IndexOf("Fence_Static", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    failureReason = "Vị trí bị vướng tường biên hoặc nguồn nước!";
                    return false;
                }
            }

            // Check distance to Player
            GameObject playerGo = GameObject.FindGameObjectWithTag("Player") ?? GameObject.Find("Player");
            if (playerGo != null)
            {
                float playerDist = Vector2.Distance(new Vector2(targetPos.x, targetPos.z), new Vector2(playerGo.transform.position.x, playerGo.transform.position.z));
                float requiredPlayerDist = (Mathf.Max(footprintSize.x, footprintSize.z) * 0.5f) + 0.6f;
                if (playerDist < requiredPlayerDist)
                {
                    failureReason = "Không thể đặt đè lên vị trí của nhân vật!";
                    return false;
                }
            }

            // 5. Distance check with all existing CropPlots (prevent overlapping even without colliders)
            CropPlot[] allPlots = UnityEngine.Object.FindObjectsByType<CropPlot>(FindObjectsSortMode.None);
            foreach (var p in allPlots)
            {
                if (p == null || (movingRoot != null && p.gameObject == movingRoot)) continue;
                float dist = Vector2.Distance(new Vector2(targetPos.x, targetPos.z), new Vector2(p.transform.position.x, p.transform.position.z));
                float requiredDist = (footprintSize.x * 0.5f) + 1.1f;
                if (dist < requiredDist)
                {
                    failureReason = $"Vị trí quá gần ô đất '{p.gameObject.name}'!";
                    return false;
                }
            }

            return true;
        }
    }
}
