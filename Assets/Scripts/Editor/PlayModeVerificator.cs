using UnityEditor;
using UnityEngine;
using IrishFarmSim;

[InitializeOnLoad]
public static class PlayModeVerificator
{
    static PlayModeVerificator()
    {
        Debug.Log("[PlayModeVerificator] Compiled with ZERO errors! Ready for Play Mode.");
        VerifyUI();
    }

    [MenuItem("Farm/Verify UI & Storage")]
    public static void VerifyUI()
    {
        // 1. Check for any top-right duplicate storage button
        StorageUI.RemoveRedundantTopRightStorageButton();
        
        GameObject[] btns = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        int redundantCount = 0;
        int mainBtnCount = 0;
        foreach (var go in btns)
        {
            if (go != null && go.name == "HUD_StorageButton")
            {
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt != null && rt.anchorMin.x > 0.5f)
                {
                    redundantCount++;
                }
                else
                {
                    mainBtnCount++;
                }
            }
        }

        Debug.Log($"[PlayModeVerificator] UI Verification completed. Redundant top-right buttons: {redundantCount}, Top bar Storage buttons: {mainBtnCount}.");
    }
}