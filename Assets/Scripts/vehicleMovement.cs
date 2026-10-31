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

    private Vector3 startingPosition;

    void Start()
    {
        // Save the starting position when the game begins
        startingPosition = transform.position;
    }

    void Update()
    {
        // Determine movement direction
        Vector3 direction = useLocalForward ? transform.forward : Vector3.forward;

        // Move the vehicle forward over time
        transform.position += direction * moveSpeed * Time.deltaTime;

        // Calculate the current distance traveled from the start point
        float distanceTraveled = Vector3.Distance(startingPosition, transform.position);

        // Check if the vehicle has reached or exceeded the max distance
        if (distanceTraveled >= maxDistance)
        {
            ResetVehiclePosition();
        }
    }

    void ResetVehiclePosition()
    {
        // Reset position back to the start
        transform.position = startingPosition;
    }
}