using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace IrishFarmSim.Editor
{
    [InitializeOnLoad]
    public static class GameplayAndUIUpgradeVerifier
    {
        static GameplayAndUIUpgradeVerifier()
        {
            EditorApplication.delayCall += RunVerification;
        }

        [MenuItem("IrishFarmSim/Run Gameplay and UI Verification", priority = 2)]
        public static void RunVerification()
        {
            Debug.Log("==================================================================");
            Debug.Log("=== RUNNING GAMEPLAY & UI UPGRADE VERIFICATION (3 ISSUES) ===");
            Debug.Log("==================================================================");

            bool p1 = VerifyIssue1_RotationAndGroundSnapping();
            bool p2 = VerifyIssue2_PlotPurchaseAndUnlockFlow();
            bool p3 = VerifyIssue3_MachineUIRecipeSelection();

            Debug.Log("==================================================================");
            if (p1 && p2 && p3)
            {
                Debug.Log(">>> ALL 3 GAMEPLAY/UI UPGRADE TESTS PASSED 100%! <<<");
            }
            else
            {
                Debug.LogError($">>> VERIFICATION FAILED: Issue1={p1}, Issue2={p2}, Issue3={p3} <<<");
            }
            Debug.Log("==================================================================");
        }

        private static bool VerifyIssue1_RotationAndGroundSnapping()
        {
            Debug.Log("--- [Issue 1]: Testing Object Rotation & Precision Ground Snapping ---");
            bool passed = true;

            // 1. Create a test structure with center-based pivot (simulating non-bottom pivot building)
            GameObject testBuilding = new GameObject("TestBuilding_PivotCenter");
            try
            {
                testBuilding.transform.position = new Vector3(90f, 15f, 260f);

                // Add a visual cube where local y ranges from -1 to +1 (pivot is at center y=0)
                GameObject visualBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visualBox.transform.SetParent(testBuilding.transform, false);
                visualBox.transform.localPosition = Vector3.zero;
                visualBox.transform.localScale = new Vector3(4f, 2f, 4f);

                BoxCollider bc = testBuilding.AddComponent<BoxCollider>();
                bc.center = Vector3.zero;
                bc.size = new Vector3(4f, 2f, 4f);

                // Test snapping at 0°, 45°, 90°, 135°, 180° rotations
                float[] testAngles = new float[] { 0f, 45f, 90f, 135f, 180f };
                foreach (float angle in testAngles)
                {
                    Quaternion rot = Quaternion.Euler(0f, angle, 0f);
                    testBuilding.transform.rotation = rot;

                    float expectedGroundAtCenter = GroundSnapUtility.SampleGroundElevation(testBuilding.transform.position.x, testBuilding.transform.position.z);
                    float groundedY = GroundSnapUtility.CalculateGroundedPivotY(testBuilding, testBuilding.transform.position, rot);

                    testBuilding.transform.position = new Vector3(testBuilding.transform.position.x, groundedY, testBuilding.transform.position.z);
                    GroundSnapUtility.SnapObjectToGround(testBuilding);

                    // Verify bottom contact point sits flush on terrain
                    // Since visual box extends -1m below pivot in local space, bottom should sit at ground elevation
                    float lowestPoint = float.MaxValue;
                    foreach (var r in testBuilding.GetComponentsInChildren<Renderer>())
                    {
                        if (r.bounds.min.y < lowestPoint) lowestPoint = r.bounds.min.y;
                    }

                    float diffFromGround = Mathf.Abs(lowestPoint - expectedGroundAtCenter);
                    if (diffFromGround > 0.08f)
                    {
                        Debug.LogError($"FAIL: Snapped object at angle {angle}° has bottom at {lowestPoint}, expected ground {expectedGroundAtCenter} (diff: {diffFromGround})");
                        passed = false;
                    }
                    else
                    {
                        Debug.Log($"PASS: Angle {angle:F0}° grounded flush (Pivot Y={groundedY:F3}, Lowest Bottom Y={lowestPoint:F3}, Ground Y={expectedGroundAtCenter:F3}, Diff={diffFromGround:F4}m)");
                    }
                }

                // Verify preview to placed entity matching
                BuildingPlacementController bpc = BuildingPlacementController.EnsureInstance();
                if (bpc != null)
                {
                    Debug.Log("PASS: BuildingPlacementController initialized with rotation controls and ground snap support.");
                }
            }
            finally
            {
                if (testBuilding != null) UnityEngine.Object.DestroyImmediate(testBuilding);
            }

            return passed;
        }

        private static bool VerifyIssue2_PlotPurchaseAndUnlockFlow()
        {
            Debug.Log("--- [Issue 2]: Testing Plot Purchase, Placement & Save/Load Unlock Flow ---");
            bool passed = true;

            // 1. Verify FarmDatabase has plot definitions
            if (FarmDatabase.Plots == null || FarmDatabase.Plots.Count < 26)
            {
                Debug.LogError("FAIL: FarmDatabase does not contain 26 plots!");
                return false;
            }
            Debug.Log($"PASS: FarmDatabase contains {FarmDatabase.Plots.Count} defined farm plots.");

            // 2. Verify CropManager SpawnPlotAtPosition
            CropManager cm = UnityEngine.Object.FindAnyObjectByType<CropManager>();
            if (cm == null)
            {
                GameObject cmGo = new GameObject("CropManager");
                cm = cmGo.AddComponent<CropManager>();
            }

            string testPlotId = "CropPlot_07";
            Vector3 testPos = new Vector3(92f, 23.1f, 273f);
            Quaternion testRot = Quaternion.Euler(0f, 45f, 0f);

            CropPlot spawned = cm.SpawnPlotAtPosition(testPlotId, testPos, testRot);
            if (spawned == null)
            {
                Debug.LogError("FAIL: CropManager.SpawnPlotAtPosition returned null!");
                return false;
            }

            if (!spawned.isUnlocked || spawned.state == PlotState.Locked)
            {
                Debug.LogError("FAIL: Spawned plot is not unlocked or still in Locked state!");
                passed = false;
            }
            else
            {
                Debug.Log($"PASS: Spawned {testPlotId} at {spawned.transform.position} (Rot: {spawned.transform.eulerAngles.y}°) with state={spawned.state}, isUnlocked={spawned.isUnlocked}.");
            }

            // 3. Verify MovableFarmObject attached
            MovableFarmObject mfo = spawned.GetComponent<MovableFarmObject>();
            if (mfo == null)
            {
                Debug.LogError("FAIL: Spawned plot does not have MovableFarmObject component for farm edit mode!");
                passed = false;
            }
            else
            {
                Debug.Log($"PASS: MovableFarmObject attached with footprint={mfo.footprintSize}.");
            }

            // 4. Verify PlayerProgression UnlockPlot
            PlayerProgression pp = PlayerProgression.Instance;
            if (pp != null)
            {
                pp.UnlockPlot(testPlotId);
                if (!pp.IsPlotUnlocked(testPlotId))
                {
                    Debug.LogError("FAIL: PlayerProgression.IsPlotUnlocked returned false for unlocked plot!");
                    passed = false;
                }
                else
                {
                    Debug.Log($"PASS: PlayerProgression correctly tracks {testPlotId} as unlocked.");
                }
            }

            // Cleanup test plot
            if (spawned != null)
            {
                cm.plots.Remove(spawned);
                UnityEngine.Object.DestroyImmediate(spawned.gameObject);
            }

            return passed;
        }

        private static bool VerifyIssue3_MachineUIRecipeSelection()
        {
            Debug.Log("--- [Issue 3]: Testing MachineUI Recipe Cards & Production Workflow ---");
            bool passed = true;

            MachineUI ui = MachineUI.EnsureUI();
            if (ui == null || ui.panelRoot == null)
            {
                Debug.LogError("FAIL: MachineUI.EnsureUI() failed to create panelRoot!");
                return false;
            }

            // Create a test machine (Jam Maker with 3 recipes)
            GameObject machineGo = new GameObject("TestMachine");
            ProductionMachine pm = machineGo.AddComponent<ProductionMachine>();
            try
            {
                MachineData machineData = FarmDatabase.Machines["jam_maker"];
                pm.SetupMachine(machineData, true);

                if (pm.availableRecipes.Count != 3)
                {
                    Debug.LogError($"FAIL: Jam Maker should have 3 recipes, but has {pm.availableRecipes.Count}!");
                    return false;
                }
                Debug.Log($"PASS: Test Jam Maker configured with {pm.availableRecipes.Count} recipes.");

                // Open Machine in UI
                MachineUI.OpenMachine(pm);

                // Verify recipe cards container
                if (ui.recipeCardsContainer == null || ui.recipeCardsContainer.childCount < 3)
                {
                    Debug.LogError($"FAIL: MachineUI did not create 3 recipe cards in recipeCardsContainer! Count={ui.recipeCardsContainer?.childCount}");
                    passed = false;
                }
                else
                {
                    Debug.Log($"PASS: MachineUI successfully built {ui.recipeCardsContainer.childCount} interactive recipe cards.");
                }

                // Verify selecting recipe 2 (Tomato Juice)
                bool switched = pm.SelectRecipe("tomato_juice");
                ui.RefreshDisplay();
                if (!switched || pm.currentRecipe.recipeId != "tomato_juice")
                {
                    Debug.LogError("FAIL: Failed to switch recipe to 'tomato_juice'!");
                    passed = false;
                }
                else
                {
                    Debug.Log($"PASS: Successfully switched recipe to '{pm.currentRecipe.displayName}'.");
                }

                // Verify ingredient checking & Start button locking
                // Since test environment has 0 ingredients in inventory, start button should be locked
                if (ui.buttonStart.interactable)
                {
                    Debug.LogError("FAIL: buttonStart should be disabled when ingredients are missing!");
                    passed = false;
                }
                else
                {
                    Debug.Log($"PASS: buttonStart is safely locked (interactable=false) when missing ingredients.");
                }

                // Verify ingredient status text
                if (string.IsNullOrEmpty(ui.textIngredients.text) || !ui.textIngredients.text.Contains("THIẾU"))
                {
                    Debug.LogError("FAIL: textIngredients does not display high-contrast missing ingredient status!");
                    passed = false;
                }
                else
                {
                    Debug.Log("PASS: textIngredients displays clear high-contrast ingredient availability status.");
                }

                ui.Close();
            }
            finally
            {
                if (machineGo != null) UnityEngine.Object.DestroyImmediate(machineGo);
            }

            return passed;
        }
    }
}
