using System;
using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    public abstract class ColliderEditor : InspectorEditor
    {
        public static Color ColliderHandleColor = new Color(0.57f, 0.96f, 0.54f, 0.82f);
        public static Color ColliderHandleColorDisabled = new Color(0.33f, 0.78f, 0.3f, 0.55f);

        protected override void RefreshFields()
        {
            Type type = typeof(Collider);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("isTrigger"),
                new DescriptorAttribute("Is Trigger", "Is the collider a trigger?", "http://docs.unity3d.com/ScriptReference/Collider-isTrigger.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("sharedMaterial"), new InspectAttribute(new InspectAttribute.InspectDelegate(IsNotTrigger)),
                new DescriptorAttribute("Physic Material", "The shared physic material of this collider.", "http://docs.unity3d.com/ScriptReference/Collider-sharedMaterial.html")));
        }

        private bool IsNotTrigger()
        {
            for (int i = 0; i < Instances.Length; i++)
            {
                Collider collider = Instances[i] as Collider;

                if (!collider.isTrigger)
                    return true;
            }

            return false;
        }

        public static float SizeHandle(Vector3 localPos, Vector3 localPullDir, Matrix4x4 matrix)
        {
            bool changed = GUI.changed;
            GUI.changed = false;

            Vector3 rhs = matrix.MultiplyVector(localPullDir);
            Vector3 position = matrix.MultiplyPoint(localPos);
            float handleSize = HandleUtility.GetHandleSize(position);

            Color color = Handles.color;
            float angle = Mathf.Cos(0.7853982f);

            float dot;
            if (Camera.current.orthographic)
                dot = Vector3.Dot(-Camera.current.transform.forward, rhs);
            else
                dot = Vector3.Dot((Camera.current.transform.position - position).normalized, rhs);

            if (dot < -angle)
                Handles.color = new Color(Handles.color.r, Handles.color.g, Handles.color.b, Handles.color.a * 0.2f);

            Vector3 point = Handles.Slider(position, rhs, handleSize * 0.03f, new Handles.CapFunction(Handles.DotHandleCap), 0f);

            float result = 0f;
            if (GUI.changed)
                result = HandleUtility.PointOnLineParameter(point, position, rhs);

            GUI.changed |= changed;
            Handles.color = color;

            return result;
        }
    }
}