using System;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Centralized configuration for all farm crop scales and visual density.
    /// Ensures consistent, balanced species-wide sizing across stages, prefabs, plots, and save/load:
    /// - Watermelon: Harvest ~2.05x (+41.4% increase from 1.45x, large fruit, clear shape, grounded on soil)
    /// - Pumpkin: Harvest ~2.05x (+41.4% increase from 1.45x, round, ribbed, grounded on soil, spacious)
    /// - Cabbage: Harvest ~1.88x (+39.3% increase from 1.35x, large, thick cabbage head, mature look)
    /// - Tomato: Harvest ~1.68x (+34.4% increase from 1.25x, larger fruit clusters, visible & non-overlapping)
    /// - Corn: ~1.55x (height emphasis, thick double-stalk clusters with broad leaves & golden cobs)
    /// - Wheat / Rice: ~1.35x (dense 5-stalk clusters with golden drooping ears, organic jitter)
    /// - Hay / Grass: ~1.30x (9 wide arching blades per clump with seed plumes & pasture clover)
    /// - Carrot / Potato / Strawberry: ~1.22x (grounded, balanced proportions)
    /// 
    /// Stage progression: Strongest boost applied to mature / harvest-ready stages (Stage 3 & 4),
    /// while early stages (0: Seed, 1: Sprout, 2: Young) scale with proportional moderation.
    /// </summary>
    public static class CropScaleConfig
    {
        // Harvest-ready peak scales (Stage 4)
        public const float WATERMELON_HARVEST_SCALE = 2.05f; // +41.4% from 1.45f
        public const float PUMPKIN_HARVEST_SCALE    = 2.05f; // +41.4% from 1.45f
        public const float CABBAGE_HARVEST_SCALE    = 1.88f; // +39.3% from 1.35f
        public const float TOMATO_HARVEST_SCALE     = 1.68f; // +34.4% from 1.25f

        // Corn has special height emphasis (Y: 1.55x, XZ: 1.30x)
        public const float CORN_SCALE_XZ    = 1.30f;
        public const float CORN_SCALE_Y     = 1.55f;

        public const float WHEAT_SCALE      = 1.35f;
        public const float HAY_SCALE        = 1.30f;

        public const float CARROT_SCALE     = 1.22f;
        public const float POTATO_SCALE     = 1.22f;
        public const float STRAWBERRY_SCALE = 1.22f;

        public const float DEFAULT_SCALE    = 1.20f;

        public static Vector3 GetScale(string cropId, int stageIndex = -1)
        {
            if (string.IsNullOrEmpty(cropId))
                return Vector3.one * DEFAULT_SCALE;

            string id = cropId.Trim().ToLowerInvariant();
            if (id.StartsWith("crop_")) id = id.Substring(5);
            if (id == "grass" || id == "feed") id = "hay";

            // Stage 0 (Seed) uses consistent visible base scale on tilled soil mounds
            if (stageIndex == 0)
            {
                return new Vector3(1.15f, 1.15f, 1.15f);
            }

            switch (id)
            {
                case "watermelon":
                {
                    float s;
                    switch (stageIndex)
                    {
                        case 1: s = 1.20f; break; // Sprout
                        case 2: s = 1.40f; break; // Young vine
                        case 3: s = 1.70f; break; // Mature with small melons
                        default: s = WATERMELON_HARVEST_SCALE; break; // 4: Harvest-ready (2.05x)
                    }
                    return new Vector3(s, s, s);
                }

                case "pumpkin":
                {
                    float s;
                    switch (stageIndex)
                    {
                        case 1: s = 1.20f; break; // Sprout
                        case 2: s = 1.40f; break; // Young vine
                        case 3: s = 1.70f; break; // Mature with developing pumpkins
                        default: s = PUMPKIN_HARVEST_SCALE; break; // 4: Harvest-ready (2.05x)
                    }
                    return new Vector3(s, s, s);
                }

                case "cabbage":
                {
                    float s;
                    switch (stageIndex)
                    {
                        case 1: s = 1.20f; break; // Sprout
                        case 2: s = 1.35f; break; // Young heart
                        case 3: s = 1.60f; break; // Mature head
                        default: s = CABBAGE_HARVEST_SCALE; break; // 4: Harvest-ready (1.88x)
                    }
                    return new Vector3(s, s, s);
                }

                case "tomato":
                {
                    float s;
                    switch (stageIndex)
                    {
                        case 1: s = 1.18f; break; // Sprout
                        case 2: s = 1.30f; break; // Young staked plant
                        case 3: s = 1.50f; break; // Mature with green fruit & blossoms
                        default: s = TOMATO_HARVEST_SCALE; break; // 4: Harvest-ready (1.68x)
                    }
                    return new Vector3(s, s, s);
                }

                case "corn":
                    return new Vector3(CORN_SCALE_XZ, CORN_SCALE_Y, CORN_SCALE_XZ);

                case "wheat":
                case "rice":
                    return new Vector3(WHEAT_SCALE, WHEAT_SCALE, WHEAT_SCALE);

                case "hay":
                    return new Vector3(HAY_SCALE, HAY_SCALE, HAY_SCALE);

                case "carrot":
                    return new Vector3(CARROT_SCALE, CARROT_SCALE, CARROT_SCALE);

                case "potato":
                    return new Vector3(POTATO_SCALE, POTATO_SCALE, POTATO_SCALE);

                case "strawberry":
                    return new Vector3(STRAWBERRY_SCALE, STRAWBERRY_SCALE, STRAWBERRY_SCALE);

                default:
                    return Vector3.one * DEFAULT_SCALE;
            }
        }

        public static float GetYOffset(string cropId, int stageIndex = -1)
        {
            // All crops are built with bottom contact calibrated at y = 0 inside container
            return 0f;
        }

        public static void ApplyScale(Transform stageTransform, string cropId, int stageIndex = -1, float progressSubScale = 1.0f)
        {
            if (stageTransform == null) return;
            Vector3 baseScale = GetScale(cropId, stageIndex);
            stageTransform.localPosition = new Vector3(0f, GetYOffset(cropId, stageIndex), 0f);
            stageTransform.localScale = new Vector3(
                baseScale.x * progressSubScale,
                baseScale.y * progressSubScale,
                baseScale.z * progressSubScale
            );
        }
    }
}
