using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
	public class CowMaker : MonoBehaviour
	{
		public static int SpawnCow(Cow cow, float PosA, float PosB, Vector3 forward)
	    {
	        Vector3 spawnLocation = new Vector3(PosA, 0f, PosB);
	        spawnLocation.y = GroundSnapUtility.GetGroundY(spawnLocation);

	        GameObject prefab = Resources.Load(cow.breed) as GameObject;
	        if (prefab == null) prefab = Resources.Load("Angus") as GameObject;
	        if (prefab == null)
	        {
	            Debug.LogWarning($"[CowMaker] Cannot find prefab for breed '{cow.breed}'.");
	            return 0;
	        }

	        GameObject cowGameObject = Instantiate(prefab, spawnLocation, forward != Vector3.zero ? Quaternion.LookRotation(forward) : Quaternion.identity);
	        AnimalScaleConfig.ApplyScale(cowGameObject, AnimalType.Cow);
	        GroundSnapUtility.SnapToGroundExact(cowGameObject);

	        CowController cowController = cowGameObject.GetComponent<CowController>();
	        if (cowController != null)
	        {
	            cow.cowController = cowController;
	            cowController.cow = cow;
	        }

	        return 1;
	    }

	    public static GameObject SpawnCowAtPosition(Cow cow, Vector3 position, Transform parent = null, AnimalPenBoundary pen = null)
	    {
	        if (cow == null) cow = GenerateCow();

	        GameObject prefab = Resources.Load(cow.breed) as GameObject;
	        if (prefab == null) prefab = Resources.Load("Angus") as GameObject;
	        if (prefab == null)
	        {
	            Debug.LogError($"[CowMaker] Missing cow prefab for breed '{cow.breed}'.");
	            return null;
	        }

	        if (pen != null)
	        {
	            position = pen.ClampPosition(position, 0.5f);
	        }

	        position.y = GroundSnapUtility.GetGroundY(position);

	        GameObject cowGo = Instantiate(prefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
	        if (parent != null)
	        {
	            cowGo.transform.SetParent(parent, true);
	        }

	        AnimalScaleConfig.ApplyScale(cowGo, AnimalType.Cow);
	        GroundSnapUtility.SnapToGroundExact(cowGo);

	        CowController cowCtrl = cowGo.GetComponent<CowController>();
	        if (cowCtrl != null)
	        {
	            cow.cowController = cowCtrl;
	            cowCtrl.cow = cow;
	            cowCtrl.penBoundary = pen;
	        }

	        if (pen != null)
	        {
	            pen.RegisterAnimal(cowGo);
	        }

	        return cowGo;
	    }

		public static Cow GenerateCow()
		{
			// Generate new cow from random selection
			int cowGen = Random.Range(1, 6);
			string cowType = "Angus";
			
			switch(cowGen)
			{
				case 1:
					cowType = "Angus";
					break;
				case 2:
					cowType = "Brangus";
					break;
				case 3:
					cowType = "Charolais";
					break;
				case 4:
					cowType = "Hereford";
					break;
				case 5:
					cowType = "Holstein Friesian";
					break;
				case 6:
					cowType = "Shorthorn";
					break;
			}

			// Create new instance with random selection, then returning the cow instance
			Cow cow = new Cow(cowType + " - Breed", Random.Range(1, 15), cowType, Random.Range(1, 10), Random.Range(5, 100), true, true, Random.Range(150, 400), GameController.GetSceneName());
			return cow;
		}
	}
}