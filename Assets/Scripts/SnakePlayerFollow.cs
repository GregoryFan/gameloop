using UnityEngine;

public class SnakePlayerFollow : MonoBehaviour
{
    [SerializeField] SnakeGrow growScript;
    [SerializeField] float speed;
    [SerializeField] float rageMult = 2;
    [SerializeField] float rotSpeed;
    [SerializeField] float rotating;
    [SerializeField] float desiredMouseOffset = 2f;

    [SerializeField] float prefillRadius = 2f;
    [SerializeField] int prefillRotations = 2;

    Camera mainCamera;

    Vector3 debugTargetPos;

    void Start()
    {
        mainCamera = Camera.main;
        growScript = GetComponent<SnakeGrow>();
        PrefillHistoryWithCircularMotion();
        growScript.AttachTail();
    }

    void Update()
    {
        FollowPlayer();
    }

    void FollowPlayer()
    {
        float eSpeed = speed;
        float eRotSpeed = rotSpeed;
        if (growScript.raging)
        {
            eSpeed *= rageMult;
            eRotSpeed *= rageMult;
        }

        // Get mouse position in world space
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // Direction from snake to mouse
        Vector3 rawDirection = mouseWorldPos - transform.position;
        Vector3 direction = rawDirection.normalized;

        // Create a fake "target" point in that direction, offset away from the mouse
        Vector3 targetPos = mouseWorldPos - direction * desiredMouseOffset;
        debugTargetPos = targetPos;

        // Compute direction toward that target point
        Vector3 toTarget = (targetPos - transform.position).normalized;

        // Move forward in current facing direction
        float currentAngle = transform.eulerAngles.z * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle));
        transform.position += eSpeed * Time.deltaTime * forward;

        // Rotate toward target point (not directly to mouse)
        float angle = Mathf.Atan2(toTarget.y, toTarget.x) * Mathf.Rad2Deg;
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, eRotSpeed * Time.deltaTime);
    }

    void PrefillHistoryWithCircularMotion()
    {
        Vector3 center = transform.position;
        float circumference = 2 * Mathf.PI * prefillRadius;
        float totalDistance = circumference * prefillRotations;
        int sampleCount = Mathf.CeilToInt(totalDistance / (speed * growScript.sampleInterval));

        for (int i = 0; i < sampleCount; i++)
        {
            float angle = (i / (float)sampleCount) * (Mathf.PI * 2f * prefillRotations);
            float x = Mathf.Cos(angle) * prefillRadius;
            float y = Mathf.Sin(angle) * prefillRadius;
            Vector3 pos = center + new Vector3(x, y, 0f);

            // Tangent direction
            Vector2 tangent = new Vector2(-Mathf.Sin(angle), Mathf.Cos(angle));
            float angleDeg = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
            Quaternion rot = Quaternion.Euler(0f, 0f, angleDeg);

            growScript.history.Add(new SnakeHistoryEntry(pos, rot));
        }

        // Set head position and rotation to match last entry
        SnakeHistoryEntry latest = growScript.history[growScript.history.Count - 1];
        transform.position = latest.position;
        transform.rotation = latest.rotation;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(debugTargetPos, 2f); // Adjust size if needed
    }
}


