using UnityEngine;
using UnityEngine.InputSystem;

public class CozyDeskSeat : MonoBehaviour
{
        public bool CanInteractWithDesk =>
        isActiveAndEnabled &&
        initialized &&
        seated &&
        !usingComputer &&
        !lookingAround;
    [Header("Computer Audio")]
    [SerializeField] private AudioSource computerAudioSource;
    [SerializeField] private AudioClip computerStartupSound;
    [SerializeField, Range(0f, 1f)] private float startupVolume = 0.5f;

    [Header("References")]
    [SerializeField] private PlayerPrototype playerMovement;
    [SerializeField] private Camera gameCamera;
    [SerializeField] private Transform deskInteractionPoint;
    [SerializeField] private Transform seatedViewPoint;
    [SerializeField] private CozyNPCInteraction coworkerInteraction;

    [Header("Seated Settings")]
    [Min(0.1f)]
    [SerializeField] private float interactionDistance = 1.5f;

    [Range(20f, 90f)]
    [SerializeField] private float seatedFieldOfView = 60f;

    [Header("Seated Look - Hold L")]
    [SerializeField] private bool enableSeatedLook = true;

    [Tooltip("Degrees of rotation per pixel of mouse movement.")]
    [Range(0.01f, 1f)]
    [SerializeField] private float mouseSensitivity = 0.12f;

    [Range(0f, 120f)]
    [SerializeField] private float horizontalLookLimit = 70f;

    [Range(0f, 60f)]
    [SerializeField] private float lookUpLimit = 30f;

    [Range(0f, 75f)]
    [SerializeField] private float lookDownLimit = 45f;

    [SerializeField] private bool invertMouseY = false;

    [Header("Computer Mode")]
    [SerializeField] private Transform computerViewPoint;
    [SerializeField] private CanvasGroup desktop;
    [SerializeField] private PlayerWorkspaceShop workspaceShop;

    [Range(20f, 90f)]
    [SerializeField] private float computerFieldOfView = 40f;

    [Min(0.1f)]
    [SerializeField] private float cameraTransitionSpeed = 8f;

    private Rigidbody playerBody;
    private CozyCameraFollow cameraFollow;
    private Renderer[] playerRenderers;
    private bool[] rendererStates;

    private bool initialized;
    private bool seated;
    private bool usingComputer;
    private bool lookingAround;

    private float lookYaw;
    private float lookPitch;

    private bool movementWasEnabled;
    private bool followWasEnabled;
    private bool coworkerWasEnabled;
    private bool bodyWasKinematic;
    private bool shopWasEnabled;

    // Cursor state before entering computer mode.
    private bool savedCursorVisible;
    private CursorLockMode savedCursorLock;

    // Separate cursor state for temporary hold-L looking.
    private bool lookCursorWasVisible;
    private CursorLockMode lookCursorWasLocked;

    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedFieldOfView;
    private float savedNearClip;

    private void Start()
    {
        if (playerMovement == null ||
            gameCamera == null ||
            deskInteractionPoint == null ||
            seatedViewPoint == null)
        {
            Debug.LogError(
                "CozyDeskSeat: Assign Player Movement, Game Camera, " +
                "Desk Interaction Point, and Seated View Point.",
                this
            );

            enabled = false;
            return;
        }

        playerBody = playerMovement.GetComponent<Rigidbody>();
        cameraFollow = gameCamera.GetComponent<CozyCameraFollow>();

        if (playerBody == null || cameraFollow == null)
        {
            Debug.LogError(
                "CozyDeskSeat: The player needs a Rigidbody, " +
                "and the camera needs CozyCameraFollow.",
                this
            );

            enabled = false;
            return;
        }

        playerRenderers =
            playerMovement.GetComponentsInChildren<Renderer>(true);

        rendererStates = new bool[playerRenderers.Length];

        // Start with the monitor turned off.
        if (desktop != null)
        {
            desktop.interactable = false;
            desktop.blocksRaycasts = false;
            desktop.gameObject.SetActive(false);
        }

        initialized = true;
    }

    private bool IsAtDesk()
    {
        return playerMovement != null &&
               deskInteractionPoint != null &&
               Vector3.Distance(
                   playerMovement.transform.position,
                   deskInteractionPoint.position
               ) <= interactionDistance;
    }

    private void Update()
    {
        if (!initialized) return;

        Keyboard keyboard = Keyboard.current;

        if (!Application.isFocused || keyboard == null)
        {
            EndLookAround();
            return;
        }

        if (seated)
        {
            if (usingComputer)
            {
                // First exit the computer, staying seated.
                if (keyboard.escapeKey.wasPressedThisFrame ||
                    keyboard.fKey.wasPressedThisFrame)
                {
                    CloseComputer();
                }

                return;
            }

            if (keyboard.fKey.wasPressedThisFrame ||
                keyboard.escapeKey.wasPressedThisFrame)
            {
                StandUp();
                return;
            }

            if (keyboard.cKey.wasPressedThisFrame)
            {
                OpenComputer();
                return;
            }

            UpdateSeatedLook(keyboard);
            return;
        }

        if (IsAtDesk() && keyboard.fKey.wasPressedThisFrame)
        {
            SitDown();
        }
    }

    private void UpdateSeatedLook(Keyboard keyboard)
    {
        Mouse mouse = Mouse.current;

        bool shouldLook =
            enableSeatedLook &&
            keyboard.lKey.isPressed &&
            mouse != null;

        if (!shouldLook)
        {
            EndLookAround();
            return;
        }

        if (!lookingAround)
        {
            BeginLookAround();

            // Ignore movement from before the cursor was locked.
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();

        // Mouse delta already represents movement this frame.
        // Do not multiply it by Time.deltaTime.
        lookYaw += mouseDelta.x * mouseSensitivity;

        float verticalDirection = invertMouseY ? 1f : -1f;
        lookPitch +=
            mouseDelta.y * mouseSensitivity * verticalDirection;

        lookYaw = Mathf.Clamp(
            lookYaw,
            -horizontalLookLimit,
            horizontalLookLimit
        );

        lookPitch = Mathf.Clamp(
            lookPitch,
            -lookUpLimit,
            lookDownLimit
        );
    }

    private void BeginLookAround()
    {
        if (lookingAround) return;

        lookCursorWasVisible = Cursor.visible;
        lookCursorWasLocked = Cursor.lockState;

        lookingAround = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EndLookAround()
    {
        if (!lookingAround) return;

        lookingAround = false;

        Cursor.lockState = lookCursorWasLocked;
        Cursor.visible = lookCursorWasVisible;

        // Keep lookYaw and lookPitch so the view stays turned.
    }

    private void SitDown()
    {
        if (seated) return;

        savedCameraPosition = gameCamera.transform.position;
        savedCameraRotation = gameCamera.transform.rotation;
        savedFieldOfView = gameCamera.fieldOfView;
        savedNearClip = gameCamera.nearClipPlane;

        movementWasEnabled = playerMovement.enabled;
        followWasEnabled = cameraFollow.enabled;
        bodyWasKinematic = playerBody.isKinematic;

        playerMovement.enabled = false;
        cameraFollow.enabled = false;

        if (!playerBody.isKinematic)
        {
            playerBody.linearVelocity = Vector3.zero;
            playerBody.angularVelocity = Vector3.zero;
        }

        playerBody.isKinematic = true;

        // Hide the character during this prototype's seated mode.
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null) continue;

            rendererStates[i] = playerRenderers[i].enabled;
            playerRenderers[i].enabled = false;
        }

        if (coworkerInteraction != null)
        {
            coworkerWasEnabled = coworkerInteraction.enabled;
            coworkerInteraction.enabled = false;
        }

        // Each new seated session starts facing the desk.
        lookYaw = 0f;
        lookPitch = 0f;
        seated = true;

        gameCamera.transform.SetPositionAndRotation(
            seatedViewPoint.position,
            seatedViewPoint.rotation
        );

        gameCamera.fieldOfView = seatedFieldOfView;
        gameCamera.nearClipPlane = 0.03f;
    }

    private void OpenComputer()
    {
        if (!seated || usingComputer) return;

        if (computerViewPoint == null || desktop == null)
        {
            Debug.LogWarning(
                "CozyDeskSeat: Assign Computer View Point and Desktop.",
                this
            );

            return;
        }

        // Restore the seated cursor before saving computer cursor state.
        EndLookAround();

        usingComputer = true;

        if (computerAudioSource != null &&
            computerStartupSound != null)
        {
            computerAudioSource.PlayOneShot(
                computerStartupSound,
                startupVolume
            );
        }

        savedCursorVisible = Cursor.visible;
        savedCursorLock = Cursor.lockState;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        desktop.gameObject.SetActive(true);
        desktop.interactable = true;
        desktop.blocksRaycasts = true;

        if (workspaceShop != null)
        {
            shopWasEnabled = workspaceShop.enabled;
            workspaceShop.enabled = false;
        }
    }

    private void CloseComputer()
    {
        if (!usingComputer) return;

        usingComputer = false;

        // Leave the monitor on, but disable desktop interaction.
        if (desktop != null)
        {
            desktop.interactable = false;
            desktop.blocksRaycasts = false;
        }

        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;

        if (workspaceShop != null)
        {
            workspaceShop.enabled = shopWasEnabled;
        }

        // The saved look offsets are used again in LateUpdate.
    }

    private void LateUpdate()
    {
        if (!initialized || !seated) return;

        ApplySeatedView();
    }

    private void ApplySeatedView()
    {
        if (gameCamera == null) return;

        Transform targetView = usingComputer
            ? computerViewPoint
            : seatedViewPoint;

        if (targetView == null) return;

        float targetFov = usingComputer
            ? computerFieldOfView
            : seatedFieldOfView;

        Quaternion targetRotation = targetView.rotation;

        if (!usingComputer)
        {
            // Turn around the world's upright axis, then tilt locally.
            // The original seated marker is never modified.
            targetRotation =
                Quaternion.AngleAxis(lookYaw, Vector3.up) *
                seatedViewPoint.rotation *
                Quaternion.AngleAxis(lookPitch, Vector3.right);
        }

        float blend = 1f - Mathf.Exp(
            -cameraTransitionSpeed * Time.deltaTime
        );

        gameCamera.transform.position = Vector3.Lerp(
            gameCamera.transform.position,
            targetView.position,
            blend
        );

        gameCamera.transform.rotation = Quaternion.Slerp(
            gameCamera.transform.rotation,
            targetRotation,
            blend
        );

        gameCamera.fieldOfView = Mathf.Lerp(
            gameCamera.fieldOfView,
            targetFov,
            blend
        );

        gameCamera.nearClipPlane = 0.03f;
    }

    private void StandUp()
    {
        EndLookAround();

        if (!seated) return;

        CloseComputer();
        seated = false;

        lookYaw = 0f;
        lookPitch = 0f;

        if (gameCamera != null)
        {
            gameCamera.transform.SetPositionAndRotation(
                savedCameraPosition,
                savedCameraRotation
            );

            gameCamera.fieldOfView = savedFieldOfView;
            gameCamera.nearClipPlane = savedNearClip;
        }

        if (playerRenderers != null)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    playerRenderers[i].enabled = rendererStates[i];
                }
            }
        }

        if (playerBody != null)
        {
            playerBody.isKinematic = bodyWasKinematic;
        }

        if (playerMovement != null)
        {
            playerMovement.enabled = movementWasEnabled;
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled = followWasEnabled;
        }

        if (coworkerInteraction != null)
        {
            coworkerInteraction.enabled = coworkerWasEnabled;
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            EndLookAround();
        }
    }

    private void OnDisable()
    {
        StandUp();
    }

    private void OnGUI()
    {
        if (!initialized) return;
        if (!seated && !IsAtDesk()) return;

        Matrix4x4 previousMatrix = GUI.matrix;

        GUI.matrix = Matrix4x4.TRS(
            Vector3.zero,
            Quaternion.identity,
            new Vector3(
                Screen.width / 1280f,
                Screen.height / 720f,
                1f
            )
        );

        GUIStyle textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            wordWrap = true
        };

        string instructions;

        if (usingComputer)
        {
            instructions = "Esc or F: Return to seated view";
        }
        else if (seated)
        {
            instructions = enableSeatedLook
                ? "Hold L + mouse: Look around\n" +
                  "C: Use computer\n" +
                  "F or Esc: Stand up"
                : "C: Use computer\nF or Esc: Stand up";
        }
        else
        {
            instructions = "Press F to sit at your desk";
        }

        GUILayout.BeginArea(
            new Rect(20, 580, 420, 120),
            GUI.skin.box
        );

        GUILayout.Label(instructions, textStyle);
        GUILayout.EndArea();

        GUI.matrix = previousMatrix;
    }
}