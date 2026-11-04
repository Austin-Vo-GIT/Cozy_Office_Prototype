using UnityEngine;
using UnityEngine.InputSystem;

public class CozyDeskSeat : MonoBehaviour
{
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

    private bool movementWasEnabled;
    private bool followWasEnabled;
    private bool coworkerWasEnabled;
    private bool bodyWasKinematic;
    private bool shopWasEnabled;

    private bool savedCursorVisible;
    private CursorLockMode savedCursorLock;

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
        if (keyboard == null) return;

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

            if (keyboard.cKey.wasPressedThisFrame)
            {
                OpenComputer();
                return;
            }

            if (keyboard.fKey.wasPressedThisFrame ||
                keyboard.escapeKey.wasPressedThisFrame)
            {
                StandUp();
            }

            return;
        }

        if (IsAtDesk() && keyboard.fKey.wasPressedThisFrame)
        {
            SitDown();
        }
    }

    private void SitDown()
    {
        if (seated) return;

        // Remember the walking camera's current settings.
        savedCameraPosition = gameCamera.transform.position;
        savedCameraRotation = gameCamera.transform.rotation;
        savedFieldOfView = gameCamera.fieldOfView;
        savedNearClip = gameCamera.nearClipPlane;

        movementWasEnabled = playerMovement.enabled;
        followWasEnabled = cameraFollow.enabled;
        bodyWasKinematic = playerBody.isKinematic;

        playerMovement.enabled = false;
        cameraFollow.enabled = false;

        // Stop motion before freezing physics movement.
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

        seated = true;

        // Enter the wider seated view immediately.
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

        usingComputer = true;

        savedCursorVisible = Cursor.visible;
        savedCursorLock = Cursor.lockState;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Turn on the monitor and allow desktop clicks.
        desktop.gameObject.SetActive(true);
        desktop.interactable = true;
        desktop.blocksRaycasts = true;

        // Hide the shop HUD and pause the B purchase shortcut.
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
            targetView.rotation,
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
        if (!seated) return;

        CloseComputer();
        seated = false;

        // Restore the walking camera.
        if (gameCamera != null)
        {
            gameCamera.transform.SetPositionAndRotation(
                savedCameraPosition,
                savedCameraRotation
            );

            gameCamera.fieldOfView = savedFieldOfView;
            gameCamera.nearClipPlane = savedNearClip;
        }

        // Restore each renderer's previous visibility.
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
            instructions = "C: Use computer\nF or Esc: Stand up";
        }
        else
        {
            instructions = "Press F to sit at your desk";
        }

        GUILayout.BeginArea(
            new Rect(20, 610, 380, 90),
            GUI.skin.box
        );

        GUILayout.Label(instructions, textStyle);

        GUILayout.EndArea();

        GUI.matrix = previousMatrix;
    }
}