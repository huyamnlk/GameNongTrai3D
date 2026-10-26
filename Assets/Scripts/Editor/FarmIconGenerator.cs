using System.IO;
using UnityEditor;
using UnityEngine;

namespace IrishFarmSim.Editor
{
    public static class FarmIconGenerator
    {
        [MenuItem("Tools/Farm/Generate All Missing Icons")]
        public static void GenerateAllIcons()
        {
            string folder = "Assets/Resources/Icons";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // Generate procedural textures for products and feeds
            GenerateAndSave(folder + "/milk.png", DrawMilkBottle());
            GenerateAndSave(folder + "/egg.png", DrawEggs());
            GenerateAndSave(folder + "/bacon.png", DrawBacon());
            GenerateAndSave(folder + "/leather.png", DrawLeather());
            GenerateAndSave(folder + "/grain.png", DrawGrainSack());
            GenerateAndSave(folder + "/pellet.png", DrawPelletBowl());
            GenerateAndSave(folder + "/flour.png", DrawFlourSack());
            GenerateAndSave(folder + "/bread.png", DrawBreadLoaf());
            GenerateAndSave(folder + "/cheese.png", DrawCheeseWedge());
            GenerateAndSave(folder + "/cake.png", DrawCakeSlice());
            GenerateAndSave(folder + "/strawberry_jam.png", DrawJamJar());
            GenerateAndSave(folder + "/tomato_juice.png", DrawJuiceBottle(new Color(0.88f, 0.20f, 0.16f), new Color(0.25f, 0.70f, 0.25f)));
            GenerateAndSave(folder + "/watermelon_juice.png", DrawJuiceBottle(new Color(0.92f, 0.25f, 0.40f), new Color(0.35f, 0.75f, 0.35f)));

            // Placeholders
            GenerateAndSave(folder + "/default_seed.png", DrawSeedPacket());
            GenerateAndSave(folder + "/default_crop.png", DrawCropSymbol());
            GenerateAndSave(folder + "/default_feed.png", DrawFeedSymbol());
            GenerateAndSave(folder + "/default_product.png", DrawProductSymbol());
            GenerateAndSave(folder + "/default_material.png", DrawMaterialSymbol());
            GenerateAndSave(folder + "/default_deco.png", DrawDecoSymbol());
            GenerateAndSave(folder + "/default_misc.png", DrawMiscSymbol());

            AssetDatabase.Refresh();

            // Configure all textures in Icons folder as Sprites
            string[] files = Directory.GetFiles(folder, "*.png");
            foreach (var f in files)
            {
                string unityPath = f.Replace('\\', '/');
                TextureImporter ti = AssetImporter.GetAtPath(unityPath) as TextureImporter;
                if (ti != null)
                {
                    bool modified = false;
                    if (ti.textureType != TextureImporterType.Sprite)
                    {
                        ti.textureType = TextureImporterType.Sprite;
                        modified = true;
                    }
                    if (ti.spriteImportMode != SpriteImportMode.Single)
                    {
                        ti.spriteImportMode = SpriteImportMode.Single;
                        modified = true;
                    }
                    if (!ti.alphaIsTransparency)
                    {
                        ti.alphaIsTransparency = true;
                        modified = true;
                    }
                    if (ti.filterMode != FilterMode.Bilinear)
                    {
                        ti.filterMode = FilterMode.Bilinear;
                        modified = true;
                    }
                    if (modified)
                    {
                        ti.SaveAndReimport();
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[FarmIconGenerator] Successfully generated and configured all icon sprites in " + folder);
        }

        private static void GenerateAndSave(string fullPath, Texture2D tex)
        {
            if (File.Exists(fullPath))
            {
                // If it already exists and is from crops, don't overwrite if existing size is larger
                FileInfo fi = new FileInfo(fullPath);
                if (fi.Length > 20000) return;
            }
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(fullPath, bytes);
            Object.DestroyImmediate(tex);
        }

        private static Texture2D CreateBaseTex(int size = 128)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color clear = new Color(0, 0, 0, 0);
            Color[] cols = new Color[size * size];
            for (int i = 0; i < cols.Length; i++) cols[i] = clear;
            tex.SetPixels(cols);
            return tex;
        }

        private static void DrawFilledCircle(Texture2D tex, int cx, int cy, int r, Color col)
        {
            int r2 = r * r;
            for (int y = -r; y <= r; y++)
            {
                int py = cy + y;
                if (py < 0 || py >= tex.height) continue;
                for (int x = -r; x <= r; x++)
                {
                    int px = cx + x;
                    if (px < 0 || px >= tex.width) continue;
                    if (x * x + y * y <= r2)
                    {
                        Color existing = tex.GetPixel(px, py);
                        tex.SetPixel(px, py, Color.Lerp(existing, col, col.a));
                    }
                }
            }
        }

        private static void DrawFilledRect(Texture2D tex, int x1, int y1, int w, int h, Color col)
        {
            for (int y = y1; y < y1 + h; y++)
            {
                if (y < 0 || y >= tex.height) continue;
                for (int x = x1; x < x1 + w; x++)
                {
                    if (x < 0 || x >= tex.width) continue;
                    Color existing = tex.GetPixel(x, y);
                    tex.SetPixel(x, y, Color.Lerp(existing, col, col.a));
                }
            }
        }

        private static void DrawPill(Texture2D tex, int x1, int y1, int w, int h, Color col)
        {
            DrawFilledRect(tex, x1, y1, w, h, col);
            int r = Mathf.Min(w, h) / 2;
            DrawFilledCircle(tex, x1 + r, y1 + h / 2, r, col);
            DrawFilledCircle(tex, x1 + w - r, y1 + h / 2, r, col);
        }

        // Milk Bottle
        private static Texture2D DrawMilkBottle()
        {
            Texture2D tex = CreateBaseTex();
            // Bottle Glass / Milk body
            DrawFilledCircle(tex, 64, 52, 42, new Color(0.94f, 0.96f, 0.98f));
            DrawFilledRect(tex, 34, 40, 60, 48, new Color(0.96f, 0.98f, 1.0f));
            // Bottle Neck
            DrawFilledRect(tex, 48, 86, 32, 22, new Color(0.85f, 0.92f, 0.98f));
            // Cork / Cap (Red)
            DrawFilledRect(tex, 44, 106, 40, 12, new Color(0.88f, 0.22f, 0.22f));
            // Blue cow milk emblem
            DrawFilledCircle(tex, 64, 62, 16, new Color(0.25f, 0.55f, 0.88f));
            DrawFilledCircle(tex, 64, 62, 12, Color.white);
            tex.Apply();
            return tex;
        }

        // Eggs
        private static Texture2D DrawEggs()
        {
            Texture2D tex = CreateBaseTex();
            // Nest / basket
            DrawFilledCircle(tex, 64, 40, 48, new Color(0.62f, 0.42f, 0.22f));
            DrawFilledCircle(tex, 64, 46, 42, new Color(0.48f, 0.32f, 0.16f));
            // 3 Eggs
            DrawFilledCircle(tex, 46, 68, 22, new Color(0.98f, 0.92f, 0.82f));
            DrawFilledCircle(tex, 82, 68, 22, new Color(0.96f, 0.88f, 0.78f));
            DrawFilledCircle(tex, 64, 76, 24, new Color(1.0f, 0.96f, 0.90f));
            tex.Apply();
            return tex;
        }

        // Bacon
        private static Texture2D DrawBacon()
        {
            Texture2D tex = CreateBaseTex();
            for (int i = 0; i < 3; i++)
            {
                int y = 28 + i * 28;
                DrawPill(tex, 16, y, 96, 20, new Color(0.78f, 0.18f, 0.22f));
                DrawPill(tex, 24, y + 4, 80, 8, new Color(0.98f, 0.88f, 0.82f));
                DrawPill(tex, 32, y + 6, 64, 4, new Color(0.86f, 0.28f, 0.26f));
            }
            tex.Apply();
            return tex;
        }

        // Leather
        private static Texture2D DrawLeather()
        {
            Texture2D tex = CreateBaseTex();
            // Rolled leather scroll
            DrawFilledCircle(tex, 36, 64, 28, new Color(0.55f, 0.35f, 0.18f));
            DrawFilledCircle(tex, 36, 64, 18, new Color(0.42f, 0.26f, 0.12f));
            DrawFilledRect(tex, 36, 40, 68, 48, new Color(0.62f, 0.40f, 0.22f));
            DrawFilledCircle(tex, 104, 64, 24, new Color(0.58f, 0.38f, 0.20f));
            // Gold strap
            DrawFilledRect(tex, 62, 38, 12, 52, new Color(0.90f, 0.75f, 0.20f));
            tex.Apply();
            return tex;
        }

        // Grain Sack
        private static Texture2D DrawGrainSack()
        {
            Texture2D tex = CreateBaseTex();
            // Burlap bag
            DrawFilledCircle(tex, 64, 50, 44, new Color(0.82f, 0.68f, 0.46f));
            DrawFilledRect(tex, 32, 40, 64, 42, new Color(0.88f, 0.74f, 0.52f));
            DrawFilledRect(tex, 44, 82, 40, 16, new Color(0.72f, 0.55f, 0.35f));
            // Tie string
            DrawFilledRect(tex, 42, 80, 44, 6, new Color(0.92f, 0.35f, 0.25f));
            // Golden Wheat grains symbol
            DrawFilledCircle(tex, 64, 56, 16, new Color(0.98f, 0.84f, 0.25f));
            tex.Apply();
            return tex;
        }

        // Pellet Bowl
        private static Texture2D DrawPelletBowl()
        {
            Texture2D tex = CreateBaseTex();
            // Feeding bowl (Green)
            DrawFilledCircle(tex, 64, 46, 48, new Color(0.22f, 0.65f, 0.40f));
            DrawFilledCircle(tex, 64, 52, 44, new Color(0.18f, 0.52f, 0.32f));
            // Feed Pellets
            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 5; x++)
                {
                    DrawFilledCircle(tex, 36 + x * 14, 54 + y * 12, 6, new Color(0.72f, 0.58f, 0.32f));
                }
            }
            tex.Apply();
            return tex;
        }

        // Flour Sack
        private static Texture2D DrawFlourSack()
        {
            Texture2D tex = CreateBaseTex();
            // White linen bag
            DrawFilledCircle(tex, 64, 50, 44, new Color(0.92f, 0.94f, 0.95f));
            DrawFilledRect(tex, 32, 40, 64, 44, new Color(0.96f, 0.97f, 0.98f));
            DrawFilledRect(tex, 44, 82, 40, 16, new Color(0.82f, 0.85f, 0.88f));
            // Blue string
            DrawFilledRect(tex, 42, 80, 44, 6, new Color(0.20f, 0.55f, 0.88f));
            // Stamp
            DrawFilledCircle(tex, 64, 56, 15, new Color(0.40f, 0.65f, 0.90f));
            tex.Apply();
            return tex;
        }

        // Bread Loaf
        private static Texture2D DrawBreadLoaf()
        {
            Texture2D tex = CreateBaseTex();
            // Loaf
            DrawFilledCircle(tex, 40, 64, 30, new Color(0.82f, 0.55f, 0.22f));
            DrawFilledCircle(tex, 88, 64, 30, new Color(0.82f, 0.55f, 0.22f));
            DrawFilledRect(tex, 40, 36, 48, 56, new Color(0.92f, 0.66f, 0.30f));
            // Score cuts
            for (int i = 0; i < 3; i++)
            {
                DrawFilledRect(tex, 44 + i * 16, 52, 8, 24, new Color(0.98f, 0.88f, 0.65f));
            }
            tex.Apply();
            return tex;
        }

        // Cheese Wedge
        private static Texture2D DrawCheeseWedge()
        {
            Texture2D tex = CreateBaseTex();
            // Round cheese wheel
            DrawFilledCircle(tex, 64, 64, 46, new Color(0.98f, 0.78f, 0.18f));
            DrawFilledCircle(tex, 64, 64, 40, new Color(1.0f, 0.88f, 0.32f));
            // Holes
            DrawFilledCircle(tex, 48, 72, 10, new Color(0.88f, 0.68f, 0.16f));
            DrawFilledCircle(tex, 76, 54, 8, new Color(0.88f, 0.68f, 0.16f));
            DrawFilledCircle(tex, 54, 46, 6, new Color(0.88f, 0.68f, 0.16f));
            DrawFilledCircle(tex, 82, 78, 7, new Color(0.88f, 0.68f, 0.16f));
            tex.Apply();
            return tex;
        }

        // Cake Slice
        private static Texture2D DrawCakeSlice()
        {
            Texture2D tex = CreateBaseTex();
            // Sponge base
            DrawFilledRect(tex, 28, 34, 72, 48, new Color(0.96f, 0.85f, 0.60f));
            // Chocolate cream layer
            DrawFilledRect(tex, 28, 54, 72, 8, new Color(0.48f, 0.28f, 0.16f));
            // Pink strawberry icing
            DrawFilledRect(tex, 24, 76, 80, 16, new Color(0.98f, 0.45f, 0.60f));
            // Cherry on top
            DrawFilledCircle(tex, 64, 98, 12, new Color(0.88f, 0.12f, 0.22f));
            DrawFilledRect(tex, 66, 108, 3, 12, new Color(0.25f, 0.60f, 0.20f));
            tex.Apply();
            return tex;
        }

        // Jam Jar
        private static Texture2D DrawJamJar()
        {
            Texture2D tex = CreateBaseTex();
            // Jar body
            DrawFilledCircle(tex, 64, 52, 38, new Color(0.85f, 0.15f, 0.25f));
            DrawFilledRect(tex, 32, 40, 64, 46, new Color(0.90f, 0.18f, 0.28f));
            // Cloth Lid
            DrawFilledRect(tex, 36, 86, 56, 18, new Color(0.95f, 0.95f, 0.95f));
            DrawFilledRect(tex, 32, 98, 64, 8, new Color(0.92f, 0.35f, 0.35f));
            // Label
            DrawFilledRect(tex, 44, 48, 40, 24, new Color(0.98f, 0.95f, 0.88f));
            // Strawberry dot
            DrawFilledCircle(tex, 64, 60, 6, new Color(0.85f, 0.15f, 0.25f));
            tex.Apply();
            return tex;
        }

        // Juice Bottle
        private static Texture2D DrawJuiceBottle(Color juiceColor, Color capColor)
        {
            Texture2D tex = CreateBaseTex();
            // Bottle Glass
            DrawFilledCircle(tex, 64, 50, 36, juiceColor);
            DrawFilledRect(tex, 36, 38, 56, 50, juiceColor);
            // Bottle neck
            DrawFilledRect(tex, 50, 84, 28, 20, new Color(0.88f, 0.94f, 0.98f));
            // Cap
            DrawFilledRect(tex, 46, 102, 36, 12, capColor);
            // Citrus slice badge
            DrawFilledCircle(tex, 64, 60, 14, Color.white);
            DrawFilledCircle(tex, 64, 60, 11, juiceColor * 1.2f);
            tex.Apply();
            return tex;
        }

        // Placeholders
        private static Texture2D DrawSeedPacket()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledRect(tex, 28, 28, 72, 72, new Color(0.35f, 0.65f, 0.40f));
            DrawFilledRect(tex, 36, 36, 56, 56, new Color(0.95f, 0.92f, 0.85f));
            DrawFilledCircle(tex, 64, 64, 16, new Color(0.88f, 0.68f, 0.25f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawCropSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledCircle(tex, 64, 64, 44, new Color(0.28f, 0.72f, 0.38f));
            DrawFilledCircle(tex, 64, 64, 34, new Color(0.98f, 0.90f, 0.35f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawFeedSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledCircle(tex, 64, 64, 44, new Color(0.85f, 0.60f, 0.25f));
            DrawFilledCircle(tex, 64, 64, 30, new Color(0.98f, 0.85f, 0.45f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawProductSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledCircle(tex, 64, 64, 44, new Color(0.45f, 0.35f, 0.65f));
            DrawFilledCircle(tex, 64, 64, 30, new Color(0.85f, 0.75f, 0.95f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawMaterialSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledRect(tex, 32, 32, 64, 64, new Color(0.58f, 0.40f, 0.28f));
            DrawFilledCircle(tex, 64, 64, 20, new Color(0.85f, 0.75f, 0.65f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawDecoSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledCircle(tex, 64, 64, 44, new Color(0.25f, 0.55f, 0.85f));
            DrawFilledCircle(tex, 64, 64, 28, new Color(1.0f, 0.85f, 0.35f));
            tex.Apply();
            return tex;
        }

        private static Texture2D DrawMiscSymbol()
        {
            Texture2D tex = CreateBaseTex();
            DrawFilledCircle(tex, 64, 64, 44, new Color(0.40f, 0.48f, 0.56f));
            DrawFilledCircle(tex, 64, 64, 26, Color.white);
            tex.Apply();
            return tex;
        }
    }
}
