using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
	public class FarmTransition : MonoBehaviour
	{
		void FixedUpdate()
		{
			// Do not rotate
		}

		void OnTriggerEnter(Collider other) 
		{
			if (other.gameObject.name.Equals ("Player"))
			{
				GameController.Instance().farmSceneTransitionUI = true;
			}
		}

		void OnTriggerExit(Collider other)
		{
			if (other.gameObject.name.Equals ("Player"))
			{
				GameController.Instance().farmSceneTransitionUI = false;
			}
		}
	}
}