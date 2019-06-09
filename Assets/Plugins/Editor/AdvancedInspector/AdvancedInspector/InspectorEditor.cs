using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AdvancedInspector
{
    /// <summary>
    /// Base class of BehaviourEditor, ScriptableEditor and ExternalEditor.
    /// </summary>
    public abstract class InspectorEditor : Editor
    {
        private static FieldInfo inspectedType;

        private static Texture pickerCursor;

        internal static Texture PickerCursor
        {
            get
            {
                if (pickerCursor == null)
                    pickerCursor = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "PickerCursor.png");

                return pickerCursor;
            }
        }

        private static GUIContent iconRemove = null;

        public static GUIContent IconRemove
        {
            get
            {
                if (iconRemove == null)
                    iconRemove = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove command buffer");

                return iconRemove;
            }
        }

        private static GUIStyle invisibleButton = null;

        public static GUIStyle InvisibleButton
        {
            get
            {
                if (invisibleButton == null)
                    invisibleButton = "InvisibleButton";

                return invisibleButton;
            }
        }

        private static Dictionary<Type, InspectorEditor> inspectorEditors = null;
        private static Dictionary<Type, InspectorEditor> inspectorEditorByTypes = null;

        internal static Dictionary<Type, InspectorEditor> InspectorEditorByTypes
        {
            get { return inspectorEditorByTypes; }
        }

        private static InspectorEditor current;

        /// <summary>
        /// The currently refreshing InspectorEditor
        /// </summary>
        public static InspectorEditor Current
        {
            get { return current; }
        }

        private List<Editor> extraEditors = new List<Editor>();

        /// <summary>
        /// Preview is handled internally and is turned on and off using IPreview interface.
        /// </summary>
        public override bool HasPreviewGUI()
        {
            foreach (object instance in Instances)
            {
                IPreview preview = instance as IPreview;
                if (preview == null || preview.Preview == null || preview.Preview.Length == 0)
                    continue;

                return true; 
            }

            return false;
        }

        private static bool picking = false;

        /// <summary>
        /// Is the Inspector currently in picking mode?
        /// </summary>
        public static bool Picking
        {
            get { return picking; }
        }

        private static Tool tool = Tool.None;
        //private static Type pickedType;
        private static object pickedTag;
        private static Action<GameObject, object> picked;

        private bool advanced = false;

        /// <summary>
        /// Return true if this editor is using advanced features.
        /// </summary>
        public bool Advanced
        {
            get { return advanced; }
        }

        private object[] instances = new object[0];

        /// <summary>
        /// We use our own targets list because we can draw object not deriving from UnityEngine.Object.
        /// </summary>
        public object[] Instances
        {
            get { return instances; }
            set
            {
                if (instances != value)
                {
                    instances = value;
                    fields.Clear();
                    RefreshFields();
                    foreach (InspectorField field in fields)
                        field.Depth = 0;
                }
            }
        }

        [SerializeField]
        private InspectorField parent;

        /// <summary>
        /// In case of value type, we need to know the parent.
        /// </summary>
        public InspectorField Parent
        {
            get { return parent; }
            internal set { parent = value; }
        }

        /// <summary>
        /// List of fields held by this inspector.
        /// </summary>
        [SerializeField]
        private List<InspectorField> fields = new List<InspectorField>();

        /// <summary>
        /// List of fields held by this inspector.
        /// </summary>
        public List<InspectorField> Fields
        {
            get { return fields; }
        }

        /// <summary>
        /// Has any changed occured in the last redraw.
        /// </summary>
        public bool Changed
        {
            get
            {
                foreach (InspectorField field in fields)
                    if (field.Changed)
                        return true;

                return false;
            }
        }

        /// <summary>
        /// Override this if you implement your own inspector and doesn't want it to be expandable.
        /// </summary>
        public virtual bool Expandable
        {
            get { return true; }
            set { }
        }

        /// <summary>
        /// When a value changed internally, this forces the fields to be refreshed.
        /// </summary>
        private bool rebuild = false;

        /// <summary>
        /// The type of the tab enum, if any.
        /// </summary>
        internal Type tab = null;

        /// <summary>
        /// The last selected tab between refresh.
        /// </summary>
        internal Enum selectedTab = null;

        /// <summary>
        /// Unity's OnEnable.
        /// </summary>
        protected virtual void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
            FieldEditor.Gather();
            Gather();

            AdvancedInspectorAttribute.OnForceRefresh += DataChanged;
            AdvancedInspectorControl.SortingChanged += Sort;

            if (targets == null || targets.Length == 0)
                return;

            Instances = targets;
            if (Instances[0] == null)
                return;

            advanced = InspectorPreferences.InspectDefaultItems;
            Type type = Instances[0].GetType();

            if (!advanced)
            {
                if (TestForDefaultInspector)
                {
                    object[] attributes = type.GetCustomAttributes(typeof(AdvancedInspectorAttribute), true);
                    if (attributes.Length != 0)
                        advanced = true;
                }
                else
                    advanced = true;
            }
            else if (type != null && type.Namespace != null)
            {
                if (type.Namespace.Contains("UnityEngine") && !InspectorEditorByTypes.ContainsKey(type))
                    advanced = false;
            }

            if (advanced)
            {
                foreach (object instance in Instances)
                    if (instance is IDataChanged)
                        ((IDataChanged)instance).OnDataChanged += DataChanged;

                InspectorField.OnDataChanged += DataChanged;
            }

            Repaint();
        }

        /// <summary>
        /// Unity's OnDisable
        /// </summary>
        protected virtual void OnDisable()
        {
            AdvancedInspectorAttribute.OnForceRefresh -= DataChanged;
            AdvancedInspectorControl.SortingChanged -= Sort;

            foreach (object instance in Instances)
                if (instance is IDataChanged)
                    ((IDataChanged)instance).OnDataChanged -= DataChanged;

            InspectorField.OnDataChanged -= DataChanged;

            foreach (InspectorField field in fields)
                field.Dispose();

            ClearExtraEditors();
        }

        /// <summary>
        /// Start the Scene View picking tool.
        /// Type can define the component we seek on a GameObject.
        /// </summary>
        public static void StartPicking(Action<GameObject, object> onPicked, object tag)
        {
            picking = true;
            pickedTag = tag;
            picked = onPicked;
            tool = Tools.current;
            Tools.current = Tool.None;
            Cursor.SetCursor(PickerCursor as Texture2D, new Vector2(12, 12), CursorMode.Auto);
            HandleUtility.Repaint();
        }

        /// <summary>
        /// Stop the picking tool before it has resolved.
        /// </summary>
        public static void StopPicking()
        {
            picking = false;
            picked.Invoke(null, null);
            Tools.current = tool;
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        /// <summary>
        /// Preview Draw
        /// </summary>
        public override void OnPreviewGUI(Rect region, GUIStyle background)
        {
            List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
            foreach (object instance in Instances)
            {
                IPreview preview = instance as IPreview;
                if (preview == null || preview.Preview == null || preview.Preview.Length == 0)
                    continue;

                foreach (UnityEngine.Object obj in preview.Preview)
                    if (obj != null)
                        objects.Add(obj);
            }

            InspectorPreview.Targets = objects.ToArray();
            InspectorPreview.OnPreviewGUI(region, background);
        }

        /// <summary>
        /// Override this method if you want to draw on the scene view.
        /// Don't forget to call the base.
        /// </summary>
        protected virtual void OnSceneGUI()
        {
            if (!picking)
                return;

            HandleUtility.Repaint();
            Tools.current = Tool.None;
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            Rect scene = SceneView.currentDrawingSceneView.position;
            EditorGUIUtility.AddCursorRect(new Rect(0, 0, scene.width, scene.height), MouseCursor.CustomCursor);

            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                StopPicking();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDown)
            {
                GameObject go = HandleUtility.PickGameObject(Event.current.mousePosition, false);
                if (go)
                {
                    picked.Invoke(go, pickedTag);

                    StopPicking();

                    Event.current.Use();
                }
            }
        }

        /// <summary>
        /// Preview Settings
        /// </summary>
        public override void OnPreviewSettings()
        {
            InspectorPreview.OnPreviewSettings();
        }

        /// <summary>
        /// Call a refresh of this editor, assuming the layout needs to be rebuilt.
        /// Expensive operation.
        /// </summary>
        protected virtual void DataChanged()
        {
            DataChanged(true);
        }

        /// <summary>
        /// Data Changed, repaint.
        /// </summary>
        protected virtual void DataChanged(bool rebuild = false)
        {
            this.rebuild = rebuild;
            Repaint();
        }

        /// <summary>
        /// Unity's Default Margins. False if running in Advanced Inspector.
        /// </summary>
        public override bool UseDefaultMargins()
        {
            return !advanced;
        }

        /// <summary>
        /// If true, will test if the type of in the inspector object has "AdvancedInspector" attributes.
        /// Default; false;
        /// </summary>
        public virtual bool TestForDefaultInspector
        {
            get { return false; }
        }

        /// <summary>
        /// Default Inspector entry point.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (Instances == null)
                return;

            current = this;

            if (advanced)
                DrawAdvancedInspector();
            else
                DrawDefaultInspector();

            if (picking)
            {
                Rect window = EditorWindow.focusedWindow.position;
                EditorGUIUtility.AddCursorRect(new Rect(0, 0, window.width, window.height), MouseCursor.CustomCursor);

                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
                {
                    StopPicking();
                    Event.current.Use();
                }
            }

            current = null;

            if (Instances.Length != 1)
                return;

            IInspect inspect = Instances[0] as IInspect;
            if (inspect == null)
                return;

            object[] inspectInstances = inspect.Inspect;
            if (inspectInstances == null || inspectInstances.Length == 0)
            {
                ClearExtraEditors();
            }
            else
            {
                bool refreshEditors = inspectInstances.Length != extraEditors.Count;
                if (!refreshEditors)
                {
                    for (int i = 0; i < inspectInstances.Length; i++)
                    {
                        if (inspectInstances[i] is UnityEngine.Object)
                        {
                            if ((UnityEngine.Object)inspectInstances[i] != extraEditors[i].target)
                            {
                                refreshEditors = true;
                                break;
                            }
                        }
                        else
                        {
                            InspectorWrapper wrapper = extraEditors[i].target as InspectorWrapper;
                            if (wrapper == null || wrapper.Tag != inspectInstances[i])
                            {
                                refreshEditors = true;
                                break;
                            }
                        }
                    }
                }

                if (refreshEditors)
                {
                    ClearExtraEditors();

                    foreach (object inspectInstance in inspectInstances)
                    {
                        if (inspectInstance == null || inspectInstance.GetType().Namespace == "System")
                            continue;

                        UnityEngine.Object unityObject = inspectInstance as UnityEngine.Object;
                        if (unityObject == null)
                        {
                            InspectorWrapper wrapper = CreateInstance<InspectorWrapper>();
                            wrapper.Tag = inspectInstance;
                            unityObject = wrapper;
                        }

                        extraEditors.Add(CreateEditor(unityObject));
                    }
                }

                foreach (Editor editor in extraEditors)
                {
                    editor.DrawHeader();
                    editor.OnInspectorGUI();
                }
            }
        }

        private void ClearExtraEditors()
        {
            foreach (Editor editor in extraEditors)
            {
                if (editor.target is InspectorWrapper)
                    DestroyImmediate(editor.target);

                DestroyImmediate(editor, true);
            }

            extraEditors.Clear();
        }

        /// <summary>
        /// Similar to "DrawDefaultInspector", except this is the Advanced one.
        /// </summary>
        protected void DrawAdvancedInspector()
        {
            if (rebuild)
            {
                fields.Clear();
                RefreshFields();
                rebuild = false;
            }

            AdvancedInspectorControl.Inspect(this, fields, false, rebuild);

            if (Changed)
            {
                foreach (object instance in Instances)
                {
                    if (instance is UnityEngine.Object)
                        EditorUtility.SetDirty((UnityEngine.Object)instance);

                    IDataChanged data = instance as IDataChanged;
                    if (data != null)
                        data.DataChanged();

                    Invoke("OnValidate", instance);
                }
            }

            for (int i = 0; i < fields.Count; i++)
                fields[i].ClearCache();
        }

        private void Invoke(string method, object target)
        {
            if (target == null || string.IsNullOrEmpty(method))
                return;

            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            if (info != null)
                info.Invoke(target, new object[0]);
        }

        private void Sort()
        {
            fields.Sort();
            foreach (InspectorField field in fields)
                field.SortFields();
        }

        /// <summary>
        /// Force this field to rebuild the list of its children.
        /// If you implement an editor specific to a type, you should only need to override this method.
        /// Consider clearing existing fields before adding new one.
        /// </summary>
        protected virtual void RefreshFields()
        {
            bool inspectDefaultItems = false;
            if (Instances.Length != 0)
            {
                object[] attributes = Instances[0].GetType().GetCustomAttributes(typeof(AdvancedInspectorAttribute), true);
                if (attributes.Length != 0)
                    inspectDefaultItems = ((AdvancedInspectorAttribute)attributes[0]).InspectDefaultItems;
                else
                    inspectDefaultItems = InspectorPreferences.InspectDefaultItems;
            }

            fields = InspectorField.GetEntries(null, instances, inspectDefaultItems);
            tab = GetTab(fields);
        }

        private static Type GetTab(IList<InspectorField> fields)
        {
            for (int i = 0; i < fields.Count; i++)
            {
                TabAttribute tabAttribute = fields[i].GetAttribute<TabAttribute>();
                if (tabAttribute != null)
                    return tabAttribute.Invoke(0, fields[i].Instances[0], fields[i].GetValue()).GetType();

                Type child = GetTab(fields[i].Fields);
                if (child != null)
                    return child;
            }

            return null;
        }

        internal static void Gather()
        {
            if (inspectorEditorByTypes != null)
                return;

            HashSet<InspectorEditor> editors = new HashSet<InspectorEditor>();
            inspectorEditors = new Dictionary<Type, InspectorEditor>();
            inspectorEditorByTypes = new Dictionary<Type, InspectorEditor>();

            inspectedType = typeof(CustomEditor).GetField("m_InspectedType", BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsGenericType || !typeof(InspectorEditor).IsAssignableFrom(type))
                        continue;

                    if (typeof(ExternalEditor) == type || typeof(BehaviourEditor) == type || typeof(ScriptableEditor) == type)
                        continue;

                    InspectorEditor editor = ScriptableObject.CreateInstance(type) as InspectorEditor;
                    editor.hideFlags = HideFlags.HideAndDontSave;
                    editors.Add(editor);

                    object[] attribute = type.GetCustomAttributes(typeof(CustomEditor), true);
                    if (attribute.Length == 0)
                        continue;

                    foreach (CustomEditor customEditor in attribute)
                    {
                        Type inspectorType = (Type)inspectedType.GetValue(customEditor);
                        if (!inspectorEditors.ContainsKey(inspectorType))
                            inspectorEditors.Add(inspectorType, editor);
                    }
                }
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    InspectorEditor editor = GetEditor(type);

                    if (editor != null)
                        inspectorEditorByTypes.Add(type, editor);
                }
            }

            foreach (InspectorEditor editor in editors)
            {
                DestroyImmediate(editor);
            }
        }

        private static InspectorEditor GetEditor(Type type)
        {
            // Prioritize direct types
            InspectorEditor direct;
            if (inspectorEditors.TryGetValue(type, out direct))
                return direct;

            // Get derived editor
            foreach (KeyValuePair<Type, InspectorEditor> pair in inspectorEditors)
                if (pair.Key.IsAssignableFrom(type))
                    return pair.Value;

            return null;
        }
    }
}