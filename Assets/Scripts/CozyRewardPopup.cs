using UnityEngine;

// Keep this component enabled while the player uses the computer.
// The shop component is temporarily disabled in computer mode.
public class CozyRewardPopup : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0f)] private float holdDuration = 2f;
    [SerializeField, Min(0.1f)] private float fadeDuration = 2f;

    private bool visible;
    private float elapsed;
    private int earnedAmount;
    private int latestBalance;

    public void ShowReward(int amount, int updatedBalance)
    {
        if (amount <= 0) return;
        // Combine rewards earned while a previous notification is visible.
        earnedAmount = visible ? earnedAmount + amount : amount;
        latestBalance = updatedBalance;
        elapsed = 0f;
        visible = true;
    }

    private void Update()
    {
        if (!visible || CozyGameMenu.IsOpen) return;

        elapsed += Time.unscaledDeltaTime;
        if (elapsed >= Mathf.Max(0f, holdDuration) + Mathf.Max(0.1f, fadeDuration))
        {
            visible = false;
            earnedAmount = 0;
        }
    }

    private void OnGUI()
    {
        if (!visible || CozyGameMenu.IsOpen) return;

        float alpha = 1f - Mathf.Clamp01(
            (elapsed - Mathf.Max(0f, holdDuration)) / Mathf.Max(0.1f, fadeDuration));

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;

        try
        {
            GUI.depth = -100;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            GUI.color = new Color(previousColor.r, previousColor.g,
                previousColor.b, previousColor.a * alpha);

            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0)
            };
            GUIContent content = new GUIContent(
                $"Earned: +${earnedAmount}\nBalance: ${latestBalance}");
            const float padding = 16f;
            float width = Mathf.Clamp(
                Mathf.Ceil(style.CalcSize(content).x) + padding * 2f, 220f, 480f);
            float height = Mathf.Ceil(style.CalcHeight(content, width - padding * 2f))
                + padding * 2f;
            Rect box = new Rect(20f, 20f, width, height);
            GUI.Box(box, GUIContent.none);
            GUI.Label(new Rect(box.x + padding, box.y + padding,
                width - padding * 2f, height - padding * 2f), content, style);
        }
        finally
        {
            GUI.matrix = previousMatrix;
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }
}
