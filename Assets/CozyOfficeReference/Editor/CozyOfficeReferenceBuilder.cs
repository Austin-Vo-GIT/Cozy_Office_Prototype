using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace CozyOffice.ReferenceLayout
{
    // Editor-only: all generated geometry is saved as normal editable scene objects.
    public static class CozyOfficeReferenceBuilder
    {
        const string Folder = "Assets/CozyOfficeReference";
        [Serializable] public class Layout { public Entry[] items; }
        [Serializable] public class Entry
        {
            public string name, group, guid, material;
            public Vector3 position, size;
            public float yaw;
            public bool solid;
        }
        static readonly Dictionary<string, Material> palette = new Dictionary<string, Material>();
        static readonly Dictionary<Material, Material> converted = new Dictionary<Material, Material>();
        static string materialsFolder;
        static Shader shader;

        [MenuItem("Cozy Office/Reference Layout/Create New Office Scene")]
        public static void CreateScene()
        {
            Layout layout = LoadAndValidate();
            if (layout == null || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string scenePath = EditorUtility.SaveFilePanelInProject("Save office scene", "CozyOffice_Starter", "unity", "Choose a new scene filename.");
            if (string.IsNullOrEmpty(scenePath)) return;
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Build(layout);
            AddPresentation();
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), scenePath);
            Debug.Log("Office created at " + scenePath + ". Place your existing player at PlayerSpawn (1.2, 0, -4.7). This scene contains an environment and camera, not a player controller.");
        }

        [MenuItem("Cozy Office/Reference Layout/Add Environment To Current Scene")]
        public static void AddEnvironment()
        {
            Layout layout = LoadAndValidate();
            if (layout == null) return;
            Build(layout);
            EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        static Layout LoadAndValidate()
        {
            var json = AssetDatabase.LoadAssetAtPath<TextAsset>(Folder + "/OfficeLayout.json");
            if (!json) { Debug.LogError("OfficeLayout.json is missing from " + Folder); return null; }
            var layout = JsonUtility.FromJson<Layout>(json.text);
            if (layout == null || layout.items == null) { Debug.LogError("Invalid office layout."); return null; }
            var missing = new HashSet<string>();
            foreach (var e in layout.items)
                if (!string.IsNullOrEmpty(e.guid) && !AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(e.guid)))
                    missing.Add(e.name + " (" + e.guid + ")");
            if (missing.Count > 0)
            {
                Debug.LogError("Missing original office prefabs. Import the LowPolyOfficeInterior assets from your uploaded project with their original .meta files first:\n" + string.Join("\n", missing));
                return null;
            }
            return layout;
        }

        static void Build(Layout layout)
        {
            palette.Clear(); converted.Clear();
            shader = GraphicsSettings.currentRenderPipeline == null ? Shader.Find("Standard") : Shader.Find("Universal Render Pipeline/Lit");
            if (!shader) throw new InvalidOperationException("This builder supports Built-in and URP. The required Lit shader was not found.");
            if (!AssetDatabase.IsValidFolder(Folder + "/Generated")) AssetDatabase.CreateFolder(Folder, "Generated");
            materialsFolder = AssetDatabase.GenerateUniqueAssetPath(Folder + "/Generated/Materials");
            AssetDatabase.CreateFolder(Folder + "/Generated", Path.GetFileName(materialsFolder));
            var root = new GameObject("Cozy Office - Reference Layout");
            var groups = new Dictionary<string, Transform>();
            try
            {
                foreach (var e in layout.items)
                {
                    Transform group = Group(e.group, root.transform, groups);
                    GameObject go;
                    if (string.IsNullOrEmpty(e.guid))
                    {
                        go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        go.transform.localScale = e.size;
                        go.GetComponent<Renderer>().sharedMaterial = ColorMaterial(e.material);
                        go.transform.position = e.position + Vector3.up * e.size.y / 2;
                    }
                    else
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(e.guid));
                        go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                        go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                        go.transform.localScale = Vector3.one;
                        Bounds bounds = BoundsOf(go);
                        go.transform.localScale = new Vector3(e.size.x / Mathf.Max(bounds.size.x, .001f), e.size.y / Mathf.Max(bounds.size.y, .001f), e.size.z / Mathf.Max(bounds.size.z, .001f));
                        // Rotate after sizing in model axes; center and floor-anchor using final visible bounds.
                        go.transform.rotation = Quaternion.Euler(0, e.yaw, 0);
                        bounds = BoundsOf(go);
                        go.transform.position += e.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                        foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                        {
                            var mats = renderer.sharedMaterials;
                            for (int i = 0; i < mats.Length; i++)
                                mats[i] = string.IsNullOrEmpty(e.material) ? CompatibleMaterial(mats[i]) : ColorMaterial(e.material);
                            renderer.sharedMaterials = mats;
                        }
                        if (e.solid && go.GetComponentsInChildren<Collider>().Length == 0)
                        {
                            // Use local mesh bounds for a stable collider independent of rotation.
                            Quaternion rotation = go.transform.rotation;
                            Vector3 position = go.transform.position;
                            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                            bounds = BoundsOf(go);
                            var c = go.AddComponent<BoxCollider>();
                            c.center = go.transform.InverseTransformPoint(bounds.center);
                            c.size = new Vector3(bounds.size.x / go.transform.localScale.x, bounds.size.y / go.transform.localScale.y, bounds.size.z / go.transform.localScale.z);
                            go.transform.SetPositionAndRotation(position, rotation);
                        }
                    }
                    if (string.IsNullOrEmpty(e.guid)) go.transform.rotation = Quaternion.Euler(0, e.yaw, 0);
                    go.name = e.name;
                    go.transform.SetParent(group, true);
                    if (!e.solid) foreach (Collider c in go.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
                }
                var spawn = new GameObject("PlayerSpawn - place feet here");
                spawn.transform.SetParent(root.transform);
                spawn.transform.position = new Vector3(1.2f, 0, -4.7f);
                Undo.RegisterCreatedObjectUndo(root, "Create reference office");
                Selection.activeGameObject = root;
                if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.FrameSelected();
                AssetDatabase.SaveAssets();
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(root);
                throw;
            }
        }
        static Transform Group(string path, Transform root, Dictionary<string, Transform> groups)
        {
            if (groups.TryGetValue(path, out var existing)) return existing;
            int slash = path.LastIndexOf('/');
            Transform parent = slash < 0 ? root : Group(path.Substring(0, slash), root, groups);
            var go = new GameObject(slash < 0 ? path : path.Substring(slash + 1));
            go.transform.SetParent(parent, false);
            groups[path] = go.transform;
            return go.transform;
        }
        static Bounds BoundsOf(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible mesh: " + go.name);
            Bounds b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
            return b;
        }
        static Material ColorMaterial(string key)
        {
            if (palette.TryGetValue(key, out var cached)) return cached;
            Color c;
            switch (key)
            {
                case "carpet": c = new Color(.40f,.41f,.43f); break;
                case "carpet2": c = new Color(.415f,.425f,.445f); break;
                case "partition": c = new Color(.51f,.52f,.54f); break;
                case "trim": c = new Color(.20f,.21f,.23f); break;
                case "glass": c = new Color(.49f,.63f,.71f); break;
                case "wood": c = new Color(.43f,.28f,.16f); break;
                case "paper": c = new Color(.91f,.90f,.85f); break;
                case "rug": c = new Color(.61f,.57f,.49f); break;
                default: c = new Color(.68f,.66f,.63f); break;
            }
            var m = new Material(shader) { name = "Reference_" + key, color = c };
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", .12f);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", .12f);
            AssetDatabase.CreateAsset(m, materialsFolder + "/" + m.name + ".mat");
            palette[key] = m;
            return m;
        }
        static Material CompatibleMaterial(Material source)
        {
            if (!source) return ColorMaterial("wall");
            if (GraphicsSettings.currentRenderPipeline == null || (source.shader && source.shader.name.StartsWith("Universal Render Pipeline/"))) return source;
            if (converted.TryGetValue(source, out var cached)) return cached;
            var m = new Material(shader) { name = source.name + "_ReferenceURP" };
            Texture texture = source.HasProperty("_MainTex") ? source.GetTexture("_MainTex") : source.mainTexture;
            m.SetTexture("_BaseMap", texture);
            m.SetColor("_BaseColor", source.HasProperty("_Color") ? source.GetColor("_Color") : Color.white);
            if (source.HasProperty("_MainTex"))
            {
                m.SetTextureScale("_BaseMap", source.GetTextureScale("_MainTex"));
                m.SetTextureOffset("_BaseMap", source.GetTextureOffset("_MainTex"));
            }
            m.SetFloat("_Smoothness", .15f);
            AssetDatabase.CreateAsset(m, AssetDatabase.GenerateUniqueAssetPath(materialsFolder + "/" + m.name + ".mat"));
            converted[source] = m;
            return m;
        }
        static void AddPresentation()
        {
            var cameraObject = new GameObject("Reference Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.orthographic = true;
            camera.orthographicSize = 9.7f;
            camera.nearClipPlane = .1f; camera.farClipPlane = 100;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.23f,.25f,.28f);
            cameraObject.transform.position = new Vector3(2.2f, 17, -19);
            cameraObject.transform.LookAt(new Vector3(0, 0, 0));
            var sun = new GameObject("Soft daylight").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f,.95f,.88f);
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50, -35, 0);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.65f,.68f,.73f);
        }
    }
}
