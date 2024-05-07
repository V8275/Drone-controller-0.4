using System;
using UnityEngine;



// MADE BY VEOdev
// For Support : merwanveo1@gmail.com
// Please don't share or sell this asset 
// Feel free to use it in your game



public class TopDownCamera : MonoBehaviour
{

    // Structs
    [Serializable]
    public struct _Inputs
    {
        public KeyCode toggleFreeCam;
        public KeyCode dragToRotate;
    }
    [Serializable]
    public struct _General
    {
        public Camera mainCamera;
        public Transform target;
        [Space]
        public LockType lockType;
        public bool Lock;
        [Space]
        [Range(0, 360)] public int rotation;
        [Range(5, 1000)] public float zoom;
        [Range(0, 10)] public float height;

        public enum LockType
        {
            LockInPlace,
            MoveToTarget,
        }
    }
    [Serializable]
    public struct _TargetFollow
    {
        [Space]
        [Header("Chose a transform to be followed or to lock the camera into it")]
        [Tooltip("Check this if you are using a rigidbody movement, or moving target in FixedUpdate")]
        public bool targetUsingRigidbody;
        [Range(0f, 1f)] public float smoothness;
    }
    [Serializable]
    public struct _EdgeScrolling
    {
        [Space]
        [Header("Scroll the camera when the mouse touches the edge of the screen")]
        public bool canScroll;
        public bool stopWhenRotating;
        public enum ScrollType
        {
            Mouse,
            Keyboard,
        }
        public ScrollType scrollType;
        public float scrollSpeed;
        public float maxScrollSpeed;
        [Range(0, 20)] public float acceleration;
        [Range(0, 1f)] public float smoothness;
        [Range(1, 100)] public float edgeTickness;
        [Space]
        [Header("Limit the camera position while scrolling")]
        public Vector2 xMinMax;
        public Vector2 zMinMax;
    }
    [Serializable]
    public struct _Rotation
    {
        [Space]
        [Header("Hold rotation key and drag to rotate the camera around it self")]
        public bool canRotate;
        public float sensitivity;
        [Range(0f, 180f)] public float maxRotation;
    }
    [Serializable]
    public struct _Zoom
    {
        [Space]
        [Header("Use the mouse wheel to zoom in and out")]
        public bool canZoom;
        public float minZoom;
        public float maxZoom;
        [Range(0f, 1000f)] public float sensitivity;
        [Range(0f, 1f)] public float smoothness;
    }

    // Inspector
    public _Inputs inputs;
    public _General general;
    public _TargetFollow TargetFollow;
    public _EdgeScrolling EdgeScrolling;
    public _Rotation Rotation;
    public _Zoom Zoom;

    // Private
    #region PrivateField
    Transform directionReference;
    Vector3 currentPos;
    Vector3 refVector3;
    Vector3 scrollDirection;
    Vector3 rotation;
    float refFloat = 0;
    float currentZoom;
    float zoomAmount;
    float vertical = 0;
    float horizontal = 0;
    float baseSpeed;
    #endregion

    // Monobehaviour
    private void OnValidate()
    {
        UpdatePosition();
    }
    private void Start()
    {
        SetUp();
        directionReference = new GameObject("Camera Direction Helper").transform;
    }
    private void Update()
    {
        HandleInputs();

        if (TargetFollow.targetUsingRigidbody)
            HandleFollowing();
    }
    private void LateUpdate()
    {
        HandleZoom();
        HandleScrolling();
        HandleRotation();

        if (!TargetFollow.targetUsingRigidbody)
            HandleFollowing();
    }

    void UpdatePosition()
    {
        if (general.mainCamera)
            general.mainCamera.transform.localPosition = new Vector3(0, 0, -general.zoom);

        transform.position = new Vector3(transform.position.x, general.height, transform.position.z);
        //transform.eulerAngles = new Vector3(general.tilt, general.rotation, 0);
    }
    void SetUp()
    {
        rotation = transform.localEulerAngles;
        currentPos = transform.localPosition;
        baseSpeed = EdgeScrolling.scrollSpeed;
        currentZoom = -general.zoom;
    }

    // Handlers
    void HandleInputs()
    {
        // Toggle Scroll - Follow
        if (Input.GetKeyDown(inputs.toggleFreeCam))
            general.Lock = !general.Lock;
    }
    void HandleFollowing()
    {
        if (!general.Lock || general.lockType == _General.LockType.LockInPlace || general.target == null)
            return;

        //Vector3 targetPos = new Vector3(general.target.position.x, transform.position.y, general.target.position.z);
        Vector3 targetPos = general.target.position;
        currentPos = Vector3.SmoothDamp(currentPos, targetPos, ref refVector3, TargetFollow.smoothness);
        transform.localPosition = currentPos;
    }
    private bool rotating;
    void HandleRotation()
    {
        if (!Rotation.canRotate)
            return;

        rotating = Input.GetKey(inputs.dragToRotate) && Rotation.canRotate;

        if (!rotating)
            return;

        rotation = transform.localEulerAngles;
        rotation.y += Input.GetAxisRaw("Mouse X") * Time.deltaTime * Rotation.sensitivity;
        rotation.x += -Input.GetAxisRaw("Mouse Y") * Time.deltaTime * Rotation.sensitivity;
        rotation.x = ClampAngle(rotation.x, -90f, 90f);

        transform.localEulerAngles = rotation;
    }

    float ClampAngle(float angle, float from, float to)
    {
        if (angle < 0f)
            angle = 360 + angle;
        if (angle > 180f)
            return Mathf.Max(angle, 360 + from);
        return Mathf.Min(angle, to);
    }

    void MouseScrollInputs()
    {
        horizontal = 0;
        vertical = 0;

        // Vertical Scroll
        if (Input.mousePosition.y >= Screen.height - EdgeScrolling.edgeTickness)
            vertical = 1;
        if (Input.mousePosition.y <= EdgeScrolling.edgeTickness)
            vertical = -1;

        // Horizontal Scroll
        if (Input.mousePosition.x >= Screen.width - EdgeScrolling.edgeTickness)
            horizontal = 1;
        if (Input.mousePosition.x <= EdgeScrolling.edgeTickness)
            horizontal = -1;
    }
    void KeyboardScrollInputs()
    {
        horizontal = Input.GetAxisRaw("Horizontal");
        vertical = Input.GetAxisRaw("Vertical");
    }
    void HandleScrolling()
    {
        if (!EdgeScrolling.canScroll || general.Lock)
            return;

        if (rotating && EdgeScrolling.stopWhenRotating)
            return;

        if (EdgeScrolling.scrollType == _EdgeScrolling.ScrollType.Mouse)
        {
            MouseScrollInputs();
        }
        else
        {
            KeyboardScrollInputs();
        }

        HandleAcceleration();

        directionReference.transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        scrollDirection = new Vector3(horizontal, 0, vertical).normalized * EdgeScrolling.scrollSpeed * Time.deltaTime;
        currentPos += directionReference.transform.TransformVector(scrollDirection);

        currentPos.x = Mathf.Clamp(currentPos.x, EdgeScrolling.xMinMax.x, EdgeScrolling.xMinMax.y);
        currentPos.z = Mathf.Clamp(currentPos.z, EdgeScrolling.zMinMax.x, EdgeScrolling.zMinMax.y);

        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, currentPos, ref refVector3, EdgeScrolling.smoothness);
    }
    void HandleAcceleration()
    {
        if (scrollDirection.sqrMagnitude > 0)
        {
            if (EdgeScrolling.scrollSpeed < EdgeScrolling.maxScrollSpeed)
                EdgeScrolling.scrollSpeed += EdgeScrolling.acceleration * Time.deltaTime;
        }
        else EdgeScrolling.scrollSpeed = baseSpeed;
    }
    void HandleZoom()
    {
        if (!Zoom.canZoom)
            return;

        zoomAmount = Mathf.SmoothDamp(zoomAmount, Input.GetAxis("Mouse ScrollWheel") * Zoom.sensitivity, ref refFloat, Zoom.smoothness);

        if (zoomAmount == 0)
            return;

        currentZoom += zoomAmount;
        currentZoom = Mathf.Clamp(currentZoom, Zoom.maxZoom * -1, Zoom.minZoom * -1);
        general.mainCamera.transform.localPosition = new Vector3(0, 0, currentZoom);
    }
}


