using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPrototype : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [SerializeField, Min(1f)]
    private float runSpeedMultiplier = 1.75f;

    [Header("Components")]
    [SerializeField] private Animator animator;

    [Header("Footstep Audio")]
    [Tooltip("Use a dedicated Audio Source for footsteps.")]
    [SerializeField] private AudioSource footstepAudioSource;

    [SerializeField] private AudioClip walkLeftFoot;
    [SerializeField] private AudioClip walkRightFoot;
    [SerializeField] private AudioClip runLeftFoot;
    [SerializeField] private AudioClip runRightFoot;

    [SerializeField, Range(0f, 1f)]
    private float walkFootstepVolume = 0.3f;

    [SerializeField, Range(0f, 1f)]
    private float runFootstepVolume = 0.4f;

    [Header("Footstep Timing")]
    [Tooltip("Seconds between consecutive footsteps, not a full left/right cycle.")]
    [SerializeField, Min(0.05f)]
    private float walkStepInterval = 0.45f;

    [SerializeField, Min(0.05f)]
    private float runStepInterval = 0.28f;

    [Tooltip("Delay before the first footstep when starting to move.")]
    [SerializeField, Min(0f)]
    private float firstStepDelay = 0.12f;

    [Tooltip("Minimum actual horizontal speed needed for footsteps.")]
    [SerializeField, Min(0.01f)]
    private float minimumFootstepSpeed = 0.08f;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Transform camTransform;
    private bool isRunning;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int IsMovingHash =
        Animator.StringToHash("IsMoving");

    private static readonly int IsRunningHash =
        Animator.StringToHash("IsRunning");

    private bool hasIsMovingParam;
    private bool hasSpeedParam;
    private bool hasIsRunningParam;

    private Vector3 previousPhysicsPosition;
    private bool hasPreviousPhysicsPosition;
    private float lastGroundContactTime = float.NegativeInfinity;

    private bool footstepsActive;
    private bool nextStepIsLeft = true;
    private bool previousStepWasRunning;
    private float stepTimer;

    private float CurrentMoveSpeed =>
        moveSpeed * (isRunning ? runSpeedMultiplier : 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning(
                "[PlayerPrototype] Main Camera not found! " +
                "Movement will default to world space.",
                this
            );
        }

        CheckAnimatorParameters();

        if (footstepAudioSource != null)
        {
            footstepAudioSource.playOnAwake = false;
            footstepAudioSource.loop = false;
            footstepAudioSource.Stop();
        }
    }

    private void OnEnable()
    {
        hasPreviousPhysicsPosition = false;
        lastGroundContactTime = float.NegativeInfinity;
        footstepsActive = false;
        nextStepIsLeft = true;
        stepTimer = firstStepDelay;
    }

    private void CheckAnimatorParameters()
    {
        hasIsMovingParam = false;
        hasSpeedParam = false;
        hasIsRunningParam = false;

        if (animator == null ||
            animator.runtimeAnimatorController == null)
        {
            return;
        }

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == IsMovingHash &&
                param.type == AnimatorControllerParameterType.Bool)
            {
                hasIsMovingParam = true;
            }

            if (param.nameHash == SpeedHash &&
                param.type == AnimatorControllerParameterType.Float)
            {
                hasSpeedParam = true;
            }

            if (param.nameHash == IsRunningHash &&
                param.type == AnimatorControllerParameterType.Bool)
            {
                hasIsRunningParam = true;
            }
        }

        if (!hasIsRunningParam)
        {
            Debug.LogWarning(
                "[PlayerPrototype] Add a Bool parameter named " +
                "'IsRunning' to the character's Animator Controller.",
                this
            );
        }
    }

    private void Update()
    {
        GetInput();
        UpdateAnimations();

        if (CozyGameMenu.BlocksGameplay || !Application.isFocused)
        {
            if (!rb.isKinematic)
            {
                rb.linearVelocity = new Vector3(
                    0f,
                    rb.linearVelocity.y,
                    0f
                );
            }

            ResetFootsteps(true);
        }
    }

    private void FixedUpdate()
    {
        // Measure the movement produced by the previous physics step.
        // This avoids using the requested speed when blocked by a wall.
        Vector3 currentPosition = rb.position;
        float actualSpeed = 0f;

        if (hasPreviousPhysicsPosition)
        {
            Vector3 displacement =
                currentPosition - previousPhysicsPosition;

            displacement.y = 0f;

            actualSpeed =
                displacement.magnitude / Time.fixedDeltaTime;
        }

        previousPhysicsPosition = currentPosition;
        hasPreviousPhysicsPosition = true;

        UpdateFootsteps(actualSpeed);
        MovePlayer();
    }

    private void GetInput()
    {
        moveInput = Vector3.zero;
        isRunning = false;

        if (CozyGameMenu.BlocksGameplay || !Application.isFocused)
        {
            return;
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;
        if (keyboard.aKey.isPressed) horizontal -= 1f;

        if (camTransform != null)
        {
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            moveInput =
                camForward * vertical +
                camRight * horizontal;
        }
        else
        {
            moveInput = new Vector3(horizontal, 0f, vertical);
        }

        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }

        bool shiftHeld =
            keyboard.leftShiftKey.isPressed ||
            keyboard.rightShiftKey.isPressed;

        isRunning =
            shiftHeld && moveInput.sqrMagnitude > 0.01f;
    }

    private void MovePlayer()
    {
        if (rb.isKinematic) return;

        if (CozyGameMenu.BlocksGameplay || !Application.isFocused)
        {
            rb.linearVelocity = new Vector3(
                0f,
                rb.linearVelocity.y,
                0f
            );

            return;
        }

        Vector3 targetVelocity = moveInput * CurrentMoveSpeed;

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z
        );

        if (moveInput != Vector3.zero)
        {
            transform.forward = moveInput;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        float currentSpeed = isMoving ? CurrentMoveSpeed : 0f;

        if (hasIsMovingParam)
        {
            animator.SetBool(IsMovingHash, isMoving);
        }

        if (hasSpeedParam)
        {
            animator.SetFloat(SpeedHash, currentSpeed);
        }

        if (hasIsRunningParam)
        {
            animator.SetBool(IsRunningHash, isRunning);
        }
    }

    private void UpdateFootsteps(float actualSpeed)
    {
        if (footstepAudioSource == null ||
            !footstepAudioSource.isActiveAndEnabled)
        {
            ResetFootsteps(false);
            return;
        }

        // A short grace period bridges consecutive physics contacts.
        float groundGrace =
            Mathf.Max(0.06f, Time.fixedDeltaTime * 2.5f);

        bool grounded =
            Time.fixedTime - lastGroundContactTime <= groundGrace;

        bool shouldPlay =
            !CozyGameMenu.BlocksGameplay &&
            Application.isFocused &&
            !rb.isKinematic &&
            grounded &&
            moveInput.sqrMagnitude > 0.01f &&
            actualSpeed >= minimumFootstepSpeed;

        if (!shouldPlay)
        {
            // Let an ordinary footstep finish its short natural tail.
            ResetFootsteps(false);
            return;
        }

        float interval = isRunning
            ? runStepInterval
            : walkStepInterval;

        if (!footstepsActive)
        {
            footstepsActive = true;
            previousStepWasRunning = isRunning;
            stepTimer = firstStepDelay;
        }
        else if (previousStepWasRunning != isRunning)
        {
            // Preserve progress through the step when changing gait.
            float previousInterval = previousStepWasRunning
                ? runStepInterval
                : walkStepInterval;

            stepTimer *= interval /
                Mathf.Max(previousInterval, 0.05f);

            previousStepWasRunning = isRunning;
        }

        stepTimer -= Time.fixedDeltaTime;

        if (stepTimer > 0f) return;

        PlayFootstep();
        stepTimer = Mathf.Max(0.05f, interval);
    }

    private void PlayFootstep()
    {
        AudioClip clip;

        if (isRunning)
        {
            clip = nextStepIsLeft
                ? runLeftFoot
                : runRightFoot;
        }
        else
        {
            clip = nextStepIsLeft
                ? walkLeftFoot
                : walkRightFoot;
        }

        // Walking clips can temporarily stand in for missing run clips.
        if (clip == null && isRunning)
        {
            clip = nextStepIsLeft
                ? walkLeftFoot
                : walkRightFoot;
        }

        if (clip != null)
        {
            float volume = isRunning
                ? runFootstepVolume
                : walkFootstepVolume;

            footstepAudioSource.PlayOneShot(clip, volume);
        }

        nextStepIsLeft = !nextStepIsLeft;
    }

    private void ResetFootsteps(bool stopAudio)
    {
        footstepsActive = false;
        stepTimer = firstStepDelay;

        if (stopAudio && footstepAudioSource != null)
        {
            footstepAudioSource.Stop();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        CheckGroundContact(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        CheckGroundContact(collision);
    }

    private void CheckGroundContact(Collision collision)
    {
        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            // Upward-facing support counts as ground.
            // Vertical desk and wall contacts do not.
            if (Vector3.Dot(contact.normal, Vector3.up) >= 0.6f)
            {
                lastGroundContactTime = Time.fixedTime;
                return;
            }
        }
    }

    private void OnDisable()
    {
        moveInput = Vector3.zero;
        isRunning = false;

        ResetFootsteps(true);
        hasPreviousPhysicsPosition = false;
        lastGroundContactTime = float.NegativeInfinity;

        UpdateAnimations();
    }
}