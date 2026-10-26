using UnityEngine;
using System.Collections;

namespace IrishFarmSim
{
	/// <summary>
	/// Object trigger cũ mang tên 'Supplies' (chứa model con bò xoay liên tục).
	/// Đã bị loại bỏ hoàn toàn do giao diện Supplies cũ được thay thế bằng StorageUI.
	/// Tự hủy an toàn khi được load vào scene để không còn con bò xoay vô dụng.
	/// </summary>
	public class Supplies : MonoBehaviour
	{
		void Awake()
		{
			if (Application.isPlaying)
			{
				Destroy(gameObject);
			}
			else
			{
				DestroyImmediate(gameObject);
			}
		}

		void Start()
		{
			if (Application.isPlaying)
			{
				Destroy(gameObject);
			}
			else
			{
				DestroyImmediate(gameObject);
			}
		}

		void FixedUpdate() 
		{
			// Không xoay
		}

		void OnTriggerEnter(Collider other) 
		{
		}

		void OnTriggerExit(Collider other)
		{
		}
	}
}