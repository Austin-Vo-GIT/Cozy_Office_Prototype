using UnityEngine;

public class CozyCameraFollow : MonoBehaviour
{
    public Transform target;

    [Min(0.01f)]
    public float smoothTime = 0.15f;

    private Vector3 offset;
    private Vector3 velocity;
    private float cameraHeight;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("Assign the Player to the camera's Target.");
            enabled = false;
            return;
        }

        // Remember the camera position you chose in the editor.
        offset = transform.position - target.position;
        cameraHeight = transform.position.y;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = target.position + offset;

        // Follow across the office without bobbing up and down.
        desiredPosition.y = cameraHeight;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothTime
        );
    }
}