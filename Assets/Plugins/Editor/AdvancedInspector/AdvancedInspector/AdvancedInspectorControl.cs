using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    /// <summary>
    /// A collection of static method made to handle the drawing of the Advanced Inspector.
    /// In most cases, you won't have to deal with this class.
    /// </summary>
    [InitializeOnLoad]
    public class AdvancedInspectorControl : IModal
    {
        #region Constants
        public const string DataPath = "Assets/Plugins/Editor/AdvancedInspector/Data/";

        private const string InspectorLevelKey = "InspectorLevel";
        private const string InspectorSortKey = "InspectorSorting";
        private const string InspectorCollectionLockKey = "InspectorCollectionLock";
        private const string InspectorIconPreviewShowKey = "InspectorIconPreviewShow";
        private const string InspectorIconPreviewSizeKey = "InspectorIconPreviewSize";
        private const string InspectorTabKey = "InspectorTab:";


        private const int MIN_FIELD_HEIGHT = 16;
        private const int BUTTON_HEIGHT = 16;
        private const int VECTOR_LABEL = 12;

        private const double DOUBLE_CLICK_TIME = 0.3;

        // For some reason, storing the Style sometimes make the Texture Ref be flushed from the memory.
        /// <summary>
        /// Group box style
        /// </summary>
        internal static GUIStyle BoxStyle
        {
            get
            {
                GUIStyle boxStyle = new GUIStyle();
                boxStyle.border = new RectOffset(6, 0, 6, 5);
                boxStyle.margin = new RectOffset(4, 0, 0, 4);
                boxStyle.padding = new RectOffset(4 + InspectorPreferences.ExtraIndentation, 0, 4, 0);
                boxStyle.normal.background = (Texture2D)Box;

                return boxStyle;
            }
        }

        internal static GUIStyle BoxTitleStyle
        {
            get
            {
                GUIStyle boxStyle = new GUIStyle();
                boxStyle.border = new RectOffset(6, 0, 6, 5);
                boxStyle.margin = new RectOffset(4, 0, 2, 0);
                boxStyle.padding = new RectOffset(4, 0, 0, 0);
                boxStyle.normal.background = (Texture2D)BoxTitle;

                return boxStyle;
            }
        }

        internal static GUIStyle Nested
        {
            get
            {
                GUIStyle empty = new GUIStyle();
                empty.border = new RectOffset(0, 0, 0, 0);
                empty.margin = new RectOffset(0, 10, 0, 0);
                empty.padding = new RectOffset(0, 0, 0, 0);
                empty.normal.background = null;
                return empty;
            }
        }
        #endregion

        private static Dictionary<Type, HelpAttribute[]> helpAttributesByType = new Dictionary<Type, HelpAttribute[]>();

        private static Vector2 mousePosition = Vector2.zero;

        private static Color proLabelColor = new Color(0.71f, 0.71f, 0.71f);
        private static Color selectedColor = new Color(0.35f, 0.55f, 0.85f, 0.5f);
        private static Color animatedLabelColor = new Color(0.7f, 0.4f, 0.35f);

        private static Separator separator = new Separator(null, null, 172, true, true);
        private static float offset = 172;

        private static Rect region;
        private static Rect previousNode;

        private static float header = 0;
        private static float footer = 0;

        private static double clickTime = 0;

        private static bool suspended = false;

        private static bool collectionLock = false;

        internal static bool watched = false;

        /// <summary>
        /// When complex operation are performed, the Inspector blocks repaint.
        /// </summary>
        public static bool Suspended
        {
            get { return suspended; }
        }

        private static InspectorLevel level = InspectorLevel.Basic;

        /// <summary>
        /// Current complexity level.
        /// </summary>
        public static InspectorLevel Level
        {
            get { return level; }
        }

        private static InspectorSorting sorting = InspectorSorting.None;

        /// <summary>
        /// How the fields are sorted.
        /// </summary>
        public static InspectorSorting Sorting
        {
            get { return sorting; }
            set
            {
                if (sorting == value)
                    return;

                sorting = value;

                if (SortingChanged != null)
                    SortingChanged();
            }
        }

        private static bool showIconPreview = false;

        /// <summary>
        /// Should an object field display a preview icon?
        /// </summary>
        public static bool ShowIconPreview
        {
            get { return showIconPreview; }
        }

        private static IconPreviewSize iconPreviewSize = IconPreviewSize.Normal;

        /// <summary>
        /// At which size the preview icon should be displayed.
        /// </summary>
        public static IconPreviewSize IconPreviewSize
        {
            get { return iconPreviewSize; }
        }

        /// <summary>
        /// Raised if the inspector needs a redraw.
        /// </summary>
        public static event GenericEventHandler SortingChanged;

        private static DragState dragState = DragState.Up;
        private static InspectorField draggedField = null;

        private static float mouseStart = 0;
        private static float mouseDrag = 0;
        private static int hoverIndex = 0;
        private static Rect selectedRect;
        private static Rect listRect;
        private static List<float> fieldsHeight = new List<float>();

        private static InspectorField selectedLabel = null;

        #region Texture
        private static bool forceLight = false;
        
        private static Texture boxEmpty;

        private static Texture box;
        private static Texture boxPro;
        private static Texture boxFlat;
        private static Texture boxProFlat;

        internal static Texture Box
        {
            get
            {
                if (InspectorPreferences.Style == InspectorStyle.Round)
                {
                    if (!EditorGUIUtility.isProSkin || forceLight)
                    {
                        if (boxPro == null)
                            boxPro = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxRound.png");

                        return boxPro;
                    }
                    else
                    {
                        if (box == null)
                            box = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxRound_Pro.png");

                        return box;
                    }
                }
                else if (InspectorPreferences.Style == InspectorStyle.Flat)
                {
                    if (!EditorGUIUtility.isProSkin || forceLight)
                    {
                        if (boxProFlat == null)
                            boxProFlat = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxFlat.png");

                        return boxProFlat;
                    }
                    else
                    {
                        if (boxFlat == null)
                            boxFlat = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxFlat_Pro.png");

                        return boxFlat;
                    }
                }
                else if (InspectorPreferences.Style == InspectorStyle.Empty)
                {
                    if (boxEmpty == null)
                        boxEmpty = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Box_Empty.png");

                    return boxEmpty;
                }

                return null;
            }
        }

        private static Texture boxTitle;
        private static Texture boxTitlePro;
        private static Texture boxTitleFlat;
        private static Texture boxTitleFlatPro;

        internal static Texture BoxTitle
        {
            get
            {
                if (InspectorPreferences.Style == InspectorStyle.Round)
                {
                    if (!EditorGUIUtility.isProSkin || forceLight)
                    {
                        if (boxTitlePro == null)
                            boxTitlePro = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxRound_Title.png");

                        return boxTitlePro;
                    }
                    else
                    {
                        if (boxTitle == null)
                            boxTitle = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxRound_TitlePro.png");

                        return boxTitle;
                    }
                }
                else if (InspectorPreferences.Style == InspectorStyle.Flat)
                {
                    if (!EditorGUIUtility.isProSkin || forceLight)
                    {
                        if (boxTitleFlatPro == null)
                            boxTitleFlatPro = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxFlat_Title.png");

                        return boxTitleFlatPro;
                    }
                    else
                    {
                        if (boxTitleFlat == null)
                            boxTitleFlat = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "BoxFlat_TitlePro.png");

                        return boxTitleFlat;
                    }
                }
                else if (InspectorPreferences.Style == InspectorStyle.Empty)
                {
                    if (boxEmpty == null)
                        boxEmpty = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Box_Empty.png");

                    return boxEmpty;
                }

                return null;
            }
        }

        private static Texture extendTop;

        internal static Texture ExtendTop
        {
            get
            {
                if (extendTop == null)
                {
                    if (EditorGUIUtility.isProSkin)
                        extendTop = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ExtendTopPro.png");
                    else
                        extendTop = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ExtendTop.png");
                }

                return extendTop;
            }
        }

        private static Texture extendBot;

        internal static Texture ExtendBot
        {
            get
            {
                if (extendBot == null)
                {
                    if (EditorGUIUtility.isProSkin)
                        extendBot = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ExtendBotPro.png");
                    else
                        extendBot = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ExtendBot.png");
                }

                return extendBot;
            }
        }

        private static Texture scrollUp;

        internal static Texture ScrollUp
        {
            get
            {
                if (scrollUp == null)
                    scrollUp = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ScrollUp.png");

                return scrollUp;
            }
        }

        private static Texture scrollDown;

        internal static Texture ScrollDown
        {
            get
            {
                if (scrollDown == null)
                    scrollDown = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "ScrollDown.png");

                return scrollDown;
            }
        }

        private static Texture folderOpen;

        internal static Texture FolderOpen
        {
            get
            {
                if (folderOpen == null)
                    folderOpen = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "FolderOpen.png");

                return folderOpen;
            }
        }

        private static Texture folderClose;

        internal static Texture FolderClose
        {
            get
            {
                if (folderClose == null)
                    folderClose = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "FolderClosed.png");

                return folderClose;
            }
        }

        private static Texture add;

        internal static Texture Add
        {
            get
            {
                if (add == null)
                    add = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Add.png");

                return add;
            }
        }

        private static Texture delete;

        internal static Texture Delete
        {
            get
            {
                if (delete == null)
                    delete = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Delete.png");

                return delete;
            }
        }

        private static Texture drag;
        private static Texture dragPro;

        internal static Texture Drag
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    if (dragPro == null)
                        dragPro = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Drag.png");

                    return dragPro;
                }
                else
                {
                    if (drag == null)
                        drag = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "Drag_Light.png");

                    return drag;
                }
            }
        }

        private static Texture moveUp;

        internal static Texture MoveUp
        {
            get
            {
                if (moveUp == null)
                    moveUp = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "MoveUp.png");

                return moveUp;
            }
        }

        private static Texture moveDown;

        internal static Texture MoveDown
        {
            get
            {
                if (moveDown == null)
                    moveDown = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "MoveDown.png");

                return moveDown;
            }
        }

        private static Texture noOrder;

        internal static Texture NoOrder
        {
            get
            {
                if (noOrder == null)
                    noOrder = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "NoOrder.png");

                return noOrder;
            }
        }

        private static Texture alphaOrder;

        internal static Texture AlphaOrder
        {
            get
            {
                if (alphaOrder == null)
                    alphaOrder = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "AlphaOrder.png");

                return alphaOrder;
            }
        }

        private static Texture antiAlphaOrder;

        internal static Texture AntiAlphaOrder
        {
            get
            {
                if (antiAlphaOrder == null)
                    antiAlphaOrder = AssetDatabase.LoadAssetAtPath<Texture>(DataPath + "AntiAlphaOrder.png");

                return antiAlphaOrder;
            }
        }
        #endregion

        #region Properties
        private static InspectorEditor inspectorEditor;

        /// <summary>
        /// Used for internal callback from a modal windows.
        /// </summary>
        public static InspectorEditor Editor
        {
            get { return inspectorEditor; }
        }

        private static InspectorField inspectorField;

        /// <summary>
        /// Used for internal callback from a modal windows.
        /// </summary>
        public static InspectorField Field
        {
            get { return inspectorField; }
        }

        private static object dictionaryKey;

        private static InspectorAction inspectorAction;
        #endregion

        private enum InspectorAction
        {
            Create,
            Set
        }

        private enum DragState
        {
            Up,
            Down,
            Fetch,
            Drag
        }

        /// <summary>
        /// Field sorting enum.
        /// </summary>
        public enum InspectorSorting
        {
            /// <summary>
            /// No sorting, using the order found in code.
            /// </summary>
            None,
            /// <summary>
            /// Alphabethic sorting A, B, C...
            /// </summary>
            Alpha,
            /// <summary>
            /// Anti-alphabethic sorting, C, B, A...
            /// </summary>
            AntiAlpha
        }

        static AdvancedInspectorControl() 
        {
            if (EditorPrefs.HasKey(InspectorLevelKey))
                level = (InspectorLevel)EditorPrefs.GetInt(InspectorLevelKey);

            if (EditorPrefs.HasKey(InspectorSortKey))
                sorting = (InspectorSorting)EditorPrefs.GetInt(InspectorSortKey);

            if (EditorPrefs.HasKey(InspectorIconPreviewShowKey))
                showIconPreview = EditorPrefs.GetBool(InspectorIconPreviewShowKey);

            if (EditorPrefs.HasKey(InspectorIconPreviewSizeKey))
                iconPreviewSize = (IconPreviewSize)EditorPrefs.GetInt(InspectorIconPreviewSizeKey);

            if (EditorPrefs.HasKey(InspectorCollectionLockKey))
                collectionLock = EditorPrefs.GetBool(InspectorCollectionLockKey);
        }

        private AdvancedInspectorControl(InspectorEditor editor, InspectorField field, InspectorAction action)
        {
            inspectorEditor = editor;
            inspectorField = field;
            inspectorAction = action;
        }

        #region Implementation of IModal
        /// <summary>
        /// IModal Implementation.
        /// </summary>
        public void ModalRequest(bool shift) { }

        /// <summary>
        /// IModal Implementation.
        /// </summary>
        public void ModalClosed(ModalWindow window)
        {
            Toolbox box = window as Toolbox;
            if (box.Result != WindowResult.Ok || box.Selection.Length == 0)
                return;

            if (inspectorAction == InspectorAction.Create)
            {
                CreateDerived(Field, (Type)box.Selection[0]);
                inspectorField.Expanded = true;
            }
            else if (inspectorAction == InspectorAction.Set)
                inspectorField.SetValue(box.Selection[0]);

            inspectorEditor.Repaint();
        }
        #endregion

        private static void CreateDerived(InspectorField field, Type type)
        {
            if (field.IsList)
                AddItem(field, type);
            else if (field.IsDictionary)
                AddItem(field, type, dictionaryKey);
            else
            {
                field.RecordObjects("Create " + field.Name);

                for (int i = 0; i < field.Instances.Length; i++)
                {
                    MonoBehaviour behaviour = field.SerializedInstances[i] as MonoBehaviour;
                    if (behaviour != null)
                        field.SetValue(field.Instances[i], Clipboard.CreateInstance(type, behaviour));

                    ScriptableObject scriptable = field.SerializedInstances[i] as ScriptableObject;
                    if (scriptable != null)
                        field.SetValue(field.Instances[i], Clipboard.CreateInstance(type, scriptable));
                }

                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Called by an Editor to have a collection of InspectorField to be displayed.
        /// Uniformized entry point of inspector drawing.
        /// </summary>
        public static bool Inspect(InspectorEditor editor, List<InspectorField> fields, bool newGroup, bool refresh)
        {
            return Inspect(editor, fields, newGroup, refresh, true, separator);
        }

        /// <summary>
        /// Called by an Editor to have a collection of InspectorField to be displayed.
        /// Uniformized entry point of inspector drawing.
        /// </summary>
        public static bool Inspect(InspectorEditor editor, List<InspectorField> fields, bool newGroup, bool refresh, bool expansion)
        {
            return Inspect(editor, fields, newGroup, refresh, expansion, separator);
        }

        /// <summary>
        /// Called by an Editor to have a collection of InspectorField to be displayed.
        /// Uniformized entry point of inspector drawing.
        /// </summary>
        internal static bool Inspect(InspectorEditor editor, List<InspectorField> fields, bool newGroup, bool refresh, bool expansion, Separator externalSeparator)
        {
            if (Event.current == null)
                return false;

            bool redraw = false;
            EditorWindow window = EditorWindow.focusedWindow;

            if (window != null)
                mousePosition = GUIUtility.GUIToScreenPoint(new Vector2(Event.current.mousePosition.x, Event.current.mousePosition.y));

            if (Event.current.type == EventType.MouseUp && draggedField != null)
            {
                MoveItemToIndex(draggedField, hoverIndex);

                draggedField = null;
                selectedRect = new Rect(0, 0, 0, 0);
                listRect = new Rect(0, 0, 0, 0);
                fieldsHeight.Clear();
                hoverIndex = 0;
                mouseDrag = 0;
                mouseStart = 0;
                dragState = DragState.Up;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && draggedField != null)
            {
                mouseDrag = mousePosition.y - mouseStart;
                hoverIndex = GetDraggedIndex();
                window.Repaint();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDown)
            {
                mouseStart = mousePosition.y;
                mouseDrag = mousePosition.y - mouseStart;

                window.Repaint();
                redraw = true;
            }

            offset = externalSeparator.Division - BUTTON_HEIGHT - Separator.WIDTH;
            region = EditorGUILayout.BeginVertical();

            inspectorEditor = editor;
            Draw(editor, null, fields, newGroup, refresh, expansion);

            region.y += header;
            region.height -= header + footer;
            if (externalSeparator.Draw(region) && window != null)
                window.Repaint();

            EditorGUILayout.EndVertical();

            region = GUILayoutUtility.GetLastRect();

            GUI.enabled = true;
            EditorGUI.indentLevel = 0;
            EditorGUI.showMixedValue = false;

            return redraw;
        }

        #region Draw
        /// <summary>
        /// Entry point of the inspection of a Property based editor.
        /// </summary>
        private static void Draw(InspectorEditor editor, InspectorField parent, List<InspectorField> fields, bool newGroup, bool refresh, bool expansion)
        {
            int start = 0;
            int stop = fields.Count;

            CollectionAttribute collection = null;
            if (parent != null && (parent.IsList || parent.IsDictionary))
            {
                collection = parent.GetAttribute<CollectionAttribute>();
                int maxItems = collection == null ? InspectorPreferences.LargeCollection : collection.MaxDisplayedItems;
                start = parent.visibleIndex;
                stop = Mathf.Min(maxItems, parent.Count, stop);
            }

            if (newGroup)
            {
                Color previous = GUI.color;
                if (parent.BackgroundColor != Color.clear)
                {
                    forceLight = true;
                    GUI.color = parent.BackgroundColor;
                }
                else
                {
                    GUI.color = EditorApplication.isPlaying ? InspectorPreferences.BoxPlayColor : InspectorPreferences.BoxDefaultColor;
                }

                EditorGUILayout.BeginVertical(BoxStyle);

                forceLight = false;
                GUI.color = previous;
            }

            if (editor.tab != null && editor.selectedTab == null)
            {
                string key = InspectorTabKey + editor.Instances[0].GetType().AssemblyQualifiedName;
                if (EditorPrefs.HasKey(key))
                    editor.selectedTab = (Enum)Enum.ToObject(editor.tab, EditorPrefs.GetInt(key));
                else
                    editor.selectedTab = (Enum)Enum.GetValues(editor.tab).GetValue(0);
            }

            editor.selectedTab = DrawHeader(parent, fields, editor, editor.selectedTab);

            bool selection = fields.Contains(draggedField);

            List<float> heights = new List<float>();
            for (int i = 0; i < stop; i++)
            {
                if (i >= fields.Count)
                    break;

                InspectorField field = fields[i];
                if (field.Index != -1 && (collection == null || collection.Display == CollectionDisplay.List))
                    field.Index = i + start;

                field.SelectedTab = editor.selectedTab;
                if (!field.Visible || field.Erased)
                    continue;

                field.InitCollection();

                if (selection && dragState == DragState.Drag)
                {
                    if ((hoverIndex >= draggedField.Index && hoverIndex + 1 == field.Index) || (hoverIndex < draggedField.Index && hoverIndex == field.Index))
                        GUILayout.Box(new GUIContent(""), GUIStyle.none, GUILayout.Height(selectedRect.height), GUILayout.Width(selectedRect.width));
                }

                if (dragState != DragState.Drag || field != draggedField)
                {
                    DisplayAsParentAttribute displayAsParent = null;
                    if (!field.IsList && !field.IsDictionary)
                        displayAsParent = field.GetAttribute<DisplayAsParentAttribute>();

                    if (displayAsParent == null || !displayAsParent.HideParent)
                        DrawNode(editor, field, refresh, expansion);
                    else
                        DrawChildren(editor, field, refresh, expansion);
                }

                if (Event.current.type == EventType.Repaint && dragState == DragState.Down && selection)
                {
                    heights.Add(GetLastFieldRect(field).height);

                    if (field == draggedField)
                        selectedRect = GetLastFieldRect(field);
                }
            }

            if (selection && hoverIndex + 1 >= (start + stop) && dragState == DragState.Drag)
                GUILayout.Box(new GUIContent(""), GUIStyle.none, GUILayout.Height(selectedRect.height), GUILayout.Width(selectedRect.width));

            if (heights.Count > 0)
            {
                fieldsHeight = heights;
                dragState = DragState.Fetch;
                Event.current.Use();
            }

            if (selection && dragState == DragState.Drag)
            {
                GUILayout.BeginArea(new Rect(selectedRect.x, Mathf.Clamp(selectedRect.y + mouseDrag, listRect.yMin + 4, listRect.yMax - 8), selectedRect.width, selectedRect.height + 2));

                Color fontColor = GUI.skin.label.normal.textColor;
                GUI.skin.label.normal.textColor = Color.black;

                DrawNode(editor, draggedField, refresh, expansion);
                Helper.DrawColor(new Rect(0, 0, selectedRect.width, selectedRect.height + 2), selectedColor);

                GUI.skin.label.normal.textColor = fontColor;

                GUILayout.EndArea(); 
            }

            EditorGUILayout.Space();

            DrawFooter(parent, editor);

            if (newGroup)
                EditorGUILayout.EndVertical();

            previousNode = GUILayoutUtility.GetLastRect();

            if (parent != null && parent.IsList && (collection == null || collection.Display == CollectionDisplay.List))
            {
                if (start > 0)
                {
                    GUI.DrawTexture(new Rect(previousNode.x, previousNode.y + 1, previousNode.width, 18), ExtendTop);
                    if (GUI.Button(new Rect(offset + 18, previousNode.y - 2, 12, 20), ScrollUp, GUIStyle.none))
                        parent.visibleIndex = Mathf.Clamp(Mathf.Min(start - (int)(stop * 0.5f), parent.Count - stop), 0, int.MaxValue);
                }

                if (stop < parent.Count)
                {
                    GUI.DrawTexture(new Rect(previousNode.x, previousNode.yMax - 20, previousNode.width, 18), ExtendBot);
                    if (GUI.Button(new Rect(offset + 18, previousNode.yMax - 14, 12, 20), ScrollDown, GUIStyle.none))
                        parent.visibleIndex = Mathf.Min(start + (int)(stop * 0.5f), parent.Count - stop);
                }
            }
        }

        private static int GetDraggedIndex()
        {
            if (fieldsHeight.Count == 0)
                return draggedField.Index;

            int visibleOffset = 0;
            if (draggedField.Parent != null)
                visibleOffset = draggedField.Parent.visibleIndex;

            int index = draggedField.Index - visibleOffset;

            float offset = GetIndexDistance(index);
            float distance = 0;
            for (int i = 0; i < fieldsHeight.Count; i++)
            {
                distance += fieldsHeight[i] * 0.50f;
                if (distance > mouseDrag + offset)
                    return i + visibleOffset;

                distance += fieldsHeight[i] * 0.50f;
            }

            return fieldsHeight.Count + visibleOffset;
        }

        private static float GetIndexDistance(int index)
        {
            if (fieldsHeight.Count == 0)
                return 0;

            float distance = 0;
            for (int i = 0; i < index; i++)
                distance += fieldsHeight[i];

            return distance;
        }

        private static Rect GetLastFieldRect(InspectorField field)
        {
            Rect rect = GUILayoutUtility.GetLastRect();

            if (field.Expanded && field.Fields.Count > 0)
            {
                rect.y -= 20;
                rect.height += 20;
            }

            return rect;
        }

        private static Dictionary<FieldAttribute, FieldEditor> GetFieldDrawers(InspectorField field)
        {
            if (field.Type == null)
                return null;

            IOrderedEnumerable<FieldAttribute> attributes;
            if (field.IsList || field.IsDictionary)
                attributes = field.GetAttributes<FieldAttribute>().Where(x => !(x is IListAttribute)).OrderBy(x => x.Order);
            else
                attributes = field.GetAttributes<FieldAttribute>().OrderBy(x => x.Order);

            if (attributes.Count() == 0)
                return null;

            Dictionary<FieldAttribute, FieldEditor> drawers = new Dictionary<FieldAttribute, FieldEditor>();
            foreach (FieldAttribute attribute in attributes)
            {
                FieldEditor editor;
                FieldEditor.AttributeEditors.TryGetValue(attribute.GetType(), out editor);
                if (editor != null)
                    drawers.Add(attribute, editor);
            }

            return drawers;
        }

        private static Enum DrawHeader(InspectorField parent, List<InspectorField> fields, InspectorEditor editor, Enum selectedTab)
        {
            EditorGUILayout.BeginVertical();

            object[] values;
            if (parent == null)
            {
                for (int i = 0; i < fields.Count; i++)
                    if (fields[i].InspectorType == InspectorType.Toolbar)
                        DrawToolbar(editor, fields[i]);

                values = editor.Instances;
            }
            else
                values = parent.GetValues();

            if (selectedTab != null && parent == null)
            {
                Enum nextTab = DrawTabs(selectedTab, values);

                if (selectedTab != nextTab)
                {
                    selectedTab = nextTab;
                    EditorPrefs.SetInt(InspectorTabKey + editor.Instances[0].GetType().AssemblyQualifiedName, (int)(object)selectedTab);
                }
            }

            DrawClassHelp(values, false);

            if (values.Length == 1 && values[0] != null && values[0] is IInspectorRunning)
                ((IInspectorRunning)values[0]).OnHeaderGUI();

            EditorGUILayout.EndVertical();

            if (parent == null)
                header = GUILayoutUtility.GetLastRect().height;

            return selectedTab;
        }

        private static void DrawFooter(InspectorField parent, InspectorEditor editor)
        {
            EditorGUILayout.BeginVertical();

            object[] values;
            if (parent == null)
                values = editor.Instances;
            else
                values = parent.GetValues();

            DrawClassHelp(values, true);

            if (values.Length == 1 && values[0] != null && values[0] is IInspectorRunning)
                ((IInspectorRunning)values[0]).OnFooterGUI();

            EditorGUILayout.EndVertical();

            if (parent == null)
                footer = GUILayoutUtility.GetLastRect().height;
        }

        private static Enum DrawTabs(Enum selected, object[] instances)
        {
            Type type = selected.GetType();
            List<GUIContent> tabs = new List<GUIContent>();

            DescriptorAttribute descriptor = null;
            object[] attributes = type.GetCustomAttributes(typeof(DescriptorAttribute), true);
            if (attributes.Length > 0)
                descriptor = attributes[0] as DescriptorAttribute;

            MethodInfo method = null;
            if (descriptor != null)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type extension in assembly.GetTypes())
                    {
                        if (!extension.IsSealed || !extension.IsAbstract)
                            continue;

                        foreach (MethodInfo info in extension.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                        {
                            ParameterInfo[] args = info.GetParameters();
                            if (info.Name == descriptor.MethodName && args.Length == 1 && args[0].ParameterType == type)
                            {
                                method = info;
                                goto Found;
                            }
                        }
                    }
                }
            }

        Found:
            if (method == null)
            {
                foreach (Enum value in Enum.GetValues(type))
                {
                    IDescriptor valueDescriptor = value.GetAttribute<IDescriptor>();
                    if (valueDescriptor is IRuntimeAttribute)
                        ParseRuntimeAttributes(valueDescriptor as IRuntimeAttribute, value.GetType(), instances);

                    if (valueDescriptor == null || string.IsNullOrEmpty(valueDescriptor.GetDescription(instances, new object[] { null }).Name))
                        tabs.Add(new GUIContent(ObjectNames.NicifyVariableName(value.ToString())));
                    else
                        tabs.Add(new GUIContent(valueDescriptor.GetDescription(instances, new object[] { null }).Name));
                }
            }
            else
            {
                try
                {
                    foreach (Enum value in Enum.GetValues(type))
                    {
                        Description item = method.Invoke(null, new object[] { value }) as Description;
                        tabs.Add(new GUIContent(item.Name, item.Icon, item.Comment));
                    }
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        e = ((TargetInvocationException)e).InnerException;

                    Debug.LogError(string.Format("Invoke method named {0} failed while trying to get a Descriptor for an Enum definition. The exception was \"{1}\" and the stack was \"{2}\"", method.Name, e.Message, e.StackTrace));
                }
            }

            return (Enum)Enum.ToObject(type, GUILayout.Toolbar((int)(ValueType)selected, tabs.ToArray()));
        }

        private static void DrawNode(InspectorEditor editor, InspectorField field, bool refresh, bool expansion)
        {
            if (field.InspectorType == InspectorType.Toolbar)
                return;

            if (refresh)
                field.RefreshFields();

            int before = -1;
            ISpacing spacing = field.GetAttribute<ISpacing>();
            SpaceAttribute space = field.GetAttribute<SpaceAttribute>();
            if (space != null)
                before = (int)space.height;

            if (spacing != null)
                before = spacing.GetBefore(field.Instances, field.GetValues());

            if (before > 0)
                GUILayout.Label(GUIContent.none, GUILayout.Height(before));

            DrawTitle(field);

            FieldEditor fieldEditor = null;
            PropertyDrawer[] propertyDrawers = field.PropertyDrawers;
            Dictionary<FieldAttribute, FieldEditor> fieldDrawers = GetFieldDrawers(field);

            if (propertyDrawers.Length == 0 && (fieldDrawers == null || fieldDrawers.Count == 0))
                fieldEditor = field.Editor;

            GUIStyle style = null;
            if (!string.IsNullOrEmpty(field.Style))
                style = GUI.skin.FindStyle(field.Style);

            Color previous = GUI.color;

            if (style != null && field.InspectorType == InspectorType.Group)
                EditorGUILayout.BeginHorizontal(style, GUILayout.MinHeight(18));
            else
            {
                DisplayAsParentAttribute displayAsParent = null;
                if (!field.IsList && !field.IsDictionary)
                    displayAsParent = field.GetAttribute<DisplayAsParentAttribute>();

                if (field.Expandable && (fieldEditor == null || fieldEditor.IsExpandable(field)) && (displayAsParent == null || displayAsParent.HideParent))
                {
                    if (field.BackgroundColor != Color.clear)
                    {
                        forceLight = true;
                        GUI.color = field.BackgroundColor;
                    }
                    else
                    {
                        GUI.color = EditorApplication.isPlaying ? InspectorPreferences.BoxPlayColor : InspectorPreferences.BoxDefaultColor;
                    }

                    EditorGUILayout.BeginHorizontal(BoxTitleStyle, GUILayout.MinHeight(18));

                    forceLight = false;
                    GUI.color = previous;
                }
                else
                    EditorGUILayout.BeginHorizontal(GUILayout.MinHeight(18));
            }

            DrawExpander(field, fieldEditor, expansion);

            bool enabled = GUI.enabled;
            enabled = !field.ReadOnly && field.Editable;

            if (DrawIndexedControl(field))
                return;

            if (DrawKeyedControl(field))
                return;

            GUI.enabled = !field.ReadOnly && field.Editable;

            if (field.Label)
                DrawLabel(fieldEditor, field, expansion);

            // The editing field
            GUILayout.BeginVertical(Nested);

            Description descriptio = field.Description;
            if (descriptio != null && descriptio.Color.a == 1)
                GUI.color = descriptio.Color;

            DrawField(editor, field, propertyDrawers, fieldDrawers, fieldEditor, style);

            GUI.color = previous;

            GUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();

            // Because helpbox get very close to the next field.
            IList<IHelp> helps = field.GetAttributes<IHelp>();
            if (helps.Count != 0 && !field.Expanded && helps.Any(x => x.GetHelp(field.Instances, field.GetValues()).Any(y => !string.IsNullOrEmpty(y.Message))))
                EditorGUILayout.Space();

            GUI.color = Color.white;
            GUI.enabled = true;

            if (expansion)
                DrawChildren(editor, field, refresh, expansion);

            if (draggedField != null && field.Fields.Contains(draggedField) && dragState == DragState.Fetch && Event.current.type == EventType.Repaint)
            {
                listRect = GetLastFieldRect(field);
                listRect.y += 18;
                listRect.height -= selectedRect.height + 18;

                dragState = DragState.Drag;
            }

            int after = -1;
            if (spacing != null)
                after = spacing.GetAfter(field.Instances, field.GetValues());
            
            if (after > 0)
                GUILayout.Label(GUIContent.none, GUILayout.Height(after));
        }

        private static void DrawTitle(InspectorField field)
        {
            HeaderAttribute[] headers = field.GetAttributes<HeaderAttribute>();
            foreach (HeaderAttribute header in headers)
                DrawTitle(field, new TitleAttribute(FontStyle.Bold, header.header));

            TitleAttribute[] titles = field.GetAttributes<TitleAttribute>();
            foreach (TitleAttribute title in titles)
            {
                if (title.Delegates.Count > 0)
                {
                    for (int i = 0; i < title.Delegates.Count; i++)
                        DrawTitle(field, title.Invoke(i, field.Instances[i], field.GetValue(field.Instances[i])));
                }
                else
                    DrawTitle(field, title);
            }
        }

        private static void DrawTitle(InspectorField field, TitleAttribute title)
        {
            EditorGUILayout.BeginHorizontal();
            if (field.Depth == 0)
                GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT + 8));
            else
                GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT + 4));

            GUI.skin.label.fontStyle = title.Style;
            GUIContent content = new GUIContent(title.Message);
            GUILayout.Label(content, GUILayout.Width(GetWidth(field)));
            GUI.skin.label.fontStyle = FontStyle.Normal;
            EditorGUILayout.EndHorizontal();
        }

        private static void DrawField(InspectorEditor editor, InspectorField field, PropertyDrawer[] properyDrawers, Dictionary<FieldAttribute, FieldEditor> fieldDrawers, FieldEditor fieldEditor, GUIStyle style)
        {
            if (field.HasAttribute<IHelp>())
                DrawHelp(field, false);

            IList<DescriptionPair> restricted = field.Restricted;
            if (properyDrawers.Length > 0 || (fieldDrawers != null && fieldDrawers.Count > 0))
            {
                foreach (PropertyDrawer properyDrawer in properyDrawers)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Space(4);

                    SerializedProperty sp = field.SerializedProperty;
                    if (sp != null && sp.serializedObject != null)
                    {
                        sp.serializedObject.Update();
                        EditorGUI.BeginChangeCheck();

                        properyDrawer.OnGUI(GUILayoutUtility.GetRect(10, properyDrawer.GetPropertyHeight(sp, GUIContent.none)), sp, GUIContent.none);

                        if (EditorGUI.EndChangeCheck())
                            sp.serializedObject.ApplyModifiedProperties();

                        GUILayout.Space(3);
                    }
                    else
                        Debug.LogError(string.Format("Trying to draw a Property Drawer of type {0} without a proper serialized property. This usually happen, while exposing a property-drawer driven value from a property or a dictionary.", properyDrawer.GetType().Name));

                    GUILayout.EndHorizontal();
                }

                if (fieldDrawers != null)
                {
                    foreach (KeyValuePair<FieldAttribute, FieldEditor> pair in fieldDrawers)
                    {
                        bool enabled = GUI.enabled;
                        GUI.enabled = true;

                        GUILayout.BeginHorizontal();
                        pair.Value.Draw(pair.Key, field);
                        GUILayout.EndHorizontal();

                        GUI.enabled = enabled;
                    }
                }
            }
            else if (field.IsList)
                DrawList(editor, field);
            else if (field.IsDictionary)
                DrawDictionary(editor, field);
            else if (restricted != null)
                DrawRestricted(editor, field, restricted, style);
            else if (field.CreateDerived)
                DrawDerived(editor, field, style);
            else if (field.InspectorType == InspectorType.Method)
                DrawMethod(field, style);
            else
            {
                if (field.InspectorType == InspectorType.Serialized)
                {
                    field.SerializedProperty.serializedObject.Update();

                    EditorGUI.BeginChangeCheck();
                    EditorGUILayout.PropertyField(field.SerializedProperty, GUIContent.none);

                    if (EditorGUI.EndChangeCheck())
                    {
                        field.SerializedProperty.serializedObject.ApplyModifiedProperties();

                        foreach (UnityEngine.Object serializable in field.SerializedInstances)
                            if (serializable != null)
                                EditorUtility.SetDirty(serializable);
                    }
                }
                else if (fieldEditor != null)
                {
                    try
                    {
                        fieldEditor.Draw(field, style);
                    }
                    catch (Exception e)
                    {
                        if (!(e is ExitGUIException))
                            Debug.LogError("Drawing the field " + field.Name + " returned the following exception: " + e.Message + " on the stack: " + e.StackTrace);
                    }
                }
                else if (field.InspectorType != InspectorType.Group && field.Type != null)
                {
                    object value = field.GetValue();

                    if (value != null)
                    {
                        if (field.OverloadToString || level == InspectorLevel.Debug)
                            GUILayout.Label(value.ToString());
                    }
                    else
                        GUILayout.Label("None");
                }
                else if (field.InspectorType == InspectorType.Group)
                {
                    GUILayout.Label(field.Group.Description);
                }
            }

            EditorGUI.showMixedValue = false;

            if (field.HasAttribute<IHelp>())
                DrawHelp(field, true);
        }

        private static void DrawMethod(InspectorField field, GUIStyle style)
        {
            GUILayout.BeginHorizontal();
            if (!field.Label && field.Toolbar == null)
            {
                if (field.Group != null)
                    GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.Width(offset - 6 - (field.Depth * 4)));
                else
                    GUILayout.Box(GUIContent.none, GUIStyle.none, GUILayout.Width(offset + 2));
            }

            MethodAttribute method = field.GetAttribute<MethodAttribute>();

            if (method == null || method.Display == MethodDisplay.Button)
            {
                GUIContent content;
                Description description = field.Description;
                if (description != null)
                    content = new GUIContent((string.IsNullOrEmpty(description.Name)) ? field.Name : description.Name, description.Icon, description.Comment);
                else
                    content = new GUIContent(field.Name);

                bool result = false;
                if (style != null)
                {
                    if (GUILayout.Button(content, style, GUILayout.Height(BUTTON_HEIGHT)))
                        result = true;
                }
                else
                {
                    if (GUILayout.Button(content, GUILayout.Height(BUTTON_HEIGHT)))
                        result = true;
                }

                if (result)
                {
                    MethodInfo info = field.Info as MethodInfo;

                    try
                    {
                        if (method != null && !string.IsNullOrEmpty(method.UndoMessageOnClick))
                            field.RecordObjects(method.UndoMessageOnClick);

                        if (method != null && method.IsCoroutine && typeof(IEnumerator).IsAssignableFrom(info.ReturnType))
                        {
                            IEnumerator enumerator = null;
                            if (info.IsStatic)
                            {
                                enumerator = info.Invoke(null, null) as IEnumerator;
                                if (enumerator != null)
                                    InspectorCoroutine.StartCoroutine(enumerator);
                            }
                            else
                            {
                                foreach (object instance in field.Instances)
                                {
                                    enumerator = info.Invoke(instance, new object[0]) as IEnumerator;
                                    if (enumerator != null)
                                        InspectorCoroutine.StartCoroutine(enumerator);
                                }
                            }
                        }
                        else
                        {
                            if (info.IsStatic)
                                info.Invoke(null, null);
                            else
                                foreach (object instance in field.Instances)
                                    info.Invoke(instance, new object[0]);
                        }
                    }
                    catch (Exception e)
                    {
                        if (e is TargetInvocationException)
                            e = ((TargetInvocationException)e).InnerException;

                        Debug.LogError(string.Format("Exception caught while invoking the method named {0} from an inspector button. The exception was \"{1}\" and the stack was \"{2}\"", info.Name, e.Message, e.StackTrace));
                    }
                }
            }
            else if (method.Display == MethodDisplay.Invoke)
            {
                MethodInfo info = field.Info as MethodInfo;

                try
                {
                    if (info.IsStatic)
                        info.Invoke(null, null);
                    else
                        foreach (object instance in field.Instances)
                            info.Invoke(instance, new object[0]);
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        e = ((TargetInvocationException)e).InnerException;

                    Debug.LogError(string.Format("Exception caught while invoking the method named {0} from an inspector draw. The exception was \"{1}\" and the stack was \"{2}\"", info.Name, e.Message, e.StackTrace));
                }
            }

            GUILayout.EndHorizontal();
        }

        private static void DrawToolbar(InspectorEditor editor, InspectorField field)
        {
            if (field.Fields.Count == 0)
                return;

            GUIStyle style = null;
            if (!string.IsNullOrEmpty(field.Style))
                style = GUI.skin.FindStyle(field.Style);

            Color previous = GUI.color;

            EditorGUILayout.Space();

            if (style != null)
                EditorGUILayout.BeginHorizontal(style, GUILayout.MinHeight(18));
            else
                EditorGUILayout.BeginHorizontal();

            foreach (InspectorField child in field.Fields)
            {
                if (!child.Visible || child.Erased)
                    continue;

                child.InitCollection();

                FieldEditor fieldEditor = null;
                PropertyDrawer[] propertyDrawers = field.PropertyDrawers;
                Dictionary<FieldAttribute, FieldEditor> fieldDrawers = GetFieldDrawers(field);

                if (propertyDrawers.Length == 0 && (fieldDrawers == null || fieldDrawers.Count == 0))
                    fieldEditor = child.Editor;

                if (child.Toolbar.Flexible)
                    GUILayout.FlexibleSpace();

                if (child.Label)
                    DrawToolbarLabel(fieldEditor, child);

                DrawField(editor, child, propertyDrawers, fieldDrawers, fieldEditor, child.Style);
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawChildren(InspectorEditor editor, InspectorField field, bool refresh, bool expansion)
        {
            DisplayAsParentAttribute displayAsParent = null;
            if (!field.IsList && !field.IsDictionary)
                displayAsParent = field.GetAttribute<DisplayAsParentAttribute>();

            if (displayAsParent == null)
            {
                if (field.Expanded && field.Fields.Count > 0)
                    Draw(editor, field, field.Fields, true, refresh, expansion);
            }
            else
                Draw(editor, field, field.Fields, false, refresh, expansion);
        }

        /// <summary>
        /// Draw the arrow icon that allow to browse sub-object property.
        /// </summary>
        private static void DrawExpander(InspectorField field, FieldEditor editor, bool expansion)
        {
            if (field.InspectorType == InspectorType.Toolbar && !field.Label)
                return;

            if (expansion)
            {
                DisplayAsParentAttribute displayAsParent = null;
                if (!field.IsList && !field.IsDictionary)
                    displayAsParent = field.GetAttribute<DisplayAsParentAttribute>();

                if (!field.AlwaysExpanded && field.Expandable && (editor == null || editor.IsExpandable(field)) && (field.Group == null || field.Group.Expandable) && displayAsParent == null)
                {
                    if (field.Instances.Length > 0)
                    {
                        object value = field.GetValue(field.Instances[0]);
                        if (value == null && field.InspectorType != InspectorType.Group && !field.IsList && !field.IsDictionary)
                            field.Expanded = false;
                    }

                    if (!field.Similar && field.Type != null)
                    {
                        bool enabled = GUI.enabled;
                        GUI.enabled = false;
                        GUILayout.Label(FolderClose, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT - 4));
                        GUI.enabled = enabled;
                    }
                    else
                    {
                        if (field.Expanded)
                        {
                            if (GUILayout.Button(FolderOpen, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT - 4)) && Event.current.button == 0)
                            {
                                if (InspectorPreferences.IsControl(InspectorPreferences.MassExpand))
                                {
                                    if (field.Fields.Count > 0)
                                    {
                                        bool childExpand = !field.Fields[0].Expanded;
                                        foreach (InspectorField child in field.Fields)
                                            child.Expanded = childExpand;
                                    }
                                }
                                else
                                    field.Expanded = false;
                            }
                        }
                        else
                        {
                            if (GUILayout.Button(FolderClose, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT - 4)) && Event.current.button == 0)
                            {
                                if (InspectorPreferences.IsControl(InspectorPreferences.MassExpand))
                                {
                                    if (field.Fields.Count > 0)
                                    {
                                        bool childExpand = !field.Fields[0].Expanded;
                                        foreach (InspectorField child in field.Fields)
                                            child.Expanded = childExpand;
                                    }
                                }
                                else
                                    field.Expanded = true;
                            }
                        }
                    }
                }
                else
                {
                    if (field.InspectorType == InspectorType.Group)
                        GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
                    else if (field.Depth == 0)
                        if (field.AlwaysExpanded)
                            GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT - 4));
                        else
                            GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT + 4));
                    else
                        GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
                }
            }
            else
                GUILayout.Label(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(0)); // How can this take 2 pixels wide?
        }

        /// <summary>
        /// Draw the control of an sortable list. 
        /// Return true if a list has been modified and requires a redraw.
        /// </summary>
        private static bool DrawIndexedControl(InspectorField field)
        {
            if (field.Index == -1)
                return false;

            CollectionAttribute collection = field.GetAttribute<CollectionAttribute>();

            if (!collectionLock && (collection == null || (collection.Sortable && collection.Display == CollectionDisplay.List)))
            {
                GUILayout.BeginVertical(GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));

                Rect rect = GUILayoutUtility.GetRect(MIN_FIELD_HEIGHT, MIN_FIELD_HEIGHT);
                if (!field.ReadOnly)
                    EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
                else
                    GUI.enabled = false;

                GUI.DrawTexture(rect, Drag);
                if (!field.ReadOnly && Event.current.type == EventType.MouseDown && Event.current.button == 0 && rect.Contains(Event.current.mousePosition))
                {
                    draggedField = field;
                    hoverIndex = field.Index;
                    GUI.FocusControl("");
                    dragState = DragState.Down;
                    EditorWindow.focusedWindow.Repaint();
                }

                GUILayout.EndVertical();

                GUI.enabled = true;
            }
            else
            {
                GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
            }

            if (!collectionLock && (collection == null || (collection.Size == -1 && collection.EnumType == null)))
            {
                GUI.enabled = !field.ReadOnly;

                if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)) && Event.current.button == 0)
                {
                    RemoveItem(field.Parent, field.Index);
                    return true;
                }

                GUI.enabled = true;
            }
            else
            {
                GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
            }

            return false;
        }

        /// <summary>
        /// Draw the - button for a Dictionary
        /// </summary>
        private static bool DrawKeyedControl(InspectorField field)
        {
            if (field.Key == null)
                return false;

            if (!collectionLock)
            {
                if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)) && Event.current.button == 0)
                {
                    RemoveItem(field.Parent, field.Key);
                    return true;
                }
            }
            else
            {
                GUILayout.Button(GUIContent.none, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
            }

            return false;
        }

        /// <summary>
        /// Draw the "Restricted" enum, which is a runtime list provide by a method.
        /// </summary>
        private static void DrawRestricted(InspectorEditor editor, InspectorField field, IList<DescriptionPair> data, GUIStyle style)
        {
            IRestrict restrict = field.GetAttribute<IRestrict>();
            if (restrict == null)
                return;

            RestrictDisplay display = restrict.GetDisplay(field.Instances, field.GetValues());
            if (display == RestrictDisplay.Toolbox)
            {
                GUILayout.BeginHorizontal();

                bool clicked = false;
                if (style != null)
                    clicked = GUILayout.Button(Add, style, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));
                else
                    clicked = GUILayout.Button(Add, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT));

                if (clicked)
                {
                    List<DescriptionPair> pairs = new List<DescriptionPair>();

                    for (int i = 0; i < data.Count; i++)
                    {
                        if (data[i] != null)
                            pairs.Add(data[i]);
                        else
                            pairs.Add(new DescriptionPair(null, new Description("None", "")));
                    }

                    Toolbox.Create(new AdvancedInspectorControl(editor, field, InspectorAction.Set), "Select", pairs, mousePosition);
                }

                if (field.Mixed)
                    GUILayout.Label("---------");
                else
                {
                    object value = field.GetValue();

                    if (value == null)
                        GUILayout.Label("None");
                    else
                        GUILayout.Label(value.ToString());
                }

                GUILayout.EndHorizontal();
            }
            else if (display == RestrictDisplay.Button)
            {
                if (data.Count != 0)
                {
                    int max = restrict.GetItemsPerRow(field.Instances, field.GetValues());
                    if (max < 1)
                        max = 6;

                    int rows = Mathf.CeilToInt((float)data.Count / (float)max);
                    int count = (data.Count / rows);
                    if (count * rows < data.Count)
                        count++;

                    object value = field.GetValue();

                    int selected = -1;
                    for (int i = 0; i < data.Count; i++)
                    {
                        if ((data[i].Value == null && value == null) || (data[i].Value != null && data[i].Value.Equals(value)))
                        {
                            selected = i;
                            break;
                        }
                    }

                    GUIContent[] names = new GUIContent[data.Count];
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (data[i].Value != null)
                            names[i] = new GUIContent(data[i].Description.Name);
                        else
                            names[i] = new GUIContent("None");
                    }

                    EditorGUI.BeginChangeCheck();

                    GUILayout.BeginVertical();

                    for (int i = 0; i < rows; i++)
                    {
                        GUILayout.BeginHorizontal();

                        for (int j = count * i; j < count * (i + 1); j++)
                        {
                            if (j >= data.Count)
                                break;

                            if (style == null)
                            {
                                if (selected == j)
                                    GUILayout.Toggle(true, names[j], EditorStyles.toolbarButton);
                                else if (GUILayout.Toggle(false, names[j], EditorStyles.toolbarButton))
                                    selected = j;
                            }
                            else
                            {
                                if (selected == j)
                                    GUILayout.Toggle(true, names[j], style);
                                else if (GUILayout.Toggle(false, names[j], style))
                                    selected = j;
                            }
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();

                    if (selected != -1 && EditorGUI.EndChangeCheck())
                        field.SetValue(data[selected].Value);
                }
            }
            else if (display == RestrictDisplay.DropDown)
            {
                if (data.Count != 0)
                {
                    object value = field.GetValue();

                    int selected = -1;
                    for (int i = 0; i < data.Count; i++)
                    {
                        if ((data[i].Value == null && value == null) || (data[i].Value != null && data[i].Value.Equals(value)))
                        {
                            selected = i;
                            break;
                        }
                    }

                    GUIContent[] names = new GUIContent[data.Count];
                    for (int i = 0; i < data.Count; i++)
                    {
                        if (data[i].Value != null)
                            names[i] = new GUIContent(data[i].Description.Name);
                        else
                            names[i] = new GUIContent("None");
                    }

                    EditorGUI.BeginChangeCheck();
                    int result = -1;
                    if (style != null)
                        result = EditorGUILayout.Popup(selected, names, style);
                    else
                        result = EditorGUILayout.Popup(selected, names);

                    if (result != -1 && EditorGUI.EndChangeCheck())
                        field.SetValue(data[result].Value);
                }
                else
                {
                    GUIContent[] names = new GUIContent[] { new GUIContent("None") };

                    if (style != null)
                        EditorGUILayout.Popup(0, names, style);
                    else
                        EditorGUILayout.Popup(0, names);
                }
            }
        }

        /// <summary>
        /// Draw the +/- icon to control the creation and deletion of user created object.
        /// </summary>
        private static void DrawDerived(InspectorEditor editor, InspectorField field, GUIStyle style)
        {
            GUILayout.BeginHorizontal();

            bool empty = true;

            for (int i = 0; i < field.Instances.Length; i++)
            {
                if (field.GetValue(field.Instances[i]) != null)
                {
                    empty = false;
                    break;
                }
            }

            if (empty)
            {
                if (GUILayout.Button(Add, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)))
                {
                    List<DescriptionPair> pairs = GetDerived(field.BaseType);

                    if (pairs.Count > 1)
                    {
                        inspectorField = field;
                        Toolbox.Create(new AdvancedInspectorControl(editor, field, InspectorAction.Create), "Create Derived", pairs, mousePosition);
                    }
                    else if (pairs.Count == 1)
                        CreateDerived(field, ((Type)pairs[0].Value));
                }

                GUILayout.Label("None");
            }
            else
            {
                if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)))
                {
                    field.RecordObjects("Delete " + field.Name);

                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        if (typeof(ComponentMonoBehaviour).IsAssignableFrom(field.Type))
                        {
                            ComponentMonoBehaviour component = field.GetValue(field.Instances[i]) as ComponentMonoBehaviour;
                            component.Erase();
                        }
                        else if (typeof(ScriptableComponent).IsAssignableFrom(field.Type))
                        {
                            ScriptableComponent component = field.GetValue(field.Instances[i]) as ScriptableComponent;
                            component.Erase();
                        }

                        field.SetValue(field.Instances[i], null);
                    }

                    AssetDatabase.SaveAssets();
                }

                Type type = null;
                for (int i = 0; i < field.Instances.Length; i++)
                {
                    object value = field.GetValue(field.Instances[i]);

                    if (value == null)
                    {
                        type = null;
                        break;
                    }

                    Type current = value.GetType();
                    if (type == null)
                        type = current;
                    else if (type != current)
                    {
                        type = null;
                        break;
                    }
                }

                if (type == null)
                    GUILayout.Label("---");
                else
                {
                    if (!field.Similar)
                    {
                        GUILayout.Label("---");
                    }
                    else
                    {
                        FieldEditor fieldEditor;
                        if (FieldEditor.FieldEditorByTypes.TryGetValue(type, out fieldEditor) && !FieldEditor.IsDefaultFieldEditor(fieldEditor))
                        {
                            fieldEditor.Draw(field, style);
                        }
                        else
                        {
                            object value = field.GetValue(field.Instances[0]);
                            CreateDerivedAttribute create = field.GetAttribute<CreateDerivedAttribute>();

                            if (value != null)
                            {
                                if (field.OverloadToString)
                                    GUILayout.Label(value.ToString() + (create.HideClassName ? "" : " (" + value.GetType().Name + ")"));
                                else
                                    GUILayout.Label((create.HideClassName ? "" : "(" + value.GetType().Name + ")"));
                            }
                            else
                                GUILayout.Label("None");
                        }
                    }
                }
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the + icon of a List
        /// </summary>
        private static void DrawList(InspectorEditor editor, InspectorField field)
        {
            if (!field.IsList)
                return;

            GUILayout.BeginHorizontal();

            CollectionAttribute collection = field.GetAttribute<CollectionAttribute>();

            if (!collectionLock && (collection == null || (collection.Size == -1 && collection.EnumType == null)))
            {
                if (GUILayout.Button(Add, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)))
                {
                    if (field.CreateDerived)
                    {
                        List<DescriptionPair> pairs = GetDerived(field.BaseType);

                        if (pairs.Count > 1)
                        {
                            inspectorField = field;
                            Toolbox.Create(new AdvancedInspectorControl(editor, field, InspectorAction.Create), "Create Derived", pairs, mousePosition);
                        }
                        else
                        {
                            AddItem(field, pairs[0].Value as Type);
                            field.RefreshFields();
                        }
                    }
                    else
                    {
                        AddItem(field, field.BaseType);
                        field.RefreshFields();
                    }
                }
            }

            if (level > InspectorLevel.Advanced)
            {
                int count = ((IList)field.GetValue(field.Instances[0])).Count;

                bool mixed = false;
                for (int i = 1; i < field.Instances.Length; i++)
                {
                    int items = ((IList)field.GetValue(field.Instances[i])).Count;

                    if (items != count)
                    {
                        if (typeof(Array).IsAssignableFrom(field.Type))
                            GUILayout.Label("Array of " + field.BaseType.Name + "[ - ]");
                        else
                            GUILayout.Label("List of " + field.BaseType.Name + "[ - ]");

                        mixed = true;
                        break;
                    }
                }

                if (!mixed)
                {
                    if (count != field.Fields.Count)
                        field.RefreshFields();

                    if (typeof(Array).IsAssignableFrom(field.Type))
                        GUILayout.Label("Array of " + field.BaseType.Name + "[" + count.ToString() + "]");
                    else
                        GUILayout.Label("List of " + field.BaseType.Name + "[" + count.ToString() + "]");
                }
            }
            else if (collection != null && collection.Display != CollectionDisplay.List)
            {
                if (field.Fields.Count == 0)
                    field.RefreshFields(true);

                int selected = field.Fields[0].Index;

                IList list = (IList)field.GetValue();
                List<string> names = new List<string>();
                for (int i = 0; i < list.Count; i++)
                {
                    object o = list[i];
                    if (collection.Delegates.Count > 0)
                        names.Add(collection.Invoke(0, field.Instances[0], field.GetValue())[i]);
                    else if (o == null)
                        names.Add("[" + i.ToString() + "] Null");
                    else if (field.OverloadToString)
                        names.Add("[" + i.ToString() + "] " + o.ToString());
                    else
                        names.Add("[" + i.ToString() + "]");
                }

                if (selected >= names.Count)
                    selected = names.Count - 1;

                if (collection.Display == CollectionDisplay.DropDown)
                {
                    selected = EditorGUILayout.Popup(selected, names.ToArray());

                    if (!collectionLock)
                        if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Width(18)))
                            RemoveItem(field, selected);
                }
                else if (collection.Display == CollectionDisplay.Button)
                {
                    int max = collection.MaxItemsPerRow;
                    if (max < 1)
                        max = 6;

                    int rows = Mathf.CeilToInt((float)names.Count / max);
                    if (rows < 1)
                        rows = 1;

                    int count = (names.Count / rows);
                    if (count * rows < names.Count)
                        count++;

                    GUILayout.BeginVertical();

                    for (int i = 0; i < rows; i++)
                    {
                        GUILayout.BeginHorizontal();

                        for (int j = count * i; j < count * (i + 1); j++)
                        {
                            if (j >= names.Count)
                                break;

                            if (selected == j)
                                GUILayout.Toggle(true, names[j], EditorStyles.toolbarButton, GUILayout.ExpandWidth(true));
                            else if (GUILayout.Toggle(false, names[j], EditorStyles.toolbarButton, GUILayout.ExpandWidth(true)))
                                selected = j;
                        }

                        GUILayout.EndHorizontal();
                    }

                    GUILayout.EndVertical();

                    if (!collectionLock)
                        if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Width(18)))
                            RemoveItem(field, selected);
                }

                field.Fields[0].Index = selected;
            }

            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw the + icon of a Dictionary
        /// </summary>
        private static void DrawDictionary(InspectorEditor editor, InspectorField field)
        {
            if (!field.IsDictionary)
                return;

            IDictionary dict = field.GetValue() as IDictionary;
            if (dict == null)
                return;

            Type type = field.KeyType;
            InspectorField keyField;
            if (field.InternalFields.Count == 0)
            {
                keyField = new InspectorField(type);
                field.InternalFields.Add(keyField);
            }
            else
                keyField = field.InternalFields[0];

            bool enabled = GUI.enabled;

            FieldEditor fieldEditor;
            FieldEditor.FieldEditorByTypes.TryGetValue(type, out fieldEditor);
            if (fieldEditor == null)
                GUI.enabled = false;

            EditorGUILayout.BeginHorizontal();

            object newKey = keyField.GetValue();
            if (newKey == null || 
                (newKey is string && string.IsNullOrEmpty((string)newKey)) ||
                (newKey is UnityEngine.Object && !(UnityEngine.Object)newKey) ||
                dict.Contains(newKey))
                GUI.enabled = false;

            if (GUILayout.Button(Add, GUIStyle.none, GUILayout.Height(MIN_FIELD_HEIGHT), GUILayout.Width(MIN_FIELD_HEIGHT)))
            {
                if (field.CreateDerived)
                {
                    List<DescriptionPair> pairs = GetDerived(field.BaseType);

                    if (pairs.Count > 1)
                    {
                        inspectorField = field;
                        dictionaryKey = newKey;
                        Toolbox.Create(new AdvancedInspectorControl(editor, field, InspectorAction.Create), "Create Derived", pairs, mousePosition);
                    }
                    else
                    {
                        AddItem(field, pairs[0].Value as Type, newKey);
                        keyField.ResetToDefault();
                        field.RefreshFields();
                    }
                }
                else
                {
                    AddItem(field, field.BaseType, newKey);
                    keyField.ResetToDefault();
                    field.RefreshFields();
                }
            }

            GUI.enabled = enabled;

            fieldEditor.Draw(keyField, null);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            CollectionAttribute collection = field.GetAttribute<CollectionAttribute>();
            if (collection != null)
            {
                object key = null;
                object selected = null;

                List<string> names = new List<string>();
                List<object> keys = new List<object>();
                foreach (object k in dict.Keys)
                {
                    keys.Add(k);
                    if (k == null)
                        names.Add("[Null]");
                    else
                        names.Add("[" + k.ToString() + "]");
                }

                if (keys.Count == 0)
                    key = null;
                else if (!keys.Contains(key))
                    key = keys[0];
                else
                    key = keys[0];

                int index = keys.IndexOf(key);

                if (collection.Display == CollectionDisplay.DropDown)
                {
                    index = EditorGUILayout.Popup(index, names.ToArray());

                    if (GUILayout.Button(Delete, GUIStyle.none, GUILayout.Width(18)))
                        RemoveItem(field, selected);
                }

                if (index != -1)
                    field.Fields[0].Key = keys[index];
            }

            EditorGUILayout.EndHorizontal();
        }

        private static void DrawToolbarLabel(FieldEditor fieldEditor, InspectorField field)
        {
            GUIContent content;
            Description description = field.Description;
            if (description != null)
                content = new GUIContent((string.IsNullOrEmpty(description.Name)) ? field.Name : description.Name, description.Comment);
            else
                content = new GUIContent(field.Name);

            bool enabled = GUI.enabled;
            GUI.enabled = true;

            if (GUILayout.Button(content, GUI.skin.label))
            {
                if (Event.current.button == 1 || (Event.current.button == 0 && InspectorPreferences.IsControl(InspectorPreferences.Contextual)))
                    InvokeContextual(fieldEditor, field, false);
                else if (fieldEditor != null)
                {
                    if ((EditorApplication.timeSinceStartup - clickTime) < DOUBLE_CLICK_TIME)
                        fieldEditor.OnLabelDoubleClick(field);
                    else
                        fieldEditor.OnLabelClick(field);

                    clickTime = EditorApplication.timeSinceStartup;
                }
            }

            GUI.enabled = enabled;
        }

        /// <summary>
        /// Draw the left side of a field, the label.
        /// </summary>
        private static void DrawLabel(FieldEditor fieldEditor, InspectorField field, bool expansion)
        {
            Color previousColor = GUI.skin.label.normal.textColor;
            if (field.Animated)
                GUI.skin.label.normal.textColor = animatedLabelColor;
            else if (EditorGUIUtility.isProSkin)
                GUI.skin.label.normal.textColor = proLabelColor;
            else
                GUI.skin.label.normal.textColor = Color.black;

            GUIContent content;
            Description description = field.Description;
            if (description != null)
                content = new GUIContent(string.IsNullOrEmpty(description.Name) ? field.Name : description.Name, description.Icon, description.Comment);
            else
                content = new GUIContent(field.Name);

            int width = GetWidth(field);

            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            bool modified = field.Modified;
            if (modified)
                GUI.skin.label.fontStyle = FontStyle.Bold;
            else
                GUI.skin.label.fontStyle = FontStyle.Normal;

            bool enabled = GUI.enabled;
            GUI.enabled = true;

            if (EditorApplication.isPlaying && InspectorPersist.Contains(field))
                GUI.skin.label.fontStyle = FontStyle.Italic;

            GUILayout.Label(content, GUI.skin.label, GUILayout.Width(width));

            Vector2 mousePosition = Event.current.mousePosition;
            Rect labelRect = GUILayoutUtility.GetLastRect();

            if (Event.current.type == EventType.Repaint)
            {
                if (fieldEditor != null)
                    fieldEditor.OnLabelDraw(field, labelRect);
            }
            else if (Event.current.type == EventType.MouseDown && labelRect.Contains(mousePosition))
            {
                selectedLabel = field;

                if (Event.current.button == 1 || (Event.current.button == 0 && InspectorPreferences.IsControl(InspectorPreferences.Contextual)))
                    InvokeContextual(fieldEditor, field, modified);
                else
                {
                    if ((EditorApplication.timeSinceStartup - clickTime) < DOUBLE_CLICK_TIME)
                    {
                        if (fieldEditor != null)
                            fieldEditor.OnLabelDoubleClick(field);

                        if (field.Expandable)
                        {
                            if (InspectorPreferences.IsControl(InspectorPreferences.MassExpand))
                            {
                                if (field.Fields.Count > 0)
                                {
                                    bool childExpand = !field.Fields[0].Expanded;
                                    foreach (InspectorField child in field.Fields)
                                        child.Expanded = childExpand;
                                }
                            }
                            else
                                field.Expanded = !field.Expanded;

                            Event.current.Use();
                        }
                    }
                    else
                    {
                        if (fieldEditor != null)
                            fieldEditor.OnLabelClick(field);
                    }

                    clickTime = EditorApplication.timeSinceStartup;
                }
            }
            else if (Event.current.type == EventType.MouseDrag && selectedLabel == field)
            {
                if (InspectorPreferences.IsDragControl(InspectorPreferences.CopyPaste) && labelRect.Contains(mousePosition) && !field.Mixed)
                {
                    DragAndDrop.PrepareStartDrag();

                    object value = field.GetValue();
                    DragDropWrapper wrapper = new DragDropWrapper(value != null ? value.GetType().Name : "", value);
                    DragAndDrop.objectReferences = new UnityEngine.Object[] { value as UnityEngine.Object };
                    DragAndDrop.SetGenericData("AdvancedInspector", wrapper);

                    field.Copy();
                    DragAndDrop.StartDrag("AdvancedInspector");

                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                }
                else
                {
                    if (fieldEditor != null)
                        fieldEditor.OnLabelDragged(field);
                }

                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragUpdated && labelRect.Contains(mousePosition))
            {
                if (!field.ReadOnly && field.Type != null)
                {
                    bool valid = false;
                    object value = null;
                    DragDropWrapper wrapper = DragAndDrop.GetGenericData("AdvancedInspector") as DragDropWrapper;

                    if (wrapper == null)
                    {
                        if (DragAndDrop.objectReferences.Length == 1)
                            value = DragAndDrop.objectReferences[0];
                        else
                            value = DragAndDrop.objectReferences;
                    }
                    else
                        value = wrapper.Data;

                    if (field.CanPaste(value) && Clipboard.CanConvert(field.Type, value))
                        valid = true;
                    else if (value is Texture2D && typeof(Sprite).IsAssignableFrom(field.Type))
                    {
                        Sprite[] sprites = TextureToSprites(value as Texture2D);
                        valid = sprites.Length > 0;
                    }
                    else if (field.IsList)
                    {
                        foreach (object o in GetDraggedObjects(wrapper))
                        {
                            if (o != null && field.BaseType.IsAssignableFrom(o.GetType()))
                            {
                                valid = true;
                                break;
                            }
                            else if (o is GameObject && !typeof(ComponentMonoBehaviour).IsAssignableFrom(field.BaseType) &&
                                typeof(Component).IsAssignableFrom(field.BaseType) && ((GameObject)o).GetComponent(field.BaseType) != null)
                            {
                                valid = true;
                                break;
                            }
                            else if (o is Texture2D && typeof(Sprite).IsAssignableFrom(field.BaseType))
                            {
                                Sprite[] sprites = TextureToSprites(o as Texture2D);
                                if (sprites.Length > 0)
                                {
                                    valid = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (valid)
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    else
                        DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                }
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;

                Event.current.Use();
            }
            else if (Event.current.type == EventType.DragPerform && labelRect.Contains(mousePosition))
            {
                if (!field.ReadOnly && field.Type != null)
                {
                    object value = null;
                    DragDropWrapper wrapper = DragAndDrop.GetGenericData("AdvancedInspector") as DragDropWrapper;
                    if (wrapper == null)
                    {
                        if (DragAndDrop.objectReferences.Length == 1)
                            value = DragAndDrop.objectReferences[0];
                        else
                            value = DragAndDrop.objectReferences;
                    }
                    else
                        value = wrapper.Data;

                    if (Clipboard.CanConvert(field.Type, value))
                        field.Paste();
                    else if (value is Texture2D && typeof(Sprite).IsAssignableFrom(field.Type))
                    {
                        Sprite[] sprites = TextureToSprites(value as Texture2D);
                        if (sprites.Length > 0)
                            field.SetValue(sprites[0]);
                    }
                    else if (field.IsList)
                    {
                        field.RecordObjects("Add Item to Collection " + field.Name);

                        IList[] lists = field.GetValues<IList>();
                        foreach (object o in GetDraggedObjects(wrapper))
                        {
                            if (o == null)
                                continue;

                            List<object> items = new List<object>() { o };

                            if (items[0] is GameObject && !field.BaseType.IsAssignableFrom(typeof(GameObject)) &&
                                !typeof(ComponentMonoBehaviour).IsAssignableFrom(field.BaseType) && typeof(Component).IsAssignableFrom(field.BaseType))
                                items[0] = ((GameObject)items[0]).GetComponent(field.BaseType);

                            if (items[0] is Texture2D && typeof(Sprite).IsAssignableFrom(field.BaseType))
                                items = new List<object>(TextureToSprites(items[0] as Texture2D));

                            if (items[0] == null)
                                continue;

                            for (int i = 0; i < items.Count; i++)
                            {
                                for (int j = 0; j < lists.Length; j++)
                                {
                                    IList list = lists[j];
                                    int index = list.IndexOf(null);
                                    if (index != -1)
                                        list[index] = items[i];
                                    else
                                    {
                                        if (list is Array)
                                        {
                                            Array source = list as Array;
                                            Array dest = Array.CreateInstance(source.GetType().GetElementType(), source.Length + 1);

                                            source.CopyTo(dest, 0);

                                            dest.SetValue(items[i], dest.Length - 1);

                                            lists[j] = dest;
                                        }
                                        else
                                            list.Add(items[i]);
                                    }
                                }
                            }
                        }

                        for (int i = 0; i < field.Instances.Length; i++)
                            field.SetValue(field.Instances[i], lists[i]);
                    }
                }

                field.RefreshFields();
                DragAndDrop.AcceptDrag();

                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                selectedLabel = null;
                EditorGUIUtility.SetWantsMouseJumping(0);
            }
            
            GUI.color = Color.white;
            GUI.enabled = enabled;
            GUI.skin.label.normal.textColor = previousColor;

            // Offset control section by the size of the separator... roughly
            GUILayout.BeginHorizontal(GUILayout.Width(4));
            GUILayout.EndHorizontal();
        }

        private static object[] GetDraggedObjects(DragDropWrapper wrapper)
        {
            if (wrapper != null)
                return new object[] { wrapper.Data };

            UnityEngine.Object[] collection = new UnityEngine.Object[0];
            if (DragAndDrop.objectReferences.Length != 0 && DragAndDrop.objectReferences[0].GetType() != typeof(UnityEngine.Object))
            {
                collection = DragAndDrop.objectReferences;
            }
            else if (DragAndDrop.paths.Length != 0 || !string.IsNullOrEmpty(DragAndDrop.paths[0]))
            {
                string path = DragAndDrop.paths[0];
                DirectoryInfo directory = new DirectoryInfo(Application.dataPath.Substring(0, Application.dataPath.Length - 6) + path);

                List<UnityEngine.Object> list = new List<UnityEngine.Object>();
                foreach (FileInfo file in directory.GetFiles())
                    list.AddRange(AssetDatabase.LoadAllAssetsAtPath(path + "/" + file.Name));

                collection = list.ToArray();
            }

            return collection.OrderBy(x => x.name).ToArray();
        }

        private static Sprite[] TextureToSprites(Texture2D texture)
        {
            UnityEngine.Object[] array = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(texture));
            List<Sprite> list = new List<Sprite>();
            for (int i = 0; i < array.Length; i++)
                if (array[i].GetType() == typeof(Sprite))
                    list.Add(array[i] as Sprite);

            return list.ToArray();
        }

        /// <summary>
        /// Width of the label part.
        /// </summary>
        private static int GetWidth(InspectorField field)
        {
            int width = (int)offset - (field.Depth * (2 + InspectorPreferences.ExtraIndentation)) - 6 - (field.Depth * 2);
            if (field.Index != -1)
                width -= MIN_FIELD_HEIGHT * 2;
            else if (field.Key != null)
                width -= MIN_FIELD_HEIGHT;

            return width;
        }

        /// <summary>
        /// Draw the help box bellow the field.
        /// </summary>
        private static void DrawHelp(InspectorField field, bool after)
        {
            bool previous = GUI.enabled;
            GUI.enabled = true;

            foreach (IHelp help in field.GetAttributes<IHelp>())
            {
                if (help == null)
                    continue;

                foreach (HelpItem item in help.GetHelp(field.Instances, field.GetValues()))
                {
                    if (item == null || string.IsNullOrEmpty(item.Message))
                        continue;

                    if (item.Position == HelpPosition.After && !after ||
                        item.Position == HelpPosition.Before && after)
                        continue;

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.HelpBox(item.Message, (MessageType)item.Type);
                    EditorGUILayout.EndHorizontal();
                }
            }

            GUI.enabled = previous;
        }

        /// <summary>
        /// Help box at the top or bottom of the whole inspector.
        /// </summary>
        private static void DrawClassHelp(object[] instances, bool after)
        {
            if (instances.Length != 1 || instances[0] == null)
                return;

            Type type = instances[0].GetType();
            HelpAttribute[] helpAttributes = null;
            if (helpAttributesByType.ContainsKey(type))
                helpAttributes = helpAttributesByType[type];
            else
            {
                object[] attributes = type.GetCustomAttributes(typeof(HelpAttribute), true);
                if (attributes.Length > 0)
                {
                    helpAttributes = new HelpAttribute[attributes.Length];
                    for (int i = 0; i < attributes.Length; i++)
                        helpAttributes[i] = attributes[i] as HelpAttribute;

                    helpAttributesByType.Add(type, helpAttributes);
                }
                else
                    helpAttributesByType.Add(type, null);
            }

            if (helpAttributes == null || helpAttributes.Length == 0)
                return;

            foreach (object helpObject in helpAttributes)
            {
                HelpAttribute help = helpObject as HelpAttribute;
                if (after && help.Position == HelpPosition.Before ||
                    !after && help.Position == HelpPosition.After)
                    continue;

                try
                {
                    if (!string.IsNullOrEmpty(help.MethodName) && help.Delegates.Count == 0)
                        ParseRuntimeAttributes(helpObject as IRuntimeAttribute, type, instances);

                    if (help.Delegates.Count != 0)
                        help = help.Delegates[0].DynamicInvoke() as HelpAttribute;
                }
                catch (Exception e)
                {
                    if (e is TargetInvocationException)
                        e = ((TargetInvocationException)e).InnerException;

                    Debug.LogError(string.Format("Invoking a method while trying to retrieve a Help attribute failed. The exception was \"{0}\" and the stack was \"{1}\"", e.Message, e.StackTrace));
                }

                EditorGUILayout.HelpBox(help.Message, (MessageType)help.Type);
            }
        }
        #endregion

        #region Contextual
        private static bool IsPartOfCollection(InspectorField field)
        {
            if (field.Index != -1 || field.Key != null)
                return true;

            if (field.Parent != null)
                return IsPartOfCollection(field.Parent);

            return false;
        }

        private static void InvokeContextual(FieldEditor editor, InspectorField field, bool modified)
        {
            inspectorField = field;

            object value = field.GetValue();
            GenericMenu menu = new GenericMenu();

            if (field.Type != null)
            {
                if (editor != null)
                    editor.OnContextualClick(field, menu);

                IMenu[] menuItems = field.GetAttributes<IMenu>();
                for (int i = 0; i < menuItems.Length; i++)
                {
                    int lambda = i;
                    if (menuItems[i].Enabled)
                        menu.AddItem(new GUIContent(menuItems[i].MenuItemName), menuItems[i].IsOn, () => DelegateInvoker(field, menuItems[lambda]));
                    else
                        menu.AddDisabledItem(new GUIContent(menuItems[i].MenuItemName));
                }

                if (menuItems.Length > 0)
                    menu.AddSeparator("");

                if (field.Type != null && !field.Mixed)
                    menu.AddItem(new GUIContent("Copy"), false, field.Copy);
                else
                    menu.AddDisabledItem(new GUIContent("Copy"));

                object data;
                if (!field.ReadOnly && Clipboard.TryConvertion(field.Type, out data) && field.CanPaste(data))
                    menu.AddItem(new GUIContent("Paste"), false, field.Paste);
                else
                    menu.AddDisabledItem(new GUIContent("Paste"));

                menu.AddSeparator("");
            }

            if (field.Prefab != null)
            {
                if (!field.ReadOnly && modified)
                {
                    menu.AddItem(new GUIContent("Apply"), false, field.Apply);
                    menu.AddItem(new GUIContent("Revert"), false, field.Revert);
                }
                else
                {
                    menu.AddDisabledItem(new GUIContent("Apply"));
                    menu.AddDisabledItem(new GUIContent("Revert"));
                }

                menu.AddSeparator("");
            }

            if ((field.InspectorType == InspectorType.Field || field.InspectorType == InspectorType.Property || field.InspectorType == InspectorType.Method) &&
                (field.Index == -1 && field.Key == null))
            {
                menu.AddItem(new GUIContent("Watch"), WatchWindow.Contains(field), field.Watch);

                if (watched)
                    menu.AddItem(new GUIContent("Select"), false, field.Select);

                menu.AddSeparator("");
            }

            if (EditorApplication.isPlaying)
            { 
                menu.AddItem(new GUIContent("Save"), InspectorPersist.Contains(field), field.Save);

                menu.AddSeparator("");
            }

            if (field.Type != null)
            {
                if (typeof(UnityEngine.Object).IsAssignableFrom(field.Type))
                {
                    if (value != null && (UnityEngine.Object)value)
                    {
                        menu.AddItem(new GUIContent("Clear"), false, ClearItem);
                        menu.AddSeparator("");
                    }

                    if (value != null && ((UnityEngine.Object)value))
                    {
                        if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(value as UnityEngine.Object)))
                            menu.AddItem(new GUIContent("Show in Project"), false, ShowInProject);
                        else
                            menu.AddItem(new GUIContent("Show in Scene"), false, ShowInScene);
                        menu.AddSeparator("");
                    }
                }
                else if (typeof(IList).IsAssignableFrom(field.Type))
                {
                    menu.AddItem(new GUIContent("Clear Array"), false, ClearArray);
                    menu.AddSeparator("");
                }

                if (field.Index != -1 && !collectionLock)
                {
                    CollectionAttribute collection = field.GetAttribute<CollectionAttribute>();
                    if (collection == null || collection.Size == -1)
                    {
                        if (!field.ReadOnly)
                        {
                            menu.AddItem(new GUIContent("Insert"), false, InsertItem);
                            menu.AddItem(new GUIContent("Remove"), false, RemoveItem);
                        }
                        else
                        {
                            menu.AddDisabledItem(new GUIContent("Insert"));
                            menu.AddDisabledItem(new GUIContent("Remove"));
                        }

                        menu.AddSeparator("");
                    }

                    if (!field.ReadOnly)
                    {
                        menu.AddItem(new GUIContent("Move to Top"), false, MoveItemTop);
                        menu.AddItem(new GUIContent("Move Up"), false, MoveItemUp);
                        menu.AddItem(new GUIContent("Move Down"), false, MoveItemDown);
                        menu.AddItem(new GUIContent("Move to Bottom"), false, MoveItemBottom);
                    }
                    else
                    {
                        menu.AddDisabledItem(new GUIContent("Move to Top"));
                        menu.AddDisabledItem(new GUIContent("Move Up"));
                        menu.AddDisabledItem(new GUIContent("Move Down"));
                        menu.AddDisabledItem(new GUIContent("Move to Bottom"));
                    }

                    menu.AddSeparator("");
                }
            }

            menu.AddItem(new GUIContent("Basic"), level == InspectorLevel.Basic, SetLevelBasic);
            menu.AddItem(new GUIContent("Advanced"), level == InspectorLevel.Advanced, SetLevelAdvanced);
            menu.AddItem(new GUIContent("Debug"), level == InspectorLevel.Debug, SetLevelDebug);

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Collection Locked"), collectionLock, LockCollection);

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("No Sorting"), sorting == InspectorSorting.None, SetNoSorting);
            menu.AddItem(new GUIContent("Alphabetic"), sorting == InspectorSorting.Alpha, SetAlphaSorting);
            menu.AddItem(new GUIContent("Counter Alphabeting"), sorting == InspectorSorting.AntiAlpha, SetAntiAlphaSorting);

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Object Preview"), showIconPreview, SetIconPreview);
            menu.AddItem(new GUIContent("Preview Size/Smaller"), iconPreviewSize == IconPreviewSize.Smallest, SetIconPreviewSizeSmallest);
            menu.AddItem(new GUIContent("Preview Size/Small"), iconPreviewSize == IconPreviewSize.Small, SetIconPreviewSizeSmall);
            menu.AddItem(new GUIContent("Preview Size/Normal"), iconPreviewSize == IconPreviewSize.Normal, SetIconPreviewSizeNormal);
            menu.AddItem(new GUIContent("Preview Size/Large"), iconPreviewSize == IconPreviewSize.Large, SetIconPreviewSizeLarge);
            menu.AddItem(new GUIContent("Preview Size/Larger"), iconPreviewSize == IconPreviewSize.Largest, SetIconPreviewSizeLargest);

            Description description = field.Description;
            if (description != null && !string.IsNullOrEmpty(description.URL))
            {
                menu.AddSeparator("");
                menu.AddItem(new GUIContent("Online Help"), false, InvokeHelp, description.URL);
            }

            menu.ShowAsContext();
        }

        private static void DelegateInvoker(InspectorField field, IMenu menuItem)
        {
            try
            {
                for (int i = 0; i < menuItem.Delegates.Count; i++)
                    menuItem.Invoke(i, field.Instances[i], field.GetValues()[i]);
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    e = ((TargetInvocationException)e).InnerException;

                Debug.LogError(string.Format("Invoking a method from a MenuItem attribute failed. The exception was \"{0}\".", e.Message));
            }
        }

        private static void SetLevelBasic()
        {
            level = InspectorLevel.Basic;
            EditorPrefs.SetInt(InspectorLevelKey, (int)level);
        }

        private static void SetLevelAdvanced()
        {
            level = InspectorLevel.Advanced;
            EditorPrefs.SetInt(InspectorLevelKey, (int)level);
        }

        private static void SetLevelDebug()
        {
            level = InspectorLevel.Debug;
            EditorPrefs.SetInt(InspectorLevelKey, (int)level);
        }

        private static void LockCollection()
        {
            collectionLock = !collectionLock;
            EditorPrefs.SetBool(InspectorCollectionLockKey, collectionLock);
        }

        private static void SetNoSorting()
        {
            Sorting = InspectorSorting.None;
            EditorPrefs.SetInt(InspectorSortKey, (int)Sorting);
        }

        private static void SetAlphaSorting()
        {
            Sorting = InspectorSorting.Alpha;
            EditorPrefs.SetInt(InspectorSortKey, (int)Sorting);
        }

        private static void SetAntiAlphaSorting()
        {
            Sorting = InspectorSorting.AntiAlpha;
            EditorPrefs.SetInt(InspectorSortKey, (int)Sorting);
        }

        private static void SetIconPreview()
        {
            showIconPreview = !showIconPreview;
            EditorPrefs.SetBool(InspectorIconPreviewShowKey, showIconPreview);
        }

        private static void SetIconPreviewSizeSmallest()
        {
            iconPreviewSize = IconPreviewSize.Smallest;
            EditorPrefs.SetInt(InspectorIconPreviewSizeKey, (int)iconPreviewSize);
        }

        private static void SetIconPreviewSizeSmall()
        {
            iconPreviewSize = IconPreviewSize.Small;
            EditorPrefs.SetInt(InspectorIconPreviewSizeKey, (int)iconPreviewSize);
        }

        private static void SetIconPreviewSizeNormal()
        {
            iconPreviewSize = IconPreviewSize.Normal;
            EditorPrefs.SetInt(InspectorIconPreviewSizeKey, (int)iconPreviewSize);
        }

        private static void SetIconPreviewSizeLarge()
        {
            iconPreviewSize = IconPreviewSize.Large;
            EditorPrefs.SetInt(InspectorIconPreviewSizeKey, (int)iconPreviewSize);
        }

        private static void SetIconPreviewSizeLargest()
        {
            iconPreviewSize = IconPreviewSize.Largest;
            EditorPrefs.SetInt(InspectorIconPreviewSizeKey, (int)iconPreviewSize);
        }

        private static void InvokeHelp(object url)
        {
            Application.OpenURL((string)url);
        }

        private static void ClearItem()
        {
            Field.RecordObjects("Clear item " + Field.Name);

            Field.SetValue(null);
        }

        private static void ClearArray()
        {
            Field.RecordObjects("Clear collection " + Field.Name);

            for (int i = 0; i < Field.Instances.Length; i++)
            {
                IList list = Field.GetValue(Field.Instances[i]) as IList;

                if (list == null || list.Count == 0)
                    return;

                if (typeof(ComponentMonoBehaviour).IsAssignableFrom(Field.BaseType))
                {
                    foreach (ComponentMonoBehaviour component in list)
                    {
                        if (component == null)
                            continue;

                        component.Erase();
                    }
                }

                if (Field.Type.IsArray)
                    Field.SetValue(Field.Instances[i], Array.CreateInstance(Field.BaseType, 0));
                else
                    list.Clear();
            }

            Field.SetDirty();
            Field.RefreshFields();
        }

        private static void ShowInScene()
        {
            UnityEngine.Object target = Field.GetValue() as UnityEngine.Object;

            if (target == null)
                return;

            SceneView view = SceneView.lastActiveSceneView;
            Quaternion rotation = view.camera.transform.rotation;

            if (target is GameObject)
                SceneView.lastActiveSceneView.LookAt(((GameObject)target).transform.position, rotation, 10);
            else if (target is Component)
                SceneView.lastActiveSceneView.LookAt(((Component)target).transform.position, rotation, 10);
        }

        private static void ShowInProject()
        {
            UnityEngine.Object target = Field.GetValue() as UnityEngine.Object;
            if (target != null)
                EditorGUIUtility.PingObject(target);
        }

        private static void AddItem(InspectorField field, Type type)
        {
            field.RecordObjects("Add item in " + field.Name);

            if (field.InspectorType == InspectorType.Serialized)
            {
                field.SerializedProperty.InsertArrayElementAtIndex(field.SerializedProperty.arraySize);
                field.SerializedProperty.serializedObject.ApplyModifiedProperties();
                field.RefreshFields();
                return;
            }

            object[] values = new object[field.Instances.Length];
            ConstructorAttribute constructor = field.GetAttribute<ConstructorAttribute>();
            for (int i = 0; i < field.Instances.Length; i++)
            {
                if (constructor == null || constructor.Delegates.Count == 0)
                {
                    if (field.SerializedInstances != null && field.SerializedInstances.Length > i)
                        values[i] = Clipboard.CreateInstance(type, field.SerializedInstances[i] as MonoBehaviour);
                    else
                        values[i] = Clipboard.CreateInstance(type);
                }
                else
                    values[i] = constructor.Invoke(i, field.Instances[i], field.GetValue(field.Instances[i]));
            }

            for (int i = 0; i < field.Instances.Length; i++)
            {
                object value = values[i];

                IList list = field.GetValue(field.Instances[i]) as IList;

                if (list is Array)
                {
                    Array source = list as Array;
                    Array dest = Array.CreateInstance(source.GetType().GetElementType(), source.Length + 1);

                    source.CopyTo(dest, 0);

                    dest.SetValue(value, dest.Length - 1);

                    field.SetValue(field.Instances[i], dest);
                }
                else
                    list.Add(value);
            }

            field.SetDirty();
            field.RefreshFields();
        }

        private static void AddItem(InspectorField field, Type type, object key)
        {
            field.RecordObjects("Add item in " + field.Name + " with key " + key.ToString());

            object[] values = new object[field.Instances.Length];
            ConstructorAttribute constructor = field.GetAttribute<ConstructorAttribute>();
            for (int i = 0; i < field.Instances.Length; i++)
            {
                if (constructor == null || constructor.Delegates.Count == 0)
                    values[i] = Clipboard.CreateInstance(type, field.SerializedInstances[i] as MonoBehaviour);
                else
                    values[i] = constructor.Invoke(i, field.Instances[i], field.GetValue(field.Instances[i]));
            }

            for (int i = 0; i < field.Instances.Length; i++)
            {
                object value = values[i];
                IDictionary dict = field.GetValue(field.Instances[i]) as IDictionary;
                dict.Add(key, value);
            }

            field.SetDirty();
            field.RefreshFields();
        }

        private static void InsertItem()
        {
            if (inspectorField == null)
                return;

            InsertItem(inspectorField, inspectorField.BaseType, inspectorField.Index + 1);
        }

        private static void InsertItem(InspectorField field, Type type, int index)
        {
            field.RecordObjects("Insert item in " + field.Name + " at index " + index.ToString());

            for (int i = 0; i < field.Instances.Length; i++)
            {
                IList list = field.Instances[i] as IList;
                if (list == null)
                    continue;

                if (list is Array)
                {
                    Array source = list as Array;
                    type = source.GetType().GetElementType();
                    Array dest = Array.CreateInstance(type, source.Length + 1);

                    for (int j = 0; j < dest.Length; j++)
                    {
                        if (j == index)
                            dest.SetValue(Clipboard.CreateInstance(type, field.SerializedInstances[i] as MonoBehaviour), j);

                        if (j < index)
                            dest.SetValue(source.GetValue(j), j);

                        if (j + 1 < dest.Length)
                            dest.SetValue(source.GetValue(j), j + 1);
                    }

                    field.Parent.SetValue(field.Parent.Instances[i], dest);
                }
                else
                {
                    Type[] genericType = list.GetType().GetGenericArguments();
                    if (genericType.Length != 0)
                        type = genericType[0];

                    list.Insert(index, Clipboard.CreateInstance(type, field.SerializedInstances[i] as MonoBehaviour));
                }
            }

            field.SetDirty();
            if (field.Parent != null)
                field.Parent.RefreshFields();
        }

        private static void RemoveItem()
        {
            if (inspectorField == null)
                return;

            RemoveItem(inspectorField.Parent, inspectorField.Index);
        }

        private static void RemoveItem(InspectorField field, int index)
        {
            field.RecordObjects("Remove item in " + field.Name + " at index " + index.ToString());

            if (field.InspectorType == InspectorType.Serialized)
            {
                field.SerializedProperty.DeleteArrayElementAtIndex(index);
                field.SerializedProperty.serializedObject.ApplyModifiedProperties();
                field.RefreshFields();

                return;
            }

            for (int i = 0; i < field.Instances.Length; i++)
            {
                IList list = field.GetValue(field.Instances[i]) as IList;
                if (list == null || list.Count == 0)
                    continue;

                object item = list[index];

                Type type = null;
                if (list is Array)
                {
                    Array source = list as Array;
                    type = source.GetType().GetElementType();
                    Array dest = Array.CreateInstance(type, source.Length - 1);

                    if (index > 0)
                        Array.Copy(source, 0, dest, 0, index);

                    if (index < source.Length - 1)
                        Array.Copy(source, index + 1, dest, index, source.Length - index - 1);

                    field.SetValue(field.Instances[i], dest);
                }
                else
                    list.RemoveAt(index);

                if (item is ComponentMonoBehaviour)
                    ((ComponentMonoBehaviour)item).Erase();
            }

            field.SetDirty();
            field.RefreshFields();
        }

        private static void RemoveItem(InspectorField field, object key)
        {
            field.RecordObjects("Remove item in " + field.Name + " at key " + key.ToString());

            for (int i = 0; i < field.Instances.Length; i++)
            {
                IDictionary dict = field.GetValue(field.Instances[i]) as IDictionary;
                if (dict == null)
                    continue;

                object item = dict[key];

                dict.Remove(key);

                if (item is ComponentMonoBehaviour)
                    ((ComponentMonoBehaviour)item).Erase();
            }

            field.SetDirty();
            field.RefreshFields();
        }

        private static void MoveItemToIndex(InspectorField field, int index)
        {
            field.RecordObjects("Move item in " + field.Parent.Name + " to index " + index.ToString());

            int originalIndex = field.Index;
            if (originalIndex == -1 || index == originalIndex)
                return;

            if (field.InspectorType == InspectorType.Serialized)
            {
                field.Parent.SerializedProperty.MoveArrayElement(originalIndex, index);
                field.Parent.SerializedProperty.serializedObject.ApplyModifiedProperties();
                field.Parent.RefreshFields();

                return;
            }

            suspended = true;

            if (originalIndex > index)
            {
                while (originalIndex != index)
                {
                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        IList list = field.Instances[i] as IList;
                        if (list == null)
                            continue;

                        if (originalIndex - 1 < 0)
                            continue;

                        object current = list[originalIndex];
                        object over = list[originalIndex - 1];

                        list[originalIndex] = over;
                        list[originalIndex - 1] = current;
                    }

                    originalIndex--;
                }
            }
            else
            {
                while (originalIndex != index)
                {
                    for (int i = 0; i < field.Instances.Length; i++)
                    {
                        IList list = field.Instances[i] as IList;
                        if (list == null)
                            continue;

                        if (list.Count == originalIndex + 1)
                            continue;

                        object current = list[originalIndex];
                        object under = list[originalIndex + 1];

                        list[originalIndex] = under;
                        list[originalIndex + 1] = current;
                    }

                    originalIndex++;
                }
            }

            suspended = false;

            field.Parent.SetDirty();
            field.Parent.RefreshFields();
        }

        private static void MoveItemUp()
        {
            if (inspectorField == null)
                return;

            MoveItemUp(inspectorField);
        }

        private static void MoveItemUp(InspectorField field)
        {
            field.RecordObjects("Move item in " + field.Parent.Name + " at index " + field.Index.ToString() + " up.");

            int index = field.Index;
            if (index == -1)
                return;

            for (int i = 0; i < field.Parent.Instances.Length; i++)
            {
                IList list = field.Parent.GetValue(field.Parent.Instances[i]) as IList;
                if (list == null)
                    continue;

                object current = list[index];
                object over = list[index - 1];

                list[index] = over;
                list[index - 1] = current;
            }

            field.SetDirty();
            field.Parent.RefreshFields();
        }

        private static void MoveItemDown()
        {
            if (inspectorField == null)
                return;

            MoveItemDown(inspectorField);
        }

        private static void MoveItemDown(InspectorField field)
        {
            field.RecordObjects("Move item in " + field.Parent.Name + " at index " + field.Index.ToString() + " down.");

            int index = field.Index;
            if (index == -1)
                return;

            for (int i = 0; i < field.Parent.Instances.Length; i++)
            {
                IList list = field.Parent.GetValue(field.Parent.Instances[i]) as IList;
                if (list == null)
                    continue;

                object current = list[index];
                object under = list[index + 1];

                list[index] = under;
                list[index + 1] = current;
            }

            field.SetDirty();
            field.Parent.RefreshFields();
        }

        private static void MoveItemTop()
        {
            if (inspectorField == null)
                return;

            MoveItemTop(inspectorField);
        }

        private static void MoveItemTop(InspectorField field)
        {
            field.RecordObjects("Move item in " + field.Parent.Name + " at index " + field.Index.ToString() + " to the top.");

            int index = field.Index;
            if (index == -1)
                return;

            suspended = true;

            while (index != 0)
            {
                for (int i = 0; i < field.Parent.Instances.Length; i++)
                {
                    IList list = field.Parent.GetValue(field.Parent.Instances[i]) as IList;
                    if (list == null)
                        continue;

                    object current = list[index];
                    object over = list[index - 1];

                    list[index] = over;
                    list[index - 1] = current;
                }

                index--;
            }

            suspended = false;

            field.SetDirty();
            field.Parent.RefreshFields();
        }

        private static void MoveItemBottom()
        {
            if (inspectorField == null)
                return;

            MoveItemBottom(inspectorField);
        }

        private static void MoveItemBottom(InspectorField field)
        {
            field.RecordObjects("Move item in " + field.Parent.Name + " at index " + field.Index.ToString() + " to the bottom.");

            int index = field.Index;
            if (index == -1)
                return;

            suspended = true;

            while (index < field.Parent.Count - 1)
            {
                for (int i = 0; i < field.Parent.Instances.Length; i++)
                {
                    IList list = field.Parent.GetValue(field.Parent.Instances[i]) as IList;
                    if (list == null)
                        continue;

                    object current = list[index];
                    object under = list[index + 1];

                    list[index] = under;
                    list[index + 1] = current;
                }

                index++;
            }

            suspended = false;

            field.SetDirty();
            field.Parent.RefreshFields();
        }
        #endregion

        /// <summary>
        /// This method is invoked to build a list of proper delegate to invoke.
        /// </summary>
        internal static void ParseRuntimeAttributes(IRuntimeAttribute attribute, Type type, object[] instances)
        {
            if (attribute == null || string.IsNullOrEmpty(attribute.MethodName))
                return;

            attribute.Delegates.Clear();

            if (attribute.MethodName.Contains('.'))
            {
                string[] path = attribute.MethodName.Split('.');
                string methodName = path[path.Length - 1];
                Type current = TypeUtility.GetTypeByName(path[0]);
                for (int i = 1; i < path.Length - 1; i++)
                {
                    if (current == null)
                        return;

                    current = current.GetNestedType(path[i], BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                }

                MethodInfo method = current.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                if (method == null)
                {
                    PropertyInfo property = type.GetProperty(attribute.MethodName, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                    if (property != null && property.CanRead)
                        method = property.GetGetMethod(true);
                }

                if (method == null)
                {
                    Debug.LogError("Fail to find method or property named " + attribute.MethodName);
                    return;
                }

                for (int i = 0; i < instances.Length; i++)
                {
                    attribute.Delegates.Add(Delegate.CreateDelegate(attribute.TemplateStatic, null, method));
                }
            }
            else
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    MethodInfo method = type.GetMethod(attribute.MethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

                    if (method == null)
                    {
                        PropertyInfo property = type.GetProperty(attribute.MethodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);
                        if (property != null && property.CanRead)
                            method = property.GetGetMethod(true);
                    }

                    if (method == null)
                    {
                        Debug.LogError("Fail to find method or property named " + attribute.MethodName);
                        continue;
                    }

                    attribute.Delegates.Add(Delegate.CreateDelegate(attribute.Template, instances[i], method));
                }
            }
        }

        /// <summary>
        /// Get the list of valid derivaton from a specific type.
        /// Used to create derived object, should always be from ComponentMonoBehaviour but is not enforced.
        /// </summary>
        private static List<DescriptionPair> GetDerived(Type baseType)
        {
            List<DescriptionPair> pairs = new List<DescriptionPair>();

            if (typeof(IList).IsAssignableFrom(baseType))
            {
                Type[] types = baseType.GetGenericArguments();

                if (types.Length != 1)
                    return pairs;

                baseType = types[0];
            }

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.IsAbstract || type.IsGenericType || !baseType.IsAssignableFrom(type))
                        continue;

                    IDescriptor descriptor = null;
                    Description description = null;
                    object[] attributes = type.GetCustomAttributes(typeof(IDescriptor), true);
                    if (attributes.Length != 0)
                        descriptor = attributes[0] as IDescriptor;

                    if (descriptor != null)
                        description = descriptor.GetDescription(null, null);
                    else
                        description = new Description(type.Name, "");

                    pairs.Add(new DescriptionPair(type, description));
                }
            }

            return pairs;
        }
    }
}