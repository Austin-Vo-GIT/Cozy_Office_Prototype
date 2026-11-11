using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWorkspaceShop : MonoBehaviour
{
    [Header("Decoration Placement Audio")]
    [SerializeField] private AudioSource placementAudioSource;
    [SerializeField] private AudioClip placementSound;
    [SerializeField, Range(0f, 1f)] private float placementVolume = 0.4f;
    [Header("Reward Audio")]
    [SerializeField] private AudioSource rewardAudioSource;
    [SerializeField] private AudioClip rewardSound;
    [SerializeField, Range(0f, 1f)] private float rewardVolume = 0.5f;
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform deskInteractionPoint;
    [SerializeField] private GameObject decoration;
    [Header("Shop Settings")]
    [SerializeField] private string decorationName = "Desk decoration";
    [SerializeField, Min(0)] private int decorationCost = 15;
    [SerializeField, Min(0)] private int taskReward = 25;
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;

    [Header("Reward Popup")]
    [SerializeField] private CozyRewardPopup rewardPopup;

    private void Awake()
    {
        if (rewardPopup == null)
            rewardPopup = GetComponent<CozyRewardPopup>();
        if (rewardPopup == null)
            rewardPopup = gameObject.AddComponent<CozyRewardPopup>();
    }

    [System.Serializable]
    public class ShopItem
    {
        public string itemName;
        [Min(0)] public int price;
        public GameObject placedObject;
        [System.NonSerialized] public bool purchased;
        public ShopItem(string name, int cost) { itemName = name; price = cost; }
    }

    [Header("Decoration Catalog")]
    [SerializeField] private ShopItem[] items =
    {
        new ShopItem("Pendulum", 15),
        new ShopItem("Wall picture", 12),
        new ShopItem("Wall calendar", 8),
        new ShopItem("Desk trophy", 25),
        new ShopItem("Stapler", 5),
        new ShopItem("Coffee mug", 10)
    };

    private static PlayerWorkspaceShop activeShop;
    private static int panelClickFrame = -1;
    private bool shopExpanded;
    private static readonly Rect BuyButtonRect = new Rect(1040f, 654f, 220f, 46f);
    private static readonly Rect ShopRect = new Rect(810f, 326f, 450f, 374f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetShopStatics()
    {
        activeShop = null;
        panelClickFrame = -1;
    }

    public int PurchasedCount
    {
        get
        {
            int count = 0;
            if (items != null)
                foreach (ShopItem item in items)
                    if (item != null && item.purchased) count++;
            return count;
        }
    }

    private bool HasAffordableItem()
    {
        if (items == null) return false;
        foreach (ShopItem item in items)
            if (item != null && item.placedObject != null && !item.purchased
                && balance >= Mathf.Max(0, item.price)) return true;
        return false;
    }

    private bool ShowShop => initialized && isActiveAndEnabled
        && !CozyGameMenu.BlocksGameplay && HasAffordableItem()
        && Cursor.lockState != CursorLockMode.Locked
        && !(Keyboard.current != null && Keyboard.current.lKey.isPressed);

    // Prevent a shop click from also straightening an object behind the panel.
    public static bool PointerOverPurchasePanel()
    {
        if (panelClickFrame == Time.frameCount) return true;
        if (activeShop == null || !activeShop.ShowShop || Mouse.current == null)
            return false;
        Vector2 mouse = Mouse.current.position.ReadValue();
        Vector2 gui = new Vector2(mouse.x * 1280f / Screen.width,
            (Screen.height - mouse.y) * 720f / Screen.height);
        return (activeShop.shopExpanded ? ShopRect : BuyButtonRect).Contains(gui);
    }

    private int balance;
    private bool rewardReceived;
    private bool inboxRewardReceived;
    private bool decorationPurchased;
    private bool initialized;
    private string notification = "";

    public int Balance => balance;
    public bool InboxCompleted => inboxRewardReceived;
    public bool DecorationPurchased => decorationPurchased;
    public string DecorationName => decorationName;
    public int DecorationCost => decorationCost;
    public string LatestNotification => notification;
    public bool CanPurchaseHere => initialized && isActiveAndEnabled && IsAtDesk();

    private void Start()
    {
        activeShop = this;
        if (items == null) items = new ShopItem[0];
        var seen = new System.Collections.Generic.HashSet<GameObject>();
        for (int i = 0; i < items.Length; i++)
        {
            ShopItem item = items[i];
            if (item == null) continue;
            // Reuse the original pendulum assignment if the first entry is empty.
            if (i == 0 && item.placedObject == null)
            {
                item.placedObject = decoration;
                item.price = decorationCost;
            }
            if (item.placedObject == null) continue;
            if (item.placedObject == gameObject ||
                transform.IsChildOf(item.placedObject.transform) ||
                !seen.Add(item.placedObject))
            {
                Debug.LogError("Shop: Each entry needs its own decoration object, "
                    + "not PlayerWorkspace or one of its parents.", this);
                item.placedObject = null;
                continue;
            }
            item.placedObject.SetActive(false);
        }
        initialized = true;
    }

    public void GrantInboxReward()
    {
        if (inboxRewardReceived) return;
        inboxRewardReceived = true;
        balance += 15;
        if (rewardPopup != null) rewardPopup.ShowReward(15, balance);
        PlayRewardSound();
        ShowNotification("Inbox cleaned! Earned $15.");
    }

    public void GrantTaskReward()
    {
        if (rewardReceived) return;
        rewardReceived = true;
        balance += taskReward;
        if (rewardPopup != null) rewardPopup.ShowReward(taskReward, balance);
        PlayRewardSound();
        ShowNotification($"Paperwork delivered! Earned ${taskReward}.");
    }

    private void PlayRewardSound()
    {
        if (rewardAudioSource != null && rewardSound != null)
            rewardAudioSource.PlayOneShot(rewardSound, rewardVolume);
    }

    private bool IsAtDesk()
    {
        return player != null && deskInteractionPoint != null &&
            Vector3.Distance(player.position, deskInteractionPoint.position)
                <= interactionDistance;
    }

    private void Update()
    {
        if (!HasAffordableItem())
            shopExpanded = false;

        if (ShowShop && Keyboard.current != null &&
            Keyboard.current.bKey.wasPressedThisFrame)
        {
            shopExpanded = !shopExpanded;
            panelClickFrame = Time.frameCount;
        }

        // Capture the press before OnGUI can spend money and hide the panel.
        if (PointerOverPurchasePanel() && Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame)
            panelClickFrame = Time.frameCount;
    }

    // Retained for compatibility; the old B shortcut is no longer used.
    public void TryPurchaseDecoration() { TryPurchaseItem(0); }

    private void TryPurchaseItem(int index, bool fromMainMenu = false)
    {
        if (!initialized || !gameObject.activeInHierarchy) return;
        if (fromMainMenu)
        {
            if (!CozyGameMenu.IsOpen) return;
        }
        else if (!ShowShop || !shopExpanded) return;
        if (items == null || index < 0 || index >= items.Length) return;
        ShopItem item = items[index];
        if (item == null || item.purchased || item.placedObject == null) return;
        int cost = Mathf.Max(0, item.price);
        if (balance < cost) return;

        balance -= cost;
        item.purchased = true;
        if (index == 0) decorationPurchased = true;
        item.placedObject.SetActive(true);
        if (placementAudioSource != null && placementSound != null)
            placementAudioSource.PlayOneShot(placementSound, placementVolume);
        ShowNotification(item.itemName + " placed in your cubicle!");
        if (!HasAffordableItem()) shopExpanded = false;
        panelClickFrame = Time.frameCount;
    }

    // Called by the main menu even when computer mode disables this component.
    // The caller owns the GUI matrix, surrounding panel and scroll view.
    public void DrawMainMenuCatalog(GUIStyle label, GUIStyle button)
    {
        GUILayout.Label($"Balance: ${balance}", label);
        GUILayout.Space(12);
        if (!initialized)
        {
            GUILayout.Label("Shop is loading...", label);
            return;
        }
        if (items == null || items.Length == 0)
        {
            GUILayout.Label("No decorations are available yet.", label);
            return;
        }

        bool previousEnabled = GUI.enabled;
        for (int i = 0; i < items.Length; i++)
        {
            ShopItem item = items[i];
            if (item == null) continue;
            int cost = Mathf.Max(0, item.price);
            GUILayout.BeginHorizontal(GUI.skin.box);
            GUILayout.Label($"{item.itemName} — ${cost}", label,
                GUILayout.MinHeight(44), GUILayout.ExpandWidth(true));

            bool available = item.placedObject != null && !item.purchased;
            GUI.enabled = previousEnabled && available && balance >= cost;
            string caption = item.purchased ? "Owned"
                : item.placedObject == null ? "Not set"
                : balance < cost ? "Save up" : "Buy";
            if (GUILayout.Button(caption, button,
                GUILayout.Width(130), GUILayout.Height(44)))
                TryPurchaseItem(i, true);
            GUI.enabled = previousEnabled;
            GUILayout.EndHorizontal();
            GUILayout.Space(6);
        }
    }

    private Vector2 shopScroll;

    private void OnGUI()
    {
        if (!ShowShop) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        bool previousEnabled = GUI.enabled;
        int previousDepth = GUI.depth;
        try
        {
            GUI.depth = -50;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUIStyle label = new GUIStyle(GUI.skin.label)
                { fontSize = 19, wordWrap = true };
            GUIStyle button = new GUIStyle(GUI.skin.button) { fontSize = 18 };
            if (!shopExpanded)
            {
                if (GUI.Button(BuyButtonRect, "B: Buy Items", button))
                {
                    shopExpanded = true;
                    panelClickFrame = Time.frameCount;
                }
                return;
            }

            GUI.Box(ShopRect, GUIContent.none);
            GUI.Label(new Rect(826, 338, 418, 30),
                $"CUBICLE SHOP    Balance: ${balance}", label);
            Rect viewport = new Rect(826, 378, 418, 264);
            shopScroll = GUI.BeginScrollView(viewport, shopScroll,
                new Rect(0, 0, 394, Mathf.Max(264, items.Length * 48)));
            for (int i = 0; i < items.Length; i++)
            {
                ShopItem item = items[i];
                if (item == null) continue;
                float y = i * 48f;
                GUI.Label(new Rect(0, y + 3, 266, 44),
                    $"{item.itemName} — ${Mathf.Max(0, item.price)}", label);
                bool available = item.placedObject != null && !item.purchased;
                GUI.enabled = previousEnabled && available && balance >= Mathf.Max(0, item.price);
                string caption = item.purchased ? "Owned"
                    : item.placedObject == null ? "Not set"
                    : balance < Mathf.Max(0, item.price) ? "Save up" : "Buy";
                if (GUI.Button(new Rect(274, y, 112, 38), caption, button))
                    TryPurchaseItem(i);
                GUI.enabled = previousEnabled;
            }
            GUI.EndScrollView();
            if (GUI.Button(new Rect(826, 652, 418, 36), "B: Collapse", button))
            {
                shopExpanded = false;
                panelClickFrame = Time.frameCount;
            }
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.enabled = previousEnabled;
            GUI.depth = previousDepth;
        }
    }

    private void OnDestroy()
    {
        if (activeShop == this) activeShop = null;
    }

    private void ShowNotification(string message) { notification = message; }

}
