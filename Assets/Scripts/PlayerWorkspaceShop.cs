using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerWorkspaceShop : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform deskInteractionPoint;
    [SerializeField] private GameObject decoration;

    [Header("Shop Settings")]
    [SerializeField] private string decorationName = "Desk decoration";
    [Min(0)]
    [SerializeField] private int decorationCost = 15;
    [Min(0)]
    [SerializeField] private int taskReward = 25;
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 1.5f;

    private int balance;
    private bool rewardReceived;
    private bool inboxRewardReceived;
    private bool decorationPurchased;

    private string notification = "";
    private float notificationUntil;

    private void Start()
    {
        if (player == null ||
            deskInteractionPoint == null ||
            decoration == null)
        {
            Debug.LogError(
                "PlayerWorkspaceShop: Assign all three references.",
                this
            );

            enabled = false;
            return;
        }

        decoration.SetActive(false);
    }
    public void GrantInboxReward()
{
        if (inboxRewardReceived) return;

    inboxRewardReceived = true;
    balance += 15;

    ShowNotification("Inbox cleaned! Earned $15.");
}
    public void GrantTaskReward()
    {
        // This task can only pay the player once per play session.
        if (rewardReceived) return;

        rewardReceived = true;
        balance += taskReward;

        ShowNotification(
            $"Paperwork delivered! Earned ${taskReward}."
        );
    }

    private bool IsAtDesk()
    {
        return player != null &&
               deskInteractionPoint != null &&
               Vector3.Distance(
                   player.position,
                   deskInteractionPoint.position
               ) <= interactionDistance;
    }

    private void Update()
    {
        if (!IsAtDesk() || decorationPurchased) return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.bKey.wasPressedThisFrame)
        {
            if (balance < decorationCost)
            {
                ShowNotification(
                    $"You need ${decorationCost - balance} more."
                );
                return;
            }

            if (decoration == null) return;

            balance -= decorationCost;
            decorationPurchased = true;
            decoration.SetActive(true);

            ShowNotification("Your cubicle feels more like yours!");
        }
    }

    private void ShowNotification(string message)
    {
        notification = message;
        notificationUntil = Time.time + 4f;
    }

    private void OnGUI()
    {
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
            wordWrap = true
        };

        GUILayout.BeginArea(
            new Rect(940, 20, 320, 70), GUI.skin.box
        );
        GUILayout.Label($"Balance: ${balance}", textStyle);
        GUILayout.EndArea();

        if (Time.time < notificationUntil)
        {
            GUILayout.BeginArea(
                new Rect(700, 105, 560, 100), GUI.skin.box
            );
            GUILayout.Label(notification, textStyle);
            GUILayout.EndArea();
        }

        if (IsAtDesk())
        {
            GUILayout.BeginArea(
                new Rect(850, 460, 410, 155), GUI.skin.box
            );

            GUILayout.Label("YOUR CUBICLE", textStyle);

            if (decorationPurchased)
            {
                GUILayout.Label(
                    $"{decorationName}: Purchased",
                    textStyle
                );
            }
            else
            {
                GUILayout.Label(
                    $"{decorationName} — ${decorationCost}",
                    textStyle
                );
                GUILayout.Label("Press B to buy", textStyle);
            }

            GUILayout.EndArea();
        }

        GUI.matrix = previousMatrix;
    }
}