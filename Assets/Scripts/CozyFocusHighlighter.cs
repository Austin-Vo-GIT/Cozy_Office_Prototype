using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Select T interactions before gameplay Update; draw after desk hover LateUpdate.
[DefaultExecutionOrder(-500)]
[RequireComponent(typeof(CozyFocusPresenter))]
public class CozyFocusHighlighter : MonoBehaviour
{
    public struct Candidate
    {
        public MonoBehaviour owner;
        public GameObject target;
        public float distance;
        public int priority;
        public Candidate(MonoBehaviour owner, GameObject target, float distance, int priority)
        {
            this.owner = owner; this.target = target;
            this.distance = distance; this.priority = priority;
        }
    }

    [Header("Task Sources")]
    [SerializeField] private CozyNPCInteraction coworkerTask;
    [SerializeField] private PrinterCleanupTask printerTask;
    [Header("Outline Shaders - assign both supplied shader assets")]
    [SerializeField] private Shader maskShader;
    [SerializeField] private Shader edgeShader;
    [Header("Appearance")]
    [SerializeField] private Color outlineColor = new Color(0.1f, 1f, 0.85f, 1f);
    [SerializeField, Range(1f, 8f)] private float widthPixels = 3f;
    [SerializeField, Range(1f, 10f)] private float glowBrightness = 4f;

    private static CozyFocusHighlighter instance;
    private Candidate selected;
    private GameObject deskTarget;
    private int deskFrame = -1;
    private GameObject shownTarget;
    private Material maskMaterial;
    private readonly List<Material> edgeMaterials = new List<Material>();
    private readonly List<GameObject> copies = new List<GameObject>();
    private readonly List<Renderer> originals = new List<Renderer>();
    private readonly List<Renderer> copyRenderers = new List<Renderer>();
    private bool ready;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { instance = null; }

    private void OnEnable()
    {
        if (instance != null && instance != this)
        {
            Debug.LogError("Use only one CozyFocusHighlighter per scene.", this);
            enabled = false;
            return;
        }
        instance = this;
        if (maskShader == null) maskShader = Shader.Find("CozyOffice/FocusMask");
        if (edgeShader == null) edgeShader = Shader.Find("CozyOffice/FocusEdge");
        ready = maskShader != null && edgeShader != null
            && maskShader.isSupported && edgeShader.isSupported;
        if (!ready)
        {
            Debug.LogError("Assign the two URP focus shaders on CozyFocusHighlighter.", this);
            return;
        }
        maskMaterial = new Material(maskShader) { hideFlags = HideFlags.DontSave };
        for (int i = 0; i < 8; i++)
            edgeMaterials.Add(new Material(edgeShader) { hideFlags = HideFlags.DontSave });
        RefreshMaterials();
    }

    // With no coordinator installed, existing task behavior remains available.
    public static bool AllowsTask(MonoBehaviour owner)
    {
        return instance == null || !instance.isActiveAndEnabled
            || instance.selected.owner == owner;
    }

    public static void SetDeskTarget(GameObject target)
    {
        if (instance == null) return;
        instance.deskTarget = target;
        instance.deskFrame = Time.frameCount;
    }

    private void ChooseTask()
    {
        selected = default;
        if (CozyGameMenu.BlocksGameplay || !Application.isFocused) return;
        if (coworkerTask != null) Consider(coworkerTask.GetFocusCandidate());
        if (printerTask != null) Consider(printerTask.GetFocusCandidate());
    }

    private void Consider(Candidate candidate)
    {
        if (candidate.owner == null) return;
        if (selected.owner == null || candidate.priority > selected.priority
            || (candidate.priority == selected.priority && candidate.distance < selected.distance))
            selected = candidate;
    }

    private void Update() { ChooseTask(); }

    // Called by the late presenter, after all interaction scripts have updated.
    public void PresentFocus()
    {
        if (!isActiveAndEnabled) return;
        ChooseTask();
        GameObject target = null;
        if (!CozyGameMenu.BlocksGameplay && Application.isFocused)
            target = deskFrame == Time.frameCount && deskTarget != null
                ? deskTarget : selected.target;
        if (target != null && !target.activeInHierarchy) target = null;
        if (!ready) return;
        RefreshMaterials();
        if (target != shownTarget)
        {
            ClearCopies();
            shownTarget = target;
            if (target != null) BuildCopies(target);
        }
        for (int i = 0; i < copyRenderers.Count; i++)
            if (copyRenderers[i] != null)
                copyRenderers[i].enabled = originals[i] != null
                    && originals[i].enabled && originals[i].gameObject.activeInHierarchy;
    }

    private void RefreshMaterials()
    {
        Color hdr = outlineColor * glowBrightness;
        hdr.a = 1f;
        for (int i = 0; i < edgeMaterials.Count; i++)
        {
            float angle = i * Mathf.PI / 4f;
            edgeMaterials[i].SetVector("_PixelOffset",
                new Vector4(Mathf.Cos(angle) * widthPixels, Mathf.Sin(angle) * widthPixels, 0, 0));
            edgeMaterials[i].SetColor("_OutlineColor", hdr);
        }
    }

    private void BuildCopies(GameObject target)
    {
        // Copies share mesh data; they do not alter original materials, meshes or colliders.
        MeshFilter[] filters = target.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter filter in filters)
        {
            MeshRenderer source = filter.GetComponent<MeshRenderer>();
            if (source == null || filter.sharedMesh == null) continue;
            AddCopy(filter, source, maskMaterial);
            foreach (Material material in edgeMaterials) AddCopy(filter, source, material);
        }
        if (copies.Count == 0)
            Debug.LogWarning("Focus outline needs a MeshFilter + MeshRenderer on this prop or its children.", target);
    }

    private void AddCopy(MeshFilter filter, MeshRenderer source, Material material)
    {
        GameObject copy = new GameObject("Focus visual (runtime)");
        copy.hideFlags = HideFlags.DontSave;
        copy.layer = source.gameObject.layer;
        copy.transform.SetParent(filter.transform, false);
        copy.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
        MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
        Material[] materials = new Material[filter.sharedMesh.subMeshCount];
        for (int i = 0; i < materials.Length; i++) materials[i] = material;
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        copies.Add(copy);
        originals.Add(source);
        copyRenderers.Add(renderer);
    }

    private void ClearCopies()
    {
        foreach (GameObject copy in copies)
        {
            if (copy == null) continue;
            copy.SetActive(false);
            Destroy(copy);
        }
        copies.Clear(); originals.Clear(); copyRenderers.Clear();
        shownTarget = null;
    }

    private void OnDisable()
    {
        ClearCopies();
        if (maskMaterial != null) Destroy(maskMaterial);
        foreach (Material material in edgeMaterials) if (material != null) Destroy(material);
        edgeMaterials.Clear();
        ready = false;
        if (instance == this) instance = null;
    }
}
