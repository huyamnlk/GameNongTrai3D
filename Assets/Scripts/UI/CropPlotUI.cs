using System;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Legacy interface / adapter that delegates directly to CropSelectionUI.
    /// </summary>
    public class CropPlotUI : MonoBehaviour
    {
        public static CropPlotUI Instance { get; private set; }

        public bool IsOpen => CropSelectionUI.Instance != null && CropSelectionUI.Instance.IsOpen;

        void Awake()
        {
            if (Instance == null) Instance = this;
        }

        public static void OpenPlantingMenu(CropPlot plot)
        {
            CropSelectionUI.OpenMenu(plot);
        }

        public void Open()
        {
            if (CropSelectionUI.Instance != null) CropSelectionUI.Instance.Open();
        }

        public void Close()
        {
            if (CropSelectionUI.Instance != null) CropSelectionUI.Instance.Close();
        }

        public static void ShowHarvestToast(string message)
        {
            CropSelectionUI.ShowToast(message);
        }

        public static CropPlotUI EnsureUI()
        {
            CropSelectionUI.EnsureUI();

            if (Instance != null) return Instance;
            CropPlotUI found = FindAnyObjectByType<CropPlotUI>();
            if (found != null)
            {
                Instance = found;
                return found;
            }

            if (CropSelectionUI.Instance != null)
            {
                Instance = CropSelectionUI.Instance.gameObject.AddComponent<CropPlotUI>();
                return Instance;
            }

            GameObject go = new GameObject("CropPlotUI");
            Instance = go.AddComponent<CropPlotUI>();
            return Instance;
        }
    }
}
