using UnityEngine;

public class CozyDeskItem : MonoBehaviour
{
    [Header("Item")]
    [SerializeField] private string itemName = "Desk item";
    [SerializeField] private string actionText = "Straighten";

    [Header("Starting Mess")]
    [Tooltip("Offset from the tidy position in the parent's local space.")]
    [SerializeField] private Vector3 messyPositionOffset = Vector3.zero;

    [Tooltip("Rotation added to the tidy rotation.")]
    [SerializeField] private Vector3 messyRotationOffset =
        new Vector3(0f, 12f, 0f);

    [Header("Straightening")]
    [Min(0.05f)]
    [SerializeField] private float straightenDuration = 0.45f;

    [Header("Item Audio")]
    [SerializeField] private AudioSource itemAudioSource;
    [SerializeField] private AudioClip actionSound;

    [Range(0f, 1f)]
    [SerializeField] private float actionVolume = 0.4f;

    private Vector3 tidyPosition;
    private Quaternion tidyRotation;
    private Vector3 startingPosition;
    private Quaternion startingRotation;

    private float elapsed;
    private bool moving;
    private bool tidy;

    public string ItemName => itemName;
    public string ActionText => actionText;

    public bool CanStraighten =>
        isActiveAndEnabled && !moving && !tidy;

    private void Awake()
    {
        // Editor placement is the tidy/closed placement.
        tidyPosition = transform.localPosition;
        tidyRotation = transform.localRotation;

        transform.localPosition =
            tidyPosition + messyPositionOffset;

        transform.localRotation =
            tidyRotation * Quaternion.Euler(messyRotationOffset);

        tidy =
            Vector3.Distance(
                transform.localPosition,
                tidyPosition
            ) < 0.001f &&
            Quaternion.Angle(
                transform.localRotation,
                tidyRotation
            ) < 0.1f;
    }

    public void Straighten()
    {
        if (!CanStraighten) return;

        startingPosition = transform.localPosition;
        startingRotation = transform.localRotation;

        elapsed = 0f;
        moving = true;

        // Play once when the item begins moving.
        if (itemAudioSource != null && actionSound != null)
        {
            itemAudioSource.PlayOneShot(
                actionSound,
                actionVolume
            );
        }
    }

    private void Update()
    {
        if (!moving) return;

        elapsed += Time.deltaTime;

        float progress = Mathf.Clamp01(
            elapsed / Mathf.Max(0.05f, straightenDuration)
        );

        float smoothProgress = Mathf.SmoothStep(
            0f,
            1f,
            progress
        );

        transform.localPosition = Vector3.Lerp(
            startingPosition,
            tidyPosition,
            smoothProgress
        );

        transform.localRotation = Quaternion.Slerp(
            startingRotation,
            tidyRotation,
            smoothProgress
        );

        if (progress >= 1f)
        {
            transform.localPosition = tidyPosition;
            transform.localRotation = tidyRotation;

            moving = false;
            tidy = true;
        }
    }
}