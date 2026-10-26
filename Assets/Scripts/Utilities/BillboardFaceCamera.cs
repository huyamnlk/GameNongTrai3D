using UnityEngine;

namespace IrishFarmSim
{
    /// <summary>
    /// Utility to keep 3D billboards facing the active main camera.
    /// Standalone MonoBehaviour file to prevent missing script serialization errors in prefabs.
    /// </summary>
    [ExecuteAlways]
    public class BillboardFaceCamera : MonoBehaviour
    {
        private Camera targetCamera;

        void LateUpdate()
        {
            if (targetCamera == null || !targetCamera.isActiveAndEnabled)
            {
                targetCamera = Camera.main;
            }

            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation;
            }
        }
    }
}
