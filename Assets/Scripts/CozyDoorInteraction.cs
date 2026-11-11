using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class CozyDoorInteraction : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerPrototype playerMovement;
    [SerializeField] private Transform interactionPoint;

    [Header("Door Settings")]
    [SerializeField] private string doorName = "Huddle room door";
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;
    [SerializeField, Range(-170f, 170f)] private float openAngle = 90f;
    [SerializeField, Min(1f)] private float swingSpeed = 100f;

    [Header("Door Audio")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorOpenSound;
    [SerializeField] private AudioClip doorCloseSound;
    [SerializeField, Range(0f, 1f)] private float openVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float closeVolume = 0.4f;

    private bool closeSoundPending;

    private static readonly List<CozyDoorInteraction> doors =
        new List<CozyDoorInteraction>();
    private Rigidbody body;
    private Quaternion closedLocalRotation;
    private bool open;
    private bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { doors.Clear(); }

    private void Awake()
    {
        if (doorAudioSource == null)
            doorAudioSource = GetComponent<AudioSource>();
        body = GetComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        closedLocalRotation = transform.localRotation;
    }

    private void OnEnable()
    {
        if (!doors.Contains(this)) doors.Add(this);
    }

    private void Start()
    {
        if (playerMovement == null || interactionPoint == null)
        {
            Debug.LogError("CozyDoorInteraction: Assign Player Movement and Interaction Point.", this);
            enabled = false;
            return;
        }
        initialized = true;
    }

    private bool CanInteract => initialized && isActiveAndEnabled
        && playerMovement != null && playerMovement.isActiveAndEnabled
        && interactionPoint != null && !CozyGameMenu.BlocksGameplay
        && Application.isFocused;

    private static CozyDoorInteraction NearestDoor()
    {
        CozyDoorInteraction nearest = null;
        float best = float.PositiveInfinity;
        foreach (CozyDoorInteraction door in doors)
        {
            if (door == null || !door.CanInteract) continue;
            Vector3 delta = door.playerMovement.transform.position
                - door.interactionPoint.position;
            // Interaction marker sits at floor height; ignore character pivot height.
            float verticalDistance = Mathf.Abs(delta.y);
            delta.y = 0f;
            float distance = delta.sqrMagnitude;
            if (verticalDistance <= 2f &&
                distance <= door.interactionDistance * door.interactionDistance &&
                distance < best)
            {
                best = distance;
                nearest = door;
            }
        }
        return nearest;
    }

    private void Update()
    {
        if (!CanInteract || Keyboard.current == null) return;
        if (Keyboard.current.oKey.wasPressedThisFrame && NearestDoor() == this)
        {
            open = !open;
            closeSoundPending = false;
            if (open)
            {
                PlayDoorSound(doorOpenSound, openVolume);
            }
            else
            {
                Quaternion closedWorld = transform.parent != null
                    ? transform.parent.rotation * closedLocalRotation
                    : closedLocalRotation;
                closeSoundPending = Quaternion.Angle(body.rotation, closedWorld) > 0.1f;
            }
        }
    }

    private void FixedUpdate()
    {
        if (!initialized) return;
        Quaternion localTarget = closedLocalRotation *
            Quaternion.Euler(0f, open ? openAngle : 0f, 0f);
        Quaternion worldTarget = transform.parent != null
            ? transform.parent.rotation * localTarget : localTarget;
        Quaternion nextRotation = Quaternion.RotateTowards(body.rotation, worldTarget,
            swingSpeed * Time.fixedDeltaTime);
        body.MoveRotation(nextRotation);

        // Play once on arrival, never just because the scene starts closed.
        if (!open && closeSoundPending &&
            Quaternion.Angle(nextRotation, worldTarget) <= 0.1f)
        {
            closeSoundPending = false;
            PlayDoorSound(doorCloseSound, closeVolume);
        }
    }

    private void PlayDoorSound(AudioClip clip, float volume)
    {
        if (doorAudioSource != null && doorAudioSource.isActiveAndEnabled && clip != null)
            doorAudioSource.PlayOneShot(clip, volume);
    }

    private void OnGUI()
    {
        if (!CanInteract || NearestDoor() != this) return;
        Matrix4x4 previous = GUI.matrix;
        try
        {
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
            CozyOverlayGUI.Draw("O: " + (open ? "Close " : "Open ") + doorName,
                500f, 540f, false);
        }
        finally { GUI.matrix = previous; }
    }

    private void OnDisable() { doors.Remove(this); }

    private void OnDrawGizmosSelected()
    {
        if (interactionPoint == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(interactionPoint.position, interactionDistance);
    }
}
