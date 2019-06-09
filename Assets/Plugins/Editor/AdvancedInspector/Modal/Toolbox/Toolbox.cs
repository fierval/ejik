using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// The toolbox is a generic way of display a selection to the user.
    /// Any EditorWindow that implement IToolbox will call it when CTRL+Q is request it.
    /// It is up to the window to fill the content of the Toolbox.
    /// </summary>
    public class Toolbox : ModalWindow
    {
        private const float SEARCHBAR_HEIGHT = 18;
        private const float SCROLLBAR_WIDTH = 24;
        private const float BUTTONS_HEIGHT = 30;

        private const float HEIGHT = 300;
        private const float WIDTH = 250;

        private TreeView view;

        /// <summary>
        /// List of items visible by this toolbox.
        /// </summary>
        public List<object> Items
        {
            get
            {
                List<object> list = new List<object>();

                foreach (TreeViewItem item in view.Items)
                    list.Add(item.Tag);

                return list;
            }
            set
            {
                view.Items.Clear();

                foreach (object o in value)
                {
                    TreeViewItem item = new TreeViewItem(view, null, o);
                    item.MayContainChildren = false;
                    view.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// Add an item to the toolbox list.
        /// </summary>
        public void AddItem(object tag, string name, string description, Texture icon)
        {
            TreeViewItem item = new TreeViewItem(view, null, tag, name, description, icon);
            item.MayContainChildren = false;
            view.Items.Add(item);
        }

        /// <summary>
        /// The current selection.
        /// </summary>
        public object[] Selection
        {
            get { return view.Selection; }
        }

        private string search = "";

        /// <summary>
        /// The search term entered in the search field.
        /// </summary>
        public string Search
        {
            get { return search; }
            set
            {
                if (search != value)
                {
                    search = value;
                    view.BuildVisibility();
                }
            }
        }

        //[MenuItem("Window/Toolbox %e")]
        static void InitToolbox()
        {
            if (Exist<Toolbox>())
                return;

            IModal box = focusedWindow as IModal;

            if (box != null)
                box.ModalRequest(false);
        }

        //[MenuItem("Window/Toolbox %#e")]
        static void InitShiftToolbox()
        {
            if (Exist<Toolbox>())
                return;

            IModal box = focusedWindow as IModal;

            if (box != null)
                box.ModalRequest(true);
        }

        /// <summary>
        /// Create a toolbox for this IModal.
        /// </summary>
        public static Toolbox Create(IModal owner, string title, List<DescriptionPair> pairs, Vector2 position)
        {
            return Create(owner, title, pairs, position, "");
        }

        /// <summary>
        /// Create a toolbox for this IModal.
        /// </summary>
        public static Toolbox Create(IModal owner, string title, List<DescriptionPair> pairs, Vector2 position, string search)
        {
            Toolbox toolbox = Toolbox.CreateInstance<Toolbox>();
            toolbox.search = search;
            toolbox.view = new TreeView();
            toolbox.view.visibility = toolbox.IsItemVisible;
            toolbox.view.AlwaysSelect = true;
            toolbox.view.CanDrag = false;
            toolbox.view.CanDrop = false;
            toolbox.view.CanDropOnSelf = false;
            toolbox.view.CanMultiSelect = false;
            toolbox.view.HiddenCanBeSelected = false;
            toolbox.view.SelectionDoubleClicked += toolbox.OnDoubleClick;

            toolbox.owner = owner;
            toolbox.titleContent.text = title;

            for (int i = 0; i < pairs.Count; i++)
            {
                if (pairs[i].Value == null)
                    continue;

                if (pairs[i].Description != null)
                    toolbox.AddItem(pairs[i].Value, pairs[i].Description.Name, pairs[i].Description.Comment, pairs[i].Description.Icon);
                else
                    toolbox.AddItem(pairs[i].Value, pairs[i].ToString(), "", null);
            }

            toolbox.view.Sort();
            toolbox.view.BuildVisibility();

            float halfWidth = WIDTH / 2;

            float x = position.x - halfWidth;
            float y = position.y;

            Rect rect = new Rect(x, y, 0, 0);

            toolbox.position = rect;
            toolbox.ShowAsDropDown(rect, new Vector2(WIDTH, HEIGHT));

            return toolbox;
        }

        private bool IsItemVisible(object o, string name)
        {
            if (string.IsNullOrEmpty(search))
                return true;

            return name.ToLower().Contains(search.ToLower());
        }

        private void OnDoubleClick()
        {
            Ok();
        }

        /// <summary>
        /// IControl implementation
        /// </summary>
        protected override void Draw(Rect region)
        {
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return)
                    Ok();

                if (Event.current.keyCode == KeyCode.Escape)
                    Cancel();

                int i = -1;
                if (Selection.Length != 0)
                    i = view.GetIndex(Selection[0]);

                if (i != -1)
                {
                    if (Event.current.keyCode == KeyCode.UpArrow)
                    {
                        if (i == 0)
                            view.SelectIndex(view.VisibleItems.Length - 1);
                        else
                            view.SelectIndex(i - 1);
                    }
                    else if (Event.current.keyCode == KeyCode.DownArrow)
                    {
                        if (i == view.VisibleItems.Length - 1)
                            view.SelectIndex(0);
                        else
                            view.SelectIndex(i + 1);
                    }
                }
            }

            GUILayout.BeginArea(region);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUI.SetNextControlName("SearchField");
            Search = EditorGUILayout.TextField(Search, GUI.skin.FindStyle("ToolbarSeachTextField"));
            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
                Search = string.Empty;

            EditorGUILayout.EndHorizontal();

            Rect listRegion = new Rect(0, SEARCHBAR_HEIGHT, region.width, region.height - SEARCHBAR_HEIGHT - BUTTONS_HEIGHT);

            if (view.Draw(listRegion))
                Repaint();

            float buttonWidth = position.width / 2 - 18;
            if (GUI.Button(new Rect(6, region.height - SCROLLBAR_WIDTH, buttonWidth, 18), "Ok"))
                Ok();

            if (GUI.Button(new Rect(region.width - buttonWidth - 6, region.height - SCROLLBAR_WIDTH, buttonWidth, 18), "Cancel"))
                Cancel();

            if (GUI.GetNameOfFocusedControl() == string.Empty)
                EditorGUI.FocusTextInControl("SearchField");

            GUILayout.EndArea();
        }
    }
}