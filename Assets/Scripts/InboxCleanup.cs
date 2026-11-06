using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InboxCleanup : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private PlayerWorkspaceShop workspaceShop;

    [Header("Text")]
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text subjectText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Buttons")]
    [SerializeField] private Button replyButton;
    [SerializeField] private Button forwardButton;
    [SerializeField] private Button archiveButton;
    [SerializeField] private Button nextButton;

    private enum EmailAction
    {
        Reply,
        Forward,
        Archive
    }

    private class Email
    {
        public string subject;
        public string body;
        public EmailAction correctAction;
        public string hint;
        public string success;

        public Email(
            string subject,
            string body,
            EmailAction correctAction,
            string hint,
            string success)
        {
            this.subject = subject;
            this.body = body;
            this.correctAction = correctAction;
            this.hint = hint;
            this.success = success;
        }
    }

    private readonly Email[] emails =
    {
        new Email(
            "Welcome! One tiny detail",
            "From: Human Resources\n\n" +
            "Please reply with the name you'd like on your desk " +
            "nameplate. Otherwise, you'll remain 'New Person' " +
            "until the next budget cycle.",
            EmailAction.Reply,
            "HR needs information from you. Try replying.",
            "Name submitted. 'New Person' has officially been promoted to 'Employee'."
        ),

        new Email(
            "Printer requesting emotional support",
            "From: Morgan, Operations\n\n" +
            "The printer shows Error 42. Please forward this " +
            "message to IT so they can investigate. " +
            "Complimenting its paper tray did not help.",
            EmailAction.Forward,
            "IT needs to see this request. Try forwarding it.",
            "Forwarded to IT. The printer's feelings remain unconfirmed."
        ),

        new Email(
            "RE: RE: RE: Missing blue mug",
            "From: Office Announcements\n\n" +
            "The mug has been found beside its owner. " +
            "This issue is resolved; no action is needed. " +
            "Please do not reply to all.",
            EmailAction.Archive,
            "The issue is resolved and needs no response. Try archiving.",
            "Archived. You spared 46 coworkers another notification."
        ),

        new Email(
            "Quick check before tomorrow",
            "From: Your Manager\n\n" +
            "Can you attend the 10 AM orientation tomorrow? " +
            "Please reply to confirm. It includes a short " +
            "presentation about making presentations shorter.",
            EmailAction.Reply,
            "Your manager needs your confirmation. Try replying.",
            "Attendance confirmed. The short presentation has 63 slides."
        ),

        new Email(
            "Weekly elevator newsletter",
            "From: Facilities\n\n" +
            "The elevator passed its routine inspection. " +
            "This is for your information only; no action is required. " +
            "It still goes down faster than it goes up. Kind of like our sales...yea we should be fine.",
            EmailAction.Archive,
            "This is an information-only update. Try archiving it.",
            "Archived. The elevator remains the company's top mover."
        )
    };

    private int currentEmail;
    private int mistakes;
    private bool waitingForNext;
    private bool completed;
    private bool initialized;

    private string feedback =
        "Choose Reply, Forward, or Archive. Take your time.";

    private void Awake()
    {
        if (workspaceShop == null ||
            progressText == null ||
            subjectText == null ||
            bodyText == null ||
            feedbackText == null ||
            replyButton == null ||
            forwardButton == null ||
            archiveButton == null ||
            nextButton == null)
        {
            Debug.LogError(
                "InboxCleanup: Assign every reference in the Inspector.",
                this
            );

            enabled = false;
            return;
        }

        replyButton.onClick.AddListener(ChooseReply);
        forwardButton.onClick.AddListener(ChooseForward);
        archiveButton.onClick.AddListener(ChooseArchive);
        nextButton.onClick.AddListener(NextEmail);

        initialized = true;
    }

    private void OnEnable()
    {
        if (initialized)
        {
            RefreshDisplay();
        }
    }

    private void ChooseReply()
    {
        ChooseAction(EmailAction.Reply);
    }

    private void ChooseForward()
    {
        ChooseAction(EmailAction.Forward);
    }

    private void ChooseArchive()
    {
        ChooseAction(EmailAction.Archive);
    }

    private void ChooseAction(EmailAction action)
    {
        if (!initialized || !isActiveAndEnabled ||
            completed || waitingForNext)
        {
            return;
        }

        Email email = emails[currentEmail];

        if (action == email.correctAction)
        {
            waitingForNext = true;
            feedback = email.success;
        }
        else
        {
            mistakes++;
            feedback = "No worries—nothing was sent. " + email.hint;
        }

        RefreshDisplay();
    }

    private void NextEmail()
    {
        if (!initialized || !isActiveAndEnabled ||
            !waitingForNext || completed)
        {
            return;
        }

        currentEmail++;
        waitingForNext = false;

        if (currentEmail >= emails.Length)
        {
            completed = true;
            workspaceShop.GrantInboxReward();
        }
        else
        {
            feedback = "Choose the best action for this email.";
        }

        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (completed)
        {
            progressText.text = "INBOX CLEANUP • COMPLETE";
            subjectText.text = "Inbox zero. Enjoy this brief moment.";

            bodyText.text =
                "All five emails are handled!\n\n" +
                "Completion bonus: $15 added to your balance.\n" +
                $"Practice mistakes: {mistakes} — no pay deducted.";

            feedbackText.text =
                "You can close this window and return to your desktop.";
        }
        else
        {
            Email email = emails[currentEmail];

            progressText.text =
                $"INBOX CLEANUP • EMAIL {currentEmail + 1} / {emails.Length}";

            subjectText.text = email.subject;
            bodyText.text = email.body;
            feedbackText.text = feedback;
        }

        bool canAnswer = !completed && !waitingForNext;

        replyButton.interactable = canAnswer;
        forwardButton.interactable = canAnswer;
        archiveButton.interactable = canAnswer;
        nextButton.interactable = !completed && waitingForNext;

        TMP_Text nextLabel = nextButton.GetComponentInChildren<TMP_Text>();

        if (nextLabel != null)
        {
            nextLabel.text =
                currentEmail == emails.Length - 1 && !completed
                ? "Finish"
                : "Next";
        }
    }

    private void OnDestroy()
    {
        if (!initialized) return;

        if (replyButton != null)
            replyButton.onClick.RemoveListener(ChooseReply);

        if (forwardButton != null)
            forwardButton.onClick.RemoveListener(ChooseForward);

        if (archiveButton != null)
            archiveButton.onClick.RemoveListener(ChooseArchive);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(NextEmail);
    }
}