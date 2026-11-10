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
        if (player == null || deskInteractionPoint == null || decoration == null)
        {
            Debug.LogError("PlayerWorkspaceShop: Assign all three references.", this);
            enabled = false;
            return;
        }
        decoration.SetActive(false);
        initialized = true;
    }

    public void GrantInboxReward()
    {
        if (inboxRewardReceived) return;
        inboxRewardReceived = true;
        balance += 15;
        PlayRewardSound();
        ShowNotification("Inbox cleaned! Earned $15.");
    }

    public void GrantTaskReward()
    {
        if (rewardReceived) return;
        rewardReceived = true;
        balance += taskReward;
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
        if (CozyGameMenu.BlocksGameplay) return;
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.bKey.wasPressedThisFrame)
            TryPurchaseDecoration();
    }

    // Also called explicitly by the menu's Buy decoration button.
    public void TryPurchaseDecoration()
    {
        if (!CanPurchaseHere || decorationPurchased || decoration == null) return;
        if (balance < decorationCost)
        {
            ShowNotification($"You need ${decorationCost - balance} more.");
            return;
        }
        balance -= decorationCost;
        decorationPurchased = true;
        decoration.SetActive(true);
        if (placementAudioSource != null && placementSound != null)
            placementAudioSource.PlayOneShot(placementSound, placementVolume);
        ShowNotification("Your cubicle feels more like yours!");
    }

    private void ShowNotification(string message) { notification = message; }
    // No persistent OnGUI panels. Balance/shop/messages are shown by CozyGameMenu.
}
