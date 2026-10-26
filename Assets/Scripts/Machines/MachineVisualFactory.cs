using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Builds distinct low-poly procedural models for every production machine using the same
    /// shared-material / collider-free primitive helpers as the animal rigs (FarmVisualFactory).
    /// Models are used when no authored prefab is available (player builds / missing assets).
    /// Spinning parts are named "SpinX_*", "SpinY_*" or "SpinZ_*", glow parts "Glow_*",
    /// and the effect anchors "FxAnchor_Steam" / "FxAnchor_Indicator" so MachineWorkAnimator can animate them.
    /// </summary>
    public static class MachineVisualFactory
    {
        public const string ModelRootName = "ProceduralModel";
        public const string SteamAnchorName = "FxAnchor_Steam";
        public const string IndicatorAnchorName = "FxAnchor_Indicator";

        public static void Build(GameObject root, string machineId)
        {
            if (root == null) return;
            if (root.transform.Find(ModelRootName) != null) return;

            Transform model = FarmVisualFactory.CreatePivot(root.transform, ModelRootName, Vector3.zero);
            string id = string.IsNullOrEmpty(machineId) ? "" : machineId.ToLowerInvariant();

            switch (id)
            {
                case "bakery": BuildBakery(model); break;
                case "dairy": BuildDairy(model); break;
                case "feed_mixer": BuildFeedMixer(model); break;
                case "jam_maker": BuildJamMaker(model); break;
                default: BuildMill(model); break;
            }

            FarmVisualFactory.EnsureBlobShadow(model, 5.2f, 5.2f, 0.03f);
        }

        public static Color GetSteamColor(string machineId)
        {
            switch ((machineId ?? "").ToLowerInvariant())
            {
                case "mill": return new Color(0.96f, 0.93f, 0.85f);
                case "dairy": return new Color(0.95f, 0.97f, 1.00f);
                case "feed_mixer": return new Color(0.86f, 0.76f, 0.46f);
                case "jam_maker": return new Color(1.00f, 0.74f, 0.82f);
                default: return new Color(0.90f, 0.90f, 0.90f);
            }
        }

        public static Color GetProductColor(string itemId)
        {
            switch (InventorySystem.NormalizeId(itemId))
            {
                case "Flour": return new Color(0.97f, 0.94f, 0.84f);
                case "Bread": return new Color(0.86f, 0.62f, 0.30f);
                case "Cheese": return new Color(0.98f, 0.82f, 0.30f);
                case "Cake": return new Color(0.98f, 0.74f, 0.84f);
                case "Pellet": return new Color(0.55f, 0.38f, 0.22f);
                case "StrawberryJam": return new Color(0.84f, 0.16f, 0.30f);
                case "TomatoJuice": return new Color(0.90f, 0.25f, 0.15f);
                case "WatermelonJuice": return new Color(0.96f, 0.40f, 0.50f);
                default: return new Color(1.00f, 0.85f, 0.25f);
            }
        }

        // ---------------------------------------------------------------- Mill
        private static void BuildMill(Transform m)
        {
            Material stone = FarmVisualFactory.GetSharedMaterial("MachStone", new Color(0.62f, 0.60f, 0.57f));
            Material wall = FarmVisualFactory.GetSharedMaterial("MachMillWall", new Color(0.90f, 0.85f, 0.72f));
            Material roof = FarmVisualFactory.GetSharedMaterial("MachMillRoof", new Color(0.62f, 0.26f, 0.20f));
            Material wood = FarmVisualFactory.GetSharedMaterial("MachWood", new Color(0.48f, 0.32f, 0.18f));
            Material sail = FarmVisualFactory.GetSharedMaterial("MachSail", new Color(0.96f, 0.95f, 0.92f));

            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "MillBase", new Vector3(0f, 0.5f, 0f), new Vector3(4.2f, 0.5f, 4.2f), stone);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "MillBody", new Vector3(0f, 3.0f, 0f), new Vector3(3.2f, 2.0f, 3.2f), wall);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "MillRoof", new Vector3(0f, 5.0f, 0f), new Vector3(3.6f, 2.2f, 3.6f), roof);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "MillDoor", new Vector3(0f, 1.7f, 1.6f), new Vector3(1.0f, 1.5f, 0.2f), wood);

            Transform blades = FarmVisualFactory.CreatePivot(m, "SpinZ_Blades", new Vector3(0f, 4.4f, 1.9f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, blades, "Hub", Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), wood);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, blades, "BladeV", Vector3.zero, new Vector3(0.45f, 6.0f, 0.12f), sail);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, blades, "BladeH", Vector3.zero, new Vector3(6.0f, 0.45f, 0.12f), sail);

            FarmVisualFactory.CreatePivot(m, SteamAnchorName, new Vector3(1.5f, 2.2f, 1.5f));
            FarmVisualFactory.CreatePivot(m, IndicatorAnchorName, new Vector3(1.1f, 2.5f, 1.7f));
        }

        // -------------------------------------------------------------- Bakery
        private static void BuildBakery(Transform m)
        {
            Material brick = FarmVisualFactory.GetSharedMaterial("MachBrick", new Color(0.78f, 0.36f, 0.24f));
            Material brickLight = FarmVisualFactory.GetSharedMaterial("MachBrickLight", new Color(0.86f, 0.46f, 0.32f));
            Material dark = FarmVisualFactory.GetSharedMaterial("MachDark", new Color(0.10f, 0.07f, 0.06f));
            Material roof = FarmVisualFactory.GetSharedMaterial("MachBakeRoof", new Color(0.35f, 0.20f, 0.15f));
            Material chimney = FarmVisualFactory.GetSharedMaterial("MachChimney", new Color(0.50f, 0.48f, 0.46f));
            Material fire = FarmVisualFactory.GetSharedMaterial("MachFire", new Color(1.00f, 0.55f, 0.15f), 0.3f);
            Material awnA = FarmVisualFactory.GetSharedMaterial("MachAwnRed", new Color(0.86f, 0.22f, 0.22f));
            Material awnB = FarmVisualFactory.GetSharedMaterial("MachAwnWhite", new Color(0.97f, 0.95f, 0.92f));
            Material wood = FarmVisualFactory.GetSharedMaterial("MachWood", new Color(0.48f, 0.32f, 0.18f));
            Material loaf = FarmVisualFactory.GetSharedMaterial("MachLoaf", new Color(0.86f, 0.62f, 0.30f));

            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "OvenBody", new Vector3(0f, 1.5f, 0f), new Vector3(4.0f, 3.0f, 3.2f), brick);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "OvenRoofSlab", new Vector3(0f, 3.15f, 0f), new Vector3(4.4f, 0.3f, 3.6f), roof);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "OvenDome", new Vector3(0f, 3.3f, 0f), new Vector3(3.0f, 1.4f, 2.6f), brickLight);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "Chimney", new Vector3(1.4f, 4.2f, -0.8f), new Vector3(0.7f, 1.6f, 0.7f), chimney);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "OvenMouth", new Vector3(0f, 1.3f, 1.62f), new Vector3(1.8f, 1.3f, 0.12f), dark);
            GameObject glow = FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "Glow_Fire", new Vector3(0f, 1.1f, 1.70f), new Vector3(1.3f, 0.7f, 0.05f), fire, default, false);
            glow.SetActive(false);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "FrontCounter", new Vector3(0f, 0.55f, 2.2f), new Vector3(3.4f, 0.4f, 1.0f), wood);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Loaf1", new Vector3(-0.8f, 0.9f, 2.2f), new Vector3(0.7f, 0.4f, 0.4f), loaf);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Loaf2", new Vector3(0.1f, 0.9f, 2.2f), new Vector3(0.7f, 0.4f, 0.4f), loaf);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Loaf3", new Vector3(1.0f, 0.9f, 2.2f), new Vector3(0.7f, 0.4f, 0.4f), loaf);

            for (int i = 0; i < 5; i++)
            {
                FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "Awning" + i,
                    new Vector3(-1.68f + i * 0.84f, 3.0f, 2.15f), new Vector3(0.84f, 0.12f, 1.3f), (i % 2 == 0) ? awnA : awnB, new Vector3(12f, 0f, 0f));
            }

            FarmVisualFactory.CreatePivot(m, SteamAnchorName, new Vector3(1.4f, 5.2f, -0.8f));
            FarmVisualFactory.CreatePivot(m, IndicatorAnchorName, new Vector3(-1.6f, 2.5f, 1.7f));
        }

        // --------------------------------------------------------------- Dairy
        private static void BuildDairy(Transform m)
        {
            Material steel = FarmVisualFactory.GetSharedMaterial("MachSteel", new Color(0.78f, 0.82f, 0.86f), 0.55f, 0.35f);
            Material tank = FarmVisualFactory.GetSharedMaterial("MachTank", new Color(0.62f, 0.82f, 0.95f), 0.45f, 0.2f);
            Material baseMat = FarmVisualFactory.GetSharedMaterial("MachDairyBase", new Color(0.50f, 0.52f, 0.55f));
            Material red = FarmVisualFactory.GetSharedMaterial("MachMotorRed", new Color(0.82f, 0.22f, 0.20f));
            Material cheese = FarmVisualFactory.GetSharedMaterial("MachCheese", new Color(0.98f, 0.82f, 0.30f));
            Material rind = FarmVisualFactory.GetSharedMaterial("MachCheeseRind", new Color(0.88f, 0.66f, 0.20f));

            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "DairyBase", new Vector3(0f, 0.25f, 0f), new Vector3(3.2f, 0.25f, 3.2f), baseMat);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "MilkTank", new Vector3(0f, 1.8f, 0f), new Vector3(2.6f, 1.55f, 2.6f), tank);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "TankBand", new Vector3(0f, 3.1f, 0f), new Vector3(2.72f, 0.1f, 2.72f), steel);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "TankLid", new Vector3(0f, 3.4f, 0f), new Vector3(2.2f, 0.15f, 2.2f), steel);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "Motor", new Vector3(0f, 3.95f, 0f), new Vector3(0.9f, 0.7f, 0.9f), red);

            Transform paddle = FarmVisualFactory.CreatePivot(m, "SpinY_Paddle", new Vector3(0f, 4.45f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, paddle, "Shaft", Vector3.zero, new Vector3(0.18f, 0.25f, 0.18f), steel);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, paddle, "Arm", new Vector3(0f, 0.12f, 0f), new Vector3(2.0f, 0.1f, 0.22f), steel);

            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "Pipe", new Vector3(1.6f, 1.0f, 0f), new Vector3(0.28f, 0.45f, 0.28f), steel, new Vector3(0f, 0f, 90f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "CheeseWheelA", new Vector3(2.35f, 0.3f, 0.9f), new Vector3(1.1f, 0.25f, 1.1f), cheese);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "CheeseWheelB", new Vector3(2.35f, 0.82f, 0.9f), new Vector3(1.0f, 0.25f, 1.0f), rind);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "MilkChurn", new Vector3(-2.2f, 0.9f, 0.7f), new Vector3(0.8f, 0.9f, 0.8f), steel);

            FarmVisualFactory.CreatePivot(m, SteamAnchorName, new Vector3(0f, 4.9f, 0f));
            FarmVisualFactory.CreatePivot(m, IndicatorAnchorName, new Vector3(0f, 2.6f, 1.45f));
        }

        // ---------------------------------------------------------- Feed mixer
        private static void BuildFeedMixer(Transform m)
        {
            Material wood = FarmVisualFactory.GetSharedMaterial("MachWood", new Color(0.48f, 0.32f, 0.18f));
            Material green = FarmVisualFactory.GetSharedMaterial("MachHopper", new Color(0.38f, 0.68f, 0.35f));
            Material greenDark = FarmVisualFactory.GetSharedMaterial("MachHopperDark", new Color(0.28f, 0.52f, 0.28f));
            Material steel = FarmVisualFactory.GetSharedMaterial("MachSteel", new Color(0.78f, 0.82f, 0.86f), 0.55f, 0.35f);
            Material pellet = FarmVisualFactory.GetSharedMaterial("MachPellet", new Color(0.55f, 0.38f, 0.22f));

            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "MixerPlatform", new Vector3(0f, 0.3f, 0f), new Vector3(4.0f, 0.6f, 3.2f), wood);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "MixerDrum", new Vector3(0.6f, 1.7f, 0f), new Vector3(1.6f, 1.8f, 1.6f), green, new Vector3(0f, 0f, 90f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "HopperBottom", new Vector3(-1.0f, 2.8f, 0f), new Vector3(1.0f, 0.8f, 1.0f), greenDark);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "HopperTop", new Vector3(-1.0f, 3.7f, 0f), new Vector3(2.0f, 1.2f, 2.0f), green);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "Chute", new Vector3(2.35f, 0.95f, 0f), new Vector3(0.8f, 0.2f, 0.9f), steel, new Vector3(0f, 0f, -25f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "PelletPile", new Vector3(2.7f, 0.5f, 0f), new Vector3(0.9f, 0.45f, 0.9f), pellet);

            Transform fan = FarmVisualFactory.CreatePivot(m, "SpinX_Auger", new Vector3(2.5f, 1.7f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, fan, "BarA", Vector3.zero, new Vector3(0.12f, 1.5f, 0.2f), steel);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, fan, "BarB", Vector3.zero, new Vector3(0.12f, 0.2f, 1.5f), steel);

            FarmVisualFactory.CreatePivot(m, SteamAnchorName, new Vector3(-1.0f, 4.5f, 0f));
            FarmVisualFactory.CreatePivot(m, IndicatorAnchorName, new Vector3(0f, 1.0f, 1.75f));
        }

        // ----------------------------------------------------------- Jam maker
        private static void BuildJamMaker(Transform m)
        {
            Material stone = FarmVisualFactory.GetSharedMaterial("MachStone", new Color(0.62f, 0.60f, 0.57f));
            Material copper = FarmVisualFactory.GetSharedMaterial("MachCopper", new Color(0.80f, 0.46f, 0.22f), 0.5f, 0.55f);
            Material copperDark = FarmVisualFactory.GetSharedMaterial("MachCopperDark", new Color(0.62f, 0.34f, 0.16f), 0.45f, 0.5f);
            Material jam = FarmVisualFactory.GetSharedMaterial("MachJamSurface", new Color(0.85f, 0.18f, 0.30f), 0.6f);
            Material wood = FarmVisualFactory.GetSharedMaterial("MachWood", new Color(0.48f, 0.32f, 0.18f));
            Material berry = FarmVisualFactory.GetSharedMaterial("MachBerry", new Color(0.88f, 0.16f, 0.22f), 0.35f);
            Material leaf = FarmVisualFactory.GetSharedMaterial("MachLeaf", new Color(0.30f, 0.62f, 0.28f));
            Material juice = FarmVisualFactory.GetSharedMaterial("MachJuiceKeg", new Color(0.92f, 0.45f, 0.25f));

            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "StoveBase", new Vector3(0f, 0.35f, 0f), new Vector3(3.6f, 0.35f, 3.6f), stone);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "Cauldron", new Vector3(0f, 2.0f, 0f), new Vector3(3.0f, 1.3f, 3.0f), copper);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "CauldronRim", new Vector3(0f, 3.3f, 0f), new Vector3(3.2f, 0.08f, 3.2f), copperDark);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "Surface_Jam", new Vector3(0f, 3.22f, 0f), new Vector3(2.7f, 0.04f, 2.7f), jam, default, false);

            Transform ladle = FarmVisualFactory.CreatePivot(m, "SpinY_Ladle", new Vector3(0f, 3.25f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, ladle, "LadleArm", new Vector3(0.7f, 0.5f, 0f), new Vector3(1.8f, 0.1f, 0.14f), wood, new Vector3(0f, 0f, 14f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, ladle, "LadleBowl", new Vector3(1.45f, 0.0f, 0f), new Vector3(0.45f, 0.3f, 0.45f), copperDark);

            FarmVisualFactory.CreatePart(PrimitiveType.Cube, m, "FruitCrate", new Vector3(2.4f, 0.45f, 1.0f), new Vector3(1.3f, 0.9f, 1.0f), wood);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Berry1", new Vector3(2.15f, 1.0f, 0.8f), new Vector3(0.38f, 0.38f, 0.38f), berry);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Berry2", new Vector3(2.6f, 1.0f, 1.0f), new Vector3(0.38f, 0.38f, 0.38f), berry);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "Berry3", new Vector3(2.3f, 1.0f, 1.25f), new Vector3(0.38f, 0.38f, 0.38f), berry);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, m, "BerryLeaf", new Vector3(2.4f, 1.18f, 1.0f), new Vector3(0.45f, 0.1f, 0.45f), leaf);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "JuiceKeg", new Vector3(-2.3f, 0.8f, 0.6f), new Vector3(1.0f, 0.8f, 1.0f), juice);
            FarmVisualFactory.CreatePart(PrimitiveType.Cylinder, m, "KegBand", new Vector3(-2.3f, 0.8f, 0.6f), new Vector3(1.04f, 0.06f, 1.04f), copperDark);

            FarmVisualFactory.CreatePivot(m, SteamAnchorName, new Vector3(0f, 3.7f, 0f));
            FarmVisualFactory.CreatePivot(m, IndicatorAnchorName, new Vector3(1.1f, 0.75f, 1.75f));
        }
    }
}
