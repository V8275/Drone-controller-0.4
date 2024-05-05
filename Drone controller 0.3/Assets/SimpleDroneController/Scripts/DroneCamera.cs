using UnityEngine;


namespace SimpleDroneController
{
    public class DroneCamera : MonoBehaviour
    {
        public enum Type
        {
            ThirdPerson,
            FirstPerson,
        }

        public KeyCode firstPersonKey = KeyCode.Alpha1;
        public KeyCode thirdPersonKey = KeyCode.Alpha2;

        public GameObject thirdPersonCamera;
        public GameObject firstPersonCamera;


        public Type currentCameraType = Type.ThirdPerson;


        private void Start()
        {
            SetDroneCamera(currentCameraType);
        }

        private void Update()
        {
            CheckForCameraChange();
        }

        private void CheckForCameraChange()
        {
            if (Input.GetKeyDown(firstPersonKey) && currentCameraType != Type.FirstPerson)
            {
                SetDroneCamera(Type.FirstPerson);
            }


            if (Input.GetKeyDown(thirdPersonKey) && currentCameraType != Type.ThirdPerson)
            {
                SetDroneCamera(Type.ThirdPerson);
            }
        }

        private void SetDroneCamera(Type p_type)
        {
            if (thirdPersonCamera != null)
                thirdPersonCamera.SetActive(false);

            if (firstPersonCamera != null)
                firstPersonCamera.SetActive(false);

            if (p_type == Type.ThirdPerson && thirdPersonCamera != null)
                thirdPersonCamera.SetActive(true);

            if (p_type == Type.FirstPerson && firstPersonCamera != null)
                firstPersonCamera.SetActive(true);

            currentCameraType = p_type;
        }

    }
}

