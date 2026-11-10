using UnityEngine;
using UnityEngine.InputSystem;

public class CozyCameraFollow : MonoBehaviour
{
    [Header("Follow")]
    public Transform target;
    [Min(0.01f)] public float smoothTime = 0.15f;
    [Header("Four-Angle Rotation")]
    [Min(1f)] public float rotationSpeed = 180f;
    private Vector3 originalOffset;
    private Quaternion originalRotation;
    private Vector3 followPosition, followVelocity;
    private float cameraHeight, currentAngle, desiredAngle;
    private bool initialized;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Assign the Player to the camera's Target.", this);
            enabled = false;
            return;
        }
        originalOffset = transform.position - target.position;
        originalOffset.y = 0f;
        originalRotation = transform.rotation;
        cameraHeight = transform.position.y;
        followPosition = new Vector3(target.position.x, cameraHeight, target.position.z);
        currentAngle = 0f;
        desiredAngle = 0f;
        initialized = true;
    }

    private void Update()
    {
        if (CozyGameMenu.BlocksGameplay || !initialized || target == null ||
            !Application.isFocused || Time.timeScale == 0f) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || Mathf.Abs(desiredAngle - currentAngle) > 0.01f) return;
        bool left = keyboard.qKey.wasPressedThisFrame;
        bool right = keyboard.eKey.wasPressedThisFrame;
        if (left == right) return;
        desiredAngle += left ? -90f : 90f;
    }

    private void LateUpdate()
    {
        if (!initialized || target == null) return;
        if (CozyGameMenu.BlocksGameplay)
        {
            followVelocity = Vector3.zero;
            return;
        }
        Vector3 desiredPosition = new Vector3(target.position.x, cameraHeight, target.position.z);
        followPosition = Vector3.SmoothDamp(followPosition, desiredPosition, ref followVelocity, smoothTime);
        currentAngle = Mathf.MoveTowards(currentAngle, desiredAngle, rotationSpeed * Time.deltaTime);
        if (Mathf.Abs(desiredAngle - currentAngle) < 0.01f)
        {
            currentAngle = Mathf.Repeat(desiredAngle, 360f);
            desiredAngle = currentAngle;
        }
        Quaternion orbit = Quaternion.Euler(0f, currentAngle, 0f);
        transform.SetPositionAndRotation(followPosition + orbit * originalOffset, orbit * originalRotation);
    }

    private void OnDisable() { followVelocity = Vector3.zero; }
}
