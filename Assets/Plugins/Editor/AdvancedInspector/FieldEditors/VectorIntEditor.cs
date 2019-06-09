using UnityEngine;
using UnityEditor;
using System;

#if !UNITY_2017_1
namespace AdvancedInspector
{
    public class VectorIntEditor : FieldEditor
    {
        public override Type[] EditedTypes
        {
            get { return new Type[] { typeof(Vector2Int), typeof(Vector3Int) }; }
        }

        public override bool IsExpandable(InspectorField field)
        {
            return false;
        }

        public override void Draw(InspectorField field, GUIStyle style)
        {
            Type type = field.BaseType;

            float width = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = VECTOR_FIELD_WIDTH;

            GUILayout.BeginHorizontal();
            if (type == typeof(Vector2Int))
            {
                Vector2Int[] values = field.GetValues<Vector2Int>();

                int[] x = new int[values.Length];
                int[] y = new int[values.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    x[i] = values[i].x;
                    y[i] = values[i].y;
                }

                int result;
                if (IntegerEditor.DrawInt("X", x, style, out result))
                {
                    field.RecordObjects("Edit " + field.Name + " X");

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        values[i].x = result;
                        field.SetValue(field.Instances[i], values[i]);
                    }
                }

                if (IntegerEditor.DrawInt("Y", y, style, out result))
                {
                    field.RecordObjects("Edit " + field.Name + " Y");

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        values[i].y = result;
                        field.SetValue(field.Instances[i], values[i]);
                    }
                }
            }
            else if (type == typeof(Vector3Int))
            {
                Vector3Int[] values = field.GetValues<Vector3Int>();

                int[] x = new int[values.Length];
                int[] y = new int[values.Length];
                int[] z = new int[values.Length];

                for (int i = 0; i < values.Length; i++)
                {
                    x[i] = values[i].x;
                    y[i] = values[i].y;
                    z[i] = values[i].z;
                }

                int result;
                if (IntegerEditor.DrawInt("X", x, style, out result))
                {
                    field.RecordObjects("Edit " + field.Name + " X");

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        values[i].x = result;
                        field.SetValue(field.Instances[i], values[i]);
                    }
                }

                if (IntegerEditor.DrawInt("Y", y, style, out result))
                {
                    field.RecordObjects("Edit " + field.Name + " Y");

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        values[i].y = result;
                        field.SetValue(field.Instances[i], values[i]);
                    }
                }

                if (IntegerEditor.DrawInt("Z", z, style, out result))
                {
                    field.RecordObjects("Edit " + field.Name + " Z");

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        values[i].z = result;
                        field.SetValue(field.Instances[i], values[i]);
                    }
                }
            }
            
            GUILayout.EndHorizontal();

            EditorGUIUtility.labelWidth = width;
        }

        public override void OnContextualClick(InspectorField field, GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Zero"), false, Zero);
            menu.AddItem(new GUIContent("One"), false, One);

            menu.AddSeparator("");
        }

        private void Zero()
        {
            if (AdvancedInspectorControl.Field.Type == typeof(Vector2Int))
                AdvancedInspectorControl.Field.SetValue(Vector2Int.zero);
            else if (AdvancedInspectorControl.Field.Type == typeof(Vector3))
                AdvancedInspectorControl.Field.SetValue(Vector3Int.zero);
        }

        private void One()
        {
            if (AdvancedInspectorControl.Field.Type == typeof(Vector2Int))
                AdvancedInspectorControl.Field.SetValue(Vector2Int.one);
            else if (AdvancedInspectorControl.Field.Type == typeof(Vector3Int))
                AdvancedInspectorControl.Field.SetValue(Vector3Int.one);
        }
    }
}
#endif