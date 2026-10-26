using UnityEngine;

namespace IrishFarmSim
{
    public enum PetKind
    {
        Dog,
        Cat
    }

    /// <summary>
    /// Builds low-poly articulated dog and cat rigs from collider-free primitives, using the same
    /// shared materials / blob shadow helpers as the farm animal rigs (FarmVisualFactory) so pets
    /// match the existing art style. Rig part names are consumed by PetWanderer.
    /// </summary>
    public static class PetVisualFactory
    {
        public static readonly string[] DogCoats = { "golden", "spotted", "husky" };
        public static readonly string[] CatCoats = { "orange", "gray", "calico" };

        public static float GetScale(PetKind kind)
        {
            // Upgraded scale for clear visibility on mobile screens (+60%)
            return kind == PetKind.Dog ? 1.85f : 1.60f;
        }

        public static string NormalizeCoat(PetKind kind, string coatId)
        {
            string[] coats = kind == PetKind.Dog ? DogCoats : CatCoats;
            foreach (string c in coats)
            {
                if (string.Equals(c, coatId, System.StringComparison.OrdinalIgnoreCase)) return c;
            }
            return coats[0];
        }

        public static void Build(GameObject root, PetKind kind, string coatId)
        {
            if (root == null) return;
            FarmVisualFactory.ClearVisualChildren(root.transform);
            coatId = NormalizeCoat(kind, coatId);
            if (kind == PetKind.Dog) BuildDog(root, coatId);
            else BuildCat(root, coatId);
        }

        // ------------------------------------------------------------------ Dog
        private static void BuildDog(GameObject root, string coatId)
        {
            bool spotted = coatId == "spotted";
            bool husky = coatId == "husky";

            Color mainColor = spotted ? new Color(0.96f, 0.95f, 0.92f) : (husky ? new Color(0.24f, 0.25f, 0.28f) : new Color(0.80f, 0.55f, 0.25f));
            Color darkColor = spotted ? new Color(0.40f, 0.26f, 0.16f) : (husky ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.55f, 0.33f, 0.15f));
            Color accentColor = spotted ? new Color(0.99f, 0.98f, 0.96f) : (husky ? new Color(0.95f, 0.95f, 0.95f) : new Color(0.96f, 0.86f, 0.66f));

            Material coat = FarmVisualFactory.GetSharedMaterial("DogCoat_" + coatId, mainColor, 0.14f);
            Material dark = FarmVisualFactory.GetSharedMaterial("DogDark_" + coatId, darkColor, 0.12f);
            Material accent = FarmVisualFactory.GetSharedMaterial("DogAccent_" + coatId, accentColor, 0.14f);
            Material nose = FarmVisualFactory.GetSharedMaterial("PetNose", new Color(0.10f, 0.08f, 0.08f), 0.5f);
            Material eye = FarmVisualFactory.GetSharedMaterial(husky ? "DogHuskyEye" : "AnimalEye", husky ? new Color(0.35f, 0.65f, 0.95f) : new Color(0.08f, 0.07f, 0.07f), 0.45f);

            // Upgraded ground shadow for mobile visibility
            FarmVisualFactory.EnsureBlobShadow(root.transform, 0.90f, 1.4f, 0.008f);

            Transform body = FarmVisualFactory.CreatePivot(root.transform, "PetBody", new Vector3(0f, 0.40f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, body, "DogTorso", Vector3.zero, new Vector3(0.30f, 0.40f, 0.30f), coat, new Vector3(90f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "DogChest", new Vector3(0f, 0.0f, 0.22f), new Vector3(0.33f, 0.34f, 0.32f), husky ? accent : coat);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "DogRump", new Vector3(0f, 0.01f, -0.22f), new Vector3(0.31f, 0.31f, 0.30f), coat);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "DogBellyPatch", new Vector3(0f, -0.08f, 0.14f), new Vector3(0.22f, 0.18f, 0.30f), accent);

            if (spotted)
            {
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "Spot_1", new Vector3(0.09f, 0.11f, 0.02f), new Vector3(0.17f, 0.08f, 0.20f), dark);
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "Spot_2", new Vector3(-0.10f, 0.09f, -0.18f), new Vector3(0.16f, 0.09f, 0.16f), dark);
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "Spot_3", new Vector3(-0.12f, 0.02f, 0.22f), new Vector3(0.10f, 0.14f, 0.14f), dark);
            }
            else if (husky)
            {
                // Husky saddle back pattern
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "HuskySaddle", new Vector3(0f, 0.12f, 0f), new Vector3(0.28f, 0.12f, 0.36f), dark);
            }

            Transform head = FarmVisualFactory.CreatePivot(body, "PetHead", new Vector3(0f, 0.12f, 0.40f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "DogHead", new Vector3(0f, 0.03f, 0.07f), new Vector3(0.27f, 0.25f, 0.27f), coat);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "DogSnout", new Vector3(0f, -0.03f, 0.19f), new Vector3(0.14f, 0.12f, 0.18f), accent);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "DogNose", new Vector3(0f, -0.005f, 0.275f), new Vector3(0.055f, 0.045f, 0.045f), nose);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "DogEye_L", new Vector3(-0.075f, 0.07f, 0.15f), new Vector3(0.04f, 0.04f, 0.04f), eye);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "DogEye_R", new Vector3(0.075f, 0.07f, 0.15f), new Vector3(0.04f, 0.04f, 0.04f), eye);

            if (husky)
            {
                // Distinctive husky white eye brows
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "HuskyBrow_L", new Vector3(-0.075f, 0.13f, 0.13f), new Vector3(0.035f, 0.035f, 0.02f), accent);
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "HuskyBrow_R", new Vector3(0.075f, 0.13f, 0.13f), new Vector3(0.035f, 0.035f, 0.02f), accent);
            }

            Transform earL = FarmVisualFactory.CreatePivot(head, "PetEar_L", new Vector3(-0.10f, 0.14f, 0.04f), new Vector3(0f, 0f, husky ? 10f : 25f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, earL, "EarFlap_L", new Vector3(-0.02f, -0.06f, 0f), new Vector3(0.07f, 0.17f, 0.05f), dark);
            Transform earR = FarmVisualFactory.CreatePivot(head, "PetEar_R", new Vector3(0.10f, 0.14f, 0.04f), new Vector3(0f, 0f, husky ? -10f : -25f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, earR, "EarFlap_R", new Vector3(0.02f, -0.06f, 0f), new Vector3(0.07f, 0.17f, 0.05f), dark);

            Transform tail = FarmVisualFactory.CreatePivot(body, "PetTail", new Vector3(0f, 0.10f, -0.36f), new Vector3(-40f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, tail, "DogTail", new Vector3(0f, 0.10f, 0f), new Vector3(0.06f, 0.11f, 0.06f), coat);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, tail, "DogTailTip", new Vector3(0f, 0.21f, 0f), new Vector3(0.07f, 0.07f, 0.07f), accent);

            BuildLegs(root.transform, "Dog", coat, accent, 0.31f, 0.11f, 0.25f, -0.24f, 0.13f, 0.10f, 0.14f, 0.28f, 0.115f, 0.06f, 0.15f);
        }

        // ------------------------------------------------------------------ Cat
        private static void BuildCat(GameObject root, string coatId)
        {
            bool gray = coatId == "gray";
            bool calico = coatId == "calico";

            Color mainColor = calico ? new Color(0.97f, 0.95f, 0.90f) : (gray ? new Color(0.62f, 0.64f, 0.68f) : new Color(0.93f, 0.58f, 0.25f));
            Color stripeColor = calico ? new Color(0.85f, 0.45f, 0.16f) : (gray ? new Color(0.44f, 0.46f, 0.52f) : new Color(0.78f, 0.40f, 0.14f));
            Color accentColor = calico ? new Color(0.22f, 0.20f, 0.20f) : new Color(0.98f, 0.95f, 0.90f);

            Material coat = FarmVisualFactory.GetSharedMaterial("CatCoat_" + coatId, mainColor, 0.16f);
            Material stripe = FarmVisualFactory.GetSharedMaterial("CatStripe_" + coatId, stripeColor, 0.12f);
            Material accent = FarmVisualFactory.GetSharedMaterial("CatAccent_" + coatId, accentColor, 0.16f);
            Material nose = FarmVisualFactory.GetSharedMaterial("CatNose", new Color(0.95f, 0.55f, 0.60f), 0.3f);
            Material eye = FarmVisualFactory.GetSharedMaterial("CatEye", calico ? new Color(0.40f, 0.75f, 0.85f) : new Color(0.55f, 0.85f, 0.30f), 0.5f);
            Material pupil = FarmVisualFactory.GetSharedMaterial("AnimalEye", new Color(0.08f, 0.07f, 0.07f), 0.45f);
            Material whisker = FarmVisualFactory.GetSharedMaterial("CatWhisker", new Color(0.96f, 0.96f, 0.96f), 0.2f);

            // Upgraded ground shadow for mobile visibility
            FarmVisualFactory.EnsureBlobShadow(root.transform, 0.75f, 1.1f, 0.008f);

            Transform body = FarmVisualFactory.CreatePivot(root.transform, "PetBody", new Vector3(0f, 0.27f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, body, "CatTorso", Vector3.zero, new Vector3(0.19f, 0.27f, 0.19f), coat, new Vector3(90f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "CatChest", new Vector3(0f, 0f, 0.15f), new Vector3(0.20f, 0.20f, 0.20f), calico ? coat : accent);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "CatRump", new Vector3(0f, 0f, -0.15f), new Vector3(0.20f, 0.20f, 0.20f), coat);

            if (calico)
            {
                // Distinctive calico orange and black calico patches
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "CalicoOrangePatch", new Vector3(0.07f, 0.08f, 0.02f), new Vector3(0.14f, 0.08f, 0.14f), stripe);
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, body, "CalicoBlackPatch", new Vector3(-0.07f, 0.06f, -0.08f), new Vector3(0.12f, 0.07f, 0.12f), accent);
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    FarmVisualFactory.CreatePart(PrimitiveType.Cube, body, "Stripe_" + i, new Vector3(0f, 0.088f, -0.12f + 0.12f * i), new Vector3(0.15f, 0.03f, 0.045f), stripe);
                }
            }

            Transform head = FarmVisualFactory.CreatePivot(body, "PetHead", new Vector3(0f, 0.09f, 0.27f));
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatHead", new Vector3(0f, 0.03f, 0.04f), new Vector3(0.20f, 0.18f, 0.18f), coat);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatMuzzle", new Vector3(0f, -0.02f, 0.12f), new Vector3(0.09f, 0.07f, 0.07f), calico ? stripe : accent);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatNose", new Vector3(0f, 0.0f, 0.155f), new Vector3(0.025f, 0.02f, 0.02f), nose);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatEye_L", new Vector3(-0.05f, 0.05f, 0.112f), new Vector3(0.04f, 0.045f, 0.025f), eye);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatEye_R", new Vector3(0.05f, 0.05f, 0.112f), new Vector3(0.04f, 0.045f, 0.025f), eye);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatPupil_L", new Vector3(-0.05f, 0.05f, 0.123f), new Vector3(0.012f, 0.032f, 0.01f), pupil);
            FarmVisualFactory.CreatePart(PrimitiveType.Sphere, head, "CatPupil_R", new Vector3(0.05f, 0.05f, 0.123f), new Vector3(0.012f, 0.032f, 0.01f), pupil);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, head, "Whisker_L", new Vector3(-0.10f, -0.01f, 0.115f), new Vector3(0.12f, 0.004f, 0.004f), whisker, new Vector3(0f, 12f, 0f), false);
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, head, "Whisker_R", new Vector3(0.10f, -0.01f, 0.115f), new Vector3(0.12f, 0.004f, 0.004f), whisker, new Vector3(0f, -12f, 0f), false);

            Transform earL = FarmVisualFactory.CreatePivot(head, "PetEar_L", new Vector3(-0.065f, 0.105f, 0.0f), new Vector3(0f, 0f, 8f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, earL, "CatEarShape_L", new Vector3(0f, 0.03f, 0f), new Vector3(0.07f, 0.07f, 0.03f), coat, new Vector3(0f, 0f, 45f));
            Transform earR = FarmVisualFactory.CreatePivot(head, "PetEar_R", new Vector3(0.065f, 0.105f, 0.0f), new Vector3(0f, 0f, -8f));
            FarmVisualFactory.CreatePart(PrimitiveType.Cube, earR, "CatEarShape_R", new Vector3(0f, 0.03f, 0f), new Vector3(0.07f, 0.07f, 0.03f), coat, new Vector3(0f, 0f, 45f));

            Transform tail = FarmVisualFactory.CreatePivot(body, "PetTail", new Vector3(0f, 0.03f, -0.28f), new Vector3(-50f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, tail, "CatTail1", new Vector3(0f, 0.09f, 0f), new Vector3(0.045f, 0.10f, 0.045f), coat);
            Transform tail2 = FarmVisualFactory.CreatePivot(tail, "PetTail2", new Vector3(0f, 0.19f, 0f), new Vector3(25f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, tail2, "CatTail2", new Vector3(0f, 0.09f, 0f), new Vector3(0.040f, 0.09f, 0.040f), stripe);
            Transform tail3 = FarmVisualFactory.CreatePivot(tail2, "PetTail3", new Vector3(0f, 0.18f, 0f), new Vector3(25f, 0f, 0f));
            FarmVisualFactory.CreatePart(PrimitiveType.Capsule, tail3, "CatTail3", new Vector3(0f, 0.07f, 0f), new Vector3(0.036f, 0.075f, 0.036f), coat);

            BuildLegs(root.transform, "Cat", coat, accent, 0.21f, 0.065f, 0.15f, -0.15f, 0.09f, 0.065f, 0.10f, 0.19f, 0.075f, 0.04f, 0.095f);
        }

        // Legs: pivots named PetLeg_FL / FR / BL / BR hang from the hip so PetWanderer can swing them.
        private static void BuildLegs(Transform root, string prefix, Material coat, Material paw,
            float hipY, float halfWidth, float frontZ, float backZ,
            float legCenterY, float legThickness, float legHalfLength,
            float pawCenterY, float pawWidth, float pawHeight, float pawLength)
        {
            string[] names = { "PetLeg_FL", "PetLeg_FR", "PetLeg_BL", "PetLeg_BR" };
            float[] xs = { -halfWidth, halfWidth, -halfWidth, halfWidth };
            float[] zs = { frontZ, frontZ, backZ, backZ };
            for (int i = 0; i < 4; i++)
            {
                Transform leg = FarmVisualFactory.CreatePivot(root, names[i], new Vector3(xs[i], hipY, zs[i]));
                FarmVisualFactory.CreatePart(PrimitiveType.Capsule, leg, prefix + "Leg" + i,
                    new Vector3(0f, -legCenterY, 0f), new Vector3(legThickness, legHalfLength, legThickness), coat);
                FarmVisualFactory.CreatePart(PrimitiveType.Sphere, leg, prefix + "Paw" + i,
                    new Vector3(0f, -pawCenterY, 0.012f), new Vector3(pawWidth, pawHeight, pawLength), paw);
            }
        }
    }
}
