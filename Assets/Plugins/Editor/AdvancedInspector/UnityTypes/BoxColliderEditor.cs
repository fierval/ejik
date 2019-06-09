using System;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(BoxCollider), true)]
    public class BoxColliderEditor : ColliderEditor
    {
        protected override void RefreshFields()
        {
            Type type = typeof(BoxCollider);

            base.RefreshFields();

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("center"),
                new DescriptorAttribute("Center", "The center of the box, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/BoxCollider-center.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("size"),
                new DescriptorAttribute("Size", "The size of the box, measured in the object's local space.", "http://docs.unity3d.com/ScriptReference/BoxCollider-size.html")));
        }

        protected override void OnSceneGUI()
        {
            base.OnSceneGUI();

            if (Event.current.type == EventType.Used)
                return;

            BoxCollider collider = (BoxCollider)target;

            Color color = Handles.color;
            if (collider.enabled)
                Handles.color = ColliderHandleColor;
            else
                Handles.color = ColliderHandleColorDisabled;

            Vector3 center = collider.center;
            Vector3 size = collider.size;
            Vector3 result;

            Matrix4x4 matrix = Matrix4x4.TRS(collider.transform.position, collider.transform.rotation, collider.transform.localScale);
            float value = SizeHandle(center + new Vector3(size.x * 0.5f, 0, 0), Vector3.right, matrix);
            if (GUI.changed)
            {
                result = new Vector3(value, 0, 0);
                EditBox(collider, center + (result * 0.5f), size + result);
            }
            else
            {
                value = SizeHandle(center - new Vector3(size.x * 0.5f, 0, 0), Vector3.right, matrix);
                if (GUI.changed)
                {
                    result = new Vector3(value, 0, 0);
                    EditBox(collider, center - (result * 0.5f), size - result);
                }
                else
                {
                    value = SizeHandle(center + new Vector3(0, size.y * 0.5f, 0), Vector3.up, matrix);
                    if (GUI.changed)
                    {
                        result = new Vector3(0, value, 0);
                        EditBox(collider, center + (result * 0.5f), size + result);
                    }
                    else
                    {
                        value = SizeHandle(center - new Vector3(0, size.y * 0.5f, 0), Vector3.up, matrix);
                        if (GUI.changed)
                        {
                            result = new Vector3(0, value, 0);
                            EditBox(collider, center - (result * 0.5f), size - result);
                        }
                        else
                        {
                            value = SizeHandle(center + new Vector3(0, 0, size.z * 0.5f), Vector3.forward, matrix);
                            if (GUI.changed)
                            {
                                result = new Vector3(0, 0, value);
                                EditBox(collider, center + (result * 0.5f), size + result);
                            }
                            else
                            {
                                value = SizeHandle(center - new Vector3(0, 0, size.z * 0.5f), Vector3.forward, matrix);
                                if (GUI.changed)
                                {
                                    result = new Vector3(0, 0, value);
                                    EditBox(collider, center - (result * 0.5f), size - result);
                                }
                            }
                        }
                    }
                }
            }
        }

        private static void EditBox(BoxCollider collider, Vector3 center, Vector3 size)
        {
            Undo.RecordObject(collider, "Edited Box Collider");
            collider.center = center;
            collider.size = new Vector3(Mathf.Max(size.x, 0.001f), Mathf.Max(size.y, 0.001f), Mathf.Max(size.z, 0.001f));
        }
    }
}
