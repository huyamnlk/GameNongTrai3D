using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
	public class MartTransition : MonoBehaviour
	{
		void FixedUpdate() 
		{
			// Do not rotate
		}

		void OnTriggerEnter(Collider other) 
		{
			if (other.gameObject.name.Equals("Player"))
			{
				GameController.Instance().martSceneTransitionUI = true;
			}
		}
		
		void OnTriggerExit(Collider other)
		{
			if (other.gameObject.name.Equals("Player"))
			{
				GameController.Instance().martSceneTransitionUI = false;
			}
		}
	}
}