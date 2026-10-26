using System;
using System.Collections.Generic;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Centralized provider for farm item icons and sprites in UI (Storage, Shop, Quests).
    /// Resolves item IDs to sprite resources, handles name aliases, and guarantees
    /// every slot has a clean, properly-proportioned icon without blanks.
    /// </summary>
    public static class FarmIconService
    {
        private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ItemType, Sprite> FallbackCache = new Dictionary<ItemType, Sprite>();

        /// <summary>
        /// Retrieves the corresponding sprite for an item by its ID and ItemType.
        /// Guaranteed never to return null.
        /// </summary>
        public static Sprite GetItemIcon(string rawId, ItemType type = ItemType.Misc)
        {
            if (string.IsNullOrEmpty(rawId))
            {
                return GetCategoryFallback(type);
            }

            string cleanKey = NormalizeIconKey(rawId);

            if (Cache.TryGetValue(cleanKey, out Sprite cached) && cached != null)
            {
                return cached;
            }

            // 1. Try loading directly from Resources/Icons/
            Sprite sp = Resources.Load<Sprite>("Icons/" + cleanKey);

            // 2. Try normalized base name
            if (sp == null)
            {
                string altKey = GetAlternativeKey(cleanKey);
                if (!string.IsNullOrEmpty(altKey))
                {
                    sp = Resources.Load<Sprite>("Icons/" + altKey);
                }
            }

            // 3. Fallback to category placeholder
            if (sp == null)
            {
                sp = GetCategoryFallback(type);
            }

            if (sp != null)
            {
                Cache[cleanKey] = sp;
            }

            return sp;
        }

        public static string NormalizeIconKey(string rawId)
        {
            if (string.IsNullOrEmpty(rawId)) return "default_misc";
            string id = rawId.Trim().ToLowerInvariant();

            // Strip shop_ or item_ prefixes
            if (id.StartsWith("shop_seed_")) id = id.Substring(10) + "_seed";
            else if (id.StartsWith("shop_feed_")) id = id.Substring(10);
            else if (id.StartsWith("shop_crop_")) id = id.Substring(10);
            else if (id.StartsWith("shop_machine_")) id = id.Substring(13);
            else if (id.StartsWith("shop_building_")) id = id.Substring(14);
            else if (id.StartsWith("shop_plot_")) id = id.Substring(10);
            else if (id.StartsWith("shop_")) id = id.Substring(5);

            // Recipe ID aliases mapping directly to their output products
            if (id == "wheat_to_flour") return "flour";
            if (id == "bake_bread") return "bread";
            if (id == "bake_cake") return "cake";
            if (id == "milk_to_cheese") return "cheese";
            if (id == "mix_pellets" || id == "mix_pellets_corn") return "pellet";

            // Normalize seeds
            if (id.EndsWith("seed"))
            {
                string crop = id.Substring(0, id.Length - 4).TrimEnd('_');
                return crop + "_seed";
            }

            // Normalize feeds
            if (id == "feed_hay") return "hay";
            if (id == "feed_grain") return "grain";
            if (id == "pellets" || id == "feed_pellet" || id == "feed_pellets") return "pellet";

            // Normalize animal products
            if (id == "eggs") return "egg";

            // Normalize juices & jam
            if (id == "strawberryjam") return "strawberry_jam";
            if (id == "tomatojuice") return "tomato_juice";
            if (id == "watermelonjuice") return "watermelon_juice";

            return id;
        }

        private static string GetAlternativeKey(string key)
        {
            switch (key)
            {
                case "wheat_seed": return "wheat";
                case "corn_seed": return "corn";
                case "carrot_seed": return "carrot";
                case "potato_seed": return "potato";
                case "tomato_seed": return "tomato";
                case "pumpkin_seed": return "pumpkin";
                case "strawberry_seed": return "strawberry";
                case "cabbage_seed": return "cabbage";
                case "watermelon_seed": return "watermelon";
                case "hay_seed": return "hay";
                default: return "";
            }
        }

        public static Sprite GetCategoryFallback(ItemType type)
        {
            if (FallbackCache.TryGetValue(type, out Sprite cached) && cached != null)
            {
                return cached;
            }

            string iconName = "default_misc";
            switch (type)
            {
                case ItemType.Seed: iconName = "default_seed"; break;
                case ItemType.Crop: iconName = "default_crop"; break;
                case ItemType.Feed: iconName = "default_feed"; break;
                case ItemType.AnimalProduct: iconName = "default_product"; break;
                case ItemType.Material: iconName = "default_material"; break;
                case ItemType.Decoration: iconName = "default_deco"; break;
                default: iconName = "default_misc"; break;
            }

            Sprite sp = Resources.Load<Sprite>("Icons/" + iconName);
            if (sp == null)
            {
                sp = CreateProceduralFallbackSprite(type);
            }

            if (sp != null)
            {
                FallbackCache[type] = sp;
            }

            return sp;
        }

        private static Sprite CreateProceduralFallbackSprite(ItemType type)
        {
            Color col = Color.gray;
            switch (type)
            {
                case ItemType.Seed: col = new Color(0.35f, 0.72f, 0.35f); break;
                case ItemType.Crop: col = new Color(0.92f, 0.65f, 0.20f); break;
                case ItemType.Feed: col = new Color(0.85f, 0.52f, 0.25f); break;
                case ItemType.AnimalProduct: col = new Color(0.85f, 0.40f, 0.60f); break;
                case ItemType.Material: col = new Color(0.60f, 0.45f, 0.30f); break;
                case ItemType.Decoration: col = new Color(0.35f, 0.55f, 0.85f); break;
                default: col = new Color(0.45f, 0.50f, 0.60f); break;
            }

            int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool isBorder = (x < 2 || x >= size - 2 || y < 2 || y >= size - 2);
                    pixels[y * size + x] = isBorder ? Color.white : col;
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
