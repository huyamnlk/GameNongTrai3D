using UnityEngine;

namespace IrishFarmSim
{
	public class FarmInit : MonoBehaviour
	{
		// Cow default spawn info & location area
		private Vector2 farmTopLeftPosA = new Vector2(102f, 261f);
		private Vector2 farmBottomRightPosA = new Vector2(57f, 242f);
		private Vector2 farmTopLeftPosB = new Vector2(102f, 333f);
		private Vector2 farmBottomRightPosB = new Vector2(73f, 309f);
		private Vector2 farmTopLeftPosC = new Vector2(246f, 267f);
		private Vector2 farmBottomRightPosC = new Vector2(230f, 244f);
		// Default values
		private Vector2 farmTopLeftPos = new Vector2(102f, 261f);
		private Vector2 farmBottomRightPos = new Vector2(57f, 242f);

		void Start()
		{
			try
			{
				AnimalManager.PurgeShowcaseAndSpriteArtifacts();

				FarmBuilding[] sceneBuildings = FindObjectsByType<FarmBuilding>(FindObjectsInactive.Include, FindObjectsSortMode.None);
				int defaultBuildingCount = 0;
				foreach (var b in sceneBuildings)
				{
					if (b != null && !b.IsGhostOrPreview() && !b.buildingId.Equals("machine_shed", System.StringComparison.OrdinalIgnoreCase))
						defaultBuildingCount++;
				}
				Debug.Log($"[FarmInit] Default buildings found: {defaultBuildingCount}");

				if (GameController.Instance() != null)
				{
					GameController.Instance().newGame = false;
					GameController.Instance().loadPlayer = false;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogWarning("[FarmInit] Warning during farm init: " + e.Message);
			}
		}

		private void InitNewGame()
		{
			if (GameController.Instance() != null)
			{
				GameController.Instance().newGame = false;
			}
		}

		private void LoadGame()
		{
			if (GameController.Instance() != null)
			{
				GameController.Instance().newGame = false;
				GameController.Instance().loadPlayer = false;
			}
		}

		// Generate a random position for cows to spawn
		private void GetRandomPos()
		{
			int randomPos = Random.Range(0, 3);
			
			switch(randomPos)
			{
				case 0:
					farmTopLeftPos = farmTopLeftPosA;
					farmBottomRightPos = farmBottomRightPosA;
					break;
				case 1:
					farmTopLeftPos = farmTopLeftPosB;
					farmBottomRightPos = farmBottomRightPosB;
					break;
				case 2:
					farmTopLeftPos = farmTopLeftPosC;
					farmBottomRightPos = farmBottomRightPosC;
					break;
				default:
					farmTopLeftPos = farmTopLeftPosA;
					farmBottomRightPos = farmBottomRightPosA;
					break;
			}
		}
	}
}