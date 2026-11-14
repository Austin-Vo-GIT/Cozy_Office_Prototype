using System.Collections.Generic;
using UnityEngine;

// One scene component stores Rapport separately from spendable money.
public class CozyRapport : MonoBehaviour
{
    [Header("Completion Audio")]
    [SerializeField] private AudioSource rewardAudioSource;
    [SerializeField] private AudioClip rewardSound;
    [SerializeField, Range(0f, 1f)] private float rewardVolume = 0.4f;

    [Header("Popup")]
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 2f;
    [Tooltip("Position in a virtual 1280 x 720 screen. Increase Y if another popup overlaps.")]
    [SerializeField] private Vector2 popupPosition = new Vector2(20f, 20f);

    private readonly HashSet<string> awardedTasks = new HashSet<string>();
    private int balance;
    private string popupText;
    private float popupElapsed;
    private bool popupActive;

    public int Balance => balance;

    public bool TryAwardOnce(string taskId, int amount, string message)
    {
        if (string.IsNullOrEmpty(taskId) || amount <= 0 || !awardedTasks.Add(taskId))
            return false;

        balance += amount;
        popupText = $"{message}\n+{amount} Rapport Points\nTotal Rapport: {balance}";
        popupElapsed = 0f;
        popupActive = true;

        if (rewardAudioSource != null && rewardAudioSource.isActiveAndEnabled && rewardSound != null)
            rewardAudioSource.PlayOneShot(rewardSound, rewardVolume);

        return true;
    }

    private void Update()
    {
        // Preserve the notification while the menu covers gameplay.
        if (!popupActive || CozyGameMenu.IsOpen || !Application.isFocused) return;
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

        GUIStyle label = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true,
            padding = new RectOffset(0, 0, 0, 0)
        };
        label.normal.textColor = Color.white;
        const float width = 360f;
        const float padding = 14f;
        float height = label.CalcHeight(new GUIContent(popupText), width - padding * 2f) + padding * 2f;
        float x = Mathf.Clamp(popupPosition.x, 0f, 1280f - width);
        float y = Mathf.Clamp(popupPosition.y, 0f, 720f - height);
        GUI.color = new Color(0.10f, 0.22f, 0.23f, 0.94f * alpha);
        GUI.DrawTexture(new Rect(x, y, width, height), Texture2D.whiteTexture);
        GUI.color = new Color(0.45f, 0.9f, 0.75f, alpha);
        GUI.DrawTexture(new Rect(x, y, 4f, height), Texture2D.whiteTexture);
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(x + padding, y + padding, width - padding * 2f,
            height - padding * 2f), popupText, label);

        GUI.depth = oldDepth;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}
