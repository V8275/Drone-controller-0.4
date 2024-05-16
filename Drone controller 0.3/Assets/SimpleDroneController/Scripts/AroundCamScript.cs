using UnityEngine;

public class AroundCamScript : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Transform turretHor, turretVert;
    [SerializeField] private float distance = 5f;
    [SerializeField] private float height = 2f;
    [SerializeField] private float rotationSpeed = 2f;
    [SerializeField] private bool blockRotate = false, blockMouse = false;
    [SerializeField] private KeyCode ModeKey = KeyCode.C;

    [SerializeField] private int WindowOffset = 20;

    private Direct dir = Direct.None;
    private float yRotation = 0f;
    private float xRotation = 0f;
    private bool lockMode = true;
    GameObject MouseInSpace;


    private void Start()
    {
        MouseInSpace = new GameObject("MouseInSpace");

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
        if ((Input.GetMouseButton(0) || !blockRotate) && !lockMode)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            yRotation += mouseX * rotationSpeed;
            xRotation -= mouseY * rotationSpeed;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        }
        else if ((Input.GetMouseButton(0) || !blockRotate) && lockMode)
        {
            float mouseX = 0;
            float mouseY = 0;

            Vector3 mousePosition = Input.mousePosition;
            Vector2 screenSize = new Vector2(Screen.width, Screen.height);

            float distTop = Mathf.Abs(screenSize.y - mousePosition.y);
            float distBottom = Mathf.Abs(mousePosition.y);
            float distLeft = Mathf.Abs(mousePosition.x);
            float distRight = Mathf.Abs(screenSize.x - mousePosition.x);

            float minDist = Mathf.Min(distTop, distBottom, distLeft, distRight);

            if (minDist == distTop) dir = Direct.Up;
            else if (minDist == distBottom) dir = Direct.Down;
            else if (minDist == distLeft) dir = Direct.Left;
            else if (minDist == distRight) dir = Direct.Right;
            else dir = Direct.None;

            if (minDist <= WindowOffset)
            {
                switch (dir)
                {
                    case Direct.Left:
                        mouseX = -0.5f;
                        break;
                    case Direct.Right:
                        mouseX = 0.5f;
                        break;
                    case Direct.Up:
                        mouseY = 0.5f;
                        break;
                    case Direct.Down:
                        mouseY = -0.5f;
                        break;
                    case Direct.None:
                        mouseY = 0;
                        mouseX = 0;
                        break;
                    default:
                        mouseY = 0;
                        mouseX = 0;
                        break;
                }
            }

            yRotation += mouseX * rotationSpeed;
            xRotation -= mouseY * rotationSpeed;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        }
        if (Input.GetKeyDown(ModeKey))
        {
            lockMode = !lockMode;
            if (lockMode)
            {
                Cursor.lockState = CursorLockMode.None;
                distance *= 2;
            }
            else
            {
                distance /= 2;
            }
        }
    }

    private void RotateTurretToCamera(Transform turretHor, Transform turretVert)
    {
        if (!lockMode)
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
        else
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit) && hit.collider.gameObject.layer != 2)
            {
                MouseInSpace.transform.position = new Vector3(hit.point.x, hit.point.y, hit.point.z);

                turretHor.LookAt(MouseInSpace.transform);
                Debug.DrawRay(Camera.main.transform.position, -hit.point, Color.yellow);
            }
        }
    }

    enum Direct
    {
        Left,
        Right,
        Up,
        Down,
        None
    }
}
