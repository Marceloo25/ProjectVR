using UnityEngine;

public class DebugPlayer : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float sprintMultiplier = 2f;

    [Header("Look")]
    [SerializeField] float lookSensitivity = 2f;
    [SerializeField] Transform cameraTransform;

    float m_Pitch = 0f;

    void Start()
    {
        if (cameraTransform == null)
        {
            var cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else cameraTransform = transform.Find("MainCamera");
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleLook();
        HandleMove();
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void HandleLook()
    {
        if (cameraTransform == null) return;

        float mx = Input.GetAxis("Mouse X") * lookSensitivity;
        float my = Input.GetAxis("Mouse Y") * lookSensitivity;

        // Yaw the player body
        transform.Rotate(Vector3.up, mx, Space.Self);

        // Pitch the camera (clamped)
        m_Pitch -= my;
        m_Pitch = Mathf.Clamp(m_Pitch, -89f, 89f);
        cameraTransform.localEulerAngles = new Vector3(m_Pitch, 0f, 0f);
    }

    void HandleMove()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f)) return;

        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
        }
        else
        {
            forward = transform.forward;
            right = transform.right;
        }

        Vector3 dir = (forward * v + right * h).normalized;
        float speed = moveSpeed * (sprint ? sprintMultiplier : 1f);
        transform.position += dir * speed * Time.deltaTime;
    }
}
