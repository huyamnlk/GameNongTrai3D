using System;
using System.Collections.Generic;

namespace IrishFarmSim
{
    [Serializable]
    public class InventoryItemSaveData
    {
        public string id;
        public int quantity;
    }

    [Serializable]
    public class PlotSaveData
    {
        public string plotName;
        public string state;
        public string cropId;
        public float growthTimer;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public bool hasCustomPosition = false;
        public bool isUnlocked = true;
        public int unlockLevel = 1;
    }

    [Serializable]
    public class AnimalSaveData
    {
        public string animalName;
        public string animalType;
        public string parentBuildingId;
        public float hunger;
        public float happiness;
        public float affection = 50f;
        public float productTimer;
        public bool isProductReady;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public bool hasCustomPosition = false;
    }

    [Serializable]
    public class MachineSaveData
    {
        public string machineId;
        public string state;
        public float currentTimer;
        public int pendingOutputAmount;
        public bool isPurchased;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float scaleX = 1.2f;
        public float scaleY = 1.2f;
        public float scaleZ = 1.2f;
        public bool hasCustomPosition = false;
        public string recipeId = "";
    }

    [Serializable]
    public class PetSaveData
    {
        public string petId;
        public string petType;
        public string coatId;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
    }

    [Serializable]
    public class MovableObjectSaveData
    {
        public string objectId;
        public string objectType; // "Building", "Plot", "Machine", "Decoration"
        public string buildingType;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float scaleX = 1f;
        public float scaleY = 1f;
        public float scaleZ = 1f;
        public string parentId;
        public string farmPlotId;
        public string cropState;
        public bool isDeleted = false;
    }

    [Serializable]
    public class FarmLayoutSaveData
    {
        public List<MovableObjectSaveData> objects = new List<MovableObjectSaveData>();
        public string lastEditedTime = "";
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string buildingId;
        public string buildingType;
        public float posX;
        public float posY;
        public float posZ;
        public float rotY;
        public float scaleX = 1f;
        public float scaleY = 1f;
        public float scaleZ = 1f;
        public string state = "";
        public bool isPurchased = true;
    }

    [Serializable]
    public class QuestSaveData
    {
        public string questId;
        public string title;
        public string description;
        public string type;
        public int targetCount;
        public int currentCount;
        public int rewardGold;
        public int rewardXP;
        public bool isClaimed;
    }

    [Serializable]
    public class AchievementSaveData
    {
        public string id;
        public string title;
        public string description;
        public int targetCount;
        public int currentCount;
        public int rewardGold;
        public int rewardXP;
        public bool isClaimed;
    }

    [Serializable]
    public class FarmSaveData
    {
        public int level = 1;
        public int currentXP = 0;
        public int gold = 100;
        public int storageCapacity = 200;
        public int currentDay = 1;
        public string currentWeather = "Sunny";
        public string currentSeason = "Spring";
        public bool isManualWeather = false;
        public string manualWeather = "Sunny";
        public int totalCompletedQuestsCount = 0;
        public int totalCropsHarvested = 0;
        public int totalDecorationsMovedOrPlaced = 0;
        public List<string> purchasedBuildings = new List<string>();
        public List<string> unlockedPlotIds = new List<string>();
        public List<BuildingSaveData> buildingsData = new List<BuildingSaveData>();
        public List<MachineSaveData> machines = new List<MachineSaveData>();
        public List<InventoryItemSaveData> inventory = new List<InventoryItemSaveData>();
        public List<PlotSaveData> plots = new List<PlotSaveData>();
        public List<AnimalSaveData> animals = new List<AnimalSaveData>();
        public List<PetSaveData> pets = new List<PetSaveData>();
        public List<MovableObjectSaveData> movedObjects = new List<MovableObjectSaveData>();
        public List<string> deletedObjectIds = new List<string>();
        public List<QuestSaveData> dailyQuests = new List<QuestSaveData>();
        public List<AchievementSaveData> achievements = new List<AchievementSaveData>();
        public string saveTimestamp = "";
    }
}
