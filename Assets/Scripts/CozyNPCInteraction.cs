using UnityEngine;
using UnityEngine.InputSystem;

public class CozyNPCInteraction : MonoBehaviour
{
    [Header("Paperwork Audio")]
    [SerializeField] private AudioSource paperworkAudioSource;
    [SerializeField] private AudioClip paperworkSound;
    [SerializeField, Range(0f, 1f)] private float paperworkVolume = 0.5f;
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private GameObject paperwork;
    [SerializeField] private Transform deliveryPoint;
    [SerializeField] private GameObject deliveredPaperwork;
    [SerializeField] private PlayerWorkspaceShop workspaceShop;
    [Header("Conversation")]
    [SerializeField] private string coworkerName = "Coworker";
    [TextArea(3, 6)]
    [SerializeField] private string dialogue =
        "Could you take this paperwork to the manager's office? " +
        "I emailed it already, but apparently it feels more official on paper.";
    [Header("Interaction")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;

    private enum TaskStage { NotStarted, CollectPaperwork, DeliverPaperwork, Completed }
    private TaskStage stage = TaskStage.NotStarted;
    private bool dialogueOpen;
    public string Objective => GetObjective();
    public bool IsAwaitingDelivery => isActiveAndEnabled &&
        stage == TaskStage.DeliverPaperwork;

    public CozyFocusHighlighter.Candidate GetFocusCandidate()
    {
        if (!isActiveAndEnabled || player == null || CozyGameMenu.BlocksGameplay)
            return default;
        PlayerPrototype movement = player.GetComponent<PlayerPrototype>();
        if (movement != null && !movement.isActiveAndEnabled) return default;
        if (dialogueOpen && IsNear(transform))
            return new CozyFocusHighlighter.Candidate(this, null, 0f, 100);
        if (stage == TaskStage.CollectPaperwork && paperwork != null
            && paperwork.activeInHierarchy && IsNear(paperwork.transform))
            return new CozyFocusHighlighter.Candidate(this, paperwork,
                Vector3.Distance(player.position, paperwork.transform.position), 20);
        if (stage == TaskStage.DeliverPaperwork && IsNear(deliveryPoint))
            return new CozyFocusHighlighter.Candidate(this, null,
                Vector3.Distance(player.position, deliveryPoint.position), 20);
        if (IsNear(transform))
            return new CozyFocusHighlighter.Candidate(this, null,
                Vector3.Distance(player.position, transform.position), 10);
        return default;
    }

    private void Start()
    {
        if (player == null || paperwork == null || deliveryPoint == null)
        {
            Debug.LogError("CozyNPCInteraction: Assign Player, Paperwork, and Delivery Point.", this);
            enabled = false;
        }
    }

    private bool IsNear(Transform point)
    {
        return player != null && point != null &&
            Vector3.Distance(player.position, point.position) <= interactionDistance;
    }

    private void PlayPaperworkSound()
    {
        if (paperworkAudioSource != null && paperworkSound != null)
            paperworkAudioSource.PlayOneShot(paperworkSound, paperworkVolume);
    }

    private System.Collections.IEnumerator GrantDeliveryRewardAfterPaperSound()
    {
        float delay = 0f;
        if (paperworkAudioSource != null &&
            paperworkAudioSource.isActiveAndEnabled && paperworkSound != null)
        {
            float pitch = Mathf.Max(0.01f, Mathf.Abs(paperworkAudioSource.pitch));
            delay = paperworkSound.length / pitch + 0.15f;
        }
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (workspaceShop != null) workspaceShop.GrantTaskReward();
    }

    private void Update()
    {
        if (CozyGameMenu.BlocksGameplay || player == null) return;
        if (dialogueOpen && !IsNear(transform)) dialogueOpen = false;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            dialogueOpen = false;
            return;
        }
        if (!keyboard.tKey.wasPressedThisFrame ||
            !CozyFocusHighlighter.AllowsTask(this)) return;
        if (dialogueOpen)
        {
            if (stage == TaskStage.NotStarted) stage = TaskStage.CollectPaperwork;
            dialogueOpen = false;
            return;
        }
        if (stage == TaskStage.CollectPaperwork &&
            paperwork != null && paperwork.activeInHierarchy && IsNear(paperwork.transform))
        {
            paperwork.SetActive(false);
            PlayPaperworkSound();
            stage = TaskStage.DeliverPaperwork;
            return;
        }
        if (stage == TaskStage.DeliverPaperwork && IsNear(deliveryPoint))
        {
            stage = TaskStage.Completed;
            PlayPaperworkSound();
            if (deliveredPaperwork != null) deliveredPaperwork.SetActive(true);
            StartCoroutine(GrantDeliveryRewardAfterPaperSound());
            return;
        }
        if (IsNear(transform)) dialogueOpen = true;
    }

    private string GetObjective()
    {
        switch (stage)
        {
            case TaskStage.CollectPaperwork:
                return "Pick up the paperwork from your coworker's desk.";
            case TaskStage.DeliverPaperwork:
                return "Deliver the paperwork to the manager's office.";
            case TaskStage.Completed:
                return "Task complete: Paperwork delivered!";
            default: return "Talk to your coworker.";
        }
    }

    private string GetDialogue()
    {
        switch (stage)
        {
            case TaskStage.CollectPaperwork:
                return "The paperwork is on my desk. Try not to read it. I certainly didn't.";
            case TaskStage.DeliverPaperwork:
                return "Take it to the manager's desk. Congratulations on becoming our email attachment.";
            case TaskStage.Completed:
                return "Thanks! You've mastered our \"paperless\" workflow.";
            default: return dialogue;
        }
    }

    private string GetPrompt()
    {
        if (stage == TaskStage.CollectPaperwork &&
            paperwork != null && paperwork.activeInHierarchy && IsNear(paperwork.transform))
            return "T: Pick up paperwork";
        if (stage == TaskStage.DeliverPaperwork && IsNear(deliveryPoint))
            return "T: Deliver paperwork";
        if (IsNear(transform)) return "T: Talk";
        return "";
    }

    private void OnGUI()
    {
        if (CozyGameMenu.BlocksGameplay || player == null ||
            !CozyFocusHighlighter.AllowsTask(this)) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        if (dialogueOpen)
        {
            string instructions = stage == TaskStage.NotStarted
                ? "T: Accept task     Esc: Not now" : "T or Esc: Close";
            string text = coworkerName + "\n\n" + GetDialogue()
                + "\n\n" + instructions;
            CozyOverlayGUI.Draw(text, 800f, 700f, fixedWidth: true);
        }
        else
        {
            CozyOverlayGUI.Draw(GetPrompt(), 600f, 700f);
        }
        GUI.matrix = previousMatrix;
    }

    private void OnDisable() { dialogueOpen = false; }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
