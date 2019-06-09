using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Camera), true)]
    public class CameraEditor : InspectorEditor
    {
        private static Handles.CapFunction cap;

        private Rect[] rects;
        private Camera camera;

        private SceneView view = null;

        private MethodInfo getCameraBufferWarnings;
        private MethodInfo getDisplayNames;
        private MethodInfo getDisplayIndices;
        private MethodInfo shouldShowMultiDisplayOption;
        private MethodInfo getCurrentTierSettings;
        private MethodInfo repaintAll;
        private MethodInfo getSizeOfMainGameView;
        private MethodInfo getMainGameViewTargetSize;

        private bool commandBufferShown = true;

        private InspectorField clearFlags;
        private InspectorField orthographic;

        public Vector2 screenshotResolution = new Vector2();

        public bool WantDeferredRendering
        {
            get
            {
                Camera camera = target as Camera;
                bool self = IsDeferredRenderingPath(camera.renderingPath);
                bool setting = IsDeferredRenderingPath(((TierSettings)getCurrentTierSettings.Invoke(null, null)).renderingPath);
                return self || (camera.renderingPath == RenderingPath.UsePlayerSettings && setting);
            }
        }

        private bool IsDeferredRenderingPath(RenderingPath path)
        {
            return path == RenderingPath.DeferredLighting || path == RenderingPath.DeferredShading;
        }

        protected override void RefreshFields()
        {
            Type editorGraphicsSettings = typeof(EditorGraphicsSettings);
            getCurrentTierSettings = editorGraphicsSettings.GetMethod("GetCurrentTierSettings", BindingFlags.Static | BindingFlags.NonPublic);

            Type displayUtility = TypeUtility.GetTypeByName("DisplayUtility");
            getDisplayNames = displayUtility.GetMethod("GetDisplayNames", BindingFlags.Static | BindingFlags.Public);
            getDisplayIndices = displayUtility.GetMethod("GetDisplayIndices", BindingFlags.Static | BindingFlags.Public);

            Type moduleManager = TypeUtility.GetTypeByName("ModuleManager");
            shouldShowMultiDisplayOption = moduleManager.GetMethod("ShouldShowMultiDisplayOption", BindingFlags.Static | BindingFlags.NonPublic);

            Type gameView = TypeUtility.GetTypeByName("GameView");
            repaintAll = gameView.GetMethod("RepaintAll", BindingFlags.Static | BindingFlags.Public);
            getSizeOfMainGameView = gameView.GetMethod("GetSizeOfMainGameView", BindingFlags.Static | BindingFlags.NonPublic);
            getMainGameViewTargetSize = gameView.GetMethod("GetMainGameViewTargetSize", BindingFlags.Static | BindingFlags.NonPublic);

            Type type = typeof(Camera);
            getCameraBufferWarnings = type.GetMethod("GetCameraBufferWarnings", BindingFlags.Instance | BindingFlags.NonPublic);

            clearFlags = new InspectorField(type, Instances, type.GetProperty("clearFlags"),
                new DescriptorAttribute("Clear Flag", "HWhat to display in empty areas of this Camera's view.\n\nChoose Skybox to display a skybox in empty areas, defaulting to a background color if no skybox is found.\n\nChoose Solid Color to display a background color in empty areas.\n\nChoose Depth Only to display nothing in empty areas.\n\nChoose Don't Clear to display whatever was displayed in the previous frame in empty areas.", "http://docs.unity3d.com/ScriptReference/Camera-clearFlags.html"));

            Fields.Add(clearFlags);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("backgroundColor"), new InspectAttribute(new InspectAttribute.InspectDelegate(ShowBackground)),
                new DescriptorAttribute("Background Color", "The Camera clears the screen to this color before rendering.", "http://docs.unity3d.com/ScriptReference/Camera-backgroundColor.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("cullingMask"), new FieldEditorAttribute("LayerMaskEditor"),
                new DescriptorAttribute("Culling Mask", "This is used to render parts of the scene selectively.", "http://docs.unity3d.com/ScriptReference/Camera-cullingMask.html")));

            orthographic = new InspectorField(type, Instances, type.GetProperty("orthographic"), new RestrictAttribute(new RestrictAttribute.RestrictDelegate(Projection)),
                new DescriptorAttribute("Projection", "How the Camera renders perspective.\n\nChoose Perspective to render objects with perspective.\n\nChoose Orthographic to render objects uniformly, with no sense of perspective.", "http://docs.unity3d.com/ScriptReference/Camera-orthographic.html"));

            Fields.Add(orthographic);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("orthographicSize"), new InspectAttribute(new InspectAttribute.InspectDelegate(IsOrthographic)),
                new DescriptorAttribute("Size", "The width of the Camera’s view angle, measured in degrees along the local Z axis.", "http://docs.unity3d.com/ScriptReference/Camera-orthographicSize.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("fieldOfView"), new InspectAttribute(new InspectAttribute.InspectDelegate(IsOrthographic), false),
                new RangeValueAttribute(1, 179), new DescriptorAttribute("Field Of View", "The field of view of the camera in degrees.", "http://docs.unity3d.com/ScriptReference/Camera-fieldOfView.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("nearClipPlane"),
                new DescriptorAttribute("Near Clip", "The near clipping plane distance.", "http://docs.unity3d.com/ScriptReference/Camera-nearClipPlane.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("farClipPlane"),
                new DescriptorAttribute("Far Clip", "The far clipping plane distance.", "http://docs.unity3d.com/ScriptReference/Camera-farClipPlane.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("rect"),
                new DescriptorAttribute("Viewport Rect", "Where on the screen is the camera rendered in normalized coordinates.", "http://docs.unity3d.com/ScriptReference/Camera-rect.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("depth"),
                new DescriptorAttribute("Depth", "Camera's depth in the camera rendering order.", "http://docs.unity3d.com/ScriptReference/Camera-depth.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("renderingPath"), new HelpAttribute(new HelpAttribute.HelpDelegate(RenderingHelp)),
                new DescriptorAttribute("Rendering Path", "Rendering path.", "http://docs.unity3d.com/ScriptReference/Camera-renderingPath.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("targetTexture"), new HelpAttribute(new HelpAttribute.HelpDelegate(TargetTextureHelp)),
                new DescriptorAttribute("Target Texture", "Destination render texture (Unity Pro only).", "http://docs.unity3d.com/ScriptReference/Camera-targetTexture.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("useOcclusionCulling"),
                new DescriptorAttribute("Occlusion Culling", "Whether or not the Camera will use occlusion culling during rendering.", "http://docs.unity3d.com/ScriptReference/Camera-useOcclusionCulling.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("allowHDR"),
                new DescriptorAttribute("Allow HDR", "High dynamic range rendering.", "https://docs.unity3d.com/ScriptReference/Camera-allowHDR.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("allowMSAA"),
                new DescriptorAttribute("Allow MSAA", "MSAA rendering.", "https://docs.unity3d.com/ScriptReference/Camera-allowMSAA.html")));
#if !UNITY_2017_1 && !UNITY_2017_2
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("allowDynamicResolution"),
                new DescriptorAttribute("Allow Dynamic Resolution", "Dynamic Resolution Scaling.", "https://docs.unity3d.com/ScriptReference/Camera-allowDynamicResolution.html")));
#endif

            if (PlayerSettings.virtualRealitySupported)
            {
                Fields.Add(new InspectorField(type, Instances, type.GetProperty("stereoSeparation"),
                    new DescriptorAttribute("Stereo Separation", "The distance between the virtual eyes.", "https://docs.unity3d.com/ScriptReference/Camera-stereoSeparation.html")));
                Fields.Add(new InspectorField(type, Instances, type.GetProperty("stereoConvergence"),
                    new DescriptorAttribute("Stereo Convergence", "Distance to a point where virtual eyes converge.", "https://docs.unity3d.com/ScriptReference/Camera-stereoConvergence.html")));
                Fields.Add(new InspectorField(type, Instances, type.GetProperty("stereoTargetEye"),
                    new DescriptorAttribute("Target Eye", "Defines which eye of a VR display the Camera renders into.", "https://docs.unity3d.com/ScriptReference/Camera-stereoTargetEye.html")));
            }

            if ((bool)shouldShowMultiDisplayOption.Invoke(null, null))
            {
                Fields.Add(new InspectorField(type, Instances, type.GetProperty("targetDisplay"), new RestrictAttribute(new RestrictAttribute.RestrictDelegate(TargetDisplay)),
                    new DescriptorAttribute("Target Display", "Set the target display for this Camera.", "https://docs.unity3d.com/ScriptReference/Camera-targetDisplay.html")));
            }

            Fields.Add(new InspectorField(null, new UnityEngine.Object[] { this }, new object[] { this }, this.GetType().GetMethod("TakeScreenshot"),
                new Attribute[] { new InspectAttribute(InspectorLevel.Advanced) }));
            Fields.Add(new InspectorField(null, new UnityEngine.Object[] { this }, new object[] { this }, null, this.GetType().GetField("screenshotResolution"), false,
                new Attribute[] { new InspectAttribute(InspectorLevel.Advanced) }));

            // Debug
            InspectorField debug = new InspectorField("Debug");
            Fields.Add(debug);

            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("aspect"), new InspectAttribute(InspectorLevel.Debug), new ReadOnlyAttribute(),
                new DescriptorAttribute("Aspect Ratio", "The aspect ratio (width divided by height).", "http://docs.unity3d.com/ScriptReference/Camera-aspect.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("clearStencilAfterLightingPass"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Clear Stencil After Lighting", "Clear Stencil After Lighting Pass.")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("depthTextureMode"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Depth Texture Mode", "How and if camera generates a depth texture.", "http://docs.unity3d.com/ScriptReference/Camera-depthTextureMode.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("eventMask"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Event Mask", "Mask to select which layers can trigger events on the camera.", "http://docs.unity3d.com/ScriptReference/Camera-eventMask.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("layerCullDistances"), new InspectAttribute(InspectorLevel.Debug), new CollectionAttribute(0),
                new DescriptorAttribute("Layer Cull Distances", "Per-layer culling distances.", "http://docs.unity3d.com/ScriptReference/Camera-layerCullDistances.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("layerCullSpherical"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Layer Cull Spherical", "How to perform per-layer culling for a Camera.", "http://docs.unity3d.com/ScriptReference/Camera-layerCullSpherical.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("pixelRect"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Pixel Rect", "Where on the screen is the camera rendered in pixel coordinates.", "http://docs.unity3d.com/ScriptReference/Camera-pixelRect.html")));
            debug.Fields.Add(new InspectorField(type, Instances, type.GetProperty("transparencySortMode"), new InspectAttribute(InspectorLevel.Debug),
                new DescriptorAttribute("Transparency Sort Mode", "Transparent object sorting mode.", "http://docs.unity3d.com/ScriptReference/Camera-transparencySortMode.html")));

            if (camera == null)
            {
                camera = EditorUtility.CreateGameObjectWithHideFlags("Preview Camera", HideFlags.HideAndDontSave, new Type[] { typeof(Camera) }).GetComponent<Camera>();
                camera.enabled = false;
            }

            rects = new Rect[Instances.Length];

            for (int i = 0; i < Instances.Length; i++)
                rects[i] = new Rect(25, 25, 0, 0);
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Used)
                return;

            if (view != SceneView.currentDrawingSceneView)
                InitPosition(SceneView.currentDrawingSceneView);

            Handles.BeginGUI();

            for (int i = 0; i < Instances.Length; i++)
            {
                Camera instance = Instances[i] as Camera;
                rects[i] = GUILayout.Window(i, rects[i], DrawWindow, instance.name + " Preview");
            }

            Handles.EndGUI();

            DrawHandles();
        }

        private bool GetFrustum(Camera camera, Vector3[] near, Vector3[] far, out float frustumAspect)
        {
            frustumAspect = GetFrustumAspectRatio(camera);
            bool result;
            if (frustumAspect < 0f)
            {
                result = false;
            }
            else
            {
                if (far != null)
                {
                    far[0] = new Vector3(0f, 0f, camera.farClipPlane);
                    far[1] = new Vector3(0f, 1f, camera.farClipPlane);
                    far[2] = new Vector3(1f, 1f, camera.farClipPlane);
                    far[3] = new Vector3(1f, 0f, camera.farClipPlane);
                    for (int i = 0; i < 4; i++)
                    {
                        far[i] = camera.ViewportToWorldPoint(far[i]);
                    }
                }
                if (near != null)
                {
                    near[0] = new Vector3(0f, 0f, camera.nearClipPlane);
                    near[1] = new Vector3(0f, 1f, camera.nearClipPlane);
                    near[2] = new Vector3(1f, 1f, camera.nearClipPlane);
                    near[3] = new Vector3(1f, 0f, camera.nearClipPlane);
                    for (int j = 0; j < 4; j++)
                    {
                        near[j] = camera.ViewportToWorldPoint(near[j]);
                    }
                }
                result = true;
            }
            return result;
        }

        private float GetFrustumAspectRatio(Camera camera)
        {
            Rect rect = camera.rect;
            float result;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                result = -1f;
            }
            else
            {
                float num = rect.width / rect.height;
                result = GetGameViewAspectRatio() * num;
            }
            return result;
        }

        private float GetGameViewAspectRatio()
        {
            Vector2 mainGameViewTargetSize = (Vector2)getMainGameViewTargetSize.Invoke(null, null);
            if (mainGameViewTargetSize.x < 0f)
            {
                mainGameViewTargetSize.x = Screen.width;
                mainGameViewTargetSize.y = Screen.height;
            }

            return mainGameViewTargetSize.x / mainGameViewTargetSize.y;
        }

        private static bool IsViewPortRectValidToRender(Rect normalizedViewPortRect)
        {
            return normalizedViewPortRect.width > 0f && normalizedViewPortRect.height > 0f && normalizedViewPortRect.x < 1f && normalizedViewPortRect.xMax > 0f && normalizedViewPortRect.y < 1f && normalizedViewPortRect.yMax > 0f;
        }

        private static Vector3 MidPointPositionSlider(Vector3 position1, Vector3 position2, Vector3 direction)
        {
            Vector3 vector = Vector3.Lerp(position1, position2, 0.5f);
            float size = HandleUtility.GetHandleSize(vector) * 0.03f;
            if (cap == null)
                cap = new Handles.CapFunction(Handles.DotHandleCap);

            return Handles.Slider(vector, direction, size, cap, 0f);
        }

        private void DrawHandles()
        {
            Camera camera = target as Camera;
            if (IsViewPortRectValidToRender(camera.rect))
            {
                Color color = Handles.color;
                Color color2 = new Color(0.9137255f, 0.9137255f, 0.9137255f, 0.5019608f);
                color2.a *= 2f;
                Handles.color = color2;
                Vector3[] array = new Vector3[4];
                float num;
                if (GetFrustum(camera, null, array, out num))
                {
                    bool changed = GUI.changed;
                    Vector3 mid = Vector3.Lerp(array[0], array[2], 0.5f);

                    float result = -1f;
                    Vector3 a = MidPointPositionSlider(array[1], array[2], camera.transform.up);

                    if (GUI.changed)
                    {
                        result = (a - mid).magnitude;
                    }
                    else
                    {
                        a = MidPointPositionSlider(array[0], array[3], -camera.transform.up);
                    }

                    GUI.changed = false;
                    a = MidPointPositionSlider(array[3], array[2], camera.transform.right);

                    if (GUI.changed)
                    {
                        result = (a - mid).magnitude / num;
                    }
                    else
                    {
                        a = MidPointPositionSlider(array[0], array[1], -camera.transform.right);
                    }

                    if (result >= 0f)
                    {
                        Undo.RecordObject(camera, "Adjust Camera");
                        if (camera.orthographic)
                        {
                            camera.orthographicSize = result;
                        }
                        else
                        {
                            Vector3 a2 = mid + camera.transform.up * result;
                            camera.fieldOfView = Vector3.Angle(camera.transform.forward, a2 - camera.transform.position) * 2f;
                        }
                        changed = true;
                    }
                    GUI.changed = changed;
                    Handles.color = color;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DisplayCameraWarnings();
            DepthTextureModeGUI();
            CommandBufferGUI();
        }

        private void DisplayCameraWarnings()
        {
            Camera camera = target as Camera;
            if (camera != null)
            {
                string[] cameraBufferWarnings = (string[])getCameraBufferWarnings.Invoke(camera, null);
                if (cameraBufferWarnings.Length > 0)
                {
                    EditorGUILayout.HelpBox(string.Join("\n\n", cameraBufferWarnings), MessageType.Warning, true);
                }
            }
        }

        private void DepthTextureModeGUI()
        {
            if (targets.Length == 1)
            {
                Camera camera = target as Camera;
                if (camera != null && camera.depthTextureMode != DepthTextureMode.None)
                {
                    List<string> list = new List<string>();
                    if ((camera.depthTextureMode & DepthTextureMode.Depth) != DepthTextureMode.None)
                    {
                        list.Add("Depth");
                    }
                    if ((camera.depthTextureMode & DepthTextureMode.DepthNormals) != DepthTextureMode.None)
                    {
                        list.Add("DepthNormals");
                    }
                    if ((camera.depthTextureMode & DepthTextureMode.MotionVectors) != DepthTextureMode.None)
                    {
                        list.Add("MotionVectors");
                    }
                    if (list.Count != 0)
                    {
                        StringBuilder stringBuilder = new StringBuilder("Info: renders ");
                        for (int i = 0; i < list.Count; i++)
                        {
                            if (i != 0)
                            {
                                stringBuilder.Append(" & ");
                            }
                            stringBuilder.Append(list[i]);
                        }
                        stringBuilder.Append((list.Count <= 1) ? " texture" : " textures");
                        EditorGUILayout.HelpBox(stringBuilder.ToString(), MessageType.None, true);
                    }
                }
            }
        }

        private void CommandBufferGUI()
        {
            if (targets.Length != 1)
                return;

            Camera camera = target as Camera;
            if (camera == null)
                return;

            int commandBufferCount = camera.commandBufferCount;
            if (commandBufferCount != 0)
            {
                commandBufferShown = GUILayout.Toggle(commandBufferShown, new GUIContent(commandBufferCount + " command buffers"), EditorStyles.foldout, new GUILayoutOption[0]);
                if (commandBufferShown)
                {
                    EditorGUI.indentLevel++;
                    CameraEvent[] array = (CameraEvent[])Enum.GetValues(typeof(CameraEvent));
                    for (int i = 0; i < array.Length; i++)
                    {
                        CameraEvent cameraEvent = array[i];
                        CommandBuffer[] commandBuffers = camera.GetCommandBuffers(cameraEvent);
                        CommandBuffer[] array2 = commandBuffers;
                        for (int j = 0; j < array2.Length; j++)
                        {
                            CommandBuffer commandBuffer = array2[j];
                            using (new GUILayout.HorizontalScope(new GUILayoutOption[0]))
                            {
                                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, EditorStyles.miniLabel);
                                rect.xMin += EditorGUI.indentLevel;
                                Rect removeButtonRect = GetRemoveButtonRect(rect);
                                rect.xMax = removeButtonRect.x;
                                GUI.Label(rect, string.Format("{0}: {1} ({2})", cameraEvent, commandBuffer.name, EditorUtility.FormatBytes(commandBuffer.sizeInBytes)), EditorStyles.miniLabel);
                                if (GUI.Button(removeButtonRect, IconRemove, InvisibleButton))
                                {
                                    camera.RemoveCommandBuffer(cameraEvent, commandBuffer);
                                    SceneView.RepaintAll();
                                    repaintAll.Invoke(null, null);
                                    GUIUtility.ExitGUI();
                                }
                            }
                        }
                    }
                    using (new GUILayout.HorizontalScope(new GUILayoutOption[0]))
                    {
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Remove all", EditorStyles.miniButton, new GUILayoutOption[0]))
                        {
                            camera.RemoveAllCommandBuffers();
                            SceneView.RepaintAll();
                            repaintAll.Invoke(null, null);
                        }
                    }

                    EditorGUI.indentLevel--;
                }
            }
        }

        private static Rect GetRemoveButtonRect(Rect r)
        {
            Vector2 vector = InvisibleButton.CalcSize(IconRemove);
            return new Rect(r.xMax - vector.x, r.y + (int)(r.height / 2f - vector.y / 2f), vector.x, vector.y);
        }

        public void TakeScreenshot()
        {
            Camera camera = target as Camera;
            if (camera == null)
                return;

            RenderTexture texture = RenderTexture.GetTemporary((int)screenshotResolution.x, (int)screenshotResolution.y);
            camera.targetTexture = texture;
            camera.Render();
            camera.targetTexture = null;
            RenderTexture.active = texture;

            Texture2D image = new Texture2D((int)screenshotResolution.x, (int)screenshotResolution.y);
            image.ReadPixels(new Rect(0, 0, (int)screenshotResolution.x, (int)screenshotResolution.y), 0, 0);
            image.Apply();

            RenderTexture.ReleaseTemporary(texture);
            RenderTexture.active = null;

            byte[] file = image.EncodeToPNG();

            int count = 1;
            string path;
            while (true)
            {
                path = Application.dataPath + "/Screenshot_" + count.ToString() + ".png";
                FileInfo info = new FileInfo(path);
                if (!info.Exists)
                    break;

                count++;
            }

            File.WriteAllBytes(path, file);
        }

        private void InitPosition(SceneView view)
        {
            this.view = view;

            int offset = 45;
            for (int i = 0; i < Instances.Length; i++)
            {
                Type gameView = TypeUtility.GetTypeByName("GameView");
                MethodInfo info = gameView.GetMethod("GetSizeOfMainGameView", BindingFlags.Static | BindingFlags.NonPublic);
                Vector2 camSize = (Vector2)info.Invoke(null, null);

                int width = (int)(camSize.x * 0.25f);
                int height = (int)(camSize.y * 0.25f);

                rects[i] = new Rect(view.position.width - width - 25, view.position.height - height - offset, 0, 0);
                offset += height + 35;
            }
        }

        private void DrawWindow(int i)
        {
            Rect rect = SceneView.currentDrawingSceneView.camera.pixelRect;

            Camera instance = Instances[i] as Camera;
            Vector2 camSize = (Vector2)getSizeOfMainGameView.Invoke(null, null);

            int width = (int)(camSize.x * 0.25f);
            int height = (int)(camSize.y * 0.25f);

            camera.CopyFrom(instance);
            camera.pixelRect = new Rect(rects[i].x + 5, rect.height - rects[i].y - (height), width, height);
            camera.Render();

            GUI.DragWindow();
            GUI.Label(GUILayoutUtility.GetRect(width, height), "", GUIStyle.none);
        }

        private IList Projection()
        {
            List<object> list = new List<object>();
            list.Add(new DescriptionPair(false, new Description("Perspective", "")));
            list.Add(new DescriptionPair(true, new Description("Orthographic", "")));
            return list;
        }

        private IList TargetDisplay()
        {
            List<object> list = new List<object>();
            int[] indexes = (int[])getDisplayIndices.Invoke(null, null);
            GUIContent[] names = (GUIContent[])getDisplayNames.Invoke(null, null);

            for (int i = 0; i < indexes.Length; i++)
                list.Add(new DescriptionPair(indexes[i], new Description(names[i].text, names[i].tooltip)));

            return list;
        }

        private bool ShowBackground()
        {
            return !clearFlags.Mixed && (int)clearFlags.GetValue<CameraClearFlags>() == 2;
        }

        private bool IsOrthographic()
        {
            return !orthographic.Mixed && orthographic.GetValue<bool>();
        }

        private HelpItem RenderingHelp()
        {
            foreach (object instance in Instances)
            {
                Camera camera = instance as Camera;
                if (camera == null)
                    return null;

                if (WantDeferredRendering && camera.orthographic)
                    return new HelpItem(HelpType.Warning, "Deferred rendering does not work with Orthographic camera, will use Forward.");
            }

            return null;
        }

        private HelpItem TargetTextureHelp()
        {
            foreach (object instance in Instances)
            {
                Camera camera = instance as Camera;
                if (camera == null)
                    return null;

                if (camera.targetTexture != null && camera.targetTexture.antiAliasing > 1 && WantDeferredRendering)
                    return new HelpItem(HelpType.Warning, "Manual MSAA target set with deferred rendering. This will lead to undefined behavior.");
            }

            return null;
        }
    }
}
