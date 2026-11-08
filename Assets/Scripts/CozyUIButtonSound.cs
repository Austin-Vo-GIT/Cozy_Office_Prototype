using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class CozyUIButtonSound : MonoBehaviour
{
    [Header("Click Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSound;

    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.5f;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(PlayClickSound);
    }

    private void PlayClickSound()
    {
        if (audioSource == null || clickSound == null) return;

        // The button has already accepted the click.
        // Its action may have just closed the window or disabled it.
        audioSource.PlayOneShot(clickSound, clickVolume);
    }

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}