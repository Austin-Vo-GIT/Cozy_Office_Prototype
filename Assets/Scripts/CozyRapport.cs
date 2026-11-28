using System.Collections.Generic;
using UnityEngine;

// One scene component stores Rapport separately from spendable money.
public class CozyRapport : MonoBehaviour
{
    [Header("Completion Audio")]
    [SerializeField] private AudioSource rewardAudioSource;
    [SerializeField] private AudioClip rewardSound;
    [SerializeField, Range(0f, 1f)] private float rewardVolume = 0.4f;

    [Header("Rapport Meter")]
    [SerializeField, Min(1)] private int pointsPerTier = 100;
    [SerializeField, Min(0.1f)] private float meterFillSpeed = 80f;
    [SerializeField, Min(0f)] private float holdDuration = 2.5f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 1.5f;
    [Tooltip("Position in a virtual 1280 x 720 screen. Increase Y if another HUD element overlaps.")]
    [SerializeField] private Vector2 meterPosition = new Vector2(20f, 20f);

    private readonly HashSet<string> awardedTasks = new HashSet<string>();
    private int balance;
    private float displayedBalance;
    private int lastAwardAmount;
    private string popupMessage;
    private float popupElapsed;
    private bool popupActive;

    public int Balance => balance;
    public float NormalizedProgress => pointsPerTier > 0
        ? Mathf.Repeat(displayedBalance, pointsPerTier) / pointsPerTier
        : 1f;
    public int CurrentTier => pointsPerTier > 0
        ? Mathf.FloorToInt(displayedBalance / pointsPerTier)
        : 0;

    public bool TryAwardOnce(string taskId, int amount, string message)
    {
        if (string.IsNullOrEmpty(taskId) || amount <= 0 || !awardedTasks.Add(taskId))
            return false;

        balance += amount;
        lastAwardAmount = amount;
        popupMessage = message;
        popupElapsed = 0f;
        popupActive = true;

        if (rewardAudioSource != null && rewardAudioSource.isActiveAndEnabled && rewardSound != null)
            rewardAudioSource.PlayOneShot(rewardSound, rewardVolume);

        return true;
    }

    private void Update()
    {
        // Preserve the meter notification while the menu covers gameplay.
        if (!popupActive || CozyGameMenu.IsOpen || !Application.isFocused) return;

        displayedBalance = Mathf.MoveTowards(
            displayedBalance,
            balance,
            Mathf.Max(0.1f, meterFillSpeed) * Time.unscaledDeltaTime);

        popupElapsed += Time.unscaledDeltaTime;
        if (popupElapsed >= Mathf.Max(0f, holdDuration) + Mathf.Max(0.1f, fadeDuration))
            popupActive = false;
    }

    private void OnGUI()
    {
        if (!popupActive || CozyGameMenu.IsOpen) return;

        float alpha = 1f - Mathf.Clamp01(
            (popupElapsed - Mathf.Max(0f, holdDuration)) / Mathf.Max(0.1f, fadeDuration));

        Matrix4x4 oldMatrix = GUI.matrix;
        Color oldColor = GUI.color;
        int oldDepth = GUI.depth;

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        GUI.depth = -500;

        const float width = 380f;
        const float padding = 14f;
        const float barHeight = 16f;
        const float gapAfterMessage = 8f;
        const float gapAfterBar = 8f;

        GUIStyle messageStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            wordWrap = true,
            padding = new RectOffset(0, 0, 0, 0)
        };
        messageStyle.normal.textColor = Color.white;

        GUIStyle deltaStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleRight,
            padding = new RectOffset(0, 0, 0, 0)
        };
        deltaStyle.normal.textColor = new Color(0.55f, 1f, 0.82f);

        GUIStyle statsStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            padding = new RectOffset(0, 0, 0, 0)
        };
        statsStyle.normal.textColor = new Color(0.82f, 0.94f, 0.92f);

        float innerWidth = width - padding * 2f;
        string headerText = string.IsNullOrWhiteSpace(popupMessage) ? "Rapport Increased" : popupMessage;
        float messageHeight = messageStyle.CalcHeight(new GUIContent(headerText), innerWidth - 76f);
        float statsHeight = statsStyle.CalcHeight(new GUIContent("Tier 0  •  0 / 100"), innerWidth);
        float totalHeight = padding * 2f + messageHeight + gapAfterMessage + barHeight + gapAfterBar + statsHeight;

        float x = Mathf.Clamp(meterPosition.x, 0f, 1280f - width);
        float y = Mathf.Clamp(meterPosition.y, 0f, 720f - totalHeight);

        // Card background + left accent strip
        GUI.color = new Color(0.10f, 0.22f, 0.23f, 0.94f * alpha);
        GUI.DrawTexture(new Rect(x, y, width, totalHeight), Texture2D.whiteTexture);

        GUI.color = new Color(0.45f, 0.9f, 0.75f, alpha);
        GUI.DrawTexture(new Rect(x, y, 4f, totalHeight), Texture2D.whiteTexture);

        // Header message + "+X" gain
        float cursorY = y + padding;
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(x + padding, cursorY, innerWidth - 76f, messageHeight), headerText, messageStyle);

        GUI.color = new Color(0.55f, 1f, 0.82f, alpha);
        GUI.Label(new Rect(x + width - padding - 72f, cursorY, 72f, messageHeight), $"+{lastAwardAmount}", deltaStyle);

        // Meter track + animated fill bar
        cursorY += messageHeight + gapAfterMessage;
        Rect trackRect = new Rect(x + padding, cursorY, innerWidth, barHeight);
        GUI.color = new Color(0.05f, 0.12f, 0.13f, 0.95f * alpha);
        GUI.DrawTexture(trackRect, Texture2D.whiteTexture);

        float fillFraction = Mathf.Clamp01(NormalizedProgress);
        if (displayedBalance > 0f && Mathf.Approximately(fillFraction, 0f))
            fillFraction = 1f;

        Rect fillRect = new Rect(trackRect.x, trackRect.y, trackRect.width * fillFraction, trackRect.height);
        GUI.color = new Color(0.35f, 0.88f, 0.68f, alpha);
        GUI.DrawTexture(fillRect, Texture2D.whiteTexture);

        // Tier and total rapport readout
        cursorY += barHeight + gapAfterBar;
        int shownBalance = Mathf.RoundToInt(displayedBalance);
        int tierProgress = shownBalance % pointsPerTier;
        string statsText = $"Tier {CurrentTier}  •  {tierProgress}/{pointsPerTier} to next  •  Total: {balance}";
        GUI.color = new Color(0.82f, 0.94f, 0.92f, alpha);
        GUI.Label(new Rect(x + padding, cursorY, innerWidth, statsHeight), statsText, statsStyle);

        GUI.depth = oldDepth;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}