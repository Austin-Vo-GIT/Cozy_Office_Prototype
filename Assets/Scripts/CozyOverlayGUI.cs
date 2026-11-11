using UnityEngine;

// Drawing helper only: do not attach this to a GameObject.
// Call from OnGUI while using the existing 1280 x 720 GUI matrix.
public static class CozyOverlayGUI
{
    public static void Draw(string text, float maxWidth, float bottom,
        bool centered = true, float left = 20f, bool fixedWidth = false)
    {
        if (string.IsNullOrEmpty(text)) return;

        const float paddingX = 16f;
        const float paddingY = 12f;
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true,
            alignment = TextAnchor.UpperLeft,
            padding = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            fixedWidth = 0f,
            fixedHeight = 0f
        };

        GUIContent content = new GUIContent(text);
        float width = fixedWidth ? maxWidth
            : Mathf.Min(maxWidth, Mathf.Ceil(style.CalcSize(content).x) + paddingX * 2f);
        width = Mathf.Clamp(width, paddingX * 2f + 1f, 1240f);
        float contentWidth = width - paddingX * 2f;
        float height = Mathf.Ceil(style.CalcHeight(content, contentWidth))
            + paddingY * 2f;
        float x = centered ? (1280f - width) * 0.5f : left;
        Rect rect = new Rect(x, bottom - height, width, height);

        GUI.Box(rect, GUIContent.none);
        GUI.Label(new Rect(rect.x + paddingX, rect.y + paddingY,
            contentWidth, height - paddingY * 2f), content, style);
    }
}
