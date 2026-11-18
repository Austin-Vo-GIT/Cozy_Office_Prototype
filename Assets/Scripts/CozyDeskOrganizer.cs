using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(100)]
public class CozyDeskOrganizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CozyDeskSeat deskSeat;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private CozyDeskItem[] deskItems;
    [Header("Rapport Reward")]
    [SerializeField] private CozyRapport rapport;
    [SerializeField, Min(1)] private int organizationReward = 10;
    private bool completionArmed;
    private bool organizationCompleted;
    public bool OrganizationCompleted => organizationCompleted;

    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionRange = 3f;
    private CozyDeskItem hoveredItem;

    private void Start()
    {
        if (deskSeat == null || gameCamera == null)
        {
            Debug.LogError("CozyDeskOrganizer: Assign Desk Seat and Game Camera.", this);
            enabled = false;
            return;
        }
        if (rapport == null)
            Debug.LogWarning("CozyDeskOrganizer: Assign Rapport to award points.", this);
        if (deskItems == null || deskItems.Length == 0)
            Debug.LogWarning("CozyDeskOrganizer: Add all desk items, including the drawer.", this);
        else
            foreach (CozyDeskItem item in deskItems)
                if (item == null)
                    Debug.LogWarning("CozyDeskOrganizer: A Desk Items entry is missing; completion is blocked.", this);
    }

    private void LateUpdate()
    {
        CheckOrganizationCompletion();
        hoveredItem = null;
        CozyFocusHighlighter.SetDeskTarget(null);
        if (PlayerWorkspaceShop.PointerOverPurchasePanel()) return;
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
        CozyFocusHighlighter.SetDeskTarget(hoveredItem != null ? hoveredItem.gameObject : null);
    }

    private void CheckOrganizationCompletion()
    {
        if (organizationCompleted || deskItems == null || deskItems.Length == 0) return;
        bool allTidy = true;
        foreach (CozyDeskItem item in deskItems)
        {
            // Missing or inactive unfinished items must never count as completed.
            if (item == null) return;
            if (!item.IsTidy) allTidy = false;
        }
        if (!allTidy)
        {
            completionArmed = true;
            return;
        }
        // An already tidy desk at scene startup does not give free points.
        if (!completionArmed || rapport == null || !rapport.isActiveAndEnabled) return;
        rapport.TryAwardOnce("desk-organization", organizationReward, "Desk organized!");
        organizationCompleted = true;
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
        string text = "Click to " + hoveredItem.ActionText.ToLowerInvariant()
            + "\n" + hoveredItem.ItemName;
        CozyOverlayGUI.Draw(text, 420f, 550f);
        GUI.matrix = previousMatrix;
    }
}
