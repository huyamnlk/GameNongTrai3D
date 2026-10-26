using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace IrishFarmSim
{
    /// <summary>
    /// Handles interaction with farm animals (Cows) and interactable scene objects.
    /// Ensures UI clicks do not conflict with 3D object interactions.
    /// </summary>
    public class FarmInteractionController : MonoBehaviour
    {
        [Header("Settings")]
        public float interactionDistance = 50f;
        public LayerMask interactableLayer = ~0;

        void Update()
        {
            HandleInteractionInput();
        }

        private void HandleInteractionInput()
        {
            if (!Input.GetMouseButtonDown(0)) return;

            // Block interaction if pointer is currently over UI (mouse or touch)
            if (EventSystem.current != null)
            {
                if (EventSystem.current.IsPointerOverGameObject()) return;
                if (Input.touchCount > 0 && EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId)) return;
            }
            if (SuppliesUI.Instance != null && SuppliesUI.Instance.IsOpen) return;
            if (CropSelectionUI.Instance != null && CropSelectionUI.Instance.IsOpen) return;
            if (CropPlotUI.Instance != null && CropPlotUI.Instance.IsOpen) return;
            if (AnimalUI.Instance != null && AnimalUI.Instance.IsOpen) return;
            if (QuestUI.Instance != null && QuestUI.Instance.IsOpen) return;
            if (AchievementUI.Instance != null && AchievementUI.Instance.IsOpen) return;
            if (ShopUI.Instance != null && ShopUI.Instance.panelRoot != null && ShopUI.Instance.panelRoot.activeSelf) return;
            if (BuildingUI.Instance != null && BuildingUI.Instance.panelRoot != null && BuildingUI.Instance.panelRoot.activeSelf) return;
            if (MachineUI.Instance != null && MachineUI.Instance.panelRoot != null && MachineUI.Instance.panelRoot.activeSelf) return;
            if (StorageUI.Instance != null && StorageUI.Instance.IsOpen) return;
            if (BuildingPlacementController.Instance != null && BuildingPlacementController.Instance.IsPlacing) return;
            if (FarmEditModeController.Instance != null && FarmEditModeController.Instance.IsEditMode) return;
            if (Time.unscaledTime < CropSelectionUI.IgnoreClicksUntilTime) return;

            Camera cam = Camera.main;
            if (cam == null) cam = FindFirstObjectByType<Camera>();
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f, interactableLayer);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (var hit in hits)
            {
                // 1. Check if clicked CropPlot
                CropPlot plot = hit.collider.GetComponentInParent<CropPlot>();
                if (plot != null)
                {
                    plot.OnPlotClicked();
                    CancelMove();
                    return;
                }

                // 2. Check if clicked ProductionMachine
                ProductionMachine machine = hit.collider.GetComponentInParent<ProductionMachine>();
                if (machine != null)
                {
                    machine.OnClickMachine();
                    CancelMove();
                    return;
                }

                // 3. Check if clicked FarmBuilding (Barn, Coop, Pen)
                FarmBuilding building = hit.collider.GetComponentInParent<FarmBuilding>();
                if (building != null && FarmBuilding.IsValidBuilding(building))
                {
                    building.OnClickBuilding();
                    CancelMove();
                    return;
                }

                // 4. Check if clicked AnimalCareController (Cow or Chicken Coop)
                AnimalCareController animal = hit.collider.GetComponentInParent<AnimalCareController>();
                if (animal != null)
                {
                    animal.OpenAnimalCareUI();
                    CancelMove();
                    return;
                }

                // 5. Fallback check for CowController
                CowController cow = hit.collider.GetComponentInParent<CowController>();
                if (cow != null)
                {
                    AnimalCareController acc = cow.GetComponent<AnimalCareController>();
                    if (acc != null)
                    {
                        acc.OpenAnimalCareUI();
                    }
                    else
                    {
                        cow.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                    }
                    CancelMove();
                    return;
                }

                // 6. Check if clicked PetWanderer (friendly pet interaction)
                PetWanderer pet = hit.collider.GetComponentInParent<PetWanderer>();
                if (pet != null)
                {
                    pet.Pet();
                    CancelMove();
                    return;
                }
            }
        }

        private void CancelMove()
        {
            ClickToMove ctm = GetComponent<ClickToMove>();
            if (ctm != null)
            {
                ctm.CancelMove();
            }
        }
    }
}
