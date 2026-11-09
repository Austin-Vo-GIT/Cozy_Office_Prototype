using UnityEngine;
using UnityEngine.InputSystem;

public class CozyNPCInteraction : MonoBehaviour
{
    [Header("Paperwork Audio")]
    [SerializeField] private AudioSource paperworkAudioSource;
    [SerializeField] private AudioClip paperworkSound;

    [SerializeField, Range(0f, 1f)]
    private float paperworkVolume = 0.5f;
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
        "I emailed it already, but apparently it feels more " +
        "official on paper.";

    [Header("Interaction")]
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 1.5f;

    private enum TaskStage
    {
        NotStarted,
        CollectPaperwork,
        DeliverPaperwork,
        Completed
    }

    private TaskStage stage = TaskStage.NotStarted;
    private bool dialogueOpen;

    private void Start()
    {
        if (player == null || paperwork == null || deliveryPoint == null)
        {
            Debug.LogError(
                "CozyNPCInteraction: Assign Player, Paperwork, " +
                "and Delivery Point in the Inspector.", this);

            enabled = false;
        }
    }

    private bool IsNear(Transform point)
    {
        if (player == null || point == null) return false;

        return Vector3.Distance(player.position, point.position)
            <= interactionDistance;
    }
    private void PlayPaperworkSound()
    {
    if (paperworkAudioSource == null || paperworkSound == null)
        return;

    paperworkAudioSource.PlayOneShot(
        paperworkSound,
        paperworkVolume
    );
    }
    private System.Collections.IEnumerator
    GrantDeliveryRewardAfterPaperSound()
{
    float delay = 0f;

    if (paperworkAudioSource != null &&
        paperworkAudioSource.isActiveAndEnabled &&
        paperworkSound != null)
    {
        // Account for the Audio Source's playback pitch.
        float pitch = Mathf.Max(
            0.01f,
            Mathf.Abs(paperworkAudioSource.pitch)
        );

        delay = paperworkSound.length / pitch;

        // A small pause separates the paper sound from the chime.
        delay += 0.15f;
    }

    if (delay > 0f)
    {
        yield return new WaitForSeconds(delay);
    }

    if (workspaceShop != null)
    {
        workspaceShop.GrantTaskReward();
    }
}
    private void Update()
    {
        if (player == null) return;

        if (dialogueOpen && !IsNear(transform))
            dialogueOpen = false;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            dialogueOpen = false;
            return;
        }

        if (!keyboard.tKey.wasPressedThisFrame) return;

        // An open conversation takes priority over other actions.
        if (dialogueOpen)
        {
            if (stage == TaskStage.NotStarted)
                stage = TaskStage.CollectPaperwork;

            dialogueOpen = false;
            return;
        }

        // Task actions take priority when near both an item and NPC.
        if (stage == TaskStage.CollectPaperwork &&
            paperwork != null && IsNear(paperwork.transform))
        {
            paperwork.SetActive(false);
            PlayPaperworkSound();
            stage = TaskStage.DeliverPaperwork;
            return;
        }

        if (stage == TaskStage.DeliverPaperwork &&
    IsNear(deliveryPoint))
{
    stage = TaskStage.Completed;
PlayPaperworkSound();
    if (deliveredPaperwork != null)
    {
        deliveredPaperwork.SetActive(true);
    }

    StartCoroutine(GrantDeliveryRewardAfterPaperSound());

    return;
}

        if (IsNear(transform))
            dialogueOpen = true;
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

            default:
                return "Talk to your coworker.";
        }
    }

    private string GetDialogue()
    {
        switch (stage)
        {
            case TaskStage.CollectPaperwork:
                return "The paperwork is on my desk. " +
                       "Try not to read it. I certainly didn't.";

            case TaskStage.DeliverPaperwork:
                return "Take it to the manager's desk. " +
                       "Congratulations on becoming our email attachment.";

            case TaskStage.Completed:
                return "Thanks! You've mastered our \"paperless\" workflow.";

            default:
                return dialogue;
        }
    }

    private string GetPrompt()
    {
        if (stage == TaskStage.CollectPaperwork &&
            paperwork != null && IsNear(paperwork.transform))
            return "Press T to pick up paperwork";

        if (stage == TaskStage.DeliverPaperwork &&
            IsNear(deliveryPoint))
            return "Press T to deliver paperwork";

        if (IsNear(transform))
            return "Press T to talk";

        return "";
    }

    private void OnGUI()
    {
        if (player == null) return;

        Matrix4x4 previousMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f)
        );

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true
        };

        GUILayout.BeginArea(new Rect(20, 20, 520, 120), GUI.skin.box);
        GUILayout.Label("FIRST DAY: SPECIAL DELIVERY", textStyle);
        GUILayout.Label(GetObjective(), textStyle);
        GUILayout.EndArea();

        if (dialogueOpen)
        {
            GUILayout.BeginArea(
                new Rect(240, 480, 800, 220), GUI.skin.box);

            GUILayout.Label(coworkerName, textStyle);
            GUILayout.Space(8);
            GUILayout.Label(GetDialogue(), textStyle);
            GUILayout.Space(12);

            string instructions = stage == TaskStage.NotStarted
                ? "T: Accept task     Esc: Not now"
                : "T or Esc: Close";

            GUILayout.Label(instructions, textStyle);
            GUILayout.EndArea();
        }
        else
        {
            string prompt = GetPrompt();

            if (!string.IsNullOrEmpty(prompt))
            {
                GUILayout.BeginArea(
                    new Rect(390, 635, 500, 60), GUI.skin.box);

                GUILayout.Label(prompt, textStyle);
                GUILayout.EndArea();
            }
        }

        GUI.matrix = previousMatrix;
    }

    private void OnDisable()
    {
        dialogueOpen = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}