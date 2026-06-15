using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    [Header("Move")]
    [SerializeField] private float moveSpeed = 8f; //移动速度

    [Header("Look")]
    [SerializeField] private float mouseSensitivity = 2.5f; //鼠标灵敏度
    [SerializeField] private float minPitch = 15f; //俯仰角最小值
    [SerializeField] private float maxPitch = 75f; //俯仰角最大值

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 8f; //缩放速度
    [SerializeField] private float minDistance = 6f; //最小距离
    [SerializeField] private float maxDistance = 22f; //最大距离

    [SerializeField] private Vector3 moveCenter = Vector3.zero; //移动中心点，摄像机围绕这个点旋转和缩放

    private float yaw; //偏航角（水平旋转）
    private float pitch = 45f; //俯仰角（垂直旋转），初始值为45度
    private float distance = 14f; //当前距离，初始值为14，确保在minDistance和maxDistance之间
    private bool isRotating; //是否正在按住右键旋转

    private void Start()
    {
        Vector3 euler = transform.rotation.eulerAngles;
        yaw = euler.y;

        if (euler.x > 180f)
        {
            pitch = euler.x - 360f;
        }
        else
        {
            pitch = euler.x;
        }

        distance = Vector3.Distance(transform.position, moveCenter); //根据初始位置计算距离
        distance = Mathf.Clamp(distance, minDistance, maxDistance); //确保距离在允许范围内

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        UpdateCursorState();
        HandleMouseLook();
        HandleMove();
        HandleZoom();
        ApplyCameraTransform();
    }

    private void UpdateCursorState() //根据右键状态控制光标显示与锁定
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleMouseLook() //处理鼠标旋转
    {
        if (!isRotating)
        {
            return;
        }

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivity;
        pitch -= mouseY * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleMove() //处理键盘移动 
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 forward = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
        Vector3 right = Quaternion.Euler(0f, yaw, 0f) * Vector3.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 move = (right * horizontal + forward * vertical) * moveSpeed * Time.deltaTime;
        moveCenter += move;
    }

    private void HandleZoom() //处理鼠标滚轮缩放
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance -= scroll * zoomSpeed;
        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void ApplyCameraTransform() //应用摄像机变换
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -distance);

        transform.rotation = rotation;
        transform.position = moveCenter + offset;
    }
}
