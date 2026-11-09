using UnityEngine;
using UnityEngine.InputSystem;

public class CozyCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;

    [Min(0.01f)]
    public float smoothTime = 0.15f;

    [Header("Four-Angle Rotation")]
    [Tooltip("Camera rotation speed in degrees per second.")]
    [Min(1f)]
    public float rotationSpeed = 180f;

    private Vector3 originalOffset;
    private Quaternion originalRotation;

    private Vector3 followPosition;
    private Vector3 followVelocity;
    private float cameraHeight;

    private float currentAngle;
    private float desiredAngle;

    private bool initialized;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning(
                "Assign the Player to the camera's Target.",
                this
            );

            enabled = false;
            return;
        }

        // Preserve the camera framing you chose in the editor.
        originalOffset = transform.position - target.position;
        originalOffset.y = 0f;

        originalRotation = transform.rotation;
        cameraHeight = transform.position.y;

        followPosition = new Vector3(
            target.position.x,
            cameraHeight,
            target.position.z
        );

        currentAngle = 0f;
        desiredAngle = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (!initialized ||
            target == null ||
            !Application.isFocused ||
            Time.timeScale == 0f)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // Finish the current turn before accepting another.
        if (Mathf.Abs(desiredAngle - currentAngle) > 0.01f)
        {
            return;
        }

        bool turnLeft = keyboard.qKey.wasPressedThisFrame;
        bool turnRight = keyboard.eKey.wasPressedThisFrame;

        // Ignore simultaneous Q and E presses.
        if (turnLeft == turnRight) return;

        if (turnLeft)
        {
            desiredAngle -= 90f;
        }
        else
        {
            desiredAngle += 90f;
        }
    }

    private void LateUpdate()
    {
        if (!initialized || target == null) return;

        // Follow horizontally without reacting to player height changes.
        Vector3 desiredFollowPosition = new Vector3(
            target.position.x,
            cameraHeight,
            target.position.z
        );

        followPosition = Vector3.SmoothDamp(
            followPosition,
            desiredFollowPosition,
            ref followVelocity,
            smoothTime
        );

        currentAngle = Mathf.MoveTowards(
            currentAngle,
            desiredAngle,
            rotationSpeed * Time.deltaTime
        );

        // Keep angles bounded after completing a turn.
        if (Mathf.Abs(desiredAngle - currentAngle) < 0.01f)
        {
            currentAngle = Mathf.Repeat(desiredAngle, 360f);
            desiredAngle = currentAngle;
        }

        Quaternion orbitRotation = Quaternion.Euler(
            0f,
            currentAngle,
            0f
        );

        Vector3 rotatedOffset = orbitRotation * originalOffset;

        transform.SetPositionAndRotation(
            followPosition + rotatedOffset,
            orbitRotation * originalRotation
        );
    }

    private void OnDisable()
    {
        // Prevent leftover follow motion when returning from the desk.
        followVelocity = Vector3.zero;
    }
}