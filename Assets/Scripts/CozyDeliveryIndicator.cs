using UnityEngine;
using UnityEngine.Rendering;

[DefaultExecutionOrder(1100)]
public class CozyDeliveryIndicator : MonoBehaviour
{
    public enum DeliveryTask { CoworkerPaperwork, PrinterCleanup }

    [Header("Task")]
    [SerializeField] private DeliveryTask task;
    [SerializeField] private CozyNPCInteraction coworkerTask;
    [SerializeField] private PrinterCleanupTask printerTask;

    [Header("Position and Camera")]
    [Tooltip("An empty object on the desktop where the stack will appear.")]
    [SerializeField] private Transform placementSpot;
    [SerializeField] private Camera gameCamera;

    [Header("Appearance")]
    [SerializeField] private Shader arrowShader;
    [SerializeField] private Color arrowColor = new Color(0.1f, 1f, 0.85f, 1f);
    [SerializeField, Range(1f, 10f)] private float glowBrightness = 4f;
    [SerializeField, Min(0.01f)] private float arrowWidth = 0.25f;
    [SerializeField, Min(0.01f)] private float arrowHeight = 0.4f;
    [Tooltip("Minimum distance of the arrow tip above Placement Spot.")]
    [SerializeField, Min(0f)] private float hoverHeight = 0.18f;
    [Tooltip("Total vertical travel from lowest to highest position.")]
    [SerializeField, Min(0f)] private float bobDistance = 0.08f;
    [SerializeField, Min(0.1f)] private float bobCyclesPerSecond = 0.7f;
    [SerializeField] private bool hideWhileMenuOpen = true;
    [Tooltip("Leave on temporarily to position the arrow during Play Mode.")]
    [SerializeField] private bool previewInPlayMode;

    private GameObject visual;
    private Mesh arrowMesh;
    private Material arrowMaterial;

    private void OnEnable()
    {
        if (arrowShader == null) arrowShader = Shader.Find("CozyOffice/DeliveryArrow");
        if (arrowShader == null || !arrowShader.isSupported)
        {
            Debug.LogError("CozyDeliveryIndicator: Assign CozyDeliveryArrow.shader.", this);
            return;
        }
        arrowMesh = new Mesh { name = "Delivery arrow (runtime)", hideFlags = HideFlags.DontSave };
        // The arrow tip is the origin; its shaft extends upward.
        arrowMesh.vertices = new[] {
            new Vector3(-0.18f, 1f, 0f), new Vector3(0.18f, 1f, 0f),
            new Vector3(0.18f, 0.45f, 0f), new Vector3(-0.18f, 0.45f, 0f),
            new Vector3(-0.5f, 0.45f, 0f), new Vector3(0.5f, 0.45f, 0f),
            Vector3.zero
        };
        arrowMesh.triangles = new[] { 0, 1, 2, 0, 2, 3, 4, 5, 6 };
        arrowMesh.RecalculateBounds();
        arrowMaterial = new Material(arrowShader) { hideFlags = HideFlags.DontSave };
        visual = new GameObject("Delivery arrow visual (runtime)");
        visual.hideFlags = HideFlags.DontSave;
        visual.layer = gameObject.layer;
        visual.SetActive(false);
        visual.AddComponent<MeshFilter>().sharedMesh = arrowMesh;
        MeshRenderer renderer = visual.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = arrowMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        if (gameCamera == null) gameCamera = Camera.main;
        bool awaitingDelivery = task == DeliveryTask.CoworkerPaperwork
            ? coworkerTask != null && coworkerTask.IsAwaitingDelivery
            : printerTask != null && printerTask.IsAwaitingDelivery;
        bool show = (awaitingDelivery || previewInPlayMode) && placementSpot != null
            && placementSpot.gameObject.activeInHierarchy && gameCamera != null
            && (!hideWhileMenuOpen || !CozyGameMenu.BlocksGameplay);
        visual.SetActive(show);
        if (!show) return;

        float bob = (Mathf.Sin(Time.time * Mathf.PI * 2f * bobCyclesPerSecond) + 1f)
            * 0.5f * bobDistance;
        visual.transform.SetPositionAndRotation(
            placementSpot.position + Vector3.up * (hoverHeight + bob),
            gameCamera.transform.rotation);
        visual.transform.localScale = new Vector3(arrowWidth, arrowHeight, 1f);
        Color hdr = arrowColor * glowBrightness;
        hdr.a = 1f;
        arrowMaterial.SetColor("_ArrowColor", hdr);
    }

    private void OnDisable()
    {
        if (visual != null) { visual.SetActive(false); Destroy(visual); }
        if (arrowMaterial != null) Destroy(arrowMaterial);
        if (arrowMesh != null) Destroy(arrowMesh);
    }
}
