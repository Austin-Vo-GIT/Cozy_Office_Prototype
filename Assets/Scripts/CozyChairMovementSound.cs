using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CozyChairMovementSound : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField] private AudioClip movementLoop;

    [SerializeField, Range(0f, 1f)]
    private float maximumVolume = 0.35f;

    [Header("Movement Detection")]
    [Tooltip("Speed required to start the sound.")]
    [SerializeField, Min(0.001f)]
    private float startSpeed = 0.06f;

    [Tooltip("Speed below which the sound fades out.")]
    [SerializeField, Min(0f)]
    private float stopSpeed = 0.025f;

    [Tooltip("Movement speed that produces full volume.")]
    [SerializeField, Min(0.01f)]
    private float fullVolumeSpeed = 1.2f;

    [Tooltip("Allows a chair turning in place to make sound. Set to 0 to ignore rotation.")]
    [SerializeField, Min(0f)]
    private float rotationRadius = 0.2f;

    [Header("Fading")]
    [SerializeField, Min(0.01f)]
    private float fadeInTime = 0.08f;

    [SerializeField, Min(0.01f)]
    private float fadeOutTime = 0.2f;

    private Rigidbody chairBody;
    private bool moving;

    private void Awake()
    {
        chairBody = GetComponent<Rigidbody>();

        if (movementAudioSource == null || movementLoop == null)
        {
            Debug.LogError(
                "CozyChairMovementSound: Assign an Audio Source " +
                "and Movement Loop.", this);

            enabled = false;
            return;
        }

        movementAudioSource.Stop();
        movementAudioSource.clip = movementLoop;
        movementAudioSource.playOnAwake = false;
        movementAudioSource.loop = true;
        movementAudioSource.volume = 0f;
    }

    private void Update()
    {
        Vector3 velocity = chairBody.linearVelocity;
        velocity.y = 0f;

        float slidingSpeed = velocity.magnitude;
        float turningSpeed =
            Mathf.Abs(chairBody.angularVelocity.y) * rotationRadius;

        float speed = chairBody.IsSleeping()
            ? 0f
            : Mathf.Max(slidingSpeed, turningSpeed);

        // Separate thresholds prevent tiny physics movements
        // from repeatedly starting and stopping the loop.
        if (moving)
        {
            if (speed <= stopSpeed)
                moving = false;
        }
        else if (speed >= startSpeed)
        {
            moving = true;
        }

        float targetVolume = 0f;

        if (moving)
        {
            float speedAmount = Mathf.Clamp01(
                speed / Mathf.Max(fullVolumeSpeed, 0.01f));

            targetVolume = maximumVolume *
                Mathf.Lerp(0.25f, 1f, speedAmount);

            if (!movementAudioSource.isPlaying)
                movementAudioSource.Play();
        }

        float fadeTime =
            targetVolume > movementAudioSource.volume
                ? fadeInTime
                : fadeOutTime;

        movementAudioSource.volume = Mathf.MoveTowards(
            movementAudioSource.volume,
            targetVolume,
            Mathf.Max(maximumVolume, 0.01f) *
                Time.deltaTime / Mathf.Max(fadeTime, 0.01f));

        if (!moving && movementAudioSource.volume <= 0.001f)
        {
            movementAudioSource.Stop();
            movementAudioSource.volume = 0f;
        }
    }

    private void OnDisable()
    {
        moving = false;

        if (movementAudioSource != null)
        {
            movementAudioSource.Stop();
            movementAudioSource.volume = 0f;
        }
    }

    private void OnValidate()
    {
        stopSpeed = Mathf.Clamp(stopSpeed, 0f, startSpeed * 0.9f);
        fullVolumeSpeed = Mathf.Max(fullVolumeSpeed, startSpeed);
    }
}