using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace AdvancedInspector
{
    /// <summary>
    /// This class handles the rendering and input of the Preview zone.
    /// </summary>
    public class InspectorPreview
    {
        private enum PreviewType
        { 
            GameObject,
            Mesh,
            Material,
            Texture2D,
            Cubemap
        }

        private static PreviewRenderUtility previewUtility;

        private static int sliderHash;

        private static GUIContent[] meshIcons = new GUIContent[4];
        private static GUIContent[] lightIcons = new GUIContent[3];

        private static int mesh = 0;
        private static int light = 0;

        private static int index = 0;

        private static PreviewType type = PreviewType.GameObject;

        private static UnityEngine.Object[] targets = new GameObject[0];

        private static bool layouted = false;

        #region Textures
        private static Texture scrollLeft;

        internal static Texture ScrollLeft
        {
            get
            {
                if (scrollLeft == null)
                    scrollLeft = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollLeft.png");

                return scrollLeft;
            }
        }

        private static Texture scrollRight;

        internal static Texture ScrollRight
        {
            get
            {
                if (scrollRight == null)
                    scrollRight = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "ScrollRight.png");

                return scrollRight;
            }
        }

        private static Texture cube;

        internal static Texture Cube
        {
            get
            {
                if (cube == null)
                    cube = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Cube.png");

                return cube;
            }
        }

        private static Texture cylinder;

        internal static Texture Cylinder
        {
            get
            {
                if (cylinder == null)
                    cylinder = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Cylinder.png");

                return cylinder;
            }
        }

        private static Texture capsule;

        internal static Texture Capsule
        {
            get
            {
                if (capsule == null)
                    capsule = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Capsule.png");

                return capsule;
            }
        }

        private static Texture sphere;

        internal static Texture Sphere
        {
            get
            {
                if (sphere == null)
                    sphere = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Sphere.png");

                return sphere;
            }
        }

        private static Texture previewLight1;

        internal static Texture PreviewLight1
        {
            get
            {
                if (previewLight1 == null)
                    previewLight1 = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "PreviewLight1.png");

                return previewLight1;
            }
        }

        private static Texture previewLight2;

        internal static Texture PreviewLight2
        {
            get
            {
                if (previewLight2 == null)
                    previewLight2 = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "PreviewLight2.png");

                return previewLight2;
            }
        }

        private static Texture previewLight3;

        internal static Texture PreviewLight3
        {
            get
            {
                if (previewLight3 == null)
                    previewLight3 = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "PreviewLight3.png");

                return previewLight3;
            }
        }
        #endregion

        /// <summary>
        /// Object to be previewed
        /// Supported type;
        /// GameObject
        /// Mesh
        /// Material
        /// Texture
        /// </summary>
        public static UnityEngine.Object[] Targets
        {
            get { return targets; }
            set 
            {
                bool different = false;
                if (targets.Length == value.Length)
                {
                    for (int i = 0; i < value.Length; i++)
                    {
                        if (targets[i] != value[i])
                        {
                            different = true;
                            break;
                        }
                    }
                }
                else
                    different = true;

                if (!different)
                    return;

                List<UnityEngine.Object> valid = new List<UnityEngine.Object>();
                foreach (UnityEngine.Object obj in value)
                    if (obj is GameObject || obj is Mesh || obj is Material || obj is Texture)
                        valid.Add(obj);

                targets = valid.ToArray();
                CreatePreviewInstances();
            }
        }

        private static GameObject previewInstance;
        private static Vector2 previewDir;

        private static GUIStyle toolbarLabel;
        private static GUIStyle button;

        private static void InitPreview()
        {
            sliderHash = "Slider".GetHashCode();
            if (previewUtility == null)
            {
                previewUtility = new PreviewRenderUtility(true);
                previewUtility.cameraFieldOfView = 30f;
                previewUtility.camera.cullingMask = -2147483648;

                Light[] lights = previewUtility.lights;
                for (int i = 0; i < lights.Length; i++)
                {
                    GameObject go = EditorUtility.CreateGameObjectWithHideFlags("PreRenderLight", HideFlags.HideAndDontSave, new Type[] { typeof(Light) });
                    lights[i] = go.GetComponent<Light>();
                    lights[i].type = LightType.Directional;
                    lights[i].intensity = 0.5f;
                    lights[i].enabled = false;
                }

                toolbarLabel = "preToolbar2";
                button = "preButton";

                meshIcons[0] = new GUIContent(Sphere);
                meshIcons[1] = new GUIContent(Cube);
                meshIcons[2] = new GUIContent(Cylinder);
                meshIcons[3] = new GUIContent(Capsule);

                lightIcons[0] = new GUIContent(PreviewLight1);
                lightIcons[1] = new GUIContent(PreviewLight2);
                lightIcons[2] = new GUIContent(PreviewLight3);

                CreatePreviewInstances();
            }
        }

        private static void CreatePreviewInstances()
        {
            DestroyPreviewInstances();

            if (targets.Length == 0)
                return;

            if (index >= targets.Length)
                index = targets.Length - 1;

            GameObject instance = null;

            if (targets[index] is GameObject)
            {
                type = PreviewType.GameObject;

                instance = GameObject.Instantiate(targets[index]) as GameObject;
            }
            else if (targets[index] is Mesh)
            {
                type = PreviewType.Mesh;

                instance = new GameObject("PreviewInstance");
                MeshFilter filter = instance.AddComponent<MeshFilter>();
                filter.sharedMesh = targets[index] as Mesh;
                MeshRenderer renderer = instance.AddComponent<MeshRenderer>();
                Material material = new Material(Shader.Find("Diffuse"));
                material.hideFlags = HideFlags.HideAndDontSave;
                renderer.sharedMaterial = material;
            }
            else if (targets[index] is Material)
            {
                type = PreviewType.Material;

                instance = GetPrimitive();
                instance.GetComponent<MeshRenderer>().sharedMaterial = targets[index] as Material;
            }
            else if (targets[index] is Texture2D)
            {
                type = PreviewType.Texture2D;

                instance = GetPrimitive();
                Material material = new Material(Shader.Find("Unlit/Transparent"));
                material.hideFlags = HideFlags.HideAndDontSave;
                material.SetTexture("_MainTex", targets[index] as Texture);
                instance.GetComponent<MeshRenderer>().sharedMaterial = material;
            }
            else if (targets[index] is Cubemap)
            {
                type = PreviewType.Cubemap;

                instance = GetPrimitive();
                Material material = new Material(Shader.Find("Reflective/Bumped Unlit"));
                material.hideFlags = HideFlags.HideAndDontSave;
                material.SetTexture("_Cube", targets[index] as Cubemap);
                instance.GetComponent<MeshRenderer>().sharedMaterial = material;
            }

            instance.hideFlags = HideFlags.HideAndDontSave;

            if (instance != null)
            {
                InitInstantiatedPreviewRecursive(instance);
                Animator component = instance.GetComponent(typeof(Animator)) as Animator;
                if (component != null)
                {
                    component.enabled = false;
                    component.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    component.logWarnings = false;
                    component.fireEvents = false;
                }

                SetEnabledRecursive(instance, false);
                previewInstance = instance;
            }
        }

        private static GameObject GetPrimitive()
        {
            if (type == PreviewType.Texture2D)
                return GameObject.CreatePrimitive(PrimitiveType.Quad);
            else if (type == PreviewType.Material || type == PreviewType.Cubemap)
            {
                if (mesh == 0)
                    return GameObject.CreatePrimitive(PrimitiveType.Sphere);
                else if (mesh == 1)
                    return GameObject.CreatePrimitive(PrimitiveType.Cube);
                else if (mesh == 2)
                    return GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                else if (mesh == 3)
                    return GameObject.CreatePrimitive(PrimitiveType.Capsule);
            }

            return new GameObject("Preview");
        }
    
        private static void DestroyPreviewInstances()
        {
            UnityEngine.Object.DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        private static void InitInstantiatedPreviewRecursive(GameObject go)
        {
            go.hideFlags = HideFlags.HideAndDontSave;
            go.layer = 0x1f;
            IEnumerator enumerator = go.transform.GetEnumerator();

            try
            {
                while (enumerator.MoveNext())
                {
                    Transform current = (Transform)enumerator.Current;
                    InitInstantiatedPreviewRecursive(current.gameObject);
                }
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        private static void SetEnabledRecursive(GameObject go, bool enabled)
        {
            foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>())
                renderer.enabled = enabled;
        }

        private static void DoRenderPreview()
        {
            if (previewInstance == null)
                return;

            GameObject go = previewInstance;
            Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
            GetRenderableBoundsRecurse(ref bounds, go);

            float extends = Mathf.Max(bounds.extents.magnitude, 0.0001f);
            float view = extends * 3.8f;

            Quaternion quaternion = Quaternion.identity;
            if (type != PreviewType.Texture2D)
                quaternion = Quaternion.Euler(-previewDir.y, -previewDir.x, 0f);
            Vector3 vector = bounds.center - (quaternion * (Vector3.forward * view));

            previewUtility.camera.transform.position = vector;
            previewUtility.camera.transform.rotation = quaternion;
            previewUtility.camera.nearClipPlane = view - (extends * 1.1f);
            previewUtility.camera.farClipPlane = view + (extends * 1.1f);

            if (light == 0)
            {
                previewUtility.lights[0].intensity = 0.5f;
                previewUtility.lights[0].color = new Color(0.7f, 0.6f, 0.4f);
                previewUtility.lights[0].transform.rotation = quaternion * Quaternion.Euler(30, 30, 0);
                previewUtility.lights[1].intensity = 0;
                previewUtility.lights[1].transform.rotation = quaternion;
            }
            else if (light == 1)
            {
                previewUtility.lights[0].intensity = 0.5f;
                previewUtility.lights[0].color = new Color(0.7f, 0.6f, 0.2f);
                previewUtility.lights[0].transform.rotation = quaternion * Quaternion.Euler(50, 50, 0);
                previewUtility.lights[1].intensity = 0.5f;
                previewUtility.lights[1].color = new Color(0.1f, 0.2f, 0.4f);
                previewUtility.lights[1].transform.rotation = quaternion;
            }
            else
            {
                previewUtility.lights[0].intensity = 0.5f;
                previewUtility.lights[0].color = Color.red;
                previewUtility.lights[0].transform.rotation = quaternion * Quaternion.Euler(0, 50, 0);
                previewUtility.lights[1].intensity = 0.5f;
                previewUtility.lights[1].color = Color.green;
                previewUtility.lights[1].transform.rotation = quaternion * Quaternion.Euler(50, 0, 0);
            }

            Color ambient = new Color(0.2f, 0.2f, 0.2f, 0f);
            InternalEditorUtility.SetCustomLighting(previewUtility.lights, ambient);

            bool fog = RenderSettings.fog;
            Unsupported.SetRenderSettingsUseFogNoDirty(false);
            SetEnabledRecursive(go, true);
            previewUtility.camera.Render();
            SetEnabledRecursive(go, false);
            Unsupported.SetRenderSettingsUseFogNoDirty(fog);
            InternalEditorUtility.RemoveCustomLighting();
        }

        internal static void OnPreviewGUI(Rect region, GUIStyle background)
        {
            if (!ShaderUtil.hardwareSupportsRectRenderTexture)
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DropShadowLabel(new Rect(region.x, region.y, region.width, 40f), "Preview requires\nrender texture support");
            }
            else
            {
                InitPreview();
                previewDir = Drag2D(previewDir, region);
                if (Event.current.type == EventType.Repaint)
                {
                    previewUtility.BeginPreview(region, background);
                    DoRenderPreview();
                    Texture image = previewUtility.EndPreview();
                    GUI.DrawTexture(region, image, ScaleMode.StretchToFill, false);
                }
            }
        }

        internal static void OnPreviewSettings()
        {
            if (targets.Length > 0 && targets[index])
            {
                if (Event.current.type == EventType.Repaint && !layouted)
                    return;
                else if (Event.current.type == EventType.Layout)
                    layouted = true;

                GUILayout.Label(targets[index].name + " (" + targets[index].GetType().Name + ")", toolbarLabel);
                GUILayout.Space(10);

                if (type == PreviewType.Material || type == PreviewType.Cubemap)
                {
                    EditorGUI.BeginChangeCheck();

                    mesh = CycleButton(mesh, meshIcons, button);

                    if (EditorGUI.EndChangeCheck())
                        CreatePreviewInstances();
                }

                if (type != PreviewType.Texture2D && type != PreviewType.Cubemap)
                    light = CycleButton(light, lightIcons, button);

                if (targets.Length > 1)
                {
                    if (GUILayout.Button(ScrollLeft, button))
                    {
                        index--;
                        if (index < 0)
                            index = targets.Length - 1;

                        CreatePreviewInstances();
                    }

                    if (GUILayout.Button(ScrollRight, button))
                    {
                        index++;
                        if (index >= targets.Length)
                            index = 0;

                        CreatePreviewInstances();
                    }
                }
            }
        }

        private static int CycleButton(int selected, GUIContent[] contents, GUIStyle style)
        {
            if (GUILayout.Button(contents[selected], style))
            {
                selected++;
                if (selected >= contents.Length)
                    selected = 0;
            }

            return selected;
        }

        private static void GetRenderableBoundsRecurse(ref Bounds bounds, GameObject go)
        {
            MeshRenderer meshRenderer = go.GetComponent(typeof(MeshRenderer)) as MeshRenderer;
            MeshFilter filter = go.GetComponent(typeof(MeshFilter)) as MeshFilter;
            if (meshRenderer != null && filter != null && filter.sharedMesh != null)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = meshRenderer.bounds;
                else
                    bounds.Encapsulate(meshRenderer.bounds);
            }

            SkinnedMeshRenderer skinRenderer = go.GetComponent(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
            if (skinRenderer != null && skinRenderer.sharedMesh != null)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = skinRenderer.bounds;
                else
                    bounds.Encapsulate(skinRenderer.bounds);
            }

            SpriteRenderer spriteRenderer = go.GetComponent(typeof(SpriteRenderer)) as SpriteRenderer;
            if (spriteRenderer != null && spriteRenderer.sprite != null)
            {
                if (bounds.extents == Vector3.zero)
                    bounds = spriteRenderer.bounds;
                else
                    bounds.Encapsulate(spriteRenderer.bounds);
            }

            IEnumerator enumerator = go.transform.GetEnumerator();
            try
            {
                while (enumerator.MoveNext())
                {
                    Transform current = (Transform)enumerator.Current;
                    GetRenderableBoundsRecurse(ref bounds, current.gameObject);
                }
            }
            finally
            {
                IDisposable disposable = enumerator as IDisposable;
                if (disposable != null)
                    disposable.Dispose();
            }
        }

        private static Vector2 Drag2D(Vector2 scrollPosition, Rect position)
        {
            int controlID = GUIUtility.GetControlID(sliderHash, FocusType.Passive);
            Event current = Event.current;

            switch (current.GetTypeForControl(controlID))
            {
                case EventType.MouseDown:
                    if (position.Contains(current.mousePosition) && (position.width > 50f))
                    {
                        GUIUtility.hotControl = controlID;
                        current.Use();
                        EditorGUIUtility.SetWantsMouseJumping(1);
                    }
                    return scrollPosition;

                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlID)
                        GUIUtility.hotControl = 0;

                    EditorGUIUtility.SetWantsMouseJumping(0);
                    return scrollPosition;

                case EventType.MouseMove:
                    return scrollPosition;

                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlID)
                    {
                        scrollPosition -= (Vector2)(((current.delta * (!current.shift ? ((float)1) : ((float)3))) / Mathf.Min(position.width, position.height)) * 140f);
                        scrollPosition.y = Mathf.Clamp(scrollPosition.y, -90f, 90f);
                        current.Use();
                        GUI.changed = true;
                    }
                    return scrollPosition;
            }
            return scrollPosition;
        }
    }
}                