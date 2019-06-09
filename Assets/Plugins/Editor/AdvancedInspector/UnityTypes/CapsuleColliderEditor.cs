using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(CapsuleCollider), true)]
    public class CapsuleColliderEditor : ColliderEditor
    {
        private int ControlID;

        protected override void RefreshFields()
        {
            Type type = typeof(CapsuleCollider);

            base.RefreshFields();

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("center"),
                new DescriptorAttribute("Center", "The center of the capsule, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/CapsuleCollider-center.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("height"),
                new DescriptorAttribute("Height", "The height of the capsule meased in the object's local space.", "http://docs.unity3d.com/ScriptReference/CapsuleCollider-height.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("radius"),
                new DescriptorAttribute("Radius", "The radius of the sphere, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/CapsuleCollider-radius.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("direction"), new RestrictAttribute(new RestrictAttribute.RestrictDelegate(Direction)),
                new DescriptorAttribute("Direction", "The direction of the capsule.", "http://docs.unity3d.com/ScriptReference/CapsuleCollider-direction.html")));
        }

        private List<DescriptionPair> Direction()
        {
            List<DescriptionPair> list = new List<DescriptionPair>();
            foreach (AxisOrientation orientation in Enum.GetValues(typeof(AxisOrientation)))
                list.Add(new DescriptionPair((int)orientation, new Description(orientation.ToString(), "")));

            return list;
        }

        private Matrix4x4 CapsuleOrientation(CapsuleCollider target)
        {
            if (target.direction == (int)AxisOrientation.YAxis)
                return Matrix4x4.TRS(target.transform.TransformPoint(target.center),
                    target.gameObject.transform.rotation, target.transform.localScale);
            else if (target.direction == (int)AxisOrientation.XAxis)
                return Matrix4x4.TRS(target.transform.TransformPoint(target.center),
                    target.transform.rotation * Quaternion.LookRotation(Vector3.up, Vector3.right), target.transform.localScale);
            else
                return Matrix4x4.TRS(target.transform.TransformPoint(target.center),
                    target.transform.rotation * Quaternion.LookRotation(Vector3.right, Vector3.forward), target.transform.localScale); 
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Used)
                return;

            CapsuleCollider collider = (CapsuleCollider)target;

            Color color = Handles.color;
            if (collider.enabled)
                Handles.color = ColliderHandleColor;
            else
                Handles.color = ColliderHandleColorDisabled;

            bool enabled = GUI.enabled;

            Matrix4x4 matrix = CapsuleOrientation(collider);

            float radius = collider.radius;
            float height = collider.height;
            float y = Mathf.Max(height * 0.5f, radius);
            float x = radius;

            Vector3 localPos = Vector3.up * y;

            float value = SizeHandle(localPos, Vector3.up, matrix);
            if (!GUI.changed)
                value = SizeHandle(-localPos, Vector3.down, matrix);

            if (GUI.changed)
            {
                Undo.RecordObject(collider, "Edited Capsule Collider");
                collider.height += value / y / collider.height;
                collider.height = Mathf.Max(collider.height, 0.001f);
            }
            else
            {
                if (!GUI.changed)
                    value = SizeHandle(Vector3.left * x, Vector3.left, matrix);

                if (!GUI.changed)
                    value = SizeHandle(-Vector3.left * x, -Vector3.left, matrix);

                if (!GUI.changed)
                    value = SizeHandle(Vector3.forward * x, Vector3.forward, matrix);

                if (!GUI.changed)
                    value = SizeHandle(-Vector3.forward * x, -Vector3.forward, matrix);

                if (GUI.changed)
                {
                    Undo.RecordObject(collider, "Edited Capsule Collider");
                    collider.radius += value;
                    collider.radius = Mathf.Max(collider.radius, 0.001f);
                }
            }

            Handles.color = color;
            GUI.enabled = enabled;
        }
    }

    public enum AxisOrientation
    { 
        XAxis = 0,
        YAxis = 1,
        ZAxis = 2
    }
}
