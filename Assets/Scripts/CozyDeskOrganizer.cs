using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class CozyDeskOrganizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CozyDeskSeat deskSeat;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private CozyDeskItem[] deskItems;
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    private CozyDeskItem hoveredItem;

    private void Start()
    {
        if (deskSeat == null || gameCamera == null)
        {
            Debug.LogError("CozyDeskOrganizer: Assign Desk Seat and Game Camera.", this);
            enabled = false;
        }
    }

    private void LateUpdate()
    {
        hoveredItem = null;
        if (CozyGameMenu.BlocksGameplay || !Application.isFocused ||
            deskSeat == null || !deskSeat.CanInteractWithDesk) return;
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (mouse == null || (keyboard != null && keyboard.lKey.isPressed)) return;
        if (Cursor.lockState == CursorLockMode.Locked) return;
        Vector2 position = mouse.position.ReadValue();
        if (!gameCamera.pixelRect.Contains(position)) return;
        Ray ray = gameCamera.ScreenPointToRay(new Vector3(position.x, position.y, 0f));
        // Preserve the fix for the invisible player blocking the drawer.
        int mask = Physics.DefaultRaycastLayers & ~LayerMask.GetMask("Player");
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionRange, mask,
            QueryTriggerInteraction.Ignore)) return;
        CozyDeskItem item = hit.collider.GetComponentInParent<CozyDeskItem>();
        if (item == null || !item.CanStraighten || !BelongsToThisDesk(item)) return;
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
            if (deskItem == item) return true;
        return false;
    }

    private void OnDisable() { hoveredItem = null; }

    private void OnGUI()
    {
        if (CozyGameMenu.BlocksGameplay || hoveredItem == null ||
            !hoveredItem.CanStraighten || deskSeat == null || !deskSeat.CanInteractWithDesk) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUIStyle style = new GUIStyle(GUI.skin.label)
            { fontSize = 22, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        Rect rect = new Rect(440, 495, 400, 70);
        GUI.Box(rect, GUIContent.none);
        GUI.Label(rect, "Click to " + hoveredItem.ActionText.ToLowerInvariant() +
            "\n" + hoveredItem.ItemName, style);
        GUI.matrix = previousMatrix;
    }
}
