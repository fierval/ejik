using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    /// <summary>
    /// A control that is a list of item that can be nested.
    /// Support drag and drop, editing and sorting.
    /// </summary>
    internal class TreeView : IControl
    {
        public delegate bool IsItemVisible(object item, string name);
        public delegate bool IsItemEnabled(object item);
        public delegate void DrawEventHandler(TreeViewItem item, Rect region);
        public delegate void NameEventHandler(object sender, NameEventArgs e);

        public IsItemVisible visibility;
        public IsItemEnabled enabled = null;

        public const float SCROLLBAR_WIDTH = 30;
        public const double FIELD_EDITION_DELAY = 0.3f;
        public const float DRAG_SCROLL_DELAY = 0.1f;

        public const int INVALID_INDEX = -1;

        #region Properties
        private bool canDrag = false;

        public bool CanDrag
        {
            get { return canDrag; }
            set { canDrag = value; }
        }

        private bool canDrop = false;

        public bool CanDrop
        {
            get { return canDrop; }
            set { canDrop = value; }
        }

        private bool canDropOnSelf = false;

        public bool CanDropOnSelf
        {
            get { return canDropOnSelf; }
            set { canDropOnSelf = value; }
        }

        private bool canDropInBetween = false;

        public bool CanDropInBetween
        {
            get { return canDropInBetween; }
            set { canDropInBetween = value; }
        }

        private bool canEdit = false;

        public bool CanEdit
        {
            get { return canEdit; }
            set { canEdit = value; }
        }

        private bool canMultiSelect = false;

        public bool CanMultiSelect
        {
            get { return canMultiSelect; }
            set { canMultiSelect = value; }
        }

        private bool alwaysSelect = false;

        public bool AlwaysSelect
        {
            get { return alwaysSelect; }
            set { alwaysSelect = value; }
        }

        private bool outsideClickUnselect = true;

        public bool OutsideClickUnselect
        {
            get { return outsideClickUnselect; }
            set { outsideClickUnselect = value; }
        }

        private bool hiddenCanBeSelected = false;

        public bool HiddenCanBeSelected
        {
            get { return hiddenCanBeSelected; }
            set { hiddenCanBeSelected = value; }
        }

        private bool scrollbarRight = true;

        public bool ScrollbarRight
        {
            get { return scrollbarRight; }
            set { scrollbarRight = value; }
        }

        private List<TreeViewItem> items;

        public List<TreeViewItem> Items
        {
            get { return items; }
        }

        private List<TreeViewItem> visibleItems;

        public TreeViewItem[] VisibleItems
        {
            get { return visibleItems.ToArray(); }
        }

        private List<object> selection = new List<object>();

        /// <summary>
        /// Current selected tags.
        /// </summary>
        public object[] Selection
        {
            get { return selection.ToArray(); }
            set
            {
                if (value != null)
                    selection = value.ToList();
                else
                    selection.Clear();

                FocusSelection();
            }
        }

        /// <summary>
        /// Number of visible nodes.
        /// </summary>
        public int Count
        {
            get
            {
                int listCount = 0;
                foreach (TreeViewItem item in visibleItems)
                    listCount += item.Count;

                return listCount;
            }
        }

        private float scroll = 0;

        public float Scroll
        {
            get { return scroll; }
            set { scroll = value; }
        }

        private float height = 0;

        private bool dragging = false;

        public bool Dragging
        {
            get { return dragging; }
        }

        private bool dragged = false;

        private bool Dragged
        {
            get { return dragged; }
            set
            {
                if (CanDrag)
                    dragged = value;
            }
        }
        #endregion

        private object target = null;
        private bool inBetween = false;
        private int lastSelection = -1;

        private int mouseHover = -1;
        private double timer = 0;
        private bool mayEdit = false;

        private bool repaint = false;

        public event GenericEventHandler SelectionChanged;
        public event GenericEventHandler SelectionClicked;
        public event GenericEventHandler SelectionDoubleClicked;
        public event GenericEventHandler SelectionRightClicked;
        public event DragEventHandler DragDropped;

        public event DrawEventHandler ItemDrawBackground;
        public event DrawEventHandler ItemDraw;

        public event NameEventHandler NameChanged;

        public event GenericEventHandler RequestRepaint;

        public TreeView()
        {
            items = new List<TreeViewItem>();
            visibleItems = new List<TreeViewItem>();
        }

        public void Update()
        {
            if (timer > 0 && timer <= EditorApplication.timeSinceStartup)
            {
                mayEdit = true;
                timer = 0;

                Repaint();
            }
        }

        private void Repaint()
        {
            if (RequestRepaint != null)
                RequestRepaint();
        }

        public bool Draw(Rect region)
        {
            height = region.height;
            float visibleCount = height / TreeViewItem.HEIGHT;

            int count = Count;
            if (visibleCount < count)
            {
                if (Event.current != null && Event.current.type == EventType.ScrollWheel && region.Contains(Event.current.mousePosition))
                {
                    scroll += Event.current.delta.y;
                    scroll = Mathf.Clamp(scroll, 0, count);
                    repaint = true;
                }

                float half = SCROLLBAR_WIDTH / 2;

                Rect scrollbar;
                if (scrollbarRight)
                    scrollbar = new Rect(region.xMax - half, region.y, SCROLLBAR_WIDTH, region.height);
                else
                    scrollbar = new Rect(region.xMin, region.y, SCROLLBAR_WIDTH, region.height);

                scroll = GUI.VerticalScrollbar(scrollbar, scroll, visibleCount, 0, count);

                if (scrollbarRight)
                    region = new Rect(region.x, region.y, region.width - half, region.height);
                else
                    region = new Rect(region.x + half, region.y, region.width - half, region.height);
            }

            GUILayout.BeginArea(region);

            int index = 0;
            foreach (TreeViewItem item in visibleItems)
                index = DrawItem(region, item, index, 0);

            GUILayout.EndArea();

            // Outside the list of object
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && region.Contains(Event.current.mousePosition))
            {
                if (!AlwaysSelect && OutsideClickUnselect)
                {
                    selection.Clear();

                    if (SelectionChanged != null)
                        SelectionChanged();
                }
            }
            else if (Event.current.type == EventType.DragUpdated && CanDrop)
            {
                object data = DragAndDrop.GetGenericData("TreeView");
                if (data != null && data is DragDropWrapper)
                {
                    DragDropWrapper wrapper = data as DragDropWrapper;

                    if (wrapper.Type == "TreeView")
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                        if (new Rect(region.x, region.y, region.width, TreeViewItem.HEIGHT).Contains(Event.current.mousePosition))
                        {
                            timer = EditorApplication.timeSinceStartup + DRAG_SCROLL_DELAY;
                            scroll = Mathf.Clamp(scroll - 1, 0, count);
                        }
                        else if (new Rect(region.x, region.yMax - TreeViewItem.HEIGHT, region.width, TreeViewItem.HEIGHT).Contains(Event.current.mousePosition))
                        {
                            timer = EditorApplication.timeSinceStartup + DRAG_SCROLL_DELAY;
                            scroll = Mathf.Clamp(scroll + 1, 0, count);
                        }
                    }
                }
            }
            else if (CanDrop && (Event.current.type == EventType.DragPerform || (target != null && CanDropOnSelf)))
            {
                if (DragDropped != null)
                    DragDropped(this, new DragEventArgs(target, inBetween, selection.ToArray()));

                target = null;

                Event.current.Use();

                TurnOffHighlight(visibleItems);
            }
            else if (Event.current.type == EventType.DragExited)
                TurnOffHighlight(visibleItems);

            if (repaint)
            {
                repaint = false;
                return true;
            }
            else
                return false;
        }

        private void TurnOffHighlight(List<TreeViewItem> items)
        {
            foreach (TreeViewItem item in items)
            {
                item.Highlighted = false;
                item.InBetween = false;

                TurnOffHighlight(item.VisibleItems);
            }
        }

        private int DrawItem(Rect area, TreeViewItem item, int index, int depth)
        {
            bool selected = selection.Contains(item.Tag);

            Rect itemRegion = new Rect(0, (index * TreeViewItem.HEIGHT) - (scroll * TreeViewItem.HEIGHT), area.width, TreeViewItem.HEIGHT * item.Count);

            if (ItemDrawBackground != null)
                ItemDrawBackground(item, itemRegion);

            item.DrawBackground(itemRegion, depth, selected);

            if (ItemDraw != null)
                ItemDraw(item, itemRegion);

            if (Event.current.type == EventType.MouseDown)
            {
                if (Event.current.button == 0)
                {
                    if (Event.current.clickCount == 1 && SelectionClicked != null)
                        SelectionClicked();

                    if (Event.current.clickCount == 2 && SelectionDoubleClicked != null)
                        SelectionDoubleClicked();
                }
                else if (Event.current.button == 1)
                {
                    if (SelectionRightClicked != null)
                        SelectionRightClicked();
                }

                if (!item.Editing && Event.current.button == 0)
                {
                    if (item.Items.Count > 0)
                    {
                        Rect openRegion = new Rect(itemRegion.x + (depth * TreeViewItem.HEIGHT / 2), itemRegion.y, TreeViewItem.HEIGHT, TreeViewItem.HEIGHT);

                        if (openRegion.Contains(Event.current.mousePosition))
                        {
                            item.Open = !item.Open;
                            Event.current.Use();

                            repaint = true;
                        }
                    }

                    if (Event.current.type != EventType.Used && item.Enabled && item.Selectable)
                    {
                        Rect mouseRegion = new Rect(itemRegion.x, itemRegion.y, itemRegion.width, TreeViewItem.HEIGHT);

                        if (mouseRegion.Contains(Event.current.mousePosition))
                        {
                            Select(index, item.Tag);
                            Event.current.Use();
                        }

                        if (selection.Contains(item.Tag))
                            Dragged = true;
                    }
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                dragging = false;
                Dragged = false;
            }
            else if (Event.current.type == EventType.MouseDrag && Dragged)
            {
                Rect mouseRegion = new Rect(itemRegion.x, itemRegion.y, itemRegion.width, TreeViewItem.HEIGHT);

                if (mouseRegion.Contains(Event.current.mousePosition))
                {
                    DragAndDrop.PrepareStartDrag();

                    List<UnityEngine.Object> objs = new List<UnityEngine.Object>();
                    foreach (object o in selection)
                        if (o is UnityEngine.Object)
                            objs.Add(o as UnityEngine.Object);

                    DragDropWrapper wrapper = new DragDropWrapper("TreeView", selection);
                    DragAndDrop.objectReferences = objs.ToArray();
                    DragAndDrop.SetGenericData("TreeView", wrapper);
                    DragAndDrop.StartDrag("TreeView");

                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;

                    dragging = true;
                    Dragged = false;
                    mayEdit = false;

                    if (item.Editing)
                        item.Editing = false;

                    Event.current.Use();
                }
            }
            else if (Event.current.type == EventType.DragUpdated && CanDrop)
            {
                Rect mouseRegion = new Rect(itemRegion.x, itemRegion.y + 1, itemRegion.width, TreeViewItem.HEIGHT - 2);

                if (mouseRegion.Contains(Event.current.mousePosition) && !selection.Contains(item.Tag))
                {
                    item.Highlighted = true;
                    item.InBetween = false;
                }
                else
                {
                    item.Highlighted = false;

                    if (canDropInBetween)
                    {
                        Rect inBetweenRect = new Rect(itemRegion.x, itemRegion.y - 1, itemRegion.width, 2);

                        if (inBetweenRect.Contains(Event.current.mousePosition))
                        {
                            item.InBetween = true;
                        }
                        else
                        {
                            item.InBetween = false;
                        }
                    }
                }

                repaint = true;
            }
            else if (Event.current.type == EventType.DragPerform && CanDrop)
            {
                dragging = false;

                if (item.Highlighted || item.InBetween)
                {
                    target = item.Tag;
                    inBetween = item.InBetween;

                    Event.current.Use();
                }
            }
            else
            {
                Rect mouseRegion = new Rect(itemRegion.x, itemRegion.y, itemRegion.width, TreeViewItem.HEIGHT);

                if (mouseRegion.Contains(Event.current.mousePosition))
                    mouseHover = index;
            }

            if (mayEdit && index == mouseHover)
            {
                mayEdit = false;
                Dragged = false;
                item.Editing = true;
            }

            item.Draw(itemRegion, depth, selected);

            index++;
            if (item.Open)
                foreach (TreeViewItem child in item.VisibleItems)
                    index = DrawItem(area, child, index, depth + 1);

            return index;
        }

        public void Sort()
        {
            TreeViewItem[] arrayItems = items.ToArray();
            Array.Sort<TreeViewItem>(arrayItems);
            items = arrayItems.ToList();

            for (int i = 0; i < items.Count; i++)
                items[i].Sort();
        }

        public void Edit()
        {
            mayEdit = true;
        }

        /// <summary>
        /// Refresh the list of visible item when the items list has changed.
        /// </summary>
        public void BuildVisibility()
        {
            scroll = 0;
            BuildVisibility(items, visibleItems);

            if (alwaysSelect && selection.Count == 0 && visibleItems.Count != 0)
            {
                selection.Add(visibleItems[0].Tag);

                if (SelectionChanged != null)
                    SelectionChanged();
            }
        }

        /// <summary>
        /// Refresh the list of visible item when the items list has changed.
        /// </summary>
        public bool BuildVisibility(List<TreeViewItem> original, List<TreeViewItem> filtered)
        {
            filtered.Clear();

            bool visible = false;

            if (original.Count > 0)
            {
                foreach (TreeViewItem item in original)
                {
                    List<TreeViewItem> sub = item.VisibleItems;
                    if (BuildVisibility(item.Items, sub) || item.AlwaysVisible || visibility == null || (visibility != null && visibility(item.Tag, item.Name)))
                    {
                        filtered.Add(item);

                        if (enabled == null || enabled(item.Tag))
                            item.Enabled = true;
                        else
                            item.Enabled = false;

                        visible = true;
                    }
                    else if (!hiddenCanBeSelected && selection.Contains(item.Tag))
                        selection.Remove(item.Tag);
                }

                return visible;
            }
            else
                return false;
        }

        public int GetIndex(object obj)
        {
            int index = 0;
            if (GetIndex(visibleItems, ref index, obj))
                return index;
            else
                return INVALID_INDEX;
        }

        private bool GetIndex(List<TreeViewItem> list, ref int index, object obj)
        {
            foreach (TreeViewItem item in list)
            {
                if (item.Tag == obj)
                    return true;

                index++;

                if (item.Open && item.VisibleItems.Count != 0)
                    if (GetIndex(item.VisibleItems, ref index, obj))
                        return true;
            }

            return false;
        }

        public void AddObject(object obj)
        {
            if (GetIndex(obj) != INVALID_INDEX)
                return;

            items.Add(new TreeViewItem(this, null, obj));
        }

        public void AddRange(object[] objs)
        {
            for (int i = 0; i < objs.Length; i++)
                AddObject(objs[i]);

            BuildVisibility();
        }

        public void ClearSelection()
        {
            selection.Clear();
        }

        public void SelectIndex(int index)
        {
            SelectIndex(visibleItems, index, 0);
        }

        private int SelectIndex(List<TreeViewItem> list, int index, int count)
        {
            foreach (TreeViewItem item in list)
            {
                if (count > index)
                    break;

                if (count == index)
                {
                    if (item.Selectable)
                        Select(index, item.Tag);

                    return -1;
                }

                count++;

                if (item.Count > 1)
                {
                    count = SelectIndex(item.VisibleItems, index, count);
                    if (count == -1)
                        return count;
                }
            }

            return count;
        }

        private void Select(int index, object obj)
        {
            bool changed = false;

            if (Event.current.type == EventType.MouseDown)
            {
                if (!selection.Contains(obj))
                {
                    if (!CanMultiSelect || (!Event.current.control && !Event.current.shift))
                        selection.Clear();

                    selection.Add(obj);

                    if (Event.current.shift)
                    {
                        if (index > lastSelection)
                            for (int i = lastSelection + 1; i < index; i++)
                                SelectIndex(i);

                        else if (index < lastSelection)
                            for (int i = index + 1; i < lastSelection; i++)
                                SelectIndex(i);
                    }

                    lastSelection = index;
                    changed = true;

                    UpdateScrollbar();
                }
                else
                {
                    if ((!AlwaysSelect || selection.Count > 1) && Event.current.control)
                    {
                        selection.Remove(obj);
                        changed = true;
                    }
                    else if (selection.Count == 1 && CanEdit)
                        timer = EditorApplication.timeSinceStartup + FIELD_EDITION_DELAY;
                }
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                if (selection.Count > 1)
                {
                    selection.Clear();
                    selection.Add(obj);
                    changed = true;
                }
            }
            else
            {
                if (!selection.Contains(obj))
                {
                    selection.Clear();
                    selection.Add(obj);
                    changed = true;
                }
            }

            if (changed)
            {
                FocusSelection();

                if (SelectionChanged != null)
                    SelectionChanged();
            }

            repaint = true;
        }

        private void FocusSelection()
        {
            if (selection.Count != 1)
                return;

            TreeViewItem item = FindItem(Items, selection[0]);

            if (item != null)
                OpenParent(item);

            int index = GetIndex(selection[0]);

            float visibleCount = height / TreeViewItem.HEIGHT;

            int listCount = 0;
            foreach (TreeViewItem visible in visibleItems)
                listCount += visible.Count;

            if (visibleCount < listCount)
            {
                if (index < scroll)
                    scroll = index;
                else if (index > scroll + visibleCount - 1)
                    scroll = index - visibleCount + 1;
            }
        }

        public void RenameItem(object tag, string name)
        {
            TreeViewItem item = FindItem(items, tag);

            if (item == null)
                return;

            item.Name = name;
        }

        public TreeViewItem FindItem(object o)
        {
            return FindItem(items, o);
        }

        private TreeViewItem FindItem(List<TreeViewItem> list, object o)
        {
            TreeViewItem result = null;
            foreach (TreeViewItem item in list)
            {
                if (o == item.Tag)
                    return item;
                else
                {
                    result = FindItem(item.Items, o);

                    if (result != null)
                        break;
                }
            }

            return result;
        }

        private void OpenParent(TreeViewItem item)
        {
            if (item.Parent != null)
            {
                item.Parent.Open = true;
                OpenParent(item.Parent);
            }
        }

        private void UpdateScrollbar()
        {
            if (selection.Count != 1)
                return;

            float visibleCount = height / TreeViewItem.HEIGHT;

            int index = GetIndex(selection[0]);

            if (scroll + visibleCount <= index)
                scroll = index - visibleCount + 1;
            else if (index <= scroll)
                scroll = index;
        }

        public void OnNameChanged(string name, object tag)
        {
            if (NameChanged != null)
                NameChanged(this, new NameEventArgs(name, tag));
        }

        public void Redraw()
        {
            Repaint();
        }

        public void Clear()
        {
            items.Clear();
            visibleItems.Clear();
            selection.Clear();
        }
    }

    internal class NameEventArgs
    {
        private string name;

        public string Name
        {
            get { return name; }
        }

        private object tag;

        public object Tag
        {
            get { return tag; }
        }

        public NameEventArgs(string name, object tag)
        {
            this.name = name;
            this.tag = tag;
        }
    }
}