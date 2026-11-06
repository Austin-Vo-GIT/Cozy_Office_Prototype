using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class CozyDeskOrganizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CozyDeskSeat deskSeat;
    [SerializeField] private Camera gameCamera;

    [Tooltip("Only items in this list can be straightened at this desk.")]
    [SerializeField] private CozyDeskItem[] deskItems;

    [Header("Interaction")]
    [Min(0.1f)]
    [SerializeField] private float interactionRange = 3f;

    private CozyDeskItem hoveredItem;

    private void Start()
    {
        if (deskSeat == null || gameCamera == null)
        {
            Debug.LogError(
                "CozyDeskOrganizer: Assign Desk Seat and Game Camera.",
                this
            );

            enabled = false;
        }
    }

    private void LateUpdate()
    {
        hoveredItem = null;

        if (!Application.isFocused ||
            deskSeat == null ||
            !deskSeat.CanInteractWithDesk)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        if (mouse == null) return;

        // Explicitly block clicks while L is held.
        if (keyboard != null && keyboard.lKey.isPressed) return;

        if (Cursor.lockState == CursorLockMode.Locked) return;

        Vector2 mousePosition = mouse.position.ReadValue();

        if (!gameCamera.pixelRect.Contains(mousePosition)) return;

        Ray ray = gameCamera.ScreenPointToRay(
            new Vector3(mousePosition.x, mousePosition.y, 0f)
        );

        // The first solid collider blocks the ray.
        // This prevents selecting an item through another object.
        // Ignore the player's colliders when selecting desk objects.
    int deskSelectionMask =
    Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");

    if (!Physics.Raycast(
    ray,
    out RaycastHit hit,
    interactionRange,
    deskSelectionMask,
    QueryTriggerInteraction.Ignore))
{
    return;
}

        CozyDeskItem item =
            hit.collider.GetComponentInParent<CozyDeskItem>();
        if (keyboard != null && keyboard.pKey.wasPressedThisFrame)
{
    Debug.Log(
        $"Desk hit: {hit.collider.name} | " +
        $"Item: {(item != null ? item.ItemName : "NONE")} | " +
        $"Can straighten: {(item != null && item.CanStraighten)} | " +
        $"Registered: {(item != null && BelongsToThisDesk(item))}",
        hit.collider
    );
}
        if (item == null ||
            !item.CanStraighten ||
            !BelongsToThisDesk(item))
        {
            return;
        }

        hoveredItem = item;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            hoveredItem.Straighten();
            hoveredItem = null;
        }
    }

    private bool BelongsToThisDesk(CozyDeskItem item)
    {
        if (deskItems == null) return false;

        foreach (CozyDeskItem deskItem in deskItems)
        {
            if (deskItem == item) return true;
        }

        return false;
    }

    private void OnDisable()
    {
        hoveredItem = null;
    }

    private void OnGUI()
    {
        if (hoveredItem == null ||
            !hoveredItem.CanStraighten ||
            deskSeat == null ||
            !deskSeat.CanInteractWithDesk)
        {
            return;
        }

        Matrix4x4 previousMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(
                Screen.width / 1280f,
                Screen.height / 720f,
                1f
            )
        );

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };

        Rect promptRect = new Rect(440, 495, 400, 70);

        GUI.Box(promptRect, GUIContent.none);

        GUI.Label(
            promptRect,
           "Click to "
           + hoveredItem.ActionText.ToLowerInvariant()
           + "\n"
           + hoveredItem.ItemName,
            textStyle
        );

        GUI.matrix = previousMatrix;
    }
}