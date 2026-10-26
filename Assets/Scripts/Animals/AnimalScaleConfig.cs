using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Centralized configuration for all farm animal scales.
    /// Ensures consistent, balanced species-wide sizing across prefabs, scenes, spawning, and save/load:
    /// - Cow: 1.75f (-12.5% reduction from 2.00f)
    /// - Horse: 1.30f (+30% increase from 1.00f)
    /// - Pig: 1.30f (+30% increase from 1.00f)
    /// - Chicken: 1.35f (+35% increase from 1.00f)
    /// </summary>
    public static class AnimalScaleConfig
    {
        public const float COW_SCALE = 2.10f;
        public const float HORSE_SCALE = 1.70f;
        public const float PIG_SCALE = 1.60f;
        public const float CHICKEN_SCALE = 1.65f;

        public static float GetScaleForType(AnimalType type)
        {
            switch (type)
            {
                case AnimalType.Cow: return COW_SCALE;
                case AnimalType.Horse: return HORSE_SCALE;
                case AnimalType.Pig: return PIG_SCALE;
                case AnimalType.Chicken: return CHICKEN_SCALE;
                default: return 1.0f;
            }
        }

        public static float GetScaleForId(string animalId)
        {
            if (string.IsNullOrEmpty(animalId)) return 1.0f;
            string id = animalId.ToLowerInvariant();
            if (id.Contains("cow") || id.Contains("angus") || id.Contains("hereford") || id.Contains("charolais") || id.Contains("brangus") || id.Contains("friesian") || id.Contains("shorthorn") || id.Contains("bò"))
                return COW_SCALE;
            if (id.Contains("horse") || id.Contains("ngựa"))
                return HORSE_SCALE;
            if (id.Contains("pig") || id.Contains("heo") || id.Contains("lợn"))
                return PIG_SCALE;
            if (id.Contains("chicken") || id.Contains("hen") || id.Contains("gà"))
                return CHICKEN_SCALE;
            return 1.0f;
        }

        public static void ApplyScale(GameObject animalGo, AnimalType type)
        {
            if (animalGo == null) return;
            float scale = GetScaleForType(type);
            animalGo.transform.localScale = new Vector3(scale, scale, scale);
        }

        public static void ApplyScale(GameObject animalGo, string animalId)
        {
            if (animalGo == null) return;
            float scale = GetScaleForId(animalId);
            animalGo.transform.localScale = new Vector3(scale, scale, scale);
        }
    }
}
