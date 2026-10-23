using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace CozyOfficeDemo
{
    public static class CozyOfficeDemoBuilder
    {
        private static Transform root;
        private const string MaterialFolder = "Assets/CozyOfficeDemo/Materials";

        [MenuItem("Cozy Office/Build Meeting Demo")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play mode first."); return; }
            var scene = SceneManager.GetActiveScene();
            if (scene.name != "Shaun's Sample Scene")
            { EditorUtility.DisplayDialog("Open the sample scene", "Open Shaun's Sample Scene, then run this menu again.", "OK"); return; }
            if (GameObject.Find("Cozy Office Meeting Demo") != null)
            { EditorUtility.DisplayDialog("Demo already exists", "The demo is already in this scene. Press Play to try it.", "OK"); return; }
            var player = Object.FindFirstObjectByType<PlayerPrototype>();
            if (player == null) { Debug.LogError("The scene needs your existing PlayerPrototype character."); return; }
            if (!AssetDatabase.IsValidFolder(MaterialFolder)) AssetDatabase.CreateFolder("Assets/CozyOfficeDemo", "Materials");
            var navy = Mat("Navy", new Color(.10f,.19f,.24f));
            var cream = Mat("Cream", new Color(.84f,.83f,.75f));
            var wood = Mat("Oak", new Color(.60f,.40f,.24f));
            var teal = Mat("Teal", new Color(.16f,.44f,.44f));
            var white = Mat("Paper", new Color(.96f,.96f,.91f));
            var dark = Mat("Equipment", new Color(.16f,.18f,.20f));
            var gold = Mat("Gold", new Color(.96f,.65f,.22f));
            root = new GameObject("Cozy Office Meeting Demo").transform;
            Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Cozy Office demo");
            Box("Office floor", new Vector3(0,-.14f,0), new Vector3(14,.25f,12), cream);
            // Open walls on the camera side keep the player visible.
            Box("Back wall", new Vector3(0,1.6f,6), new Vector3(14,3.2f,.2f), cream);
            Box("Right wall", new Vector3(7,1.6f,0), new Vector3(.2f,3.2f,12), cream);
            Box("Front boundary", new Vector3(0,-.02f,-6), new Vector3(14,.4f,.2f), navy);
            Box("Left boundary", new Vector3(-7,-.02f,0), new Vector3(.2f,.4f,12), navy);
            Box("Back skirting", new Vector3(0,.15f,5.85f), new Vector3(14,.3f,.12f), navy);
            Box("Office rug", new Vector3(0,.002f,0), new Vector3(6,.015f,5), teal, false);
            for (int i=0;i<3;i++)
            {
                float x=-4+i*3.6f;
                Box("Window frame", new Vector3(x,2.05f,5.84f), new Vector3(2.6f,1.5f,.08f), navy, false);
                Box("Daylight window", new Vector3(x,2.05f,5.77f), new Vector3(2.35f,1.28f,.04f), Mat("Sky",new Color(.58f,.77f,.84f)), false);
            }
            var desk = Desk(new Vector3(2.6f,0,2), wood, dark, teal);
            Desk(new Vector3(-2.6f,0,3.5f), wood, dark, teal);
            Box("Printer cabinet",new Vector3(-4.8f,.5f,-1.7f),new Vector3(1.7f,1,1.1f),navy);
            var printer = Prop("Int_Props/Printer_01",new Vector3(-4.8f,1.01f,-1.7f),1.2f);
            var paper=Box("Collectible printout",new Vector3(-4.8f,1.51f,-1.9f),new Vector3(.6f,.025f,.42f),white,false);
            Label("PRINT & PANIC",new Vector3(-4.8f,2.15f,-1.7f),gold);
            Label("YOUR DESK",new Vector3(2.6f,2.15f,2),gold);
            Label("SAME TEAM. BRIGHTER DAYS.",new Vector3(0,3.0f,5.6f),navy);
            Box("Break counter",new Vector3(4.9f,.5f,-2.3f),new Vector3(2,1,1),wood);
            Prop("Int_Food/CoffeeCup_01",new Vector3(4.9f,1.01f,-2.3f),.24f);
            Prop("Int_Props/VendingMachine_01",new Vector3(5.8f,0,3.8f),1.2f);
            Label("COFFEE = PRODUCTIVITY",new Vector3(4.8f,2.15f,-2.3f),navy);
            var task=root.gameObject.AddComponent<CozyOfficeDemoTask>();
            task.player=player.transform; task.printer=printer.transform; task.desk=desk.transform; task.printout=paper;
            Undo.RecordObject(player.transform,"Position demo player");
            player.transform.position=new Vector3(0,.6f,-2.8f);
            var rb=player.GetComponent<Rigidbody>();
            if(rb != null) { Undo.RecordObject(rb,"Smooth demo movement"); rb.interpolation=RigidbodyInterpolation.Interpolate; }
            var cam=Camera.main;
            if(cam != null)
            {
                Undo.RecordObject(cam,"Frame demo camera");
                Undo.RecordObject(cam.transform,"Frame demo camera");
                cam.orthographic=true; cam.orthographicSize=7.5f; cam.farClipPlane=150;
                var follow=cam.GetComponent<IsometricCameraFollow>();
                Vector3 offset=new Vector3(-9,11,-9);
                Vector3 rotation=Quaternion.LookRotation(-offset).eulerAngles;
                if(follow != null) { Undo.RecordObject(follow,"Set demo camera follow"); follow.target=player.transform; follow.offset=offset; follow.cameraRotation=rotation; }
                cam.transform.position=player.transform.position+offset;
                cam.transform.rotation=Quaternion.Euler(rotation);
            }
            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject=root.gameObject;
            SceneView.lastActiveSceneView?.FrameSelected();
            Debug.Log("Cozy Office demo built. Save the scene (Cmd+S), then press Play. WASD to move; E at the printer, then E at your desk. R resets tasks.");
        }

        private static Material Mat(string name, Color color)
        {
            string path=MaterialFolder+"/"+name+".mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat != null) return mat;
            var shader=Shader.Find("Universal Render Pipeline/Lit");
            if(shader == null) shader=Shader.Find("Standard");
            mat=new Material(shader) { name=name, color=color };
            AssetDatabase.CreateAsset(mat,path); return mat;
        }
        private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool solid=true)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name=name; go.transform.SetParent(root); go.transform.position=position; go.transform.localScale=scale;
            go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }
        private static GameObject Desk(Vector3 p,Material wood,Material dark,Material teal)
        {
            var desk=Prop("Int_Furniture/Desk1",p,2.6f);
            var bounds=BoundsOf(desk);
            float top=bounds.max.y;
            Prop("Int_Props/Laptop_01",new Vector3(p.x,top+.01f,p.z),.8f);
            Prop("Int_Furniture/WorkChair1_C1",p+new Vector3(0,0,-1.3f),.75f);
            Prop("Int_Props/DeskOrganizer_01",new Vector3(p.x+.8f,top+.01f,p.z),.3f);
            Prop("Int_Food/CoffeeCup_01",new Vector3(p.x-.8f,top+.01f,p.z-.15f),.18f);
            return desk;
        }
        private static Bounds BoundsOf(GameObject go)
        {
            var renderers=go.GetComponentsInChildren<Renderer>();
            var bounds=new Bounds(go.transform.position,Vector3.zero);
            if(renderers.Length > 0) { bounds=renderers[0].bounds; foreach(var r in renderers) bounds.Encapsulate(r.bounds); }
            return bounds;
        }
        private static GameObject Prop(string relativePath,Vector3 bottomCenter,float width)
        {
            string path="Assets/LowPolyOfficeInterior/Prefabs/"+relativePath+".prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if(asset == null) throw new System.InvalidOperationException("Missing office prefab: "+path);
            var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.transform.SetParent(root); go.transform.position=Vector3.zero;
            var bounds=BoundsOf(go);
            if(bounds.size.x > .001f) go.transform.localScale *= width/bounds.size.x;
            bounds=BoundsOf(go);
            go.transform.position+=bottomCenter-new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            foreach(var renderer in go.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)
                {
                    var source=materials[i];
                    if(source == null || source.shader == null || source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                    string materialPath=MaterialFolder+"/Office_"+AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source))+".mat";
                    var converted=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                    if(converted == null)
                    {
                        converted=new Material(Shader.Find("Universal Render Pipeline/Lit"));
                        converted.name=source.name+" Demo URP";
                        if(source.HasProperty("_Color")) converted.SetColor("_BaseColor",source.GetColor("_Color"));
                        if(source.HasProperty("_MainTex"))
                        {
                            converted.SetTexture("_BaseMap",source.GetTexture("_MainTex"));
                            converted.SetTextureScale("_BaseMap",source.GetTextureScale("_MainTex"));
                            converted.SetTextureOffset("_BaseMap",source.GetTextureOffset("_MainTex"));
                        }
                        converted.SetFloat("_Smoothness",.15f);
                        AssetDatabase.CreateAsset(converted,materialPath);
                    }
                    materials[i]=converted;
                }
                renderer.sharedMaterials=materials;
            }
            return go;
        }
        private static void Label(string text, Vector3 position, Material color)
        {
            var go=new GameObject(text); go.transform.SetParent(root); go.transform.position=position;
            go.transform.rotation=Quaternion.Euler(35,45,0);
            var label=go.AddComponent<TextMesh>(); label.text=text; label.fontSize=60; label.characterSize=.035f;
            label.anchor=TextAnchor.MiddleCenter; label.alignment=TextAlignment.Center; label.color=color.color;
        }
    }
}
