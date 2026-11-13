using UnityEngine;

// Separate late phase keeps target selection before Update, visuals after hover selection.
[DefaultExecutionOrder(1000)]
public class CozyFocusPresenter : MonoBehaviour
{
    private CozyFocusHighlighter highlighter;
    private void Awake() { highlighter = GetComponent<CozyFocusHighlighter>(); }
    private void LateUpdate()
    {
        if (highlighter != null) highlighter.PresentFocus();
    }
}
