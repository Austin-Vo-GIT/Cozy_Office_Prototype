using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CabinetCleanupTask : MonoBehaviour
{
    [Serializable]
    public class CabinetDoor
    {
        public Transform hinge;
        public Vector3 closedLocalEuler;
        public Vector3 openLocalEuler;
        [NonSerialized] public Quaternion startRotation;
    }

    [Header("References")]
    [SerializeField] private PlayerPrototype playerMovement;
    [SerializeField] private Transform interactionPoint;
    [SerializeField] private GameObject cabinetHighlight;
    [SerializeField] private CozyRapport rapport;

    [Header("Doors - assign two hinge transforms")]
    [SerializeField] private CabinetDoor[] doors = new CabinetDoor[2];
    [SerializeField, Min(0.1f)] private float swingDuration = 0.8f;

    [Header("Binders - exactly three messy originals")]
    [SerializeField] private GameObject[] messyBinders = new GameObject[3];
    [Tooltip("Parent of three neatly arranged binder copies on the top shelf.")]
    [SerializeField] private GameObject organizedBinders;

    [Header("Interaction and Reward")]
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;
    [SerializeField, Min(1)] private int rapportReward = 10;

    [Header("Audio")]
    [SerializeField] private AudioSource cabinetAudioSource;
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip placementSound;
    [SerializeField, Range(0f, 1f)] private float soundVolume = 0.4f;

    [Header("Binder Pickup Audio")]
    [SerializeField] private AudioClip binderPickupSound;
    [SerializeField, Range(0f, 1f)] private float binderPickupVolume = 0.4f;

    private enum Stage { NotStarted, Opening, Collecting, ReadyToPlace, ReadyToClose, Closing, Completed }
    private Stage stage;
    private readonly bool[] collected = new bool[3];
    private int collectedCount;
    private float swingElapsed;
    private bool initialized;
    private bool rewardHandled;

    public bool Completed => stage == Stage.Completed;
    public string ProgressSummary
    {
        get
        {
            switch (stage)
            {
                case Stage.Opening: return "Opening the cabinet.";
                case Stage.Collecting: return $"Binders collected: {collectedCount}/3.";
                case Stage.ReadyToPlace: return "Place all three binders on the top shelf.";
                case Stage.ReadyToClose: return "Close the cabinet doors to finish.";
                case Stage.Closing: return "Closing the cabinet.";
                case Stage.Completed: return "Complete! Cabinet organized.";
                default: return "Open the messy cabinet.";
            }
        }
    }

    private void OnEnable() { CozyFocusHighlighter.RegisterSource(this); }

    private void OnDisable() { CozyFocusHighlighter.UnregisterSource(this); }

    private void Start()
    {
        bool valid = playerMovement != null && interactionPoint != null &&
            organizedBinders != null && rapport != null &&
            doors != null && doors.Length == 2 &&
            messyBinders != null && messyBinders.Length == 3;
        if (valid)
        {
            foreach (CabinetDoor door in doors)
                if (door == null || door.hinge == null) valid = false;
            for (int i = 0; i < messyBinders.Length; i++)
            {
                GameObject binder = messyBinders[i];
                if (binder == null || !binder.activeInHierarchy ||
                    binder == organizedBinders ||
                    (binder != null && organizedBinders.transform.IsChildOf(binder.transform)) ||
                    (binder != null && binder.transform.IsChildOf(organizedBinders.transform)))
                    valid = false;
                for (int j = 0; j < i; j++)
                    if (binder == messyBinders[j]) valid = false;
            }
            if (transform.IsChildOf(organizedBinders.transform)) valid = false;
            if (doors[0] != null && doors[1] != null && doors[0].hinge == doors[1].hinge)
                valid = false;
        }
        if (!valid)
        {
            Debug.LogError("CabinetCleanupTask: Assign Player Movement, Interaction Point, Rapport, two distinct hinges, three distinct active messy binders, and a separate organized-binder group.", this);
            enabled = false;
            return;
        }
        organizedBinders.SetActive(false);
        // Deliberately leave both hinges in their authored starting poses.
        initialized = true;
    }

    [ContextMenu("Capture Current Door Rotations As CLOSED")]
    private void CaptureClosed()
    {
        if (Application.isPlaying || doors == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Capture closed cabinet doors");
#endif
        foreach (CabinetDoor door in doors)
            if (door != null && door.hinge != null)
                door.closedLocalEuler = door.hinge.localEulerAngles;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    [ContextMenu("Capture Current Door Rotations As OPEN")]
    private void CaptureOpen()
    {
        if (Application.isPlaying || doors == null) return;
#if UNITY_EDITOR
        UnityEditor.Undo.RecordObject(this, "Capture open cabinet doors");
#endif
        foreach (CabinetDoor door in doors)
            if (door != null && door.hinge != null)
                door.openLocalEuler = door.hinge.localEulerAngles;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private bool CanInteract => initialized && isActiveAndEnabled &&
        playerMovement != null && playerMovement.isActiveAndEnabled &&
        !CozyGameMenu.BlocksGameplay && Application.isFocused &&
        stage != Stage.Opening && stage != Stage.Closing && !Completed;

    private float DistanceTo(Vector3 point)
    {
        Vector3 delta = playerMovement.transform.position - point;
        if (Mathf.Abs(delta.y) > 2.5f) return float.PositiveInfinity;
        delta.y = 0f;
        return delta.magnitude;
    }

    private int NearestBinder()
    {
        int nearest = -1;
        float best = interactionDistance;
        for (int i = 0; i < 3; i++)
        {
            if (collected[i] || messyBinders[i] == null || !messyBinders[i].activeInHierarchy) continue;
            float distance = DistanceTo(messyBinders[i].transform.position);
            if (distance <= best) { best = distance; nearest = i; }
        }
        return nearest;
    }

    public CozyFocusHighlighter.Candidate GetFocusCandidate()
    {
        if (!CanInteract) return default;
        if (stage == Stage.Collecting)
        {
            int index = NearestBinder();
            if (index < 0) return default;
            return new CozyFocusHighlighter.Candidate(this, messyBinders[index],
                DistanceTo(messyBinders[index].transform.position), 20);
        }
        float distance = DistanceTo(interactionPoint.position);
        if (distance > interactionDistance) return default;
        return new CozyFocusHighlighter.Candidate(this, cabinetHighlight, distance, 20);
    }

    private void Update()
    {
        if (!initialized) return;
        if (stage == Stage.Opening || stage == Stage.Closing) AnimateDoors();
        // If the menu is open when doors finish, defer the reward until gameplay resumes.
        if (Completed && !rewardHandled && !CozyGameMenu.BlocksGameplay &&
            Application.isFocused && rapport != null && rapport.isActiveAndEnabled)
        {
            rapport.TryAwardOnce("cabinet-cleanup", rapportReward, "Cabinet organized!");
            rewardHandled = true;
        }
        if (!CanInteract || Keyboard.current == null ||
            !CozyFocusHighlighter.TryInteract(this)) return;
        switch (stage)
        {
            case Stage.NotStarted:
                BeginSwing(true);
                break;
            case Stage.Collecting:
                int index = NearestBinder();
                if (index < 0) return;
                collected[index] = true;
                messyBinders[index].SetActive(false);
                if (binderPickupSound != null && cabinetAudioSource != null &&
                    cabinetAudioSource.isActiveAndEnabled)
                    cabinetAudioSource.PlayOneShot(binderPickupSound, binderPickupVolume);
                collectedCount++;
                if (collectedCount == 3) stage = Stage.ReadyToPlace;
                break;
            case Stage.ReadyToPlace:
                organizedBinders.SetActive(true);
                PlaySound(placementSound);
                stage = Stage.ReadyToClose;
                break;
            case Stage.ReadyToClose:
                BeginSwing(false);
                break;
        }
    }

    private void BeginSwing(bool opening)
    {
        foreach (CabinetDoor door in doors)
            door.startRotation = door.hinge.localRotation;
        swingElapsed = 0f;
        stage = opening ? Stage.Opening : Stage.Closing;
        PlaySound(opening ? openSound : closeSound);
    }

    private void AnimateDoors()
    {
        bool opening = stage == Stage.Opening;
        swingElapsed += Time.deltaTime;
        float t = Mathf.Clamp01(swingElapsed / Mathf.Max(0.1f, swingDuration));
        foreach (CabinetDoor door in doors)
        {
            if (door.hinge == null) return;
            Quaternion target = Quaternion.Euler(opening ? door.openLocalEuler : door.closedLocalEuler);
            door.hinge.localRotation = Quaternion.Slerp(door.startRotation, target,
                Mathf.SmoothStep(0f, 1f, t));
        }
        if (t < 1f) return;
        stage = opening ? Stage.Collecting : Stage.Completed;
        
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && cabinetAudioSource != null && cabinetAudioSource.isActiveAndEnabled)
            cabinetAudioSource.PlayOneShot(clip, soundVolume);
    }

    private void OnGUI()
    {
        if (!CanInteract || !CozyFocusHighlighter.AllowsTask(this) ||
            GetFocusCandidate().owner == null) return;
        string prompt;
        switch (stage)
        {
            case Stage.Collecting: prompt = $"I: Collect binder ({collectedCount}/3 collected)"; break;
            case Stage.ReadyToPlace: prompt = "I: Organize all 3 binders on the top shelf"; break;
            case Stage.ReadyToClose: prompt = "I: Close cabinet and finish cleanup"; break;
            default: prompt = "I: Open cabinet"; break;
        }
        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        CozyOverlayGUI.Draw(prompt, 600f, 620f);
        GUI.matrix = previous;
    }
}
