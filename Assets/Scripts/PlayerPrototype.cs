using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPrototype : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField, Min(1f)] private float runSpeedMultiplier = 1.75f;

    [Header("Components")]
    [SerializeField] private Animator animator;

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
                "Movement will default to world space.");
        }

        CheckAnimatorParameters();
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
                "'IsRunning' to the character's Animator Controller.");
        }
    }

    private void Update()
    {
        GetInput();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void GetInput()
    {
        moveInput = Vector3.zero;
        isRunning = false;

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
                (camForward * vertical) + (camRight * horizontal);
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

        isRunning = shiftHeld && moveInput.sqrMagnitude > 0.01f;
    }

    private void MovePlayer()
    {
        Vector3 targetVelocity = moveInput * CurrentMoveSpeed;

        rb.linearVelocity = new Vector3(
            targetVelocity.x,
            rb.linearVelocity.y,
            targetVelocity.z);

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
}