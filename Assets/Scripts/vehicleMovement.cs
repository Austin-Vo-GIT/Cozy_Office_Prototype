using UnityEngine;

public class vehicleMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The speed at which the vehicle moves.")]
    public float moveSpeed = 10f;

    [Tooltip("The maximum distance the vehicle can travel before resetting.")]
    public float maxDistance = 50f;

    [Header("Direction Settings")]
    [Tooltip("Check this to move along the vehicle's local forward axis, or uncheck to move along world Z.")]
    public bool useLocalForward = true;

    [Header("Ground Snapping Settings")]
    [Tooltip("Assign your terrain or ground layer here so the raycast knows what to hit.")]
    public LayerMask groundLayer;

    [Tooltip("Height offset to keep the car's wheels sitting right on the ground.")]
    public float heightOffset = 0.5f;

    [Tooltip("How far above the car the raycast starts checking downwards.")]
    public float raycastCheckDistance = 2f;

    [Tooltip("Check this if you want the car to tilt and match the angle of the slope.")]
    public bool alignToSlope = true;

    [Tooltip("How fast the car smooths its tilt when changing slopes (higher = faster).")]
    public float rotationSmoothSpeed = 15f;

    private Vector3 startingPosition;

    void Start()
    {
        // Save the starting position when the game begins
        startingPosition = transform.position;
        SnapToGround();
    }

    void Update()
    {
        // Determine movement direction (ignoring Y so it moves flat across the world first)
        Vector3 direction = useLocalForward ? transform.forward : Vector3.forward;
        direction.y = 0f;
        direction.Normalize();

        // Move the vehicle forward over time
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Snap to the ground/slope and smooth the rotation after moving
        SnapToGround();

        // Calculate the current distance traveled from the start point
        float distanceTraveled = Vector3.Distance(startingPosition, transform.position);

        // Check if the vehicle has reached or exceeded the max distance
        if (distanceTraveled >= maxDistance)
        {
            ResetVehiclePosition();
        }
    }

    void SnapToGround()
    {
        // Cast a ray from slightly above the car pointing straight down
        Vector3 rayStart = transform.position + (Vector3.up * raycastCheckDistance);

        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, raycastCheckDistance * 2f, groundLayer))
        {
            // Smoothly snap the Y position to the ground hit point plus the offset
            float targetY = hit.point.y + heightOffset;
            float newY = Mathf.Lerp(transform.position.y, targetY, Time.deltaTime * rotationSmoothSpeed);
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);

            // Smoothly rotate the car to match the slope angle (prevents popping)
            if (alignToSlope)
            {
                Vector3 forward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
                if (forward != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(forward, hit.normal);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
                }
            }
        }
    }

    void ResetVehiclePosition()
    {
        // Reset position back to the start and re-snap to ground
        transform.position = startingPosition;
        SnapToGround();
    }
}