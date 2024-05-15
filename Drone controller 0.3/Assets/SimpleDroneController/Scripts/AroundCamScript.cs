using UnityEngine;

public class AroundCamScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform turretHor, turretVert;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool blockRotate = false, blockMouse = false;

    private float yRotation = 0f;
    private float xRotation = 0f;

    private void Start()
    {
        yRotation = transform.eulerAngles.y;
        xRotation = transform.eulerAngles.x;

        if (blockMouse)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void LateUpdate()
    {
        Vector3 newPosition = target.position + Quaternion.Euler(xRotation, yRotation, 0f) * new Vector3(0f, height, -distance);
        transform.position = newPosition;

        transform.LookAt(target);

        // ѕоворачиваем турель в точку центра камеры
        RotateTurretToCamera(turretHor, turretVert);
    }

    private void Update()
    {
        if (Input.GetMouseButton(0) || !blockRotate)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yRotation += mouseX * rotationSpeed;
            xRotation -= mouseY * rotationSpeed;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        }
    }

    private void RotateTurretToCamera(Transform turretHor, Transform turretVert)
    {
        RaycastHit hit;
        if (Physics.Raycast(transform.position, transform.forward, out hit))
        {
            if (hit.transform != null)
            {
                Vector3 targetPosition = hit.point;

                // ¬ычисл€ем новый поворот дл€ турели
                Vector3 targetDirection = targetPosition - turretHor.position;
                Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
                Quaternion targetVertRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

                // ѕоворачиваем турель без вращени€ вокруг оси Z
                turretHor.rotation = Quaternion.Euler(targetVertRotation.eulerAngles.x, targetRotation.eulerAngles.y, 0f);
                Debug.DrawRay(turretHor.position, turretHor.forward * hit.distance, Color.red);
                //turretVert.rotation = Quaternion.Euler(targetVertRotation.eulerAngles.x, 0f, 0f);
            }
        }
    }
}
