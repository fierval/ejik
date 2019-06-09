using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    [Serializable]
    internal class InspectorReference
    {
        public string path;

        private int[] id = new int[0];
        public UnityEngine.Object[] Objects
        {
            get 
            {
                if (id == null || id.Length == 0)
                    return new UnityEngine.Object[0];

                List<UnityEngine.Object> collection = new List<UnityEngine.Object>();

                for (int i = 0; i < id.Length; i++)
                {
                    UnityEngine.Object o = EditorUtility.InstanceIDToObject(id[i]);
                    if (o)
                        collection.Add(o);
                }

                return collection.ToArray();
            }

            set
            {
                List<int> collection = new List<int>();

                for (int i = 0; i < value.Length; i++)
                    if (value[i])
                        collection.Add(value[i].GetInstanceID());

                id = collection.ToArray();
            }
        }

        private AnimationCurve curve;
        private bool boolean;
        private Bounds bounds;
        private Color color;
        private float number;
        private Gradient gradient;
        private int integer;
        private object generic;
        private Quaternion quaterion;
        private Rect rect;
        private RectOffset rectOffset;
        private string text;
        private Vector2 vector2;
        private Vector3 vector3;
        private Vector4 vector4;

        private SerializedType serializedType;

        public InspectorReference(InspectorField field)
        {
            path = field.Path;

            List<int> id = new List<int>();
            for (int i = 0; i < field.SerializedInstances.Length; i++)
                if (field.SerializedInstances[i] != null)
                    id.Add(field.SerializedInstances[i].GetInstanceID());

            this.id = id.ToArray();

            if (!field.Mixed)
            {
                Type type = field.Type;
                if (type == typeof(AnimationCurve))
                {
                    serializedType = SerializedType.AnimationCurve;
                    curve = field.GetValue<AnimationCurve>();
                }
                else if (type == typeof(bool))
                {
                    serializedType = SerializedType.Boolean;
                    boolean = field.GetValue<bool>();
                }
                else if (type == typeof(Bounds))
                {
                    serializedType = SerializedType.Bounds;
                    bounds = field.GetValue<Bounds>();
                }
                else if (type == typeof(Color))
                {
                    serializedType = SerializedType.Color;
                    color = field.GetValue<Color>();
                }
                else if (type == typeof(float))
                {
                    serializedType = SerializedType.Float;
                    number = field.GetValue<float>();
                }
                else if (type == typeof(Gradient))
                {
                    serializedType = SerializedType.Gradient;
                    gradient = field.GetValue<Gradient>();
                }
                else if (type == typeof(int))
                {
                    serializedType = SerializedType.Interger;
                    integer = field.GetValue<int>();
                }
                else if (type == typeof(Quaternion))
                {
                    serializedType = SerializedType.Quaternion;
                    quaterion = field.GetValue<Quaternion>();
                }
                else if (type == typeof(Rect))
                {
                    serializedType = SerializedType.Rect;
                    rect = field.GetValue<Rect>();
                }
                else if (type == typeof(RectOffset))
                {
                    serializedType = SerializedType.RectOffset;
                    rectOffset = field.GetValue<RectOffset>();
                }
                else if (type == typeof(string))
                {
                    serializedType = SerializedType.String;
                    text = field.GetValue<string>();
                }
                else if (type == typeof(Vector2))
                {
                    serializedType = SerializedType.Vector2;
                    vector2 = field.GetValue<Vector2>();
                }
                else if (type == typeof(Vector3))
                {
                    serializedType = SerializedType.Vector3;
                    vector3 = field.GetValue<Vector3>();
                }
                else if (type == typeof(Vector4))
                {
                    serializedType = SerializedType.Vector4;
                    vector4 = field.GetValue<Vector4>();
                }
                else
                {
                    serializedType = SerializedType.Generic;
                    generic = field.GetValue();
                }
            }
        }

        #region Operators
        public static bool operator ==(InspectorReference a, InspectorReference b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(InspectorReference a, InspectorReference b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            InspectorReference other = obj as InspectorReference;
            if (other == null)
                return false;

            if (this.path != other.path)
                return false;

            if (this.id.Length != other.id.Length)
                return false;

            for (int i = 0; i < this.id.Length; i++)
                if (this.id[i] != other.id[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        public void Save()
        {
            InspectorField field = new InspectorField(Objects, path);

            switch (serializedType)
            {
                case SerializedType.AnimationCurve:
                    field.SetValue(curve);
                    break;
                case SerializedType.Boolean:
                    field.SetValue(boolean);
                    break;
                case SerializedType.Bounds:
                    field.SetValue(bounds);
                    break;
                case SerializedType.Color:
                    field.SetValue(color);
                    break;
                case SerializedType.Float:
                    field.SetValue(number);
                    break;
                case SerializedType.Generic:
                    field.SetValue(generic);
                    break;
                case SerializedType.Gradient:
                    field.SetValue(gradient);
                    break;
                case SerializedType.Interger:
                    field.SetValue(integer);
                    break;
                case SerializedType.Quaternion:
                    field.SetValue(quaterion);
                    break;
                case SerializedType.Rect:
                    field.SetValue(rect);
                    break;
                case SerializedType.RectOffset:
                    field.SetValue(rectOffset);
                    break;
                case SerializedType.String:
                    field.SetValue(text);
                    break;
                case SerializedType.Vector2:
                    field.SetValue(vector2);
                    break;
                case SerializedType.Vector3:
                    field.SetValue(vector3);
                    break;
                case SerializedType.Vector4:
                    field.SetValue(vector4);
                    break;
            }
        }

        private enum SerializedType
        { 
            AnimationCurve,
            Boolean,
            Bounds,
            Color,
            Float,
            Generic,
            Gradient,
            Interger,
            Quaternion,
            Rect,
            RectOffset,
            String,
            Vector2,
            Vector3,
            Vector4
        }
    }
}
