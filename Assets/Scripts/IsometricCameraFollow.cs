using UnityEngine;

public class IsometricCameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Assign your player/character GameObject here.")]
    public Transform target;

    [Header("Isometric Offset")]
    [Tooltip("Distance from the target. Standard isometric starting point: (X: -10, Y: 10, Z: -10)")]
    public Vector3 offset = new Vector3(-10f, 10f, -10f);

    [Header("Movement Settings")]
    [Tooltip("How smoothly the camera catches up to the target. Lower values mean smoother/slower following.")]
    public float smoothSpeed = 5f;

    [Header("Camera Rotation")]
    [Tooltip("Standard true isometric angle pitch is 30 or 45 degrees, yaw is 45 degrees.")]
    public Vector3 cameraRotation = new Vector3(30f, 45f, 0f);

    private void Start()
    {
        // Apply the fixed isometric rotation to the camera on start
        transform.rotation = Quaternion.Euler(cameraRotation);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Calculate the desired target position based on the offset
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate between current position and desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Update camera position
        transform.position = smoothedPosition;
    }
}