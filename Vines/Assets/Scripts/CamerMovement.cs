using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float lookSensitivity = 2f;

    private float yaw;
    private float pitch;

    void Start()
    {
        // Initialize yaw and pitch with the current camera rotation.
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
    }

    void Update()
    {
        // Rotate the camera when right mouse button is held
        if (Input.GetMouseButton(1))
        {
            yaw += lookSensitivity * Input.GetAxis("Mouse X");
            pitch -= lookSensitivity * Input.GetAxis("Mouse Y");

            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.eulerAngles = new Vector3(pitch, yaw, 0f);
        }

        // Movement input (WASD + E/Q for up/down)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        float ascendDescend = 0f;
        if (Input.GetKey(KeyCode.E)) ascendDescend = 1f;
        if (Input.GetKey(KeyCode.Q)) ascendDescend = -1f;

        Vector3 direction = (transform.forward * vertical) + (transform.right * horizontal) + (Vector3.up * ascendDescend);
        transform.position += direction * movementSpeed * Time.deltaTime;

        // Clamp the position so the camera stays within the bounds
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, -25, 25f);
        pos.z = Mathf.Clamp(pos.z, -38, 38f);
        pos.y = Mathf.Clamp(pos.y, 0.5f, 40f);  // Allow vertical movement between 4 and 8
        transform.position = pos;

    }
}
