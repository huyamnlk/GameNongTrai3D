using System;
using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// DEPRECATED: Giao diện Supplies cũ đã được thay thế hoàn toàn bằng StorageUI và InventorySystem.
    /// SuppliesUI chỉ đóng vai trò dọn dẹp SuppliesCanvas nếu còn sót lại và chuyển hướng sang StorageUI.
    /// </summary>
    public class SuppliesUI : MonoBehaviour
    {
        public static SuppliesUI Instance { get; private set; }

        public bool IsOpen => false;

        void Awake()
        {
            // Tự hủy ngay lập tức nếu object SuppliesCanvas hay SuppliesUI được tạo
            if (gameObject != null)
            {
                if (Application.isPlaying) Destroy(gameObject);
                else DestroyImmediate(gameObject);
            }
        }

        /// <summary>
        /// Dọn dẹp hoàn toàn SuppliesCanvas trong scene nếu có. Không tạo mới.
        /// </summary>
        public static SuppliesUI EnsureSuppliesUIInScene()
        {
            // Tìm và xóa SuppliesCanvas nếu tồn tại
            GameObject oldCanvas = GameObject.Find("SuppliesCanvas");
            if (oldCanvas != null)
            {
                if (Application.isPlaying) Destroy(oldCanvas);
                else DestroyImmediate(oldCanvas);
            }

            SuppliesUI[] existingUIs = FindObjectsByType<SuppliesUI>(FindObjectsSortMode.None);
            if (existingUIs != null)
            {
                foreach (var ui in existingUIs)
                {
                    if (ui != null && ui.gameObject != null)
                    {
                        if (Application.isPlaying) Destroy(ui.gameObject);
                        else DestroyImmediate(ui.gameObject);
                    }
                }
            }

            Instance = null;
            return null;
        }

        public void Open()
        {
            // Chuyển hướng sang StorageUI
            StorageUI.EnsureUI()?.Open();
        }

        public void Close()
        {
            StorageUI.EnsureUI()?.Close();
        }

        public void Toggle()
        {
            StorageUI.EnsureUI()?.Toggle();
        }
    }
}
