using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerAnimationController : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private Animator animator;

    private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private bool hasIsMovingParam;
    private bool hasSpeedParam;

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        CheckAnimatorParameters();
    }

    private void CheckAnimatorParameters()
    {
        if (animator == null || animator.runtimeAnimatorController == null) return;

        // Verify parameters exist on the Animator Controller asset
        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            if (param.nameHash == IsMovingHash) hasIsMovingParam = true;
            if (param.nameHash == SpeedHash) hasSpeedParam = true;
        }

        if (!hasIsMovingParam)
            Debug.LogWarning("[PlayerAnimationController] Missing 'IsMoving' (Bool) parameter in Animator.");
        if (!hasSpeedParam)
            Debug.LogWarning("[PlayerAnimationController] Missing 'Speed' (Float) parameter in Animator.");
    }

    private void Update()
    {
        if (animator == null) return;

        UpdateAnimationStates();
    }

    private void UpdateAnimationStates()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool isMoving = keyboard.wKey.isPressed || keyboard.aKey.isPressed ||
                        keyboard.sKey.isPressed || keyboard.dKey.isPressed ||
                        keyboard.upArrowKey.isPressed || keyboard.downArrowKey.isPressed ||
                        keyboard.leftArrowKey.isPressed || keyboard.rightArrowKey.isPressed;

        // Only set values if the parameter actually exists
        if (hasIsMovingParam) animator.SetBool(IsMovingHash, isMoving);
        if (hasSpeedParam) animator.SetFloat(SpeedHash, isMoving ? 1f : 0f);
    }
}