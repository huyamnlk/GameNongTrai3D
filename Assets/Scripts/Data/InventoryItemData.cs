using System;
using UnityEngine;

namespace IrishFarmSim
{
    public enum ItemType
    {
        Seed,
        Crop,
        AnimalProduct,
        Feed,
        Material,
        Decoration,
        Tool,
        Misc
    }

    [System.Serializable]
    public class InventoryItemData
    {
        public string id;
        public string displayName;
        public ItemType itemType;
        public int quantity;
        public int maxStack;
        public string iconPath;

        public ItemType type
        {
            get => itemType;
            set => itemType = value;
        }

        public InventoryItemData()
        {
            this.maxStack = 999;
        }

        public InventoryItemData(string id, string displayName, ItemType itemType, int quantity, int maxStack = 999, string iconPath = "")
        {
            this.id = id;
            this.displayName = displayName;
            this.itemType = itemType;
            this.quantity = quantity;
            this.maxStack = maxStack;
            this.iconPath = iconPath;
        }

        public InventoryItemData(string id, string displayName, ItemType type, int quantity)
            : this(id, displayName, type, quantity, 999, "")
        {
        }

        public InventoryItemData Clone()
        {
            return new InventoryItemData(id, displayName, itemType, quantity, maxStack, iconPath);
        }
    }
}
