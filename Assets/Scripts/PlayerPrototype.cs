using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerPrototype : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Components")]
    [SerializeField] private Animator animator;

    private Rigidbody rb;
    private Vector3 moveInput;
    private Transform camTransform;

    // Animator Parameter Hashes
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    // Parameter Existence Flags
    private bool hasIsMovingParam;
    private bool hasSpeedParam;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // Cache the main camera's transform for isometric movement calculations
        if (Camera.main != null)
        {
            camTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogWarning("[PlayerPrototype] Main Camera not found! Movement will default to world space.");
        }

        CheckAnimatorParameters();
    }

    private void CheckAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == IsMovingHash) hasIsMovingParam = true;
            if (param.nameHash == SpeedHash) hasSpeedParam = true;
        }

        if (!hasSpeedParam)
        {
            Debug.LogWarning("[PlayerPrototype] 'Speed' parameter missing from Animator. Skipping setFloat.");
        }
        if (!hasIsMovingParam)
        {
            Debug.LogWarning("[PlayerPrototype] 'IsMoving' parameter missing from Animator. Skipping setBool.");
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
            // Get camera forward and right vectors, then flatten them on the XZ plane (y = 0)
            Vector3 camForward = camTransform.forward;
            Vector3 camRight = camTransform.right;

            camForward.y = 0f;
            camRight.y = 0f;

            camForward.Normalize();
            camRight.Normalize();

            // Calculate movement direction relative to the camera view
            moveInput = (camForward * vertical) + (camRight * horizontal);
        }
        else
        {
            // Fallback if camera is missing
            moveInput = new Vector3(horizontal, 0f, vertical);
        }

        // Normalize if input magnitude exceeds 1 to prevent faster diagonal movement
        if (moveInput.sqrMagnitude > 1f)
        {
            moveInput.Normalize();
        }
    }

    private void MovePlayer()
    {
        Vector3 targetVelocity = moveInput * moveSpeed;
        rb.linearVelocity = new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.z);

        if (moveInput != Vector3.zero)
        {
            transform.forward = moveInput;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        bool isMoving = moveInput.sqrMagnitude > 0.01f;
        float currentSpeed = isMoving ? moveSpeed : 0f;

        if (hasIsMovingParam) animator.SetBool(IsMovingHash, isMoving);
        if (hasSpeedParam) animator.SetFloat(SpeedHash, currentSpeed);
    }
}