using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Pooled information about a PropertyInfo and translated for the Inspector.
    /// </summary>
    public class InspectorField : IComparable, IComparable<InspectorField>
    {
        #region Properties
        private object[] instances = new object[0];

        /// <summary>
        /// The object owning this specific field.
        /// </summary>
        public object[] Instances
        {
            get { return instances; }
        }

        internal object[] internalValue = new object[0];

        private UnityEngine.Object[] serializedInstances = new UnityEngine.Object[0];

        /// <summary>
        /// The serialized object owning this specific field.
        /// May not be the same as the instance, or even the currently Selected item in the Inspector.
        /// </summary>
        public UnityEngine.Object[] SerializedInstances
        {
            get { return serializedInstances; }
        }

        private UnityEngine.Object prefab;

        /// <summary>
        /// The prefab parent to this object.
        /// </summary>
        public UnityEngine.Object Prefab
        {
            get
            {
                if (parent != null && (index != -1 || !(typeof(UnityEngine.Object).IsAssignableFrom(Type))))
                    return parent.Prefab;

                return prefab;
            }
        }

        private MemberInfo info;

        /// <summary>
        /// The member info of this field.
        /// PropertyInfo, FieldInfo or MethodInfo. Otherwise null.
        /// </summary>
        public MemberInfo Info
        {
            get { return info; }
        }

        private Attribute[] attributes;

        /// <summary>
        /// The attributes of this field, manually and by reflection.
        /// </summary>
        public Attribute[] Attributes
        {
            get { return attributes; }
        }

        private SerializedProperty serializedProperty = null;

        /// <summary>
        /// Used only for the Script object reference at the top, or hidden Unity property.
        /// </summary>
        public SerializedProperty SerializedProperty
        {
            get
            {
                if (serializedProperty == null)
                {
                    SerializedObject so = new SerializedObject(SerializedInstances);
                    string path = "";
                    InspectorField parent = this;
                    while (parent != null && !typeof(UnityEngine.Object).IsAssignableFrom(parent.Type))
                    {
                        if (parent.Index != -1)
                        {
                            if (string.IsNullOrEmpty(path))
                                path = parent.Info.Name + ".Array.data[" + parent.Index.ToString() + "]";
                            else
                                path = parent.Info.Name + ".Array.data[" + parent.Index.ToString() + "]." + path;

                            if (parent.Parent != null)
                                parent = parent.Parent.Parent;
                            else
                                parent = null;
                        }
                        else
                        {
                            if (string.IsNullOrEmpty(path))
                                path = parent.Info.Name;
                            else
                                path = parent.Info.Name + "." + path;

                            parent = parent.Parent;
                        }
                    }

                    serializedProperty = so.FindProperty(path);
                }

                return serializedProperty;
            }
            internal set { serializedProperty = value; }
        }



        /// <summary>
        /// Get the parents GameObjects in cases of components. Useful for undoes.
        /// </summary>
        public GameObject[] GameObjects
        {
            get
            {
                if (serializedInstances.Length == 0 || !typeof(Component).IsAssignableFrom(serializedInstances[0].GetType()))
                    return null;

                GameObject[] gameObjects = new GameObject[serializedInstances.Length];
                for (int i = 0; i < serializedInstances.Length; i++)
                    gameObjects[i] = ((Component)serializedInstances[i]).gameObject;

                return gameObjects;
            }
        }

        private InspectorField parent;

        /// <summary>
        /// The parent field in case of nesting.
        /// Null if directly at the top.
        /// </summary>
        public InspectorField Parent
        {
            get { return parent; }
        }

        private List<InspectorField> fields = new List<InspectorField>();

        /// <summary>
        /// Subfields, such as field in an expandable object, or a list.
        /// </summary>
        public List<InspectorField> Fields
        {
            get { return fields; }
        }

        private List<InspectorField> internalFields = new List<InspectorField>();

        /// <summary>
        /// Fields used for edition, are not drawn by the Inspector automaticly.
        /// </summary>
        public List<InspectorField> InternalFields
        {
            get { return internalFields; }
        }

        private int depth = 0;

        /// <summary>
        /// Indentation depth of this field.
        /// </summary>
        public int Depth
        {
            get 
            {
                if (parent != null && parent.GetAttribute<DisplayAsParentAttribute>() != null)
                    return parent.Depth;

                return depth; 
            }

            internal set
            {
                depth = value;
                foreach (InspectorField field in Fields)
                    field.Depth = depth + 1;
            }
        }

        private InspectorType inspectorType = InspectorType.None;

        /// <summary>
        /// Is this field a method, field, property or a group?
        /// </summary>
        public InspectorType InspectorType
        {
            get { return inspectorType; }
        }

        /// <summary>
        /// External editor are sometimes used to browse non-Unity object.
        /// Sadly, undo only works on valid Unity objects.
        /// Also return false if one of the serializable parent became null or none.
        /// </summary>
        public bool Undoable
        {
            get
            {
                if (SerializedInstances.Length > 0)
                {
                    bool serializable = true;
                    for (int i = 0; i < SerializedInstances.Length; i++)
                        if (SerializedInstances[i] == null)
                            serializable = false;

                    return serializable;
                }

                return false;
            }
        }

        private Type type;

        /// <summary>
        /// The type of this field.
        /// In most case, would return the FieldInfo.FieldType or PropertyInfo.PropertyType
        /// In case of a group, it returns null.
        /// If this field is flagged as Runtime-Resolved, it returns the type of the current value.
        /// </summary>
        public Type Type
        {
            get
            {
                if (inspectorType == InspectorType.Unlinked)
                    return type;

                if (inspectorType == InspectorType.Group || inspectorType == InspectorType.Toolbar)
                    return null;

                if (inspectorType == InspectorType.Script)
                    return typeof(MonoScript);

                IRuntimeType runtime = GetAttribute<IRuntimeType>();
                Type runtimeType = null;
                if (runtime != null)
                {
                    runtimeType = runtime.GetType(instances, GetValues());
                    if (runtimeType != null)
                        return runtimeType;

                    object value = GetValue();
                    if (value != null)
                        return value.GetType();
                }

                if (inspectorType == InspectorType.Property)
                {
                    if (index != -1)
                    {
                        ICollection collection = instances[0] as ICollection;
                        Type collectionType = collection.GetType();
                        if (collectionType.IsGenericType)
                            return collectionType.GetGenericArguments()[0];
                        else
                            return collectionType.GetElementType();
                    }
                    else if (key != null)
                    {
                        ICollection collection = instances[0] as ICollection;
                        Type collectionType = collection.GetType().GetBaseGenericType();
                        if (collectionType != null)
                            return collectionType.GetGenericArguments()[1];
                        else
                            return null;
                    }
                    else
                        return ((PropertyInfo)info).PropertyType;
                }
                else if (inspectorType == InspectorType.Field)
                {
                    if (index != -1)
                    {
                        ICollection collection = instances[0] as ICollection;
                        Type collectionType = collection.GetType();
                        if (collectionType.IsGenericType)
                            return collectionType.GetGenericArguments()[0];
                        else
                            return collectionType.GetElementType();
                    }
                    else if (key != null)
                    {
                        ICollection collection = instances[0] as ICollection;
                        Type collectionType = collection.GetType().GetBaseGenericType();
                        if (collectionType != null)
                            return collectionType.GetGenericArguments()[1];
                        else
                            return null;
                    }
                    else
                        return ((FieldInfo)info).FieldType;
                }
                else if (inspectorType == InspectorType.Serialized)
                {
                    switch (serializedProperty.propertyType)
                    {
                        case SerializedPropertyType.AnimationCurve:
                            return typeof(AnimationCurve);
                        case SerializedPropertyType.ArraySize:
                            return typeof(int);
                        case SerializedPropertyType.Boolean:
                            return typeof(bool);
                        case SerializedPropertyType.Bounds:
                            return typeof(Bounds);
                        case SerializedPropertyType.Character:
                            return typeof(char);
                        case SerializedPropertyType.Color:
                            return typeof(Color);
                        case SerializedPropertyType.Enum:
                            return typeof(Enum);
                        case SerializedPropertyType.Float:
                            return typeof(float);
                        case SerializedPropertyType.Generic:
                            if (instances.Length > 0 && instances[0] != null)
                                return instances[0].GetType();
                            else
                                return typeof(object);
                        case SerializedPropertyType.Gradient:
                            return typeof(Gradient);
                        case SerializedPropertyType.Integer:
                            return typeof(int);
                        case SerializedPropertyType.LayerMask:
                            return typeof(LayerMask);
                        case SerializedPropertyType.ObjectReference:
                            return typeof(UnityEngine.Object);
                        case SerializedPropertyType.Quaternion:
                            return typeof(Quaternion);
                        case SerializedPropertyType.Rect:
                            return typeof(Rect);
                        case SerializedPropertyType.String:
                            return typeof(string);
                        case SerializedPropertyType.Vector2:
                            return typeof(Vector2);
                        case SerializedPropertyType.Vector3:
                            return typeof(Vector3);
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// In case of a collection, returns the base type.
        /// Ex.: int[] or List(int), base type if int.
        /// In a dictionary, return the value type.
        /// </summary>
        public Type BaseType
        {
            get
            {
                Type type = Type;

                if (type.IsArray)
                    return type.GetElementType();
                else if (typeof(IList).IsAssignableFrom(type))
                    return type.GetGenericArguments()[0];
                else if (typeof(IDictionary).IsAssignableFrom(type))
                {
                    Type dictType = GetDictionaryType(type);
                    if (dictType != null)
                        return dictType.GetGenericArguments()[1];
                    else
                        return type;
                }
                else
                    return type;
            }
        }

        /// <summary>
        /// In case of a dictionary, returns the key type.
        /// Return null otherwise.
        /// </summary>
        public Type KeyType
        {
            get
            {
                if (!IsDictionary)
                    return null;

                Type dictType = GetDictionaryType(Type);
                if (dictType != null)
                    return dictType.GetGenericArguments()[0];
                else
                    return null;
            }
        }

        private PropertyDrawer[] propertyDrawers = new PropertyDrawer[0];

        /// <summary>
        /// Associated property drawers, if any.
        /// </summary>
        public PropertyDrawer[] PropertyDrawers
        {
            get { return propertyDrawers; }
        }

        /// <summary>
        /// The default FieldEditor associated with this InspectorField type.
        /// Null is no FieldEditor found.
        /// </summary>
        public FieldEditor Editor
        {
            get
            {
                Type type = Type;
                FieldEditorAttribute attribute = GetAttribute<FieldEditorAttribute>();

                if (attribute != null && !string.IsNullOrEmpty(attribute.Type) && FieldEditor.FieldEditorByNames.ContainsKey(attribute.Type))
                {
                    return FieldEditor.FieldEditorByNames[attribute.Type];
                }
                else if (type != null)
                {
                    FieldEditor editor;
                    FieldEditor.FieldEditorByTypes.TryGetValue(type, out editor);
                    return editor;
                }

                return null;
            }
        }


        /// <summary>
        /// A restrictor turns any field into a drop down list of specific choices.
        /// </summary>
        public IList<DescriptionPair> Restricted
        {
            get
            {
                IRestrict restrictor = GetAttribute<IRestrict>();
                if (restrictor == null || instances.Length == 0)
                    return null;

                return DescriptionPair.GetDescriptions(restrictor.GetRestricted(instances, GetValues()));
            }
        }

        /// <summary>
        /// Is this field part of a group?
        /// </summary>
        public GroupAttribute Group
        {
            get { return GetAttribute<GroupAttribute>(); }
        }

        /// <summary>
        /// Is this field part of a toolbar?
        /// </summary>
        public ToolbarAttribute Toolbar
        {
            get { return GetAttribute<ToolbarAttribute>(); }
        }

        /// <summary>
        /// Is this field visibility controlled by a tab?
        /// </summary>
        public TabAttribute Tab
        {
            get { return GetAttribute<TabAttribute>(); }
        }

        private Enum selectedTab;

        /// <summary>
        /// In cases of tabs, this is the currently selected tab enum value.
        /// </summary>
        public Enum SelectedTab
        {
            get { return selectedTab; }
            internal set 
            {
                if (inspectorType == InspectorType.Group || inspectorType == InspectorType.Toolbar)
                    for (int i = 0; i < fields.Count; i++)
                        fields[i].SelectedTab = value;

                selectedTab = value; 
            }
        }

        /// <summary>
        /// The Descriptor assigned to this field. Name, tooltip, icon, etc.
        /// In case of multi-objects, there is no runtime invokation and the descriptor returned is the one on the member.
        /// </summary>
        public Description Description
        {
            get
            {
                IDescriptor descriptor = GetAttribute<IDescriptor>();
                if (descriptor == null)
                {
                    TooltipAttribute tooltip = GetAttribute<TooltipAttribute>();
                    if (tooltip == null)
                        return null;

                    return new Description(Name, tooltip.tooltip);
                }

                return descriptor.GetDescription(instances, GetValues());
            }
        }

        /// <summary>
        /// Returns true if the item inspected is static.
        /// </summary>
        public bool Static
        {
            get
            {
                if (inspectorType == InspectorType.Property)
                {
                    MethodInfo method = ((PropertyInfo)info).GetGetMethod();
                    if (method != null)
                        return method.IsStatic;

                    method = ((PropertyInfo)info).GetSetMethod();
                    if (method != null)
                        return method.IsStatic;
                }
                else if (inspectorType == InspectorType.Field)
                    return ((FieldInfo)info).IsStatic;
                else if (inspectorType == InspectorType.Method)
                    return ((MethodInfo)info).IsStatic;

                return false;
            }
        }

        private CacheValue<bool> readOnlyCache = new CacheValue<bool>();

        /// <summary>
        /// Readonly field cannot be edited.
        /// In case of multi-objects, if any is readonly, all are.
        /// </summary>
        public bool ReadOnly
        {
            get
            {
                if (!readOnlyCache.Cached)
                    readOnlyCache.Cache(ReadOnlyInternal);

                return readOnlyCache.Value;
            }
        }

        private bool ReadOnlyInternal
        {
            get
            {
                for (int i = 0; i < serializedInstances.Length; i++)
                {
                    if (!AssetDatabase.IsOpenForEdit(serializedInstances[i], StatusQueryOptions.UseCachedIfPossible))
                        return true;
                }

                // Property without a setter
                if (inspectorType == InspectorType.Property && !((PropertyInfo)info).CanWrite)
                    return true;

                if (inspectorType == InspectorType.Serialized && !serializedProperty.editable)
                    return true;

                // Parent that are manually flagged read only propagate downward
                if (Parent != null && Parent.ReadOnly)
                    return true;

                if (!EditorApplication.isPlaying && Static && inspectorType != InspectorType.Method)
                    return true;

                IReadOnly[] readOnly = GetAttributes<IReadOnly>();
                for (int i = 0; i < readOnly.Length; i++)
                    if (readOnly[i].IsReadOnly(instances, GetValues()))
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Is this field animated. Only works for fields, not property.
        /// </summary>
        public bool Animated
        {
            get
            {
                if (!AnimationMode.InAnimationMode() || inspectorType != InspectorType.Field || serializedInstances.Length == 0)
                    return false;

                if (serializedProperty != null)
                    return AnimationMode.IsPropertyAnimated(serializedInstances[0], serializedProperty.propertyPath);

                return false;
            }
        }

        private bool bypass = false;

        /// <summary>
        /// This field is a bypass field, and sub-field automaticly recieve that flag.
        /// </summary>
        public bool Bypass
        {
            get { return bypass; }
        }

        private CacheValue<bool> expandableCache = new CacheValue<bool>();

        private bool expandable = false;

        /// <summary>
        /// Can this field be expanded?
        /// It display the arrow button.
        /// </summary>
        public bool Expandable
        {
            get
            {
                if (!expandableCache.Cached)
                    expandableCache.Cache(ExpandableInternal);

                return expandableCache.Value;
            }

            set { expandable = value; }
        }

        private bool ExpandableInternal
        {
            get
            {
                if (expandable || IsList || IsDictionary || InspectorType == InspectorType.Group)
                    return true;

                Type type = Type;
                if (type == null || InspectorType == InspectorType.Toolbar)
                    return false;

                IExpandable expand = GetAttribute<IExpandable>();

                bool isExpandable = true;
                if (expand != null)
                    isExpandable = expand.IsExpandable(instances, GetValues());

                if (!isExpandable)
                    return false;

                FieldEditor fieldEditor;
                if (FieldEditor.FieldEditorByTypes.TryGetValue(type, out fieldEditor) && !FieldEditor.IsDefaultFieldEditor(fieldEditor) && !fieldEditor.IsExpandable(this))
                    return false;

                if (propertyDrawers.Length > 0)
                    return false;

                InspectorEditor inspectorEditor;
                if (InspectorEditor.InspectorEditorByTypes.TryGetValue(type, out inspectorEditor))
                    return inspectorEditor.Expandable;

                if (!bypass && type.Namespace != null && type.Namespace.Contains("UnityEngine"))
                    return false;

                ExpandableAttribute expandableAttribute = null;
                if (expandableAttributeByType.ContainsKey(type))
                    expandableAttribute = expandableAttributeByType[type];
                else
                {
                    object[] inspectorAttributes = type.GetCustomAttributes(typeof(ExpandableAttribute), true);
                    if (inspectorAttributes.Length > 0)
                    {
                        expandableAttribute = inspectorAttributes[0] as ExpandableAttribute;
                        expandableAttributeByType.Add(type, expandableAttribute);
                    }
                    else
                        expandableAttributeByType.Add(type, null);
                }

                if (expandableAttribute != null && !expandableAttribute.Expandable)
                    return false;

                if (bypass ||
                    (InspectorPreferences.InspectDefaultItems && isExpandable) ||
                    (expandableAttribute != null && expandableAttribute.Expandable && isExpandable) ||
                    (expandableAttribute == null && expand != null && isExpandable))
                    return true;

                return false;
            }
        }

        private bool expanded = false;

        /// <summary>
        /// Is this field open and displaying internal fields?
        /// </summary>
        public bool Expanded
        {
            get 
            {
                if (Group != null && !Group.Expandable)
                    return true;

                return expanded; 
            }

            set
            {
                if (expanded == value)
                    return;

                expanded = value;

                string path = Path;
                EditorPrefs.SetBool(path, expanded);

                if (expanded)
                    RefreshFields();
            }
        }

        private bool alwaysExpanded = false;

        /// <summary>
        /// This field cannot be collapsed.
        /// </summary>
        public bool AlwaysExpanded
        {
            get { return alwaysExpanded; }
        }

        private bool erased = false;

        /// <summary>
        /// Was this field erased?
        /// </summary>
        public bool Erased
        {
            get { return erased; }
            set { erased = value; }
        }

        /// <summary>
        /// Can this object field display the "+" sign to create a derived object from the base type?
        /// </summary>
        public bool CreateDerived
        {
            get { return GetAttribute<CreateDerivedAttribute>() != null && !typeof(ICollection).IsAssignableFrom(BaseType); }
        }

        private CacheValue<string> nameCache = new CacheValue<string>();

        private string name = "";

        /// <summary>
        /// Name of the label of that field.
        /// Can be modified by Descriptor.
        /// </summary>
        public string Name
        {
            get
            {
                if (!nameCache.Cached)
                    nameCache.Cache(NameInternal);

                return nameCache.Value;
            }
        }

        private string NameInternal
        {
            get
            {
                IDescriptor descriptor = GetAttribute<IDescriptor>();
                if (descriptor != null)
                {
                    Description description = descriptor.GetDescription(instances, GetValues());
                    if (description != null && !string.IsNullOrEmpty(description.Name))
                        return description.Name;
                }

                if (!string.IsNullOrEmpty(name))
                    return name;

                if (index != -1)
                {
                    CollectionAttribute collection = GetAttribute<CollectionAttribute>();
                    if (collection != null && collection.Delegates.Count > 0)
                        return collection.Invoke(0, instances[0], GetValue())[index];
                    else if (collection == null || collection.EnumType == null)
                    {
                        switch (InspectorPreferences.CollectionItemNaming)
                        {
                            case InspectorPreferences.InspectorCollectionItemNaming.Index:
                                return "[" + index.ToString() + "]";
                            case InspectorPreferences.InspectorCollectionItemNaming.Numbered:
                                return index.ToString();
                            case InspectorPreferences.InspectorCollectionItemNaming.IndexedElements:
                                return "Element [" + index.ToString() + "]";
                            case InspectorPreferences.InspectorCollectionItemNaming.NumberedElements:
                                return "Element " + index.ToString();
                            case InspectorPreferences.InspectorCollectionItemNaming.IndexedNames:
                                return parent.Name + "[" + index.ToString() + "]";
                            case InspectorPreferences.InspectorCollectionItemNaming.NumberedNames:
                                return parent.Name + " " + index.ToString();
                            default:
                                return "";
                        }
                    }
                    else
                        return "[" + ObjectNames.NicifyVariableName(Enum.GetName(collection.EnumType, index)) + "]";
                }
                else if (key != null)
                {
                    if (key is string)
                        return (string)key;

                    switch (InspectorPreferences.CollectionItemNaming)
                    {
                        case InspectorPreferences.InspectorCollectionItemNaming.Index:
                            return "[" + key.ToString() + "]";
                        case InspectorPreferences.InspectorCollectionItemNaming.Numbered:
                            return key.ToString();
                        case InspectorPreferences.InspectorCollectionItemNaming.IndexedElements:
                            return "Element [" + key.ToString() + "]";
                        case InspectorPreferences.InspectorCollectionItemNaming.NumberedElements:
                            return "Element " + key.ToString();
                        case InspectorPreferences.InspectorCollectionItemNaming.IndexedNames:
                            return parent.Name + "[" + key.ToString() + "]";
                        case InspectorPreferences.InspectorCollectionItemNaming.NumberedNames:
                            return parent.Name + " " + key.ToString();
                        default:
                            return "";
                    }
                }
                else
                {
                    if (inspectorType == InspectorType.Unlinked)
                        return "";
                    else if (inspectorType == InspectorType.Serialized)
                        return ObjectNames.NicifyVariableName(serializedProperty.name);
                    else if (info != null)
                        return ObjectNames.NicifyVariableName(info.Name);
                    else
                        return "";
                }
            }
        }

        private bool changed = false;

        /// <summary>
        /// Has this field changed in the last redraw?
        /// Reading this value reset it to false.
        /// </summary>
        public bool Changed
        {
            get
            {
                if (changed)
                {
                    changed = false;
                    return true;
                }

                foreach (InspectorField field in fields)
                    if (field.Changed)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// The color of the editable field.
        /// </summary>
        public Color FieldColor
        {
            get
            {
                Description description = Description;
                if (description != null)
                    return description.Color;

                return Color.clear;
            }
        }

        /// <summary>
        /// The color of the background (ex.: Expandable box)
        /// </summary>
        public Color BackgroundColor
        {
            get
            {
                BackgroundAttribute background = GetAttribute<BackgroundAttribute>();
                if (background != null)
                    return background.Invoke(0, instances[0], GetValue(instances[0]));

                if (Group != null)
                    return Group.Color;

                return Color.clear;
            }
        }

        /// <summary>
        /// Priority of a field, used when sorting.
        /// </summary>
        public int Priority
        {
            get
            {
                if (InspectorType == InspectorType.Script)
                    return int.MinValue;
                else if (InspectorType == InspectorType.Group)
                    return Group.Priority;
                else if (InspectorType == InspectorType.Toolbar)
                    return Toolbar.Priority;
                else
                {
                    IVisibility visibility = GetAttribute<IVisibility>();
                    if (visibility != null)
                        return visibility.GetItemPriority(instances, GetValues());
                    else
                        return 0;
                }
            }
        }

        private int order = -1;

        /// <summary>
        /// The order at which this field was found in the code.
        /// If an indexed field, return the index.
        /// </summary>
        public int Order
        {
            get
            {
                if (index != -1)
                    return index;

                return order;
            }
        }

        /// <summary>
        /// If false, this field does not draw its label.
        /// Useful for toolbar.
        /// </summary>
        public bool Label
        {
            get
            {
                if (InspectorType == InspectorType.Toolbar)
                    return Toolbar.Label;

                StyleAttribute style = GetAttribute<StyleAttribute>();
                if (style != null)
                    return style.Label;

                if (InspectorType == InspectorType.Method)
                    return false;
                else
                    return true;
            }
        }

        /// <summary>
        /// The specific style this field is draw with.
        /// </summary>
        public string Style
        {
            get
            {
                if (InspectorType == InspectorType.Group)
                {
                    if (Group != null)
                        return Group.Style;
                }
                else if (InspectorType == InspectorType.Toolbar)
                {
                    if (Toolbar != null)
                        return Toolbar.Style;
                }
                else
                {
                    StyleAttribute style = GetAttribute<StyleAttribute>();
                    if (style != null)
                        return style.Style;
                }

                return "";
            }
        }

        /// <summary>
        /// Internal path to this specific field.
        /// Used to keep persistant data across selection.
        /// </summary>
        public string Path
        {
            get
            {
                InspectorType type = InspectorType;

                if (index != -1)
                    return Parent.Path + "[" + index.ToString() + "]";
                else if (key != null)
                    return Parent.Path + "[" + key.GetHashCode() + "]";
                else if (info != null)
                {
                    if (!(typeof(UnityEngine.Object).IsAssignableFrom(Type)))
                    {
                        if (Parent != null)
                            return Parent.Path + "/" + type.ToString() + "-" + info.Name;
                        else
                            return type.ToString() + "-" + info.Name;
                    }
                    else
                        return type.ToString() + "-" + info.Name;
                }
                else if (Group != null && Parent != null)
                    return Parent.Path;
                else
                    return "";
            }
        }

        /// <summary>
        /// Many thing can prevent a field from being drawn. Ex.: InspectAttribute can hide it.
        /// If internal data from Unity return an exception on read, we skip it.
        /// </summary>
        public bool Visible
        {
            get
            {
                if (AdvancedInspectorControl.watched)
                    return true;

                if (InspectorType == InspectorType.Group || InspectorType == InspectorType.Toolbar)
                {
                    foreach (InspectorField field in fields)
                        if (field.Visible)
                            return true;

                    return false;
                }

                if (InspectorType == InspectorType.Serialized && SerializedProperty == null)
                    return false;

                object[] values = null;
                TabAttribute tab = Tab;
                if (tab != null)
                {
                    values = GetValues();
                    for (int i = 0; i < instances.Length; i++)
                        if (!tab.Invoke(i, instances[i], values[i]).Equals(selectedTab))
                            return false;
                }

                IVisibility visibility = GetAttribute<IVisibility>();
                if (visibility != null)
                {
                    if (values == null)
                        values = GetValues();

                    if (visibility.GetItemLevel(instances, values) > AdvancedInspectorControl.Level)
                        return false;

                    return visibility.IsItemVisible(instances, values);
                }

                return true;
            }
        }

        /// <summary>
        /// Return false is any of the serialized parent have the hide flag "Not Editable".
        /// </summary>
        public bool Editable
        {
            get
            {
                if ((InspectorType == InspectorType.Field || InspectorType == InspectorType.Property) && SerializedInstances != null && SerializedInstances.Length > 0)
                    foreach (UnityEngine.Object obj in SerializedInstances)
                        if (obj != null && obj && !(obj is Editor))
                            if (obj.hideFlags.Has(HideFlags.NotEditable))
                                return false;

                return true;
            }
        }

        /// <summary>
        /// Return true if the item is different from it's prefab parent.
        /// Always return false if it's a non-value type and not a ref (UnityEngine.Object).
        /// </summary>
        public bool Modified
        {
            get
            {
                if (Prefab == null || info == null)
                    return false;

                if (Prefab.GetType() != info.DeclaringType)
                    return false;

                object original = null;
                if (InspectorType == InspectorType.Field)
                    original = ((FieldInfo)info).GetValue(Prefab);
                else if (InspectorType == InspectorType.Property)
                    original = ((PropertyInfo)info).GetValue(Prefab, new object[0]);
                else
                    return false;

                if (index != -1 && original != null)
                {
                    IList list = (IList)original;
                    if (list.Count <= index)
                        return true;
                    else
                        original = ((IList)original)[index];
                }

                foreach (object instance in GetValues())
                    if (Different(original, instance))
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Return true when editing multiple object that doesn't have the same value.
        /// Always return true if they are objects and not the same instance.
        /// </summary>
        public bool Mixed
        {
            get
            {
                if (Instances.Length <= 1)
                    return false;

                object value = GetValue(Instances[0]);
                for (int i = 1; i < Instances.Length; i++)
                {
                    object next = GetValue(Instances[i]);

                    if ((value == null && next != null) ||
                        (value != null && next == null))
                        return true;

                    if (value != null && next != null)
                    {
                        if (Expandable && value.GetType() == next.GetType())
                            continue;

                        if (!value.Equals(next))
                            return true;
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Return true if all instances are of the same type.
        /// Which mean they are expandable even in multi-objects edition
        /// </summary>
        public bool Similar
        {
            get
            {
                if (inspectorType == InspectorType.Serialized)
                    return true;

                if (inspectorType == InspectorType.Group || inspectorType == InspectorType.Toolbar)
                    return true;

                if (instances == null || instances.Length == 0)
                    return true;

                Type type = null;
                for (int i = 0; i < Instances.Length; i++)
                {
                    object next = GetValue(Instances[i]);
                    if (next == null || (next is UnityEngine.Object && !((UnityEngine.Object)next)))
                        return false;

                    if (type == null)
                        type = next.GetType();
                    else if (type != next.GetType())
                        return false;
                }

                return true;
            }
        }

        private int index = -1;

        /// <summary>
        /// Index of the field in a list or array.
        /// Return -1 if not part of a collection.
        /// </summary>
        public int Index
        {
            get { return index; }
            set 
            {
                if (index != value)
                {
                    index = value;
                    RefreshFields();
                }
            }
        }

        internal int visibleIndex = 0;

        /// <summary>
        /// Is this a collection? (array or list)
        /// </summary>
        public bool IsList
        {
            get
            {
                if (InspectorType != InspectorType.Field && InspectorType != InspectorType.Property)
                    return false;

                if (serializedProperty != null)
                    return serializedProperty.isArray;
                else
                    return typeof(IList).IsAssignableFrom(Type);
            }
        }

        private object key = null;

        /// <summary>
        /// When in a dictionary, the key of this value.
        /// </summary>
        public object Key
        {
            get { return key; }
            set
            {
                if (key != value)
                {
                    key = value;
                    RefreshFields();
                }
            }
        }

        /// <summary>
        /// Is this a dictionary? (UDictionary)
        /// </summary>
        public bool IsDictionary
        {
            get
            {
                if (InspectorType != InspectorType.Field && InspectorType != InspectorType.Property)
                    return false;

                return typeof(IDictionary).IsAssignableFrom(Type);
            }
        }

        /// <summary>
        /// Returns the current number of children.
        /// In case of list, returns the number of items in it.
        /// In case of multiples list, return -1 if all list doesn't have the same number of items.
        /// </summary>
        public int Count
        {
            get
            {
                if (!IsList && !IsDictionary)
                    return Fields.Count;

                if (InspectorType == InspectorType.Serialized)
                    return SerializedProperty.arraySize;

                ICollection collection = GetValue(Instances[0]) as ICollection;
                if (collection == null)
                {
                    InitCollection();
                    collection = GetValue(Instances[0]) as ICollection;
                }

                int count = collection.Count;
                for (int i = 1; i < Instances.Length; i++)
                    count = Mathf.Min(((ICollection)GetValue(Instances[i])).Count, count);

                return count;
            }
        }

        /// <summary>
        /// Test if the current type overload ToString properly.
        /// </summary>
        public bool OverloadToString
        {
            get
            {
                if (Instances.Length != 1)
                    return false;

                object value = GetValue();
                MethodInfo info = value.GetType().GetMethod("ToString", new Type[0]);
                return info != null && info.DeclaringType != typeof(object) && info.DeclaringType != typeof(UnityEngine.Object) && info.DeclaringType != typeof(ValueType);
            }
        }
        #endregion

        internal static event GenericEventHandler OnDataChanged;

        private static Dictionary<Type, ExpandableAttribute> expandableAttributeByType = new Dictionary<Type, ExpandableAttribute>();

        #region Constructors
        /// <summary>
        /// This constructor is used as a duplication.
        /// Note that this field is free and not parented to anything.
        /// Use with great care!
        /// </summary>
        public InspectorField(InspectorField original)
        {
            instances = original.instances;
            serializedInstances = original.serializedInstances;
            info = original.info;
            inspectorType = original.inspectorType;
            attributes = original.attributes;
            prefab = original.prefab;

            SetInfos(null, serializedInstances, instances, prefab, info, false);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();
        }

        /// <summary>
        /// Entry point for a generic manually created field.
        /// </summary>
        public InspectorField(Type type, object[] instances, MemberInfo info, params Attribute[] attributes)
            : this(null, type, instances, info, attributes) { }

        /// <summary>
        /// Entry point for a generic manually created field.
        /// </summary>
        public InspectorField(InspectorField parent, Type type, object[] instances, MemberInfo info, params Attribute[] attributes)
        {
            if (type == null)
                throw new NullReferenceException("Type cannot be null in an InspectorField field constructor.");

            if (instances == null || instances.Length == 0)
                throw new NullReferenceException("Instances cannot be null in an InspectorField field constructor.");

            if (info == null)
                throw new NullReferenceException("MemberInfo cannot be null in an InspectorField field constructor.");

            UnityEngine.Object[] serializedInstances = new UnityEngine.Object[0];
            if (instances.Length > 0 && instances[0] is UnityEngine.Object)
            {
                serializedInstances = new UnityEngine.Object[instances.Length];
                for (int i = 0; i < instances.Length; i++)
                    serializedInstances[i] = instances[i] as UnityEngine.Object;
            }

            UnityEngine.Object prefab = null;

            if (instances.Length == 1 && instances[0] is UnityEngine.Object)
                prefab = PrefabUtility.GetPrefabParent((UnityEngine.Object)instances[0]);

            Type memberType = info.GetType();
            if (typeof(PropertyInfo).IsAssignableFrom(memberType))
                inspectorType = InspectorType.Property;
            else if (typeof(FieldInfo).IsAssignableFrom(memberType))
                inspectorType = InspectorType.Field;
            else if (typeof(MethodInfo).IsAssignableFrom(memberType))
                inspectorType = InspectorType.Method;

            SetInfos(parent, serializedInstances, instances, prefab, info, false);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();

            if (GetAttribute<ObsoleteAttribute>() != null)
                Debug.LogWarning("Advanced Inspector is inspecting an obsolete member; " + info.Name + " on type " + type.Name);
        }

        /// <summary>
        /// Entry point for a generic indexed serialized created field.
        /// </summary>
        public InspectorField(InspectorField parent, int index, SerializedProperty serializedProperty, params Attribute[] attributes)
            : this (parent, index, parent.Type, new object[0], serializedProperty, attributes) { }

        /// <summary>
        /// Entry point for a generic serialized created field.
        /// </summary>
        public InspectorField(Type type, object[] instances, SerializedProperty serializedProperty, params Attribute[] attributes)
            : this(null, -1, type, instances, serializedProperty, attributes) { }

        private InspectorField(InspectorField parent, int index, Type type, object[] instances, SerializedProperty serializedProperty, params Attribute[] attributes)
        {
            if (type == null)
                throw new NullReferenceException("Type cannot be null in an InspectorField field constructor.");

            if ((instances == null || instances.Length == 0) && index == -1)
                throw new NullReferenceException("Instances cannot be null in an InspectorField field constructor.");

            if (serializedProperty == null)
                throw new NullReferenceException("SerializedProperty cannot be null in an InspectorField field constructor.");

            UnityEngine.Object[] serializedInstances = new UnityEngine.Object[instances.Length];
            for (int i = 0; i < instances.Length; i++)
                serializedInstances[i] = instances[i] as UnityEngine.Object;

            this.serializedProperty = serializedProperty;
            this.instances = instances;
            this.index = index;

            inspectorType = InspectorType.Serialized;

            UnityEngine.Object prefab = null;
            if (instances.Length == 1 && instances[0] is UnityEngine.Object)
                prefab = PrefabUtility.GetPrefabParent((UnityEngine.Object)instances[0]);

            SetInfos(parent, serializedInstances, instances, prefab, info, false);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();
        }

        /// <summary>
        /// Entry point for an empty field with no editor.
        /// </summary>
        public InspectorField(string name)
        {
            inspectorType = InspectorType.Group;

            this.name = name;

            GetMemberAttributes(new Attribute[] { new GroupAttribute(name) });
            ExpansionState();
        }

        /// <summary>
        /// Entry point of a group.
        /// </summary>
        public InspectorField(GroupAttribute group)
        {
            inspectorType = InspectorType.Group;

            this.name = group.Name;

            GetMemberAttributes(new Attribute[] { group });
            ExpansionState();
        }

        /// <summary>
        /// Entry point of a toolbar.
        /// </summary>
        public InspectorField(ToolbarAttribute toolbar)
        {
            inspectorType = InspectorType.Toolbar;

            this.name = toolbar.Name;

            GetMemberAttributes(new Attribute[] { toolbar });
            ExpansionState();
        }

        /// <summary>
        /// Entry point for a indexed field.
        /// You should not create this manually.
        /// </summary>
        private InspectorField(InspectorField parent, object[] instances, int index, params Attribute[] attributes)
        {
            this.index = index;
            SetInfos(parent, parent.SerializedInstances, instances, null, parent.Info, parent.bypass);
            inspectorType = parent.inspectorType;

            Type baseType = null;
            ICollection collection = instances[0] as ICollection;
            if (collection is Array)
                baseType = collection.GetType().GetElementType();
            else
            {
                Type[] args = collection.GetType().GetGenericArguments();
                if (args.Length > 0)
                    baseType = args[0];
            }

            SetCollection(parent, baseType, attributes);
        }

        /// <summary>
        /// Entry point of a Dictionary item.
        /// </summary>
        private InspectorField(InspectorField parent, object[] instances, object key, params Attribute[] attributes)
        {
            this.key = key;
            SetInfos(parent, parent.SerializedInstances, instances, null, parent.Info, parent.bypass);
            inspectorType = parent.inspectorType;

            Type baseType = null;
            ICollection collection = instances[0] as ICollection;
            if (typeof(IDictionary).IsAssignableFrom(collection.GetType()))
            {
                Type dictType = GetDictionaryType(collection.GetType());
                if (dictType != null)
                    baseType = dictType.GetGenericArguments()[1];
                else
                    baseType = null;
            }

            SetCollection(parent, baseType, attributes);
        }

        /// <summary>
        /// Entry point for a non-member related field.
        /// Useful to draw a single FieldEditor.
        /// </summary>
        public InspectorField(Type type)
        {
            this.type = type;
            inspectorType = InspectorType.Unlinked;
            ResetToDefault();
        }

        /// <summary>
        /// Script entry point.
        /// </summary>
        internal InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, SerializedProperty serializedProperty)
        {
            inspectorType = InspectorType.Script;
            this.name = "Script";
            this.serializedProperty = serializedProperty;
            SetInfos(parent, serializedInstances, instances, null, null, false);
        }

        /// <summary>
        /// Entry point for a Field created from a path and Object ids.
        /// </summary>
        public InspectorField(UnityEngine.Object[] serializedInstances, string path)
        {
            List<object> instances = new List<object>();

            for (int i = 0; i < serializedInstances.Length; i++)
            {
                UnityEngine.Object o = serializedInstances[i];
                if (o == null)
                    continue;

                List<string> paths = path.Split('/').ToList();
                if (paths.Count <= 1)
                    instances.Add(o);
                else
                {
                    paths.RemoveAt(paths.Count - 1);

                    string subpath = "";
                    for (int x = 0; x < paths.Count; x++)
                    {
                        subpath += paths[x];
                        if (x < paths.Count - 1)
                            subpath += "/";
                    }

                    instances.Add(GetValueByPath(o, subpath));
                }
            }

            this.serializedInstances = serializedInstances.ToArray();
            this.instances = instances.ToArray();

            this.info = GetInfoByPath(serializedInstances[0], path);

            if (this.info is FieldInfo)
                this.inspectorType = InspectorType.Field;
            else if (this.info is PropertyInfo)
                this.inspectorType = InspectorType.Property;
            else if (this.info is MethodInfo)
                this.inspectorType = InspectorType.Method;

            SetInfos(null, this.serializedInstances, this.instances, null, this.info, false);
            GetMemberAttributes(new Attribute[0]);
            ParseMemberAttributes();
        }

        /// <summary>
        /// Entry point for a method field. (Button)
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, MethodInfo info)
            : this(parent, serializedInstances, instances, info, new Attribute[0]) { }

        /// <summary>
        /// Entry point for a method field. (Button)
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, MethodInfo info, params Attribute[] attributes)
        {
            inspectorType = InspectorType.Method;

            SetInfos(parent, serializedInstances, instances, null, info, bypass);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();
        }

        /// <summary>
        /// Entry point for a standard field.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, FieldInfo info)
            : this(parent, serializedInstances, instances, prefab, info, false, new Attribute[0]) { }

        /// <summary>
        /// Entry point for a standard field.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, FieldInfo info, bool bypass)
            : this(parent, serializedInstances, instances, prefab, info, bypass, new Attribute[0]) { }

        /// <summary>
        /// Entry point for a standard field.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, FieldInfo info, bool bypass, params Attribute[] attributes)
        {
            inspectorType = InspectorType.Field;

            SetInfos(parent, serializedInstances, instances, prefab, info, bypass);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();
        }

        /// <summary>
        /// Entry point for a standard property.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, PropertyInfo info)
            : this(parent, serializedInstances, instances, prefab, info, false, new Attribute[0]) { }

        /// <summary>
        /// Entry point for a standard property.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, PropertyInfo info, bool bypass)
            : this(parent, serializedInstances, instances, prefab, info, bypass, new Attribute[0]) { }

        /// <summary>
        /// Entry point for a standard property.
        /// </summary>
        public InspectorField(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, PropertyInfo info, bool bypass, params Attribute[] attributes)
        {
            inspectorType = InspectorType.Property;

            SetInfos(parent, serializedInstances, instances, prefab, info, bypass);
            GetMemberAttributes(attributes);
            ParseMemberAttributes();
        }
        #endregion

        #region Operator
        /// <summary>
        /// Compares if two InspectorField contains the same data.
        /// </summary>
        public static bool operator ==(InspectorField a, InspectorField b)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(a, b))
                return true;

            // If one is null, but not both, return false.
            if (((object)a == null) || ((object)b == null))
                return false;

            return a.Equals(b);
        }

        /// <summary>
        /// Compares if two InspectorField contains the same data.
        /// </summary>
        public static bool operator !=(InspectorField a, InspectorField b)
        {
            return !(a == b);
        }

        /// <summary>
        /// Compares if two InspectorField contains the same data.
        /// </summary>
        public override bool Equals(object obj)
        {
            InspectorField other = obj as InspectorField;
            if (other == null)
                return false;

            if (this.inspectorType != other.inspectorType)
                return false;

            if (info != null)
            {
                if (this.info != other.info)
                    return false;

                if (this.Instances.Length != other.Instances.Length)
                    return false;

                for (int i = 0; i < this.Instances.Length; i++)
                    if (this.Instances[i] != other.Instances[i])
                        return false;

                if (this.Index != other.Index)
                    return false;

                if (this.Key != other.Key)
                    return false;
            }
            else if (serializedProperty != null)
            {
                return (this.serializedProperty == other.serializedProperty);
            }
            else
            {
                if (this.name != other.name)
                    return false;

                if (this.internalValue.Length != other.internalValue.Length)
                    return false;

                for (int i = 0; i < this.internalValue.Length; i++)
                    if (this.internalValue[i] != other.internalValue[i])
                        return false;
            }

            return true;
        }

        /// <summary>
        /// HashCode
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        #region Private
        private void SetInfos(InspectorField parent, UnityEngine.Object[] serializedInstances, object[] instances, UnityEngine.Object prefab, MemberInfo info, bool bypass)
        {
            this.parent = parent;
            this.serializedInstances = serializedInstances;
            this.instances = instances;
            this.prefab = prefab;
            this.info = info;
            this.bypass = bypass;

            foreach (object instance in instances)
                if (instance is IDataChanged)
                    ((IDataChanged)instance).OnDataChanged += DataChanged;
        }

        internal void Dispose()
        {
            foreach (object instance in instances)
                if (instance is IDataChanged)
                    ((IDataChanged)instance).OnDataChanged -= DataChanged;

            foreach (InspectorField field in fields)
                field.Dispose();
        }

        private void DataChanged()
        {
            if (OnDataChanged != null)
                OnDataChanged();
        }

        private void SetCollection(InspectorField parent, Type baseType, Attribute[] extras)
        {
            inspectorType = parent.InspectorType;
            
            List<Attribute> parentAttributes = new List<Attribute>();
            parentAttributes.AddRange(extras);
            foreach (Attribute attribute in parent.attributes)
                if (attribute is IListAttribute)
                    parentAttributes.Add(attribute);

            GetMemberAttributes(parentAttributes.ToArray());
            ParseMemberAttributes();
        }

        private void GetMemberAttributes(Attribute[] extra)
        {
            if (info != null && Index == -1 && Key == null)
            {
                object[] memberAttributes = info.GetCustomAttributes(true);

                attributes = new Attribute[extra.Length + memberAttributes.Length];

                for (int i = 0; i < memberAttributes.Length; i++)
                    attributes[i] = memberAttributes[i] as Attribute;

                for (int i = 0; i < extra.Length; i++)
                    attributes[i + memberAttributes.Length] = extra[i];
            }
            else
                attributes = extra;
        }
        private void ParseMemberAttributes()
        {
            List<PropertyDrawer> propertyDrawers = new List<PropertyDrawer>();
            propertyDrawers.AddRange(GetPropertyDrawers());
            propertyDrawers.AddRange(GetAttributeDrawers());
            this.propertyDrawers = propertyDrawers.ToArray();

            if (info != null && index == -1 && key == null)
            {
                for (int i = 0; i < attributes.Length; i++)
                {
                    AdvancedInspectorControl.ParseRuntimeAttributes(attributes[i] as IRuntimeAttribute, info.DeclaringType, Instances);
                }
            }

            bypass = GetAttribute<BypassAttribute>() != null || bypass;

            if (InspectorType == InspectorType.Method)
                return;

            DisplayAsParentAttribute displayAsParent = GetAttribute<DisplayAsParentAttribute>();
            if (displayAsParent != null)
                RefreshFields();
            else
                ExpansionState();
        }

        private PropertyDrawer[] GetPropertyDrawers()
        {
            if (Type == null)
                return new PropertyDrawer[0];

            Type drawerType;
            FieldEditor.PropertyDrawersByTypes.TryGetValue(Type, out drawerType);

            if (drawerType != null)
            {
                FieldInfo fieldField = drawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                PropertyDrawer drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;

                if (Info is FieldInfo)
                    fieldField.SetValue(drawer, Info);

                return new PropertyDrawer[] { drawer };
            }
            else
                return new PropertyDrawer[0];
        }

        private PropertyDrawer[] GetAttributeDrawers()
        {
            if (Type == null)
                return new PropertyDrawer[0];

            PropertyAttribute[] attributes = GetAttributes<PropertyAttribute>().OrderBy(x => x.order).ToArray();
            if (attributes.Length == 0)
                return new PropertyDrawer[0];

            List<PropertyDrawer> drawers = new List<PropertyDrawer>();
            foreach (PropertyAttribute attribute in attributes)
            {
                if ((IsList || IsDictionary) && attribute is IListAttribute)
                    continue;

                Type drawerType;
                FieldEditor.PropertyDrawersByTypes.TryGetValue(attribute.GetType(), out drawerType);
                if (drawerType != null)
                {
                    PropertyDrawer drawer = Activator.CreateInstance(drawerType) as PropertyDrawer;
                    FieldInfo attributeField = drawerType.GetField("m_Attribute", BindingFlags.Instance | BindingFlags.NonPublic);
                    attributeField.SetValue(drawer, attribute);

                    FieldInfo fieldField = drawerType.GetField("m_FieldInfo", BindingFlags.Instance | BindingFlags.NonPublic);
                    fieldField.SetValue(drawer, Info);

                    drawers.Add(drawer);
                }
            }

            return drawers.ToArray();
        }

        private bool Different(object original, object instance)
        {
            if (original == null && instance != null || original != null && instance == null)
                return true;

            if (original == null && instance == null)
                return false;

            Type originalType = original.GetType();
            Type instanceType = instance.GetType();
            if (originalType != instanceType)
                return true;

            if (typeof(ComponentMonoBehaviour).IsAssignableFrom(originalType))
            {
                return false;
            }
            else if (typeof(UnityEngine.Object).IsAssignableFrom(originalType))
            {
                if ((UnityEngine.Object)original != (UnityEngine.Object)instance)
                    return true;
            }
            else if (typeof(IList).IsAssignableFrom(originalType))
            {
                IList originalList = original as IList;
                IList instanceList = instance as IList;

                if (originalList.Count != instanceList.Count)
                    return true;

                for (int i = 0; i < originalList.Count; i++)
                {
                    object originalValue = originalList[i];
                    object instanceValue = instanceList[i];
                    if (object.ReferenceEquals(original, originalValue) || object.ReferenceEquals(instance, instanceValue))
                        return false;

                    if (Different(originalValue, instanceValue))
                        return true;
                }
            }
            else if (typeof(IDictionary<,>).IsAssignableFrom(originalType))
            {
                IDictionary originalDict = original as IDictionary;
                IDictionary instanceDict = instance as IDictionary;

                if (originalDict.Count != instanceDict.Count)
                    return true;

                foreach (DictionaryEntry entry in originalDict)
                {
                    if (!instanceDict.Contains(entry.Key))
                        return true;

                    object originalValue = originalDict[entry.Key];
                    object instanceValue = instanceDict[entry.Key];
                    if (object.ReferenceEquals(original, originalValue) || object.ReferenceEquals(instance, instanceValue))
                        return false;

                    if (Different(originalValue, instanceValue))
                        return true;
                }
            }
            else if (originalType.Namespace == "System")
            {
                if (!original.Equals(instance))
                    return true;
            }
            else if (original is IComparable)
            {
                if (((IComparable)original).CompareTo(instance) != 0)
                    return true;
            }
            else if (originalType.IsClass || originalType.IsValueType)
            {
                if (originalType != instanceType)
                    return true;

                foreach (FieldInfo field in Clipboard.GetFields(originalType))
                {
                    object originalValue = field.GetValue(original);
                    object instanceValue = field.GetValue(instance);
                    if (object.ReferenceEquals(original, originalValue) || object.ReferenceEquals(instance, instanceValue))
                        return false;

                    if (IsSerialized(field) && Different(originalValue, instanceValue))
                        return true;
                }
            }

            return false;
        }

        private bool RecursiveExpansion(InspectorField child)
        {
            if (this == child)
                return true;

            if (parent == null)
                return false;

            return parent.RecursiveExpansion(child);
        }

        private void ExpansionState()
        {
            if (parent != null && parent.RecursiveExpansion(this))
            {
                Expanded = false;
                return;
            }

            object[] values = GetValues();
            IExpandable expand = GetAttribute<IExpandable>();
            if (expand != null && !IsList && !IsDictionary)
                alwaysExpanded = expand.IsAlwaysExpanded(instances, values);

            CollectionAttribute collection = GetAttribute<CollectionAttribute>();
            if (collection != null && (IsList || IsDictionary))
                alwaysExpanded = collection.AlwaysExpanded;

            if (alwaysExpanded)
                Expanded = true;
            else
            {
                if (!Expandable)
                {
                    Expanded = false;
                }
                else
                {
                    string path = Path;
                    if (EditorPrefs.HasKey(path))
                        Expanded = EditorPrefs.GetBool(path);
                    else
                    {
                        if (expand != null)
                            Expanded = expand.IsExpanded(instances, values);
                        else
                            Expanded = false;
                    }
                }
            }
        }

        private Type GetDictionaryType(Type type)
        {
            Type parentType = type;
            while (parentType != null)
            {
                if (parentType.GetGenericArguments().Length == 2)
                    return parentType;

                parentType = parentType.BaseType;
            }

            return null;
        }
        #endregion

        #region Internal
        internal void ClearCache()
        {
            readOnlyCache.Clear();
            expandableCache.Clear();
            nameCache.Clear();

            for (int i = 0; i < fields.Count; i++)
                fields[i].ClearCache();
        }
        #endregion

        #region Public
        /// <summary>
        /// IComparable implementation
        /// </summary>
        public int CompareTo(object obj)
        {
            return CompareTo((InspectorField)obj);
        }

        /// <summary>
        /// IComparable implementation
        /// </summary>
        public int CompareTo(InspectorField field)
        {
            if (field == null)
                return 0;

            if (field.Index != -1)
                return index.CompareTo(field.index);

            if (AdvancedInspectorControl.Sorting == AdvancedInspectorControl.InspectorSorting.None)
            {
                if (field.Priority > Priority)
                    return -1;
                if (field.Priority < Priority)
                    return 1;

                return order.CompareTo(field.order);
            }
            else if (AdvancedInspectorControl.Sorting == AdvancedInspectorControl.InspectorSorting.Alpha)
            {
                if (field.Priority > Priority)
                    return -1;
                if (field.Priority < Priority)
                    return 1;

                return Name.CompareTo(field.Name);
            }
            else if (AdvancedInspectorControl.Sorting == AdvancedInspectorControl.InspectorSorting.AntiAlpha)
            {
                if (field.Priority > Priority)
                    return -1;
                if (field.Priority < Priority)
                    return 1;

                return -Name.CompareTo(field.Name);
            }
            else
                return 0;
        }

        /// <summary>
        /// Force a new attribute to be added a runtime.
        /// </summary>
        public void AddAttribute(Attribute attribute)
        {
            List<Attribute> added = attributes.ToList();
            added.Add(attribute);
            attributes = added.ToArray();
        }

        /// <summary>
        /// Get an attribute by type that was applied to the original item.
        /// </summary>
        public T GetAttribute<T>()
        {
            if (attributes == null)
                return default(T);

            for (int i = 0; i < attributes.Length; i++)
                if (typeof(T).IsAssignableFrom(attributes[i].GetType()))
                    return (T)(object)attributes[i];

            return default(T);
        }

        /// <summary>
        /// Get all the attributes by type that were applied to the original item.
        /// </summary>
        public T[] GetAttributes<T>()
        {
            if (attributes == null)
                return new T[0];

            return attributes.OfType<T>().ToArray();
        }

        /// <summary>
        /// Return true if the memberinfo of this field is sporting that specific attribute type.
        /// </summary>
        public bool HasAttribute<T>()
        {
            T t = GetAttribute<T>();

            return t != null;
        }

        /// <summary>
        /// Add this field to the watch list, which is displayed on the Watch window.
        /// </summary>
        public void Watch()
        {
            if (WatchWindow.Contains(this))
                WatchWindow.RemoveField(this);
            else
                WatchWindow.AddField(this);
        }

        /// <summary>
        /// Force the selection of the serialized instances of this field.
        /// </summary>
        public void Select()
        {
            Type type = serializedInstances[0].GetType();
            if (typeof(Component).IsAssignableFrom(type))
            {
                List<GameObject> gos = new List<GameObject>();
                for (int i = 0; i < serializedInstances.Length; i++)
                    gos.Add(((Component)serializedInstances[i]).gameObject);

                Selection.objects = gos.ToArray();
            }
            else
                Selection.objects = serializedInstances;
        }

        /// <summary>
        /// Attempt to save this field value in play mode the next time it is stopped.
        /// </summary>
        public void Save()
        {
            if (InspectorPersist.Contains(this))
                InspectorPersist.RemoveField(this);
            else
                InspectorPersist.AddField(this);
        }

        /// <summary>
        /// Apply this field modification to its prefab parent.
        /// </summary>
        public void Apply()
        {
            if (InspectorType != InspectorType.Field && InspectorType != InspectorType.Property)
                return;

            object current = GetValue();
            IList list = current as IList;
            IDictionary dict = current as IDictionary;

            RecordObjects("Apply to Prefab " + Name);

            if (list != null)
            {
                IList instance;
                if (Type.IsArray)
                    instance = Array.CreateInstance(BaseType, list.Count) as IList;
                else
                    instance = Activator.CreateInstance(Type) as IList;

                if (instance == null)
                    return;

                for (int i = 0; i < list.Count; i++)
                {
                    if (Type.IsArray)
                        instance[i] = list[i];
                    else
                        instance.Add(list[i]);
                }

                SetValue(Prefab, instance);
            }
            else if (dict != null)
            {
                IDictionary instance = Activator.CreateInstance(Type) as IDictionary;
                if (instance == null)
                    return;

                foreach (DictionaryEntry entry in dict)
                    instance.Add(entry.Key, entry.Value);

                SetValue(Prefab, instance);
            }
            else
            {
                foreach (InspectorField field in Fields)
                    field.Apply();

                Clipboard.Data = current;
                SetValueByPath(Prefab, Path, Clipboard.Data);
            }

            SerializedProperty sp = SerializedProperty;
            if (sp != null)
            {
                sp.prefabOverride = false;
                sp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Revert any modification of this field and retreived the original value of its prefab parent.
        /// </summary>
        public void Revert()
        {
            if (InspectorType != InspectorType.Field && InspectorType != InspectorType.Property)
                return;

            object original = GetValueByPath(Prefab, Path);
            IList list = original as IList;
            IDictionary dict = original as IDictionary;

            if (list != null)
            {
                IList instance;
                if (Type.IsArray)
                    instance = Array.CreateInstance(BaseType, list.Count) as IList;
                else
                    instance = Activator.CreateInstance(Type) as IList;

                if (instance == null)
                    return;

                for (int i = 0; i < list.Count; i++)
                {
                    if (Type.IsArray)
                        instance[i] = list[i];
                    else
                        instance.Add(list[i]);
                }

                SetValue(instance);
            }
            else if (dict != null)
            {
                IDictionary instance = Activator.CreateInstance(Type) as IDictionary;
                if (instance == null)
                    return;

                foreach (DictionaryEntry entry in dict)
                    instance.Add(entry.Key, entry.Value);

                SetValue(instance);
            }
            else
            {
                foreach (InspectorField field in Fields)
                    field.Revert();

                Clipboard.Data = original;
                SetValueByPath(SerializedInstances[0], Path, Clipboard.Data);
            }

            SerializedProperty sp = SerializedProperty;
            if (sp != null)
            {
                sp.prefabOverride = false;
                sp.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Record the object state for undo purpose.
        /// </summary>
        public void RecordObjects(string text)
        {
            if (!Undoable)
                return;

            /*GameObject[] gameObjects = GameObjects;
            if (gameObjects != null && gameObjects.Length > 0)
                Undo.RecordObjects(gameObjects, text);
            else*/

            Undo.RecordObjects(SerializedInstances, text);
            foreach (UnityEngine.Object o in SerializedInstances)
                EditorUtility.SetDirty(o);
        }

        /// <summary>
        /// Copy the current value of this field to the clipboard.
        /// </summary>
        public void Copy()
        {
            Clipboard.Data = GetValue();
        }

        /// <summary>
        /// Return true if the passed value can be copied over this field.
        /// </summary>
        public bool CanPaste(object value)
        {
            if (value is ICopiable)
                return ((ICopiable)value).Copiable(GetValue());

            return true;
        }

        /// <summary>
        /// Paste the clipboard data to this field.
        /// Careful, you should do a type test before.
        /// </summary>
        public void Paste()
        {
            object value;
            if (Clipboard.TryConvertion(Type, out value))
            {
                RecordObjects("Paste " + Name);

                for (int i = 0; i < Instances.Length; i++)
                {
                    if (value is IList || value is IDictionary)
                    {
                        SetValue(Instances[i], Clipboard.Copy(value, SerializedInstances[i]));
                    }
                    else if (value is ICopy)
                    {
                        SetValue(Instances[i], ((ICopy)value).Copy(Instances[i]));
                    }
                    else if (value is ICloneable)
                    {
                        SetValue(Instances[i], ((ICloneable)value).Clone());
                    }
                    else if (inspectorType == InspectorType.Serialized)
                    {
                        SetValue(Instances[i], value);
                    }
                    else if (Clipboard.Type == null)
                    {
                        if (!Type.IsValueType)
                            SetValue(Instances[i], null);
                        else
                            return;
                    }
                    else if (Clipboard.Type.Namespace == "System")
                    {
                        SetValue(Instances[i], value);
                    }
                    else
                    {
                        SetValue(Instances[i], Clipboard.Copy(value, SerializedInstances[i]));
                    }
                }
            }
        }

        /// <summary>
        /// The current value of this field.
        /// If not a field, returns null.
        /// If multi object, return null if all the value aren't the same.
        /// </summary>
        public object GetValue()
        {
            if (inspectorType == InspectorType.Unlinked)
            {
                if (internalValue.Length == 1)
                    return internalValue[0];
                else if (internalValue.Length > 1)
                    return internalValue;
                else
                    return null;
            }

            if (InspectorType == InspectorType.Serialized)
                return GetValue(new object());

            if (Instances == null || Instances.Length == 0)
                return null;

            object value = GetValue(Instances[0]);
            for (int i = 1; i < Instances.Length; i++)
            {
                object next = GetValue(Instances[i]);
                if ((value == null && next != null) || value != null && !value.Equals(next))
                    return null;
            }

            return value;
        }

        /// <summary>
        /// The current value of this field.
        /// If not a field, returns null.
        /// If multi object, return null if all the value aren't the same.
        /// </summary>
        public T GetValue<T>()
        {
            object value = GetValue();

            if (value == null)
                return default(T);

            return (T)value;
        }

        /// <summary>
        /// The value of this field, based on a specific object instance.
        /// </summary>
        public object GetValue(object instance)
        {
            if (Static)
                instance = null;
            else
            {
                if (instance == null)
                    return null;

                if ((InspectorType == InspectorType.Property || InspectorType == InspectorType.Field) &&
                    (index == -1 && key == null) &&
                    (info == null || !info.DeclaringType.IsAssignableFrom(instance.GetType())))
                    return null;

                if (InspectorType == InspectorType.Serialized && serializedProperty == null)
                    return null;
            }

            try
            {
                if (inspectorType == InspectorType.Property)
                {
                    PropertyInfo prop = info as PropertyInfo;
                    if (index == -1 && key == null)
                        return prop.GetValue(instance, new object[0]);
                    else if (index != -1)
                        return ((IList)instance)[index];
                    else if (key != null)
                        return ((IDictionary)instance)[key];
                }
                else if (inspectorType == InspectorType.Field)
                {
                    FieldInfo field = info as FieldInfo;
                    if (index == -1 && key == null)
                        return field.GetValue(instance);
                    else if (index != -1)
                        return ((IList)instance)[index];
                    else if (key != null)
                        return ((IDictionary)instance)[key];
                }
                else if (inspectorType == InspectorType.Serialized)
                {
                    switch (serializedProperty.propertyType)
                    {
                        case SerializedPropertyType.AnimationCurve:
                            return serializedProperty.animationCurveValue;
                        case SerializedPropertyType.ArraySize:
                            return serializedProperty.arraySize;
                        case SerializedPropertyType.Boolean:
                            return serializedProperty.boolValue;
                        case SerializedPropertyType.Bounds:
                            return serializedProperty.boundsValue;
                        case SerializedPropertyType.Character:
                            return (char)serializedProperty.intValue;
                        case SerializedPropertyType.Color:
                            return serializedProperty.colorValue;
                        case SerializedPropertyType.Enum:
                            return serializedProperty.enumValueIndex;
                        case SerializedPropertyType.Float:
                            return serializedProperty.floatValue;
                        case SerializedPropertyType.Generic:
                            return null;
                        case SerializedPropertyType.Gradient:
                            return typeof(SerializedProperty).GetProperty("gradientValue", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(serializedProperty, new object[0]);
                        case SerializedPropertyType.Integer:
                            return serializedProperty.intValue;
                        case SerializedPropertyType.LayerMask:
                            return serializedProperty.intValue;
                        case SerializedPropertyType.ObjectReference:
                            return serializedProperty.objectReferenceValue;
                        case SerializedPropertyType.Quaternion:
                            return serializedProperty.quaternionValue;
                        case SerializedPropertyType.Rect:
                            return serializedProperty.rectValue;
                        case SerializedPropertyType.String:
                            return serializedProperty.stringValue;
                        case SerializedPropertyType.Vector2:
                            return serializedProperty.vector2Value;
                        case SerializedPropertyType.Vector3:
                            return serializedProperty.vector3Value;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(info.Name + " return the exception: " + e.Message + " while trying to get the value with the stack: " + e.StackTrace);
            }

            return null;
        }

        /// <summary>
        /// The value of this field, based on a specific object instance.
        /// </summary>
        public T GetValue<T>(object instance)
        {
            object value = GetValue(instance);

            if (value == null || !typeof(T).IsAssignableFrom(value.GetType()))
                return default(T);

            return (T)value;
        }

        /// <summary>
        /// Get all instances value.
        /// </summary>
        public object[] GetValues()
        {
            if (inspectorType == InspectorType.Unlinked)
                return internalValue;

            object[] values = new object[Instances.Length];
            for (int i = 0; i < Instances.Length; i++)
                values[i] = GetValue(Instances[i]);

            return values;
        }

        /// <summary>
        /// Get all instances value.
        /// </summary>
        public T[] GetValues<T>()
        {
            T[] values;
            if (inspectorType == InspectorType.Unlinked)
                values = new T[1];
            else
                values = new T[Instances.Length];

            for (int i = 0; i < Instances.Length; i++)
            {
                object value = GetValue(Instances[i]);
                if (value == null || !typeof(T).IsAssignableFrom(value.GetType()))
                    values[i] = default(T);
                else
                    values[i] = (T)value;
            }

            return values;
        }

        /// <summary>
        /// Reset this field to its default value.
        /// Object = null
        /// Value Type = default(T)
        /// </summary>
        public void ResetToDefault()
        { 
            if (type.IsClass)
                SetValue(null);
            else if (type == typeof(string))
                SetValue("");
            else
                SetValue(Activator.CreateInstance(type));
        }

        /// <summary>
        /// Unity doesn't always properly init it's array/list if a serialization reload didn't occur.
        /// </summary>
        public void InitCollection()
        {
            if (!IsList || InspectorType == InspectorType.Serialized)
                return;

            CollectionAttribute collection = GetAttribute<CollectionAttribute>();
            ConstructorAttribute constructor = GetAttribute<ConstructorAttribute>();
            int size = -1;

            if (collection != null)
            {
                if (collection.EnumType != null)
                    size = Enum.GetValues(collection.EnumType).Length;
                else
                    size = collection.Size;
            }

            for (int i = 0; i < Instances.Length; i++)
            {
                object instance = Instances[i];

                IList value = GetValue(instance) as IList;

                if (value != null)
                {
                    if (size <= 0 || value.Count == size)
                        continue;

                    if (typeof(Array).IsAssignableFrom(Type))
                    {
                        Array array;
                        if (size > 0)
                        {
                            array = Array.CreateInstance(BaseType, size);
                            Array original = (Array)value;
                            if (original.Length < size)
                                Array.Copy(original, 0, array, 0, original.Length);
                            else
                                Array.Copy(original, 0, array, 0, size);
                        }
                        else
                            array = value as Array;

                        for (int x = 0; x < array.Length; x++)
                        {
                            if (array.GetValue(x) != null)
                                continue;

                            array.SetValue(CreateCollectionInstance(constructor, i, x), x);
                        }

                        SetValue(instance, array);
                    }
                    else
                    {
                        if (value.Count > size)
                        {
                            for (int x = value.Count - 1; x >= size; x--)
                                value.RemoveAt(x);
                        }
                        else if (value.Count < size)
                        {
                            for (int x = value.Count; x < size; x++)
                                value.Add(CreateCollectionInstance(constructor, i, x));
                        }
                    }

                    RefreshFields();
                }
                else
                {
                    if (typeof(Array).IsAssignableFrom(Type))
                    {
                        Array array;
                        if (size <= 0)
                            array = Array.CreateInstance(BaseType, 0);
                        else
                            array = Array.CreateInstance(BaseType, size);

                        for (int x = 0; x < size; x++)
                            array.SetValue(CreateCollectionInstance(constructor, i, x), x);

                        SetValue(instance, array);
                    }
                    else
                    {
                        IList list = (IList)Activator.CreateInstance(Type);
                        for (int x = 0; x < size; x++)
                            list.Add(CreateCollectionInstance(constructor, i, x));

                        SetValue(instance, list);
                    }

                    RefreshFields();
                }
            }
        }

        private object CreateCollectionInstance(ConstructorAttribute constructor, int instance, int index)
        {
            if (constructor != null && constructor.Delegates.Count > index)
            {
                try
                {
                    return constructor.Delegates[index].DynamicInvoke();
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        e = ((TargetInvocationException)e).InnerException;

                    Debug.LogError(string.Format("Invoking a method from a collection constructor failed. The exception was \"{0}\", and the callstack \"{1}\"", e.Message, e.StackTrace));
                    return Clipboard.CreateInstance(BaseType, SerializedInstances[instance] as MonoBehaviour);
                }
            }
            else
                return Clipboard.CreateInstance(BaseType, SerializedInstances[instance] as MonoBehaviour);
        }

        /// <summary>
        /// Set the value of this field.
        /// Support undo. When calling this, assume undo will be "Set Value MyField"
        /// </summary>
        public void SetValue(object value)
        {
            RecordObjects("Set Value " + Name);

            if (inspectorType == InspectorType.Unlinked)
                internalValue = new object[] { value };
            else if (InspectorType == InspectorType.Serialized)
                SetValue(new object(), value);
            else if (inspectorType == InspectorType.Property || inspectorType == InspectorType.Field)
                foreach (object instance in Instances)
                    SetValue(instance, value);
        }

        /// <summary>
        /// Set the value of this field, based on a specific object instance.
        /// Does not support undo, should be recorded before calling.
        /// </summary>
        public void SetValue(object instance, object value)
        {
            if (Static)
                instance = null;
            else
            {
                if (instance == null)
                    return;

                if ((InspectorType == InspectorType.Property || InspectorType == InspectorType.Field) &&
                    (index == -1 && key == null) &&
                    (info == null || !info.DeclaringType.IsAssignableFrom(instance.GetType())))
                    return;

                if (InspectorType == InspectorType.Serialized && serializedProperty == null)
                    return;
            }

            if (value != null && !Type.IsAssignableFrom(value.GetType()))
                value = Convert.ChangeType(value, Type);

            object old = GetValue(instance);
            if (ReadOnly || value == old || (value != null && value.Equals(old)))
                return;

            changed = true;

            try
            {
                if (inspectorType == InspectorType.Property)
                {
                    if (index == -1 && key == null)
                        ((PropertyInfo)info).SetValue(instance, value, new object[0]);
                    else if (index != -1)
                        ((IList)instance)[index] = value;
                    else if (key != null)
                        ((IDictionary)instance)[key] = value;
                }
                else if (inspectorType == InspectorType.Field)
                {
                    if (index == -1 && key == null)
                        ((FieldInfo)info).SetValue(instance, value);
                    else if (index != -1)
                        ((IList)instance)[index] = value;
                    else if (key != null)
                        ((IDictionary)instance)[key] = value;
                }
                else if (inspectorType == InspectorType.Serialized)
                {
                    if (!serializedProperty.editable)
                        return;

                    switch (serializedProperty.propertyType)
                    {
                        case SerializedPropertyType.AnimationCurve:
                            serializedProperty.animationCurveValue = (AnimationCurve)value;
                            break;
                        case SerializedPropertyType.ArraySize:
                            serializedProperty.arraySize = (int)value;
                            break;
                        case SerializedPropertyType.Boolean:
                            serializedProperty.boolValue = (bool)value;
                            break;
                        case SerializedPropertyType.Bounds:
                            serializedProperty.boundsValue = (Bounds)value;
                            break;
                        case SerializedPropertyType.Character:
                            serializedProperty.intValue = (int)value;
                            break;
                        case SerializedPropertyType.Color:
                            serializedProperty.colorValue = (Color)value;
                            break;
                        case SerializedPropertyType.Enum:
                            serializedProperty.enumValueIndex = (int)value;
                            break;
                        case SerializedPropertyType.Float:
                            serializedProperty.floatValue = (float)value;
                            break;
                        case SerializedPropertyType.Generic:
                            // Huh?
                            break;
                        case SerializedPropertyType.Gradient:
                            typeof(SerializedProperty).GetProperty("gradientValue", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(serializedProperty, value, new object[0]);
                            break;
                        case SerializedPropertyType.Integer:
                            serializedProperty.intValue = (int)value;
                            break;
                        case SerializedPropertyType.LayerMask:
                            serializedProperty.intValue = (int)value;
                            break;
                        case SerializedPropertyType.ObjectReference:
                            serializedProperty.objectReferenceValue = (UnityEngine.Object)value;
                            break;
                        case SerializedPropertyType.Quaternion:
                            serializedProperty.quaternionValue = (Quaternion)value;
                            break;
                        case SerializedPropertyType.Rect:
                            serializedProperty.rectValue = (Rect)value;
                            break;
                        case SerializedPropertyType.String:
                            serializedProperty.stringValue = (string)value;
                            break;
                        case SerializedPropertyType.Vector2:
                            serializedProperty.vector2Value = (Vector2)value;
                            break;
                        case SerializedPropertyType.Vector3:
                            serializedProperty.vector3Value = (Vector3)value;
                            break;
                    }

                    serializedProperty.serializedObject.ApplyModifiedProperties();
                }

                SetDirty();

                RefreshFields();
            }
            catch (Exception e)
            {
                //Happens in some rare case, such as Texture.Width which has a Setter but never works.
                Debug.LogWarning(info.Name + " return the exception: " + e.Message + " and will be read only. The stack trace was: " + e.StackTrace);
                AddAttribute(new ReadOnlyAttribute());
            }

            if (parent != null)
            {
                if (index != -1 || key != null || (parent.Type != null && parent.Type.IsValueType))
                {
                    int i = Array.IndexOf(Instances, instance);
                    if (i == -1)
                        return;

                    parent.SetValue(parent.Instances[i], instance);
                }
            }
        }

        /// <summary>
        /// Flags the related serialized instance as dirty.
        /// </summary>
        public void SetDirty()
        {
            changed = true;
            foreach (UnityEngine.Object serializable in SerializedInstances)
                if (serializable != null)
                    EditorUtility.SetDirty(serializable);
        }

        /// <summary>
        /// Reparse the internal sub-fields of this field.
        /// Useful when a list or an object changed.
        /// </summary>
        public void RefreshFields()
        {
            RefreshFields(false);        
        }

        internal void RefreshFields(bool forced)
        {
            if (!forced)
            {
                DisplayAsParentAttribute displayAsParent = GetAttribute<DisplayAsParentAttribute>();
                if ((!Expanded || AdvancedInspectorControl.Suspended ||
                    InspectorType == InspectorType.Group || InspectorType == InspectorType.Unlinked ||
                    InspectorType == InspectorType.Toolbar) && displayAsParent == null)
                    return;
            }

            fields.Clear();

            if (inspectorType == InspectorType.Serialized)
            {
                if (IsList)
                {
                    for (int i = 0; i < serializedProperty.arraySize; i++)
                        fields.Add(new InspectorField(this, i, serializedProperty.GetArrayElementAtIndex(i)));
                }

                return;
            }

            if (IsList)
            {
                List<IList> lists = new List<IList>();

                foreach (object instance in Instances)
                    lists.Add((IList)GetValue(instance));

                fields = GetIndexedProperties(this, lists);
            }
            else if (IsDictionary)
            {
                List<IDictionary> dicts = new List<IDictionary>();

                foreach (object instance in Instances)
                    dicts.Add((IDictionary)GetValue(instance));

                fields = GetKeyedProperties(this, dicts);
            }
            else
            {
                object[] values = new object[Instances.Length];
                for (int i = 0; i < Instances.Length; i++)
                    values[i] = GetValue(Instances[i]);

                AdvancedInspectorAttribute advanced = null;
                object[] attributes = Type.GetCustomAttributes(typeof(AdvancedInspectorAttribute), true);
                if (attributes.Length > 0)
                    advanced = attributes[0] as AdvancedInspectorAttribute;

                if (advanced == null)
                {
                    if (Expandable || Expanded)
                        fields = GetEntries(this, values, true, bypass);
                    else
                        fields = GetEntries(this, values, false, bypass);
                }
                else
                    fields = GetEntries(this, values, advanced.InspectDefaultItems, bypass);
            }

            foreach (InspectorField field in Fields)
            {
                //field.RefreshFields();
                field.Depth = depth + 1;
            }

            SortFields();
        }

        /// <summary>
        /// Sort all children fields.
        /// </summary>
        public void SortFields()
        {
            foreach (InspectorField field in fields)
                field.SortFields();

            fields.Sort();
        }
        #endregion

        #region Static
        private static bool IsSerialized(FieldInfo info)
        {
            if (info.GetCustomAttributes(typeof(HideInInspector), true).Length != 0)
                return false;

            if (info.Attributes.Has(FieldAttributes.Public) && info.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                return true;
            else if (info.Attributes.Has(FieldAttributes.Private) && info.GetCustomAttributes(typeof(SerializeField), true).Length != 0)
                return true;
            else if (!info.Attributes.Has(FieldAttributes.Public) && !info.Attributes.Has(FieldAttributes.Private) && info.GetCustomAttributes(typeof(SerializeField), true).Length != 0)
                return true;

            return false;
        }

        /// <summary>
        /// Get a MemberInfo from a serializable path.
        /// The "Path" property from a InspectorField is a valid path.
        /// </summary>
        public static MemberInfo GetInfoByPath(UnityEngine.Object instance, string path)
        {
            List<string> paths = path.Split('/').ToList();
            return GetInfoByPath(instance, null, paths);
        }

        private static MemberInfo GetInfoByPath(object instance, MemberInfo memberInfo, List<string> path)
        {
            if (instance == null || path.Count == 0)
                return memberInfo;

            string[] infos = path[0].Split('-');
            if (infos.Length < 2)
                return null;

            Type type = instance.GetType();
            if (infos[0] == "Field")
            {
                string[] indexes = infos[1].Split('[');
                FieldInfo info = type.GetField(indexes[0]);
                if (info == null)
                    return null;

                path.RemoveAt(0);

                if (indexes.Length > 1)
                {
                    int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                    object collection = info.GetValue(instance);

                    if (collection is IList)
                        return GetInfoByPath(((IList)collection)[index], info, path);
                    else if (collection is IDictionary)
                    {
                        foreach (object key in ((IDictionary)collection).Keys)
                            if (key.GetHashCode() == index)
                                return GetInfoByPath(((IDictionary)collection)[key], info, path);
                    }
                }
                else
                    return GetInfoByPath(info.GetValue(instance), info, path);
            }
            else if (infos[0] == "Property")
            {
                string[] indexes = infos[1].Split('[');
                PropertyInfo info = type.GetProperty(indexes[0]);

                if (info == null)
                    return null;

                path.RemoveAt(0);

                if (indexes.Length > 1)
                {
                    int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                    object collection = info.GetValue(instance, new object[0]);

                    if (collection is IList)
                        return GetInfoByPath(((IList)collection)[index], info, path);
                    else if (collection is IDictionary)
                    {
                        foreach (object key in ((IDictionary)collection).Keys)
                            if (key.GetHashCode() == index)
                                return GetInfoByPath(((IDictionary)collection)[key], info, path);
                    }
                }
                else
                    return GetInfoByPath(info.GetValue(instance, new object[0]), info, path);
            }

            return null;
        }

        /// <summary>
        /// Get a value from a serializable path.
        /// The "Path" property from a InspectorField is a valid path.
        /// </summary>
        public static object GetValueByPath(UnityEngine.Object instance, string path)
        {
            List<string> paths = path.Split('/').ToList();
            return GetValueByPath(instance, paths);
        }

        private static object GetValueByPath(object instance, List<string> path)
        {
            if (instance == null || path.Count == 0)
                return instance;

            string[] infos = path[0].Split('-');
            if (infos.Length < 2)
                return null;

            Type type = instance.GetType();
            if (infos[0] == "Field")
            {
                string[] indexes = infos[1].Split('[');
                FieldInfo info = type.GetField(indexes[0]);
                if (info == null)
                    return null;

                path.RemoveAt(0);

                if (indexes.Length > 1)
                {
                    int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                    object collection = info.GetValue(instance);

                    if (collection is IList)
                        return GetValueByPath(((IList)collection)[index], path);
                    else if (collection is IDictionary)
                    {
                        foreach (object key in ((IDictionary)collection).Keys)
                            if (key.GetHashCode() == index)
                                return GetValueByPath(((IDictionary)collection)[key], path);
                    }
                }
                else
                    return GetValueByPath(info.GetValue(instance), path);
            }
            else if (infos[0] == "Property")
            {
                string[] indexes = infos[1].Split('[');
                PropertyInfo info = type.GetProperty(indexes[0]);

                if (info == null)
                    return null;

                path.RemoveAt(0);

                if (indexes.Length > 1)
                {
                    int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                    object collection = info.GetValue(instance, new object[0]);

                    if (collection is IList)
                        return GetValueByPath(((IList)collection)[index], path);
                    else if (collection is IDictionary)
                    {
                        foreach (object key in ((IDictionary)collection).Keys)
                            if (key.GetHashCode() == index)
                                return GetValueByPath(((IDictionary)collection)[key], path);
                    }
                }
                else
                    return GetValueByPath(info.GetValue(instance, new object[0]), path);
            }

            return null;
        }

        /// <summary>
        /// Set a value by a serializable path.
        /// The "Path" property from a InspectorField is a valid path.
        /// </summary>
        public static void SetValueByPath(UnityEngine.Object instance, string path, object value)
        {
            List<string> paths = path.Split('/').ToList();
            SetValueByPath(instance, paths, value);
        }

        private static void SetValueByPath(object instance, List<string> path, object value)
        {
            if (instance == null || path.Count == 0)
                return;

            string[] infos = path[0].Split('-');
            if (infos.Length < 2)
                return;

            Type type = instance.GetType();
            if (infos[0] == "Field")
            {
                string[] indexes = infos[1].Split('[');
                FieldInfo info = type.GetField(indexes[0]);
                if (info == null)
                    return;

                path.RemoveAt(0);

                if (path.Count == 0)
                {
                    if (indexes.Length > 1)
                    {
                        int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                        object collection = info.GetValue(instance);

                        if (collection is IList)
                        {
                            if (((IList)collection).Count > index)
                                ((IList)collection)[index] = value;
                        }
                        else if (collection is IDictionary)
                        {
                            foreach (object key in ((IDictionary)collection).Keys)
                                if (key.GetHashCode() == index)
                                    ((IDictionary)collection)[key] = value;
                        }
                    }
                    else
                        info.SetValue(instance, value);
                }
                else
                    SetValueByPath(info.GetValue(instance), path, value);
            }
            else if (infos[0] == "Property")
            {
                string[] indexes = infos[1].Split('[');
                PropertyInfo info = type.GetProperty(indexes[0]);
                if (info == null)
                    return;

                path.RemoveAt(0);

                if (path.Count == 0)
                {
                    if (indexes.Length > 1)
                    {
                        int index = int.Parse(indexes[1].Substring(0, indexes[1].Length - 1));

                        object collection = info.GetValue(instance, new object[0]);

                        if (collection is IList)
                        {
                            if (((IList)collection).Count > index)
                                ((IList)collection)[index] = value;
                        }
                        else if (collection is IDictionary)
                        {
                            foreach (object key in ((IDictionary)collection).Keys)
                                if (key.GetHashCode() == index)
                                    ((IDictionary)collection)[key] = value;
                        }
                    }
                    else
                        info.SetValue(instance, value, new object[0]);
                }
                else
                    SetValueByPath(info.GetValue(instance, new object[0]), path, value);
            }
            else
                return;
        }

        /// <summary>
        /// Return a list of indexed field. Work with List and Array.
        /// </summary>
        public static List<InspectorField> GetIndexedProperties(InspectorField parent, List<IList> lists)
        {
            List<InspectorField> fields = new List<InspectorField>();
            if (lists == null || lists.Count == 0 || parent.BaseType == null)
                return fields;

            foreach (IList list in lists)
                if (list == null)
                    return new List<InspectorField>();

            CollectionAttribute collection = parent.GetAttribute<CollectionAttribute>();
            int min = collection == null ? InspectorPreferences.LargeCollection : collection.MaxDisplayedItems;
            if (collection == null || collection.Display == CollectionDisplay.List)
            {
                foreach (IList list in lists)
                    if (list.Count < min)
                        min = list.Count;

                for (int i = 0; i < min; i++)
                {
                    InspectorField field = new InspectorField(parent, lists.ToArray(), i);
                    fields.Add(field);
                }
            }
            else
            {
                InspectorField field = new InspectorField(parent, lists.ToArray(), 0);

                FieldEditor editor = null;
                if (FieldEditor.FieldEditorByTypes.ContainsKey(parent.BaseType))
                    editor = FieldEditor.FieldEditorByTypes[parent.BaseType];

                if (editor != null && !editor.IsExpandable(field))
                    fields.Add(field);
                else
                    fields.Add(new InspectorField(parent, lists.ToArray(), 0, new DisplayAsParentAttribute()));
            }

            return fields;
        }

        /// <summary>
        /// Return a list of keyed field. Work with IDictionary.
        /// </summary>
        public static List<InspectorField> GetKeyedProperties(InspectorField parent, List<IDictionary> dicts)
        {
            List<InspectorField> fields = new List<InspectorField>();

            if (dicts == null || dicts.Count != 1 || dicts[0] == null || parent.BaseType == null)
                return fields;

            CollectionAttribute collection = parent.GetAttribute<CollectionAttribute>();
            if (collection == null || collection.Display == CollectionDisplay.List)
            {
                foreach (DictionaryEntry entry in dicts[0])
                {
                    InspectorField field = new InspectorField(parent, dicts.ToArray(), entry.Key);
                    fields.Add(field);
                }
            }
            else
            {
                InspectorField field = new InspectorField(parent, dicts.ToArray(), new object());

                FieldEditor editor = null;
                if (FieldEditor.FieldEditorByTypes.ContainsKey(parent.BaseType))
                    editor = FieldEditor.FieldEditorByTypes[parent.BaseType];

                if (editor != null && !editor.IsExpandable(field))
                    fields.Add(field);
                else
                    fields.Add(new InspectorField(parent, dicts.ToArray(), new object(), new DisplayAsParentAttribute()));
            }

            return fields;
        }

        /// <summary>
        /// Get fields from an object. 
        /// </summary>
        public static List<InspectorField> GetEntries(InspectorField parent, object[] instances)
        {
            return GetEntries(parent, instances, false, false);
        }

        /// <summary>
        /// Get fields from an object. 
        /// </summary>
        public static List<InspectorField> GetEntries(InspectorField parent, object[] instances, bool inspectDefaultItems)
        {
            return GetEntries(parent, instances, inspectDefaultItems, false);
        }

        /// <summary>
        /// Get fields from an object. Bypass ignore Inspect and Expandable attributes.
        /// </summary>
        public static List<InspectorField> GetEntries(InspectorField parent, object[] instances, bool inspectDefaultItems, bool bypass)
        {
            List<InspectorField> fields = new List<InspectorField>();
            if (instances == null || instances.Length == 0 || instances[0] == null)
                return fields;

            Type type = instances[0].GetType();

            FieldEditor fieldEditor;
            if (FieldEditor.FieldEditorByTypes.TryGetValue(type, out fieldEditor))
            {
                if (!FieldEditor.IsDefaultFieldEditor(fieldEditor) && !fieldEditor.IsExpandable(parent))
                    return fields;

                List<InspectorField> returned = fieldEditor.GetFields(parent, instances, inspectDefaultItems, bypass);
                if (returned != null)
                    return returned;
            }

            if (parent != null && fieldEditor != null && !fieldEditor.IsExpandable(parent))
                return fields;

            bool serializable = true;
            UnityEngine.Object[] serializedInstances = new UnityEngine.Object[instances.Length];
            for (int i = 0; i < instances.Length; i++)
            {
                if (!typeof(UnityEngine.Object).IsAssignableFrom(instances[i].GetType()))
                {
                    serializable = false;
                    break;
                }

                serializedInstances[i] = instances[i] as UnityEngine.Object;
            }

            UnityEngine.Object prefab = null;
            if (serializable && instances.Length == 1)
                prefab = PrefabUtility.GetPrefabParent((UnityEngine.Object)instances[0]);

            bool similar = true;
            object previous = instances[0];
            foreach (object instance in instances)
            {
                if (instance == null)
                    return fields;

                if (instance.GetType() != previous.GetType())
                {
                    similar = false;
                    break;
                }
            }

            if (!similar)
                return fields;
            
            int order = 0;

            InspectorEditor editor = null;
            InspectorEditor.InspectorEditorByTypes.TryGetValue(type, out editor);
            MethodInfo refresh = null;

            if (!ReferenceEquals(editor, null))
                refresh = editor.GetType().GetMethod("RefreshFields", BindingFlags.NonPublic | BindingFlags.Instance);

            if (!ReferenceEquals(editor, null) && refresh != null && refresh.DeclaringType == editor.GetType())
            {
                editor.Parent = parent;
                editor.Instances = instances;
                fields = editor.Fields;
            }
            else
            {
                object[] advancedAttributes = type.GetCustomAttributes(typeof(AdvancedInspectorAttribute), true);
                AdvancedInspectorAttribute advancedAttribute = null;
                if (advancedAttributes.Length > 0)
                    advancedAttribute = advancedAttributes[0] as AdvancedInspectorAttribute;

                if (serializable)
                {
                    if (advancedAttribute == null || advancedAttribute.ShowScript)
                    {
                        SerializedObject serializedObject = new SerializedObject(serializedInstances);
                        SerializedProperty serializedProperty = serializedObject.FindProperty("m_Script");
                        InspectorField script = new InspectorField(parent, serializedInstances, instances, serializedProperty);
                        script.order = order++;
                        fields.Add(script);
                    }
                }
                else if (parent != null)
                {
                    serializedInstances = parent.SerializedInstances;
                }

                #region Members
                foreach (MemberInfo info in Clipboard.GetMembers(type))
                {
                    if (info is PropertyInfo)
                    {
                        PropertyInfo property = info as PropertyInfo;
                        if (!property.CanRead || property.GetIndexParameters().Length != 0)
                            continue;
                    }

                    if (info is MethodInfo)
                    {
                        MethodInfo method = info as MethodInfo;
                        if (method.GetParameters().Length != 0 || method.IsConstructor || method.IsFinal || method.IsGenericMethod)
                            continue;
                    }

                    object[] attributes = info.GetCustomAttributes(true);

                    IVisibility visibility = null;
                    foreach (object o in attributes)
                    {
                        if (!typeof(IVisibility).IsAssignableFrom(o.GetType()))
                            continue;

                        visibility = o as IVisibility;
                        if (visibility != null)
                            break;
                    }

                    if (visibility == null && bypass && (info is FieldInfo || info is PropertyInfo))
                        visibility = new InspectAttribute();

                    if (visibility == null && info is FieldInfo && inspectDefaultItems)
                    {
                        FieldInfo fieldInfo = info as FieldInfo;
                        if (IsSerialized(fieldInfo))
                            visibility = new InspectAttribute();
                    }

                    if (visibility == null)
                        continue;

                    InspectorField field = null;
                    if (info is PropertyInfo)
                    {
                        PropertyInfo propertyInfo = info as PropertyInfo;
                        field = new InspectorField(parent, serializedInstances, instances, prefab, propertyInfo, bypass);
                        field.order = order++;
                    }
                    else if (info is FieldInfo)
                    {
                        FieldInfo fieldInfo = info as FieldInfo;
                        field = new InspectorField(parent, serializedInstances, instances, prefab, fieldInfo, bypass);
                        field.order = order++;
                    }
                    else if (info is MethodInfo)
                    {
                        MethodInfo methodInfo = info as MethodInfo;
                        field = new InspectorField(parent, serializedInstances, instances, methodInfo);
                        field.order = order++;
                    }

                    if (field == null)
                        continue;

                    if ((field.Group == null || string.IsNullOrEmpty(field.Group.Name)) &&
                        (field.Toolbar == null || string.IsNullOrEmpty(field.Toolbar.Name)))
                        fields.Add(field);
                    else if (field.Group != null && !string.IsNullOrEmpty(field.Group.Name))
                    {
                        InspectorField group = null;

                        foreach (InspectorField existing in fields)
                        {
                            if (existing.InspectorType == InspectorType.Group && existing.Name == field.Group.Name)
                            {
                                existing.Fields.Add(field);
                                if (!field.Group.Expandable && existing.Group.Expandable)
                                    existing.Group.Expandable = false;

                                group = existing;
                                break;
                            }
                        }

                        if (group == null)
                        {
                            group = new InspectorField(field.Group);
                            group.Fields.Add(field);
                            fields.Add(group);
                        }

                        if (group.Group != null)
                        {
                            if (!string.IsNullOrEmpty(field.Group.Style))
                                group.Group.Style = field.Group.Style;

                            if (field.Group.Priority != 0)
                                group.Group.Priority = field.Group.Priority;

                            if (!string.IsNullOrEmpty(field.Group.Description))
                                group.Group.Description = field.Group.Description;
                        }
                    }
                    else if (field.Toolbar != null && !string.IsNullOrEmpty(field.Toolbar.Name))
                    {
                        InspectorField toolbar = null;
                        foreach (InspectorField existing in fields)
                        {
                            if (existing.InspectorType == InspectorType.Toolbar && existing.Name == field.Toolbar.Name)
                            {
                                existing.Fields.Add(field);
                                toolbar = existing;
                                break;
                            }
                        }

                        if (toolbar == null)
                        {
                            toolbar = new InspectorField(field.Toolbar);
                            toolbar.Fields.Add(field);
                            fields.Add(toolbar);
                        }

                        if (toolbar.Toolbar != null)
                        {
                            if (!string.IsNullOrEmpty(field.Toolbar.Style))
                                toolbar.Toolbar.Style = field.Toolbar.Style;

                            if (field.Toolbar.Label)
                                toolbar.Toolbar.Label = true;

                            if (field.Toolbar.Priority != 0)
                                toolbar.Toolbar.Priority = field.Toolbar.Priority;
                        }
                    }
                }
                #endregion
            }

            fields.Sort();
            foreach (InspectorField field in fields)
                field.SortFields();

            return fields;
        }
        #endregion
    }

    /// <summary>
    /// Type of InspectorField.
    /// </summary>
    public enum InspectorType
    {
        /// <summary>
        /// Empty unset field
        /// </summary>
        None,
        /// <summary>
        /// Field containing a FieldInfo
        /// </summary>
        Field,
        /// <summary>
        /// Field containing a PropertyInfo
        /// </summary>
        Property,
        /// <summary>
        /// Field containing a MethodInfo
        /// </summary>
        Method,
        /// <summary>
        /// The InspectorField is only a wrapper around a SerializedProperty object.
        /// To use in case of hidden property in the Unity API.
        /// </summary>
        Serialized,
        /// <summary>
        /// Field that is a group of other field.
        /// </summary>
        Group,
        /// <summary>
        /// Field that is drawn as a toolbar. (horizontal group)
        /// </summary>
        Toolbar,
        /// <summary>
        /// Unlinked field just stores a value internally and is not bound to an object.
        /// </summary>
        Unlinked,
        /// <summary>
        /// The top field, the reference towards the original script.
        /// </summary>
        Script
    }
}