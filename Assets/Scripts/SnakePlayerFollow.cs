using UnityEngine;

public class SnakePlayerFollow : MonoBehaviour
{
    [SerializeField] float speed;
    [SerializeField] float rotSpeed;

    Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        // Get mouse position in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Calculate direction
        Vector3 direction = (mouseWorldPos - transform.position).normalized;

        // Move forward depending on rotation
        float currentAngle = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector3 dir = new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
        transform.position += speed * Time.deltaTime * dir;


        // Rotate toward the mouse
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotSpeed * Time.deltaTime);
    }
}
