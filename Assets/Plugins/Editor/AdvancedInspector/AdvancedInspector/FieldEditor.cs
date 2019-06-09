using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedInspector
{
    /// <summary>
    /// When a specific type is encounter by the Inspector, we check if someone defined an field editor for it.
    /// When it occurs, all the field drawing is left to the custom editor.
    /// </summary>
    public abstract class FieldEditor
    {
        /// <summary>
        /// Minimum field height
        /// </summary>
        public const int MIN_FIELD_HEIGHT = 18;

        /// <summary>
        /// Button Height
        /// </summary>
        public const int BUTTON_HEIGHT = 16;

        /// <summary>
        /// Standard float-in-vector label width
        /// </summary>
        public const int VECTOR_FIELD_WIDTH = 16;

        /// <summary>
        /// If true and no fieldeditor is found for a specific type, this fieldeditor is used for the derived type.
        /// </summary>
        public virtual bool EditDerived
        {
            get { return false; }
        }

        /// <summary>
        /// List of type this FieldEditor edit.
        /// </summary>
        public abstract Type[] EditedTypes { get; }

        /// <summary>
        /// The draw call from the Inspector when inspecting a specific type.
        /// </summary>
        public virtual void Draw(InspectorField field, GUIStyle style) { }

        /// <summary>
        /// The draw call from the Inspector when drawing a FieldAttribute.
        /// </summary>
        public virtual void Draw(FieldAttribute attribute, InspectorField field) { }

        /// <summary>
        /// Called after the label is drawn.
        /// Useful to add dragging modifier.
        /// </summary>
        public virtual void OnLabelDraw(InspectorField field, Rect rect) { }

        /// <summary>
        /// Event raised when someone click a label.
        /// </summary>
        public virtual void OnLabelClick(InspectorField field) { }

        /// <summary>
        /// Event raised when someone double click a label.
        /// </summary>
        public virtual void OnLabelDoubleClick(InspectorField field) { }

        /// <summary>
        /// Fired when someone click and drag the label with modified key (control/shift/alt) pressed.
        /// A normal drag performs a copy/paste.
        /// </summary>
        public virtual void OnLabelDragged(InspectorField field) { }

        /// <summary>
        /// Event raised when someone right-click a label.
        /// The GenenicMenu is empty and can add new items in it.
        /// </summary>
        public virtual void OnContextualClick(InspectorField field, GenericMenu menu) { }

        private static List<FieldEditor> fieldEditors = new List<FieldEditor>();

        private static Dictionary<Type, FieldEditor> fieldEditorByTypes = new Dictionary<Type, FieldEditor>();

        internal static Dictionary<Type, FieldEditor> FieldEditorByTypes
        {
            get { return fieldEditorByTypes; }
        }

        private static Dictionary<string, FieldEditor> fieldEditorByNames = new Dictionary<string, FieldEditor>();

        internal static Dictionary<string, FieldEditor> FieldEditorByNames
        {
            get { return fieldEditorByNames; }
        }

        private static Dictionary<Type, FieldEditor> attributeEditors = new Dictionary<Type, FieldEditor>();

        internal static Dictionary<Type, FieldEditor> AttributeEditors
        {
            get { return attributeEditors; }
        }

        private static FieldEditor objectEditor;

        private static List<PropertyDrawerWrapper> propertyDrawers = new List<PropertyDrawerWrapper>();

        private static Dictionary<Type, Type> propertyDrawersByTypes = new Dictionary<Type, Type>();

        internal static Dictionary<Type, Type> PropertyDrawersByTypes
        {
            get { return propertyDrawersByTypes; }
        }

        /// <summary>
        /// Override if you want to prevent a type from being expandable at any time.
        /// Override the Expandable attributes.
        /// </summary>
        public virtual bool IsExpandable(InspectorField field)
        {
            return false;
        }

        internal static void Gather()
        {
            if (fieldEditors.Count != 0)
                return;

            fieldEditors.Clear();
            fieldEditorByTypes.Clear();
            fieldEditorByNames.Clear();

            propertyDrawers.Clear();
            propertyDrawersByTypes.Clear();

            Type propertyType = typeof(CustomPropertyDrawer);
            FieldInfo fieldType = propertyType.GetField("m_Type", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo fieldDerived = propertyType.GetField("m_UseForChildren", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsGenericType)
                        continue;

                    if (typeof(FieldEditor).IsAssignableFrom(type))
                    {
                        FieldEditor editor = (FieldEditor)Activator.CreateInstance(type);
                        fieldEditors.Add(editor);
                        fieldEditorByNames.Add(type.Name, editor);

                        if (editor.EditedTypes.Length == 1 && editor.EditedTypes[0] == typeof(UnityEngine.Object))
                            objectEditor = editor;
                    }
                    else if (typeof(PropertyDrawer).IsAssignableFrom(type))
                    {
                        object[] attributes = type.GetCustomAttributes(typeof(CustomPropertyDrawer), true);
                        if (attributes.Length == 0)
                            continue;

                        propertyDrawers.Add(new PropertyDrawerWrapper(type, (Type)fieldType.GetValue(attributes[0]), (bool)fieldDerived.GetValue(attributes[0])));
                    }
                }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    Type drawer = GetDrawer(type, false);

                    if (drawer != null)
                    {
                        propertyDrawersByTypes.Add(type, drawer);
                        continue;
                    }

                    FieldEditor editor = GetEditor(type, false);

                    if (editor != null)
                        fieldEditorByTypes.Add(type, editor);
                }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (!typeof(FieldEditor).IsAssignableFrom(type) || type.IsAbstract)
                        continue;

                    FieldEditor drawer = Activator.CreateInstance(type) as FieldEditor;
                    if (drawer == null)
                        continue;

                    foreach (Type attribute in drawer.EditedTypes)
                    {
                        if (attributeEditors.ContainsKey(attribute))
                        {
                            Debug.LogError(string.Format("Two FieldDrawers - {0} and {1} - are linked to the same FieldAttribute type: {2}.", type.Name, attributeEditors[attribute].GetType().Name, attribute.Name));
                            continue;
                        }

                        attributeEditors.Add(attribute, drawer);
                    }
                }
            }
        }

        private static FieldEditor GetEditor(Type type, bool derived)
        {
            foreach (FieldEditor editor in fieldEditors)
                foreach (Type fieldType in editor.EditedTypes)
                    if (fieldType == type && (!derived || editor.EditDerived))
                        return editor;

            if (type.BaseType != null)
                return GetEditor(type.BaseType, true);

            return null;
        }

        private static Type GetDrawer(Type type, bool derived)
        {
            foreach (PropertyDrawerWrapper wrapper in propertyDrawers)
                if (wrapper.type == type && (!derived || wrapper.derived))
                    return wrapper.drawer;

            if (type.BaseType != null)
                return GetDrawer(type.BaseType, true);

            return null;
        }

        /// <summary>
        /// Get the value of a field.
        /// Flag "Show Mixed Value" automaticly and return null if a multi-selection has different values.
        /// </summary>
        public object GetValue(InspectorField field)
        {
            object value = field.GetValue();
            if (field.Mixed)
            {
                EditorGUI.showMixedValue = true;
                if (field.Type.IsValueType)
                    return Activator.CreateInstance(field.Type);

                return null;
            }
            else
            {
                EditorGUI.showMixedValue = false;
                return value;
            }
        }

        /// <summary>
        /// Returning null makes Advanced Inspector inspect the object default items.
        /// If you want no fields, return an empty list.
        /// </summary>
        public virtual List<InspectorField> GetFields(InspectorField parent, object[] instances, bool inspectDefaultItems, bool bypass)
        {
            return null;
        }

        internal static bool IsDefaultFieldEditor(FieldEditor editor)
        {
            return editor == objectEditor;
        }

        internal class PropertyDrawerWrapper
        {
            public Type drawer;
            public Type type;
            public bool derived;

            public PropertyDrawerWrapper(Type drawer, Type type, bool derived)
            {
                this.drawer = drawer;
                this.type = type;
                this.derived = derived;
            }
        }
    }
}