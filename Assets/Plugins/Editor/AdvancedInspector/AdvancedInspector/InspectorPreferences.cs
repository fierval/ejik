using System.Collections.Generic;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEditor.VersionControl;
using System.Globalization;

namespace AdvancedInspector
{
    /// <summary>
    /// Handles the Advanced Inspector preferences.
    /// </summary>
    [InitializeOnLoad]
    public class InspectorPreferences
    {
        private const string version = "Version: 2.00a (Unity 2017)";

        private const string editorPath = "/Plugins/Editor/AdvancedInspector/UnityTypes/";

        private const string InspectDefaultItemsKey = "InspectDefaultItems";
        private const string EditorKey = "InspectEditor_";
        private const string IndentationKey = "InspectIndentation";
        private const string LargeCollectionKey = "LargeCollection";
        private const string StyleKey = "InspectorStyle";
        private const string SeparatorKey = "InspectorSeparatorStyle";
        private const string SeparatorDefaultKey = "InspectorSeparatorDefaultKey";
        private const string SeparatorSelectedKey = "InspectorSeparatorSelectedKey";
        private const string BoxDefaultKey = "InspectorBoxDefaultKey";
        private const string BoxPlayKey = "InspectorBoxPlayKey";
        private const string ValueScrollKey = "InspectorScrollValue";
        private const string CopyPasteKey = "InspectorCopyPaste";
        private const string ContextualKey = "InspectorContextual";
        private const string MassExpandKey = "InspectorMassExpand";
        private const string ExpandableReferencesKey = "InspectorExpandableReferences";
        private const string CollectionItemNamingKey = "InspectorCollectionItemNaming";

        private static Texture logo;

        internal static Texture Logo
        {
            get
            {
                if (logo == null)
                    logo = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Logo.png");

                return logo;
            }
        }

        private static InspectorStyle style = InspectorStyle.Round;

        /// <summary>
        /// The style collection used for the different element.
        /// </summary>
        public static InspectorStyle Style
        {
            get { return style; }
        }

        private static SeparatorStyle separator = SeparatorStyle.Gradient;

        /// <summary>
        /// The separator's style.
        /// </summary>
        public static SeparatorStyle Separator
        {
            get { return separator; }
        }

        private static Color separatorDefaultColor = new Color(0.35f, 0.35f, 0.35f);

        /// <summary>
        /// Unselected color of the separator.
        /// </summary>
        public static Color SeparatorDefaultColor
        {
            get { return separatorDefaultColor; }
        }

        private static Color separatorSelectedColor = new Color(0.2f, 0.6f, 1);

        /// <summary>
        /// Selected color of the separator.
        /// </summary>
        public static Color SeparatorSelectedColor
        {
            get { return separatorSelectedColor; }
        }

        private static Color boxDefaultColor = new Color(1, 1, 1, 1);

        /// <summary>
        /// Selected color of the separator.
        /// </summary>
        public static Color BoxDefaultColor
        {
            get { return boxDefaultColor; }
        }

        private static Color boxPlayColor = new Color(1, 1, 1, 1);

        /// <summary>
        /// Selected color of the separator.
        /// </summary>
        public static Color BoxPlayColor
        {
            get { return boxPlayColor; }
        }

        private static bool inspectDefaultItems = true;

        /// <summary>
        /// If true, all classes that do not have a Custom Editor are drawn by Advanced Inspector.
        /// </summary>
        public static bool InspectDefaultItems
        {
            get { return inspectDefaultItems; }
        }

        private static bool expandableReferences = true;

        /// <summary>
        /// If true, all classes that do not have a Custom Editor are drawn by Advanced Inspector.
        /// </summary>
        public static bool ExpandableReferences
        {
            get { return expandableReferences; }
        }

        private static InspectorDragControl valueScroll = InspectorDragControl.Drag;

        /// <summary>
        /// The control for scrolling numbers.
        /// </summary>
        public static InspectorDragControl ValueScroll
        {
            get { return valueScroll; }
        }

        private static InspectorDragControl copyPaste = InspectorDragControl.ShiftDrag;

        /// <summary>
        /// The control to copy-paste by drag.
        /// </summary>
        public static InspectorDragControl CopyPaste
        {
            get { return copyPaste; }
        }

        private static InspectorModifierControl massExpand = InspectorModifierControl.Alt;

        /// <summary>
        /// The control modifier used when clicking on an expansion arrow. Expand/Collapse sub-nodes.
        /// </summary>
        public static InspectorModifierControl MassExpand
        {
            get { return massExpand; }
        }

        private static InspectorModifierControl contextual = InspectorModifierControl.Control;

        /// <summary>
        /// Usually to open the contextual menu, you do Right-Click.
        /// In some cases, people may not have access to a proper right-click, so they can do CTRL+Click for example.
        /// </summary>
        public static InspectorModifierControl Contextual
        {
            get { return contextual; }
        }

        private static InspectorCollectionItemNaming collectionItemNaming = InspectorCollectionItemNaming.Index;

        /// <summary>
        /// How collection names are displayed.
        /// </summary>
        public static InspectorCollectionItemNaming CollectionItemNaming
        {
            get { return collectionItemNaming; }
        }

        private static int extraIndentation = 0;

        /// <summary>
        /// Extra indentation applied to boxes.
        /// </summary>
        public static int ExtraIndentation
        {
            get { return extraIndentation; }
        }

        private static int largeCollection = 50;

        /// <summary>
        /// The maximum number of displayed item in a collection. Trigger the large collection arrow display beyond that.
        /// </summary>
        public static int LargeCollection
        {
            get { return largeCollection; }
        }

        private static Dictionary<FileInfo, string[]> editors = new Dictionary<FileInfo, string[]>();

        private static Vector2 scrollView;

        private static int tool = 0;

        static InspectorPreferences()
        {
            if (EditorPrefs.HasKey(InspectDefaultItemsKey))
                inspectDefaultItems = EditorPrefs.GetBool(InspectDefaultItemsKey);

            if (EditorPrefs.HasKey(StyleKey))
                style = (InspectorStyle)EditorPrefs.GetInt(StyleKey);

            if (EditorPrefs.HasKey(SeparatorKey))
                separator = (SeparatorStyle)EditorPrefs.GetInt(SeparatorKey);

            if (EditorPrefs.HasKey(SeparatorDefaultKey))
                separatorDefaultColor = StringToColor(EditorPrefs.GetString(SeparatorDefaultKey));

            if (EditorPrefs.HasKey(SeparatorSelectedKey))
                separatorSelectedColor = StringToColor(EditorPrefs.GetString(SeparatorSelectedKey));

            if (EditorPrefs.HasKey(BoxDefaultKey))
                boxDefaultColor = StringToColor(EditorPrefs.GetString(BoxDefaultKey));

            if (EditorPrefs.HasKey(BoxPlayKey))
                boxPlayColor = StringToColor(EditorPrefs.GetString(BoxPlayKey));

            if (EditorPrefs.HasKey(IndentationKey))
                extraIndentation = EditorPrefs.GetInt(IndentationKey);

            if (EditorPrefs.HasKey(LargeCollectionKey))
                largeCollection = EditorPrefs.GetInt(LargeCollectionKey);

            if (EditorPrefs.HasKey(ValueScrollKey))
                valueScroll = (InspectorDragControl)EditorPrefs.GetInt(ValueScrollKey);

            if (EditorPrefs.HasKey(CopyPasteKey))
                copyPaste = (InspectorDragControl)EditorPrefs.GetInt(CopyPasteKey);

            if (EditorPrefs.HasKey(MassExpandKey))
                massExpand = (InspectorModifierControl)EditorPrefs.GetInt(MassExpandKey);

            if (EditorPrefs.HasKey(ContextualKey))
                contextual = (InspectorModifierControl)EditorPrefs.GetInt(ContextualKey);

            if (EditorPrefs.HasKey(ExpandableReferencesKey))
                expandableReferences = EditorPrefs.GetBool(ExpandableReferencesKey);

            if (EditorPrefs.HasKey(CollectionItemNamingKey))
                collectionItemNaming = (InspectorCollectionItemNaming)EditorPrefs.GetInt(CollectionItemNamingKey);

            GetAllEditor();
        }

        [PreferenceItem("Adv. Inspector")]
        private static void Preference()
        {
            tool = GUILayout.Toolbar(tool, new string[] { "Preferences", "Editors", "About" });

            if (tool == 0)
            {
                EditorGUI.BeginChangeCheck();
                inspectDefaultItems = EditorGUILayout.Toggle(new GUIContent("Inspect Default Items", "Draws all types that do not have a Custom Editor associated. If false, your classes are drawn by Unity unless they have the AdvancedInspector attribute."), inspectDefaultItems);
                expandableReferences = EditorGUILayout.Toggle(new GUIContent("Expandable References", "All MonoBehaviour/ScriptableObject references are expandable from the displayed field."), expandableReferences);

                collectionItemNaming = (InspectorCollectionItemNaming)EditorGUILayout.EnumPopup(new GUIContent("Collection Naming", "How items in a collection are named."), collectionItemNaming);

                valueScroll = (InspectorDragControl)EditorGUILayout.EnumPopup("Number Drag", valueScroll);
                if (valueScroll == copyPaste)
                    copyPaste = Next(copyPaste);

                copyPaste = (InspectorDragControl)EditorGUILayout.EnumPopup("Copy/Paste Drag", copyPaste);
                if (valueScroll == copyPaste)
                    valueScroll = Next(valueScroll);

#if UNITY_2017_1 || UNITY_2017_2
                massExpand = (InspectorModifierControl)EditorGUILayout.EnumMaskField("Expand/Collapse Children", massExpand);

                contextual = (InspectorModifierControl)EditorGUILayout.EnumMaskField("Contextual Menu", contextual);
#else
                massExpand = (InspectorModifierControl)EditorGUILayout.EnumFlagsField("Expand/Collapse Children", massExpand);

                contextual = (InspectorModifierControl)EditorGUILayout.EnumFlagsField("Contextual Menu", contextual);
#endif

                extraIndentation = Mathf.Clamp(EditorGUILayout.IntField("Indentation", extraIndentation), 0, int.MaxValue);
                largeCollection = Mathf.Clamp(EditorGUILayout.IntField("Large Collection", largeCollection), 10, 200);

                separator = (SeparatorStyle)EditorGUILayout.EnumPopup("Separator", separator);
                separatorDefaultColor = EditorGUILayout.ColorField("Separator Default", separatorDefaultColor);
                separatorSelectedColor = EditorGUILayout.ColorField("Separator Selected", separatorSelectedColor);
                boxDefaultColor = EditorGUILayout.ColorField("Box Default", boxDefaultColor);
                boxPlayColor = EditorGUILayout.ColorField("Box Playmode", boxPlayColor);

                style = (InspectorStyle)EditorGUILayout.EnumPopup("Style", style);

                Color previous = GUI.color;
                GUI.color = EditorApplication.isPlaying ? boxPlayColor : boxDefaultColor;

                GUILayout.Box("", AdvancedInspectorControl.BoxTitleStyle);
                GUILayout.Box("", AdvancedInspectorControl.BoxStyle);

                GUI.color = previous;

                if (EditorGUI.EndChangeCheck())
                {
                    EditorPrefs.SetBool(InspectDefaultItemsKey, inspectDefaultItems);
                    EditorPrefs.SetInt(StyleKey, (int)style);
                    EditorPrefs.SetInt(SeparatorKey, (int)separator);
                    EditorPrefs.SetString(SeparatorDefaultKey, ColorToString(separatorDefaultColor));
                    EditorPrefs.SetString(SeparatorSelectedKey, ColorToString(separatorSelectedColor));
                    EditorPrefs.SetString(BoxDefaultKey, ColorToString(boxDefaultColor));
                    EditorPrefs.SetString(BoxPlayKey, ColorToString(boxPlayColor));
                    EditorPrefs.SetInt(IndentationKey, extraIndentation);
                    EditorPrefs.SetInt(LargeCollectionKey, largeCollection);
                    EditorPrefs.SetInt(ValueScrollKey, (int)valueScroll);
                    EditorPrefs.SetInt(CopyPasteKey, (int)copyPaste);
                    EditorPrefs.SetInt(MassExpandKey, (int)massExpand);
                    EditorPrefs.SetBool(ExpandableReferencesKey, expandableReferences);
                    EditorPrefs.SetInt(CollectionItemNamingKey, (int)collectionItemNaming);
                }
            }
            else if (tool == 1)
            {
                bool offAll = false;
                bool onAll = false;

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Turn Off"))
                    offAll = true;

                if (GUILayout.Button("Turn On"))
                    onAll = true;
                GUILayout.EndHorizontal();

                bool changed = false;
                scrollView = GUILayout.BeginScrollView(scrollView);
                foreach (KeyValuePair<FileInfo, string[]> pair in editors)
                {
                    bool active = !pair.Value[0].Contains("///");
                    bool result = EditorGUILayout.ToggleLeft(pair.Key.Name, active);

                    if (offAll)
                        result = false;

                    if (onAll)
                        result = true;

                    if (result != active)
                    {
                        if (!result)
                        {
                            for (int i = 0; i < pair.Value.Length; i++)
                                pair.Value[i] = "///" + pair.Value[i];
                        }
                        else
                        {
                            for (int i = 0; i < pair.Value.Length; i++)
                                pair.Value[i] = pair.Value[i].Substring(3);
                        }

                        string path = "Assets" + pair.Key.FullName.Substring(Application.dataPath.Length);
                        if (!AssetDatabase.IsOpenForEdit(path, StatusQueryOptions.UseCachedIfPossible))
                            Provider.Checkout(path, CheckoutMode.Both);

                        File.WriteAllLines(pair.Key.FullName, pair.Value);
                        UnityEngine.Object[] selection = Selection.objects;
                        Selection.objects = new UnityEngine.Object[0];
                        Selection.objects = selection;
                        changed = true;
                    }
                }
                GUILayout.EndScrollView();

                if (changed)
                    AssetDatabase.Refresh();
            }
            else if (tool == 2)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.blue;

                GUI.DrawTexture(new Rect(-16, 36, 379, 93), Logo, ScaleMode.ScaleToFit);

                GUILayout.Space(100);

                EditorGUILayout.LabelField(version);

                GUILayout.Space(10);

                if (GUILayout.Button("Manual", style))
                    Application.OpenURL("http://lightstrikersoftware.com/docs/AdvancedInspector_Manual.pdf");

                GUILayout.Space(10);

                if (GUILayout.Button("Tutorials", style))
                    Application.OpenURL("http://lightstrikersoftware.com/docs/AdvancedInspector_Tutorials.pdf");

                GUILayout.Space(10);
                EditorGUILayout.LabelField("Need Help? Found A Bug?");

                if (GUILayout.Button("admin@lightstrikersoftware.com", style))
                    Application.OpenURL("mailto:admin@lightstrikersoftware.com");
            }
        }

        private static InspectorDragControl Next(InspectorDragControl control)
        {
            if (control == InspectorDragControl.None)
                return InspectorDragControl.None;

            switch (control)
            {
                case InspectorDragControl.Drag:
                    return InspectorDragControl.AltDrag;
                case InspectorDragControl.AltDrag:
                    return InspectorDragControl.ControlDrag;
                case InspectorDragControl.ControlDrag:
                    return InspectorDragControl.ShiftDrag;
                case InspectorDragControl.ShiftDrag:
                    return InspectorDragControl.Drag;
            }

            return InspectorDragControl.None;
        }

        /// <summary>
        /// Returns true if the control is valid in the current Event context.
        /// </summary>
        public static bool IsDragControl(InspectorDragControl control)
        {
            Event e = Event.current;

            if (control == InspectorDragControl.None || e == null)
                return false;
            else if (control == InspectorDragControl.Drag)
                return true;
            else if (control == InspectorDragControl.AltDrag && e.alt)
                return true;
            else if (control == InspectorDragControl.ControlDrag && e.control)
                return true;
            else if (control == InspectorDragControl.ShiftDrag && e.shift)
                return true;

            return false;
        }

        /// <summary>
        /// Returns true if the control is valid in the current Event context.
        /// </summary>
        public static bool IsControl(InspectorModifierControl control)
        {
            Event e = Event.current;

            if (e == null)
                return false;
            
            if (control.Has(InspectorModifierControl.Alt) && !e.alt)
                return false;
            
            if (control.Has(InspectorModifierControl.Control) && !e.control)
                return false;
        
            if (control.Has(InspectorModifierControl.Shift) && !e.shift)
                return false;

            return true;
        }

        private static void GetAllEditor()
        { 
            DirectoryInfo directory = new DirectoryInfo(Application.dataPath + editorPath);
            if (directory.Exists)
            {
                foreach (FileInfo file in directory.GetFiles("*.cs", SearchOption.TopDirectoryOnly).OrderBy(x => x.Name))
                {
                    if (file.Extension != ".cs")
                        continue;

                    string[] lines = File.ReadAllLines(file.FullName);
                    bool isAbstract = false;
                    foreach (string line in lines)
                    {
                        if (line.Contains("abstract"))
                        {
                            isAbstract = true;
                            break;
                        }
                    }

                    if (!isAbstract)
                        editors.Add(file, lines);
                }
            }
        }

        private static string ColorToString(Color color)
        {
            return color.r.ToString(CultureInfo.InvariantCulture) + "," + 
                   color.g.ToString(CultureInfo.InvariantCulture) + "," + 
                   color.b.ToString(CultureInfo.InvariantCulture) + "," + 
                   color.a.ToString(CultureInfo.InvariantCulture);
        }

        private static Color StringToColor(string text)
        {
            string[] split = text.Split(',');
            if (split.Length != 4)
                return Color.white;

            return new Color(float.Parse(split[0], CultureInfo.InvariantCulture), 
                             float.Parse(split[1], CultureInfo.InvariantCulture), 
                             float.Parse(split[2], CultureInfo.InvariantCulture), 
                             float.Parse(split[3], CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// The accepted control that involves dragging.
        /// </summary>
        public enum InspectorDragControl
        { 
            /// <summary>
            /// The control is turned off.
            /// </summary>
            None,
            /// <summary>
            /// Single drag.
            /// </summary>
            Drag,
            /// <summary>
            /// Alt+Drag
            /// </summary>
            AltDrag,
            /// <summary>
            /// Control+Drag
            /// </summary>
            ControlDrag,
            /// <summary>
            /// Shift+Drag
            /// </summary>
            ShiftDrag
        }

        /// <summary>
        /// The modifier keys involded in non-dragging action.
        /// </summary>
        public enum InspectorModifierControl
        { 
            /// <summary>
            /// Alt Key
            /// </summary>
            Alt = 0x01,
            /// <summary>
            /// Control Key
            /// </summary>
            Control = 0x02,
            /// <summary>
            /// Shift Key
            /// </summary>
            Shift = 0x04,
        }

        /// <summary>
        /// Controls how a collection displayed its indexed items.
        /// </summary>
        public enum InspectorCollectionItemNaming
        {
            /// <summary>
            /// Advanced Inspector default: "[4]"
            /// </summary>
            Index,
            /// <summary>
            /// "4"
            /// </summary>
            Numbered,
            /// <summary>
            /// "Element[4]"
            /// </summary>
            IndexedElements,
            /// <summary>
            /// Unity's default: "Element 4"
            /// </summary>
            NumberedElements,
            /// <summary>
            /// "ParentName[4]"
            /// </summary>
            IndexedNames,
            /// <summary>
            /// ParentName 4
            /// </summary>
            NumberedNames,
        }
    }
}
