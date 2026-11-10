using UnityEngine;
using UnityEngine.InputSystem;

// Runs before gameplay input scripts, so M blocks other actions immediately.
[DefaultExecutionOrder(-10000)]
public class CozyGameMenu : MonoBehaviour
{
    [Header("Menu References")]
    [SerializeField] private CozyNPCInteraction coworkerTask;
    [SerializeField] private PrinterCleanupTask printerTask;
    [SerializeField] private PlayerWorkspaceShop workspaceShop;
    [SerializeField] private CozyDeskSeat deskSeat;
    [SerializeField] private CanvasGroup computerDesktop;

    [Header("Optional Menu Click Sound")]
    [SerializeField] private AudioSource menuAudioSource;
    [SerializeField] private AudioClip menuClickSound;
    [SerializeField, Range(0f, 1f)] private float clickVolume = 0.4f;

    private static CozyGameMenu instance;
    private bool open;
    private bool firstMenuDismissed;
    private int closedFrame = -1;
    private bool cursorWasVisible;
    private CursorLockMode cursorWasLocked;
    private Vector2 scrollPosition;
    private bool desktopWasInteractable;
    private bool desktopBlockedRaycasts;

    public static bool IsOpen => instance != null && instance.open;
    public static bool BlocksGameplay => instance != null &&
        (instance.open || instance.closedFrame == Time.frameCount);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Only one CozyGameMenu should be active.", this);
            enabled = false;
            return;
        }
        instance = this;
        OpenMenu();
    }

    private void Update()
    {
        if (!Application.isFocused) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.mKey.wasPressedThisFrame) return;
        if (!open) OpenMenu();
        else if (firstMenuDismissed) CloseMenu();
        // The first menu must be dismissed with its Close button.
    }

    private void OpenMenu()
    {
        if (open) return;
        if (deskSeat != null) deskSeat.SuspendLookForMenu();
        cursorWasVisible = Cursor.visible;
        cursorWasLocked = Cursor.lockState;
        open = true;
        if (computerDesktop != null)
        {
            desktopWasInteractable = computerDesktop.interactable;
            desktopBlockedRaycasts = computerDesktop.blocksRaycasts;
            computerDesktop.interactable = false;
            computerDesktop.blocksRaycasts = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseMenu()
    {
        if (!open) return;
        open = false;
        firstMenuDismissed = true;
        closedFrame = Time.frameCount;
        Cursor.lockState = cursorWasLocked;
        Cursor.visible = cursorWasVisible;
        RestoreDesktopInput();
    }

    private void RestoreDesktopInput()
    {
        if (computerDesktop == null) return;
        computerDesktop.interactable = desktopWasInteractable;
        computerDesktop.blocksRaycasts = desktopBlockedRaycasts;
    }

    private void ClickSound()
    {
        if (menuAudioSource != null && menuClickSound != null)
            menuAudioSource.PlayOneShot(menuClickSound, clickVolume);
    }

    private void OnGUI()
    {
        if (instance != this) return;
        Matrix4x4 savedMatrix = GUI.matrix;
        Color savedColor = GUI.color;
        int savedDepth = GUI.depth;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUI.depth = -1000;
        GUIStyle label = new GUIStyle(GUI.skin.label)
            { fontSize = 21, wordWrap = true };
        GUIStyle title = new GUIStyle(label)
            { fontSize = 30, fontStyle = FontStyle.Bold };
        GUIStyle button = new GUIStyle(GUI.skin.button)
            { fontSize = 21, wordWrap = true };

        if (!open)
        {
            if (GUI.Button(new Rect(1080, 20, 180, 48), "M: Menu", button))
            {
                ClickSound();
                OpenMenu();
            }
        }
        else
        {
            GUI.color = new Color(0f, 0f, 0f, 0.62f);
            GUI.DrawTexture(new Rect(0, 0, 1280, 720), Texture2D.whiteTexture);
            GUI.color = new Color(0.12f, 0.15f, 0.21f, 1f);
            GUI.DrawTexture(new Rect(240, 35, 800, 650), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.BeginArea(new Rect(270, 55, 740, 600));
            GUILayout.Label("First Day Tasks:", title);
            GUILayout.Space(12);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition,
                GUILayout.Height(480));
            GUILayout.Label("Special Delivery - Talk to your coworker.", label);
            GUILayout.Label("Printer Cleanup - The printer broke again.", label);
            GUILayout.Label("Inbox Cleanup - Check your computer.", label);
            GUILayout.Label("Desk Organization - Keeps the boss happy.", label);
            GUILayout.Space(20);

            if (firstMenuDismissed)
            {
                GUILayout.Label("Current progress", title);
                if (coworkerTask != null)
                    GUILayout.Label("Special Delivery: " + coworkerTask.Objective, label);
                if (printerTask != null)
                    GUILayout.Label("Printer Cleanup: " + printerTask.ProgressSummary, label);
                if (workspaceShop != null)
                    GUILayout.Label("Inbox Cleanup: " +
                        (workspaceShop.InboxCompleted ? "Complete" : "Check your computer."), label);
                GUILayout.Space(15);
            }

            if (firstMenuDismissed && workspaceShop != null)
            {
                GUILayout.Label($"Balance: ${workspaceShop.Balance}", label);
                if (!string.IsNullOrEmpty(workspaceShop.LatestNotification))
                    GUILayout.Label(workspaceShop.LatestNotification, label);
                GUILayout.Space(12);
                GUILayout.Label("Your cubicle", title);
                if (workspaceShop.DecorationPurchased)
                    GUILayout.Label(workspaceShop.DecorationName + ": Purchased", label);
                else
                {
                    GUILayout.Label($"{workspaceShop.DecorationName} - ${workspaceShop.DecorationCost}", label);
                    if (workspaceShop.CanPurchaseHere)
                    {
                        if (GUILayout.Button("Buy decoration", button, GUILayout.Height(42)))
                        {
                            ClickSound();
                            workspaceShop.TryPurchaseDecoration();
                        }
                    }
                    else GUILayout.Label("Visit your desk to buy this decoration.", label);
                }
            }
            GUILayout.EndScrollView();
            GUILayout.Space(12);
            if (GUILayout.Button("Close", button, GUILayout.Height(48)))
            {
                ClickSound();
                CloseMenu();
            }
            GUILayout.EndArea();
        }
        GUI.color = savedColor;
        GUI.depth = savedDepth;
        GUI.matrix = savedMatrix;
    }

    private void OnDisable()
    {
        if (instance != this) return;
        if (open)
        {
            Cursor.lockState = cursorWasLocked;
            Cursor.visible = cursorWasVisible;
            RestoreDesktopInput();
        }
        instance = null;
        open = false;
    }
}
