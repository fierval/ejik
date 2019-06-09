using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

namespace AdvancedInspector
{
    public abstract class RendererEditor : InspectorEditor
    {
        private InspectorField lightProbeUsage;
        private InspectorField reflectionProbeUsage;

        private InspectorField materials;

        private SerializedObject gameObjects;
        private SerializedProperty staticFlags;

        private MethodInfo hasInstancing;
        private MethodInfo tierSettings;

        protected override void RefreshFields()
        {
            gameObjects = new SerializedObject((from t in targets select ((MeshRenderer)t).gameObject).ToArray());
            staticFlags = gameObjects.FindProperty("m_StaticEditorFlags");

            hasInstancing = typeof(ShaderUtil).GetMethod("HasInstancing", BindingFlags.Static | BindingFlags.NonPublic);
            tierSettings = typeof(EditorGraphicsSettings).GetMethod("GetCurrentTierSettings", BindingFlags.Static | BindingFlags.NonPublic);

            Type type = typeof(Renderer);

            lightProbeUsage = new InspectorField(type, Instances, type.GetProperty("lightProbeUsage"),
                new DescriptorAttribute("Light Probes", "The light probe interpolation type.", "http://docs.unity3d.com/540/Documentation/ScriptReference/Renderer-lightProbeUsage.html"));

            Fields.Add(lightProbeUsage);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("lightProbeProxyVolumeOverride"),
                new DescriptorAttribute("Proxy Volume Override", "If set, the Renderer will use the Light Probe Proxy Volume component attached to the source game object.", "http://docs.unity3d.com/540/Documentation/ScriptReference/Renderer-lightProbeProxyVolumeOverride.html"),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsUsingLightProbeOverride))));

            reflectionProbeUsage = new InspectorField(type, Instances, type.GetProperty("reflectionProbeUsage"),
                new ReadOnlyAttribute(new ReadOnlyAttribute.ReadOnlyDelegate(IsDeferredRenderingPath)),
                new InspectAttribute(new InspectAttribute.InspectDelegate(IsDeferredRenderingPath)),
                new DescriptorAttribute("Reflection Probes", "Should reflection probes be used for this Renderer?", "http://docs.unity3d.com/ScriptReference/Renderer-reflectionProbeUsage.html"));

            Fields.Add(reflectionProbeUsage);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("probeAnchor"), new InspectAttribute(new InspectAttribute.InspectDelegate(IsUsingLightProbes)),
                new DescriptorAttribute("Anchor Override", "If set, Renderer will use this Transform's position to find the interpolated light probe.", "http://docs.unity3d.com/ScriptReference/Renderer-lightProbeAnchor.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("shadowCastingMode"),
                new DescriptorAttribute("Cast Shadows", "Does this object cast shadows?", "http://docs.unity3d.com/ScriptReference/Renderer-shadowCastingMode.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("receiveShadows"),
                new ReadOnlyAttribute(new ReadOnlyAttribute.ReadOnlyDelegate(IsDeferredRenderingPath)),
                new DescriptorAttribute("Receive Shadows", "Does this object receive shadows?", "http://docs.unity3d.com/ScriptReference/Renderer-receiveShadows.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("motionVectorGenerationMode"),
                new DescriptorAttribute("Motion Vectors", "Specifies the mode for motion vector rendering.", "https://docs.unity3d.com/ScriptReference/Renderer-motionVectorGenerationMode.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("allowOcclusionWhenDynamic"),
                new DescriptorAttribute("Dynamic Occluded", "Controls if dynamic occlusion culling should be performed for this renderer.", "https://docs.unity3d.com/ScriptReference/Renderer-allowOcclusionWhenDynamic.html")));

            materials = new InspectorField(type, Instances, type.GetProperty("sharedMaterials"),
                new HelpAttribute(new HelpAttribute.HelpDelegate(MaterialWarning)),
                new HelpAttribute(new HelpAttribute.HelpDelegate(MaterialInstancingWarning)),
                new DescriptorAttribute("Materials", "All the shared materials of this object.", "http://docs.unity3d.com/ScriptReference/Renderer-sharedMaterials.html"));

            Fields.Add(materials);

            Type editor = typeof(RendererEditor);
            Fields.Add(new InspectorField(editor, new UnityEngine.Object[] { this }, editor.GetProperty("ReflectionProbes", BindingFlags.NonPublic | BindingFlags.Instance),
                       new InspectAttribute(new InspectAttribute.InspectDelegate(IsUsingReflectionProbes)), 
                       new CollectionAttribute(0, false), new ReadOnlyAttribute(), new DisplayAsParentAttribute()));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("isPartOfStaticBatch"),
                new DescriptorAttribute("Static Batched", "Has this renderer been statically batched with any other renderers?", "http://docs.unity3d.com/ScriptReference/Renderer-isPartOfStaticBatch.html"), new InspectAttribute(InspectorLevel.Debug)));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("isVisible"),
                new DescriptorAttribute("Is Visible", "Is this renderer visible in any camera? (Read Only)", "http://docs.unity3d.com/ScriptReference/Renderer-isVisible.html"), new InspectAttribute(InspectorLevel.Debug)));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("lightmapIndex"),
                new DescriptorAttribute("Lightmap Index", "The index of the lightmap applied to this renderer.", "http://docs.unity3d.com/ScriptReference/Renderer-lightmapIndex.html"), new InspectAttribute(InspectorLevel.Debug)));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("sortingLayerID"),
                new DescriptorAttribute("Sorting Layer ID", "ID of the Renderer's sorting layer.", "http://docs.unity3d.com/ScriptReference/Renderer-sortingLayerID.html"), new InspectAttribute(InspectorLevel.Debug)));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("sortingLayerName"),
                new DescriptorAttribute("Sorting Layer Name", "Name of the Renderer's sorting layer.", "http://docs.unity3d.com/ScriptReference/Renderer-sortingLayerName.html"), new InspectAttribute(InspectorLevel.Debug)));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("sortingOrder"),
                new DescriptorAttribute("Sorting Order", "Renderer's order within a sorting layer.", "http://docs.unity3d.com/ScriptReference/Renderer-sortingOrder.html"), new InspectAttribute(InspectorLevel.Debug)));
        }

        private bool IsUsingLightProbes()
        {
            return !lightProbeUsage.Mixed && lightProbeUsage.GetValue<LightProbeUsage>() != LightProbeUsage.Off;
        }

        private bool IsUsingLightProbeOverride()
        {
            return !lightProbeUsage.Mixed && lightProbeUsage.GetValue<LightProbeUsage>() == LightProbeUsage.UseProxyVolume;
        }

        private bool IsUsingReflectionProbes()
        {
            return !reflectionProbeUsage.Mixed && reflectionProbeUsage.GetValue<ReflectionProbeUsage>() != ReflectionProbeUsage.Off;
        }

        private HelpItem MaterialWarning()
        {
            if (materials.Mixed)
                return null;

            Material[] array = materials.GetValue<Material[]>();
            MeshFilter component = ((MeshRenderer)serializedObject.targetObject).GetComponent<MeshFilter>();
            if (component != null && component.sharedMesh != null && array.Length > component.sharedMesh.subMeshCount)
                return new HelpItem(HelpType.Warning, "This renderer has more materials than the Mesh has submeshes. Multiple materials will be applied to the same submesh, which costs performance. Consider using multiple shader passes.");

            return null;
        }

        private HelpItem MaterialInstancingWarning()
        {
            if (materials.Mixed)
                return null;

            if (staticFlags.hasMultipleDifferentValues || (staticFlags.intValue & 4) == 0)
                return null;

            Material[] array = materials.GetValue<Material[]>();
            for (int i = 0; i < array.Length; i++)
            {
                Material material = array[i];
                if (material != null && material.enableInstancing && material.shader != null && (bool)hasInstancing.Invoke(null, new object[] { material.shader }))
                {
                    return new HelpItem(HelpType.Warning, "This renderer is statically batched and uses an instanced shader at the same time. Instancing will be disabled in such a case. Consider disabling static batching if you want it to be instanced.");
                }
            }

            return null;
        }

        private bool IsDeferredRenderingPath()
        {
            Camera[] cameras = SceneView.GetAllSceneCameras();
            if (cameras.Length == 0 || cameras[0].renderingPath == RenderingPath.UsePlayerSettings)
                return ((TierSettings)tierSettings.Invoke(null, null)).renderingPath == RenderingPath.DeferredShading;
            else
                return cameras[0].renderingPath == RenderingPath.DeferredShading;
        }

        private ReflectionProbeBlendInfo[] ReflectionProbes
        {
            get
            {
                if (Instances.Length > 1)
                    return new ReflectionProbeBlendInfo[0];

                Renderer renderer = Instances[0] as Renderer;
                List<ReflectionProbeBlendInfo> blends = new List<ReflectionProbeBlendInfo>();
                renderer.GetClosestReflectionProbes(blends);
                return blends.ToArray();
            }
        }
    }
}
