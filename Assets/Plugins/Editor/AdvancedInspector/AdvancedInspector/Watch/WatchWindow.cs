using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    internal class WatchWindow : EditorWindow
    {
        private static WatchWindow instance = null;

        public static WatchWindow Instance
        {
            get 
            {
                if (instance == null)
                {
                    instance = (WatchWindow)EditorWindow.GetWindow(typeof(WatchWindow));
                    instance.titleContent.text = "Watch";
                    instance.titleContent.image = Icon;
                    instance.wantsMouseMove = true;
                    CreateEditor();
                }

                return instance;
            }
        }

        private static ExternalEditor editor = null;

        [SerializeField]
        private List<InspectorReference> references = new List<InspectorReference>();

        private static Texture icon;

        internal static Texture Icon
        {
            get
            {
                if (icon == null)
                    icon = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "Visible.png");

                return icon;
            }
        }

        [MenuItem("Window/Watch")]
        private static void Init()
        {
            WatchWindow window = Instance;
        }

        private static void CreateEditor()
        {
            if (editor == null)
            {
                editor = ExternalEditor.CreateInstance<ExternalEditor>();
                editor.DraggableSeparator = true;
                editor.DivisionSeparator = 150;
            }
        }

        public static bool Contains(InspectorField field)
        {
            CreateEditor();

            foreach (InspectorField parent in editor.Fields)
                foreach (InspectorField child in parent.Fields)
                    if (child.Equals(field))
                        return true;

            return false;
        }

        public static void AddField(InspectorField field)
        {
            Instance.references.Add(new InspectorReference(field));

            InspectorField parent = CreateContainer(field.SerializedInstances, editor.Fields);
            parent.Fields.Add(new InspectorField(field));
            parent.Depth = 0;

            if (!editor.Fields.Contains(parent))
                editor.Fields.Add(parent);

            Instance.Repaint();
        }

        public static void RemoveField(InspectorField field)
        {
            for (int i = editor.Fields.Count - 1; i >= 0; i--)
            {
                int index = editor.Fields[i].Fields.IndexOf(field);
                if (index == -1)
                    continue;

                editor.Fields[i].Fields.RemoveAt(index);
                if (editor.Fields[i].Fields.Count == 0)
                    editor.Fields.RemoveAt(i);
            }

            Instance.references.Remove(new InspectorReference(field));
            Instance.Repaint();
        }

        private void Update()
        {
            Repaint();

            if (editor == null)
                return;

            int count = 0;
            for (int i = 0; i < editor.Fields.Count; i++)
                count += editor.Fields.Count;

            if (count != references.Count)
                RebuildFields();
        }

        private void OnGUI()
        {
            CreateEditor();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton))
            {
                editor.Fields.Clear();
                references.Clear();
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            AdvancedInspectorControl.watched = true;
            editor.Draw(new Rect(0, 18, position.width, position.height - 18));
            AdvancedInspectorControl.watched = false;
        }

        private static InspectorField CreateContainer(UnityEngine.Object[] objs, List<InspectorField> existing)
        {
            string name = "";
            if (objs.Length == 1)
                name = objs[0].name + " [" + objs[0].GetType().Name + "]";
            else
                name = "List of " + objs[0].GetType().Name;

            for (int i = 0; i < existing.Count; i++)
            {
                if (existing[i].Name != name)
                    continue;

                if (existing[i].internalValue.Length != objs.Length)
                    continue;

                bool same = true;
                for (int j = 0; j < objs.Length; j++)
                {
                    if ((existing[i].internalValue[j] as UnityEngine.Object) != objs[j])
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                    return existing[i];
            }

            InspectorField container = new InspectorField(name);
            container.internalValue = objs;

            return container;
        }

        private void RebuildFields()
        {
            editor.Fields.Clear();

            if (references == null)
            {
                references = new List<InspectorReference>();
                return;
            }

            List<InspectorField> fields = new List<InspectorField>();
            for (int i = 0; i < references.Count; i++)
            {
                if (references[i] == null)
                    continue;

                UnityEngine.Object[] objs = references[i].Objects;

                if (objs != null && objs.Length > 0)
                {
                    InspectorField parent = CreateContainer(objs.ToArray(), fields);

                    Type type = objs[0].GetType();
                    if (InspectorEditor.InspectorEditorByTypes.ContainsKey(type))
                    {
                        InspectorEditor.InspectorEditorByTypes[type].Instances = objs.ToArray();
                        foreach (InspectorField field in InspectorEditor.InspectorEditorByTypes[type].Fields)
                        {
                            if (field.Path != references[i].path)
                                continue;

                            parent.Fields.Add(new InspectorField(field));
                            parent.Depth = 0;

                            if (!fields.Contains(parent))
                                fields.Add(parent);
                        }
                    }
                    else
                    {
                        parent.Fields.Add(new InspectorField(references[i].Objects, references[i].path));
                        parent.Depth = 0;

                        if (!fields.Contains(parent))
                            fields.Add(parent);
                    }
                }
            }

            for (int i = references.Count - 1; i >= 0; i--)
                if (references[i] == null || references[i].Objects == null || references[i].Objects.Length == 0)
                    references.RemoveAt(i);

            editor.Fields.AddRange(fields);
        }
    }
}
