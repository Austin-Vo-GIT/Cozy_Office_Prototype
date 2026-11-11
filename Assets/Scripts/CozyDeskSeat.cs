using UnityEngine;
using UnityEngine.InputSystem;

public class CozyDeskSeat : MonoBehaviour
{
    public bool CanInteractWithDesk => isActiveAndEnabled && initialized &&
        seated && !usingComputer && !lookingAround && !CozyGameMenu.BlocksGameplay;

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
    [SerializeField, Min(0.1f)] private float interactionDistance = 1.5f;
    [SerializeField, Range(20f, 90f)] private float seatedFieldOfView = 60f;
    [Header("Seated Look - Hold L")]
    [SerializeField] private bool enableSeatedLook = true;
    [SerializeField, Range(0.01f, 1f)] private float mouseSensitivity = 0.12f;
    [SerializeField, Range(0f, 120f)] private float horizontalLookLimit = 70f;
    [SerializeField, Range(0f, 60f)] private float lookUpLimit = 30f;
    [SerializeField, Range(0f, 75f)] private float lookDownLimit = 45f;
    [SerializeField] private bool invertMouseY = false;
    [Header("Computer Mode")]
    [SerializeField] private Transform computerViewPoint;
    [SerializeField] private CanvasGroup desktop;
    [SerializeField] private PlayerWorkspaceShop workspaceShop;
    [SerializeField, Range(20f, 90f)] private float computerFieldOfView = 40f;
    [SerializeField, Min(0.1f)] private float cameraTransitionSpeed = 8f;

    private Rigidbody playerBody;
    private CozyCameraFollow cameraFollow;
    private Renderer[] playerRenderers;
    private bool[] rendererStates;
    private bool initialized, seated, usingComputer, lookingAround;
    private float lookYaw, lookPitch;
    private bool movementWasEnabled, followWasEnabled, coworkerWasEnabled;
    private bool bodyWasKinematic, shopWasEnabled;
    private bool savedCursorVisible, lookCursorWasVisible;
    private CursorLockMode savedCursorLock, lookCursorWasLocked;
    private Vector3 savedCameraPosition;
    private Quaternion savedCameraRotation;
    private float savedFieldOfView, savedNearClip;

    private void Start()
    {
        if (playerMovement == null || gameCamera == null ||
            deskInteractionPoint == null || seatedViewPoint == null)
        {
            Debug.LogError("CozyDeskSeat: Assign Player Movement, Game Camera, Desk Interaction Point, and Seated View Point.", this);
            enabled = false;
            return;
        }
        playerBody = playerMovement.GetComponent<Rigidbody>();
        cameraFollow = gameCamera.GetComponent<CozyCameraFollow>();
        if (playerBody == null || cameraFollow == null)
        {
            Debug.LogError("CozyDeskSeat: The player needs a Rigidbody, and the camera needs CozyCameraFollow.", this);
            enabled = false;
            return;
        }
        playerRenderers = playerMovement.GetComponentsInChildren<Renderer>(true);
        rendererStates = new bool[playerRenderers.Length];
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
        return playerMovement != null && deskInteractionPoint != null &&
            Vector3.Distance(playerMovement.transform.position, deskInteractionPoint.position)
                <= interactionDistance;
    }

    public void SuspendLookForMenu() { EndLookAround(); }

    private void Update()
    {
        if (!initialized) return;
        if (CozyGameMenu.BlocksGameplay)
        {
            EndLookAround();
            return;
        }
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
                if (keyboard.escapeKey.wasPressedThisFrame || keyboard.fKey.wasPressedThisFrame)
                    CloseComputer();
                return;
            }
            if (keyboard.fKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame)
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
        if (IsAtDesk() && keyboard.fKey.wasPressedThisFrame) SitDown();
    }

    private void UpdateSeatedLook(Keyboard keyboard)
    {
        Mouse mouse = Mouse.current;
        if (!enableSeatedLook || !keyboard.lKey.isPressed || mouse == null)
        {
            EndLookAround();
            return;
        }
        if (!lookingAround)
        {
            BeginLookAround();
            return;
        }
        Vector2 delta = mouse.delta.ReadValue();
        lookYaw = Mathf.Clamp(lookYaw + delta.x * mouseSensitivity,
            -horizontalLookLimit, horizontalLookLimit);
        lookPitch = Mathf.Clamp(lookPitch + delta.y * mouseSensitivity * (invertMouseY ? 1f : -1f),
            -lookUpLimit, lookDownLimit);
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
        lookYaw = 0f;
        lookPitch = 0f;
        seated = true;
        gameCamera.transform.SetPositionAndRotation(seatedViewPoint.position, seatedViewPoint.rotation);
        gameCamera.fieldOfView = seatedFieldOfView;
        gameCamera.nearClipPlane = 0.03f;
    }

    private void OpenComputer()
    {
        if (!seated || usingComputer) return;
        if (computerViewPoint == null || desktop == null)
        {
            Debug.LogWarning("CozyDeskSeat: Assign Computer View Point and Desktop.", this);
            return;
        }
        EndLookAround();
        usingComputer = true;
        if (computerAudioSource != null && computerStartupSound != null)
            computerAudioSource.PlayOneShot(computerStartupSound, startupVolume);
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
        if (desktop != null)
        {
            desktop.interactable = false;
            desktop.blocksRaycasts = false;
        }
        Cursor.lockState = savedCursorLock;
        Cursor.visible = savedCursorVisible;
        if (workspaceShop != null) workspaceShop.enabled = shopWasEnabled;
    }

    private void LateUpdate()
    {
        if (!initialized || !seated || CozyGameMenu.BlocksGameplay) return;
        ApplySeatedView();
    }

    private void ApplySeatedView()
    {
        if (gameCamera == null) return;
        Transform view = usingComputer ? computerViewPoint : seatedViewPoint;
        if (view == null) return;
        float fov = usingComputer ? computerFieldOfView : seatedFieldOfView;
        Quaternion rotation = view.rotation;
        if (!usingComputer)
            rotation = Quaternion.AngleAxis(lookYaw, Vector3.up) * seatedViewPoint.rotation *
                Quaternion.AngleAxis(lookPitch, Vector3.right);
        float blend = 1f - Mathf.Exp(-cameraTransitionSpeed * Time.deltaTime);
        gameCamera.transform.position = Vector3.Lerp(gameCamera.transform.position, view.position, blend);
        gameCamera.transform.rotation = Quaternion.Slerp(gameCamera.transform.rotation, rotation, blend);
        gameCamera.fieldOfView = Mathf.Lerp(gameCamera.fieldOfView, fov, blend);
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
            gameCamera.transform.SetPositionAndRotation(savedCameraPosition, savedCameraRotation);
            gameCamera.fieldOfView = savedFieldOfView;
            gameCamera.nearClipPlane = savedNearClip;
        }
        if (playerRenderers != null)
            for (int i = 0; i < playerRenderers.Length; i++)
                if (playerRenderers[i] != null) playerRenderers[i].enabled = rendererStates[i];
        if (playerBody != null) playerBody.isKinematic = bodyWasKinematic;
        if (playerMovement != null) playerMovement.enabled = movementWasEnabled;
        if (cameraFollow != null) cameraFollow.enabled = followWasEnabled;
        if (coworkerInteraction != null) coworkerInteraction.enabled = coworkerWasEnabled;
    }

    private void OnApplicationFocus(bool hasFocus) { if (!hasFocus) EndLookAround(); }
    private void OnDisable() { StandUp(); }

    private void OnGUI()
    {
        if (!initialized || CozyGameMenu.BlocksGameplay) return;
        if (!seated && !IsAtDesk()) return;
        Matrix4x4 previousMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
            new Vector3(Screen.width / 1280f, Screen.height / 720f, 1f));
        string instructions;
        if (usingComputer) instructions = "Esc or F: Return to seated view";
        else if (seated) instructions = enableSeatedLook
            ? "Hold L + mouse: Look around\nC: Use computer\nF or Esc: Stand up"
            : "C: Use computer\nF or Esc: Stand up";
        else instructions = "F: Sit at your desk";
        CozyOverlayGUI.Draw(instructions, 460f, 700f, false);
        GUI.matrix = previousMatrix;
    }
}
