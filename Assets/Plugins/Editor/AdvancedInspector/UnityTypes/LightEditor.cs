using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Light), true)]
    public class LightEditor : InspectorEditor
    {
        private static Color disabledLightColor = new Color(0.5f, 0.45f, 0.2f, 0.5f);
        private static Color lightColor = new Color(0.95f, 0.95f, 0.5f, 0.5f);

        private static Texture2D kelvinTexture;

        private InspectorField lightType;
        private InspectorField lightShadows;
        private InspectorField lightBaking;
        private InspectorField lightCookie;

        private MethodInfo repaintAll;

        private bool commandBufferShown = true;

        protected override void RefreshFields()
        {
            if (kelvinTexture != null)
                kelvinTexture = CreateKelvinGradientTexture("KelvinGradientTexture", 300, 16, 1000f, 20000f);

            Type gameView = TypeUtility.GetTypeByName("GameView");
            repaintAll = gameView.GetMethod("RepaintAll", BindingFlags.Static | BindingFlags.Public);

            Type type = typeof(Light);
            if (Instances == null || Instances.Length == 0)
                return;

            SerializedObject so = new SerializedObject(Instances.Cast<UnityEngine.Object>().ToArray());

            lightType = new InspectorField(type, Instances, type.GetProperty("type"),
                new DescriptorAttribute("Type", "The type of the light.", "http://docs.unity3d.com/ScriptReference/Light-type.html"));

            Fields.Add(lightType);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("range"),
                new ReadOnlyAttribute(new ReadOnlyAttribute.ReadOnlyDelegate(IsArea)),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsDirectional), false),
                new DescriptorAttribute("Range", "The range of the light.", "http://docs.unity3d.com/ScriptReference/Light-range.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("spotAngle"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsSpot)),
                new DescriptorAttribute("Spot Angle", "The angle of the light's spotlight cone in degrees.", "http://docs.unity3d.com/ScriptReference/Light-spotAngle.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("areaSize"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsArea)),
                new DescriptorAttribute("Area Size", "The size of the area light. Editor only.", "http://docs.unity3d.com/ScriptReference/Light-areaSize.html")));

            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_UseColorTemperature"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(AllowTemperature)),
                new DescriptorAttribute("Use Temperature", "Allow this light to use Kelvin Temperature.", "https://docs.unity3d.com/ScriptReference/Light-colorTemperature.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("color"),
                new MenuAttribute("Temperature Lighting", new MenuAttribute.MenuDelegate(ToggleTemperatureLighting), null, AllowTemperature),
                new DescriptorAttribute("Color", "The color of the light.", "http://docs.unity3d.com/ScriptReference/Light-color.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("colorTemperature"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(ShowTemperature)),
                new DescriptorAttribute("Temperature", "Also known as CCT (Correlated color temperature).The color temperature of the electromagnetic radiation emitted from an ideal black body is defined as its surface temperature in Kelvin.White is 6500K", "https://docs.unity3d.com/ScriptReference/Light-colorTemperature.html")));

            lightBaking = new InspectorField(type, Instances, type.GetProperty("lightmapBakeType"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsArea), false),
                new HelpAttribute(new HelpAttribute.HelpDelegate(LightmapModeWarning)),
                new DescriptorAttribute("Mode", "This property describes what part of a light's contribution can be baked.", "http://docs.unity3d.com/ScriptReference/Light-type.html"));

            Fields.Add(lightBaking);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("intensity"),
                new RangeValueAttribute(0f, 8f),
                new DescriptorAttribute("Intensity", "The Intensity of a light is multiplied with the Light color.", "http://docs.unity3d.com/ScriptReference/Light-intensity.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("bounceIntensity"),
                new RangeValueAttribute(0f, 8f), new HelpAttribute(new HelpAttribute.HelpDelegate(HelpBouncedGI)),
                new DescriptorAttribute("Bounce Intensity", "The multiplier that defines the strength of the bounce lighting.", "http://docs.unity3d.com/ScriptReference/Light-bounceIntensity.html")));

            //Acts like a group
            lightShadows = new InspectorField(type, Instances, type.GetProperty("shadows"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsArea), false),
                new DescriptorAttribute("Shadow Type", "How this light casts shadows", "http://docs.unity3d.com/ScriptReference/Light-shadows.html"));

            Fields.Add(lightShadows);

            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_ShadowAngle"),
                new ReadOnlyAttribute(new ReadOnlyAttribute.ReadOnlyDelegate(IsSoft), false),
                new RangeAttribute(0, 90),
                new InspectAttribute(new InspectAttribute.InspectDelegate(ShowShadowAngle)),
                new DescriptorAttribute("Baked Shadow Angle", "", "")));

            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_ShadowRadius"),
                new ReadOnlyAttribute(new ReadOnlyAttribute.ReadOnlyDelegate(IsSoft), false),
                new InspectAttribute(new InspectAttribute.InspectDelegate(ShowShadowRadius)),
                new DescriptorAttribute("Baked Shadow Radius", "", "")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("shadowStrength"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(HasRealimeShadow)),
                new RangeValueAttribute(0f, 1f),
                new DescriptorAttribute("Strength", "How this light casts shadows.", "http://docs.unity3d.com/ScriptReference/Light-shadowStrength.html")));
            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_Shadows.m_Resolution"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(HasRealimeShadow)),
                new DescriptorAttribute("Resolution", "The shadow's resolution.")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("shadowBias"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(HasRealimeShadow)),
                new RangeValueAttribute(0f, 2f),
                new DescriptorAttribute("Bias", "Shadow mapping bias.", "http://docs.unity3d.com/ScriptReference/Light-shadowBias.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("shadowNormalBias"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(HasRealimeShadow)),
                new RangeValueAttribute(0f, 3f),
                new DescriptorAttribute("Normal Bias", "Shadow mapping normal-based bias.", "http://docs.unity3d.com/ScriptReference/Light-shadowNormalBias.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("shadowNearPlane"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(HasRealimeShadow)),
                new RangeValueAttribute(0.1f, 10f),
                new DescriptorAttribute("Near Plane", "Near plane value to use for shadow frustums.", "https://docs.unity3d.com/ScriptReference/Light-shadowNearPlane.html")));

            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_DrawHalo"),
                new DescriptorAttribute("Draw Halo", "Draw a halo around the light. Now work with the Halo class.")));

            lightCookie = new InspectorField(type, Instances, type.GetProperty("cookie"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(CanCookie)),
                new DescriptorAttribute("Cookie", "The cookie texture projected by the light.", "http://docs.unity3d.com/ScriptReference/Light-cookie.html"));

            Fields.Add(lightCookie);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("cookieSize"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(CanCookieSize)),
                new DescriptorAttribute("Cookie Size", "The size of a directional light's cookie.", "http://docs.unity3d.com/ScriptReference/Light-cookieSize.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("flare"),
                new DescriptorAttribute("Flare", "The flare asset to use for this light.", "http://docs.unity3d.com/ScriptReference/Light-flare.html")));

            
            Fields.Add(new InspectorField(type, Instances, so.FindProperty("m_RenderMode"),
                new DescriptorAttribute("Render Mode", "The rendering path for the lights.")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("cullingMask"), new FieldEditorAttribute("LayerMaskEditor"),
                new HelpAttribute(new HelpAttribute.HelpDelegate(HelpSceneLighting)),
                new DescriptorAttribute("Culling Mask", "The object that are affected or ignored by the light.")));
        }

        private IList Baking()
        {
            return new DescriptionPair[] { new DescriptionPair(4, "Realtime", ""), new DescriptionPair(2, "Baked", ""), new DescriptionPair(1, "Mixed", "") };
        }

        public bool IsPoint()
        {
            return !lightType.Mixed && lightType.GetValue<LightType>() == LightType.Point;
        }

        public bool IsSpot()
        {
            return !lightType.Mixed && lightType.GetValue<LightType>() == LightType.Spot;
        }

        public bool IsPointOrSpot()
        {
            return IsPoint() || IsSpot();
        }

        public bool IsDirectional()
        {
            return !lightType.Mixed && lightType.GetValue<LightType>() == LightType.Directional;
        }

        public bool IsArea()
        {
            return !lightType.Mixed && lightType.GetValue<LightType>() == LightType.Area;
        }

        public bool HasShadow()
        {
            return !IsArea() && !lightShadows.Mixed && lightShadows.GetValue<LightShadows>() != LightShadows.None;
        }

        public bool HasRealimeShadow()
        {
            return HasShadow() && !lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Baked;
        }

        public bool ShowShadowAngle()
        {
            return HasShadow() && IsDirectional() && !lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Realtime;
        }

        public bool ShowShadowRadius()
        {
            return HasShadow() && IsPointOrSpot() && !lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Realtime;
        }

        public bool IsSoft()
        {
            return !IsArea() && !lightShadows.Mixed && lightShadows.GetValue<LightShadows>() != LightShadows.Soft;
        }

        public bool CanCookie()
        {
            return !IsArea() && !lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Baked;
        }

        public bool CanCookieSize()
        {
            return IsDirectional() && !lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Baked;
        }

        public bool AllowTemperature()
        {
            return GraphicsSettings.lightsUseColorTemperature && GraphicsSettings.lightsUseLinearIntensity;
        }

        public bool ShowTemperature()
        {
            SerializedProperty property = serializedObject.FindProperty("m_UseColorTemperature");
            return AllowTemperature() && !property.hasMultipleDifferentValues && property.boolValue;
        }

        public void ToggleTemperatureLighting()
        {
            GraphicsSettings.lightsUseLinearIntensity = !GraphicsSettings.lightsUseLinearIntensity;
            GraphicsSettings.lightsUseColorTemperature = !GraphicsSettings.lightsUseColorTemperature;
        }

        public bool DoesAnyCameraUseDeferred()
        {
            Camera[] allCameras = Camera.allCameras;
            for (int i = 0; i < allCameras.Length; i++)
                if (allCameras[i].actualRenderingPath == RenderingPath.DeferredLighting || allCameras[i].actualRenderingPath == RenderingPath.DeferredShading)
                    return true;

            return false;
        }

        public HelpItem LightmapModeWarning()
        {
            if (lightBaking.Mixed && lightBaking.GetValue<LightmapBakeType>() != LightmapBakeType.Realtime && !Lightmapping.bakedGI)
                return new HelpItem(HelpType.Info, "Light mode is currently overridden to Realtime mode. Enable Baked Global Illumination to use Mixed or Baked light modes.");

            return null;
        }

        public HelpItem CookieWarning()
        {
            if (!CanCookie() || lightCookie.Mixed)
                return null;

            Texture cookie = lightCookie.GetValue<Texture>();
            if (cookie != null && cookie.wrapMode != TextureWrapMode.Clamp)
                return new HelpItem(HelpType.Info, "Cookie textures for spot lights should be set to clamp, not repeat, to avoid artifacts.");

            return null;
            
        }

        public HelpItem HelpBouncedGI()
        {
            Light light = target as Light;
            if (light.bounceIntensity > 0 && (IsPoint() || IsSpot()) && light.lightmapBakeType != LightmapBakeType.Baked)
                return new HelpItem(HelpType.Warning, "Currently realtime indirect bounce light shadowing for spot and point lights is not supported.");

            return null;
        }

        public HelpItem HelpSceneLighting()
        {
            if (SceneView.currentDrawingSceneView != null && !SceneView.currentDrawingSceneView.sceneLighting)
                return new HelpItem(HelpType.Warning, "One of your scene views has lighting disable, please keep this in mind when editing lighting.");

            return null;
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Used)
                return;

            Light light = (Light)target;
            Color color = Handles.color;

            if (light.enabled)
                Handles.color = lightColor;
            else
                Handles.color = disabledLightColor;

            float range = light.range;
            switch (light.type)
            {
                case LightType.Spot:
                {
                    Color color2 = Handles.color;
                    color2.a = Mathf.Clamp01(color.a * 2f);
                    Handles.color = color2;
                    Vector2 angleAndRange = new Vector2(light.spotAngle, light.range);
                    angleAndRange = ConeHandle(light.transform.rotation, light.transform.position, angleAndRange, 1f, 1f, true);
                    if (GUI.changed)
                    {
                        Undo.RecordObject(light, "Adjust Spot Light");
                        light.spotAngle = angleAndRange.x;
                        light.range = Mathf.Max(angleAndRange.y, 0.01f);
                    }

                    break;
                }

                case LightType.Point:
                {
                    range = Handles.RadiusHandle(Quaternion.identity, light.transform.position, range, true);
                    if (GUI.changed)
                    {
                        Undo.RecordObject(light, "Adjust Point Light");
                        light.range = range;
                    }

                    break;
                }

                case LightType.Area:
                {
                    EditorGUI.BeginChangeCheck();
                    Vector2 vector2 = RectHandles(light.transform.rotation, light.transform.position, light.areaSize);
                    if (EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(light, "Adjust Area Light");
                        light.areaSize = vector2;
                    }

                    break;
                }
            }
            Handles.color = color;
        }

        private void CommandBufferGUI()
        {
            if (targets.Length != 1)
                return;

            Light light = target as Light;
            if (light == null)
                return;

            int commandBufferCount = light.commandBufferCount;
            if (commandBufferCount != 0)
            {
                commandBufferShown = GUILayout.Toggle(commandBufferShown, new GUIContent(commandBufferCount + " command buffers"), EditorStyles.foldout, new GUILayoutOption[0]);
                if (commandBufferShown)
                {
                    EditorGUI.indentLevel++;
                    LightEvent[] array = (LightEvent[])Enum.GetValues(typeof(LightEvent));
                    for (int i = 0; i < array.Length; i++)
                    {
                        LightEvent lightEvent = array[i];
                        CommandBuffer[] commandBuffers = light.GetCommandBuffers(lightEvent);
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
                                GUI.Label(rect, string.Format("{0}: {1} ({2})", lightEvent, commandBuffer.name, EditorUtility.FormatBytes(commandBuffer.sizeInBytes)), EditorStyles.miniLabel);
                                if (GUI.Button(removeButtonRect, IconRemove, InvisibleButton))
                                {
                                    light.RemoveCommandBuffer(lightEvent, commandBuffer);
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
                            light.RemoveAllCommandBuffers();
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

        private Vector2 ConeHandle(Quaternion rotation, Vector3 position, Vector2 angleAndRange, float angleScale, float rangeScale, bool handlesOnly)
        {
            float x = angleAndRange.x;
            float y = angleAndRange.y;
            float r = y * rangeScale;
            
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            bool changed = GUI.changed;
            GUI.changed = false;
            r = SizeSlider(position, forward, r);
            if (GUI.changed)
                y = Mathf.Max(0f, r / rangeScale);

            GUI.changed |= changed;
            changed = GUI.changed;
            GUI.changed = false;

            float angle = (r * Mathf.Tan((0.01745329f * x) / 2f)) * angleScale;
            angle = SizeSlider(position + (forward * r), up, angle);
            angle = SizeSlider(position + (forward * r), -up, angle);
            angle = SizeSlider(position + (forward * r), right, angle);
            angle = SizeSlider(position + (forward * r), -right, angle);

            if (GUI.changed)
                x = Mathf.Clamp((57.29578f * Mathf.Atan(angle / (r * angleScale))) * 2f, 0f, 179f);

            GUI.changed |= changed;
            if (!handlesOnly)
            {
                Handles.DrawLine(position, (position + (forward * r)) + (up * angle));
                Handles.DrawLine(position, (position + (forward * r)) - (up * angle));
                Handles.DrawLine(position, (position + (forward * r)) + (right * angle));
                Handles.DrawLine(position, (position + (forward * r)) - (right * angle));
                Handles.DrawWireDisc(position + r * forward, forward, angle);
            }

            return new Vector2(x, y);
        }

        private Vector2 RectHandles(Quaternion rotation, Vector3 position, Vector2 size)
        {
            Vector3 forward = rotation * Vector3.forward;
            Vector3 up = rotation * Vector3.up;
            Vector3 right = rotation * Vector3.right;

            float radiusX = 0.5f * size.x;
            float radiusY = 0.5f * size.y;

            Vector3 v1 = (position + (up * radiusY)) + (right * radiusX);
            Vector3 v2 = (position - (up * radiusY)) + (right * radiusX);
            Vector3 v3 = (position - (up * radiusY)) - (right * radiusX);
            Vector3 v4 = (position + (up * radiusY)) - (right * radiusX);

            Handles.DrawLine(v1, v2);
            Handles.DrawLine(v2, v3);
            Handles.DrawLine(v3, v4);
            Handles.DrawLine(v4, v1);

            Color color = Handles.color;
            color.a = Mathf.Clamp01(color.a * 2f);
            Handles.color = color;

            radiusY = SizeSlider(position, up, radiusY);
            radiusY = SizeSlider(position, -up, radiusY);
            radiusX = SizeSlider(position, right, radiusX);
            radiusX = SizeSlider(position, -right, radiusX);

            if (((Tools.current != Tool.Move) && (Tools.current != Tool.Scale)) || Tools.pivotRotation != PivotRotation.Local)
                Handles.DrawLine(position, position + forward);

            size.x = 2f * radiusX;
            size.y = 2f * radiusY;

            return size;
        }

        private float SizeSlider(Vector3 p, Vector3 direction, float radius)
        {
            Vector3 position = p + (direction * radius);
            float handleSize = HandleUtility.GetHandleSize(position);

            bool changed = GUI.changed;
            GUI.changed = false;

            position = Handles.Slider(position, direction, handleSize * 0.03f, new Handles.CapFunction(Handles.DotHandleCap), 0f);

            if (GUI.changed)
                radius = Vector3.Dot(position - p, direction);

            GUI.changed |= changed;
            return radius;
        }

        private static Texture2D CreateKelvinGradientTexture(string name, int width, int height, float minKelvin, float maxKelvin)
        {
            Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, false);
            texture2D.name = name;
            texture2D.hideFlags = HideFlags.HideAndDontSave;
            Color32[] array = new Color32[width * height];
            float num = Mathf.Pow(maxKelvin, 0.5f);
            float num2 = Mathf.Pow(minKelvin, 0.5f);
            for (int i = 0; i < width; i++)
            {
                float num3 = (float)i / (width - 1);
                float f = (num - num2) * num3 + num2;
                float kelvin = Mathf.Pow(f, 2f);
                Color color = Mathf.CorrelatedColorTemperatureToRGB(kelvin);
                for (int j = 0; j < height; j++)
                {
                    array[j * width + i] = color.gamma;
                }
            }
            texture2D.SetPixels32(array);
            texture2D.wrapMode = TextureWrapMode.Clamp;
            texture2D.Apply();
            return texture2D;
        }
    }
}
