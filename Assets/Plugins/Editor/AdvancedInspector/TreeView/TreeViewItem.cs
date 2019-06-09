using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using UnityEngine;
using UnityEditor;

namespace AdvancedInspector
{
    internal class TreeViewItem : INotifyPropertyChanged, IComparable<TreeViewItem>
    {
        public const float HEIGHT = 16;
        public const float BOARDER = 2;

        public static Color SELECTED = new Color(0.2f, 0.6f, 1);
        public static Color HIGHLIGHTED = new Color(0.1f, 0.3f, 0.5f);

        private TreeView treeView;

        private TreeViewItem parent;

        public TreeViewItem Parent
        {
            get { return parent; }
            set { parent = value; }
        }

        protected object tag;

        public object Tag
        {
            get { return tag; }
        }

        protected string name;

        private string cachedName;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        protected string comment;

        public string Description
        {
            get { return comment; }
        }

        private bool highlighted = false;

        public bool Highlighted
        {
            get { return highlighted; }
            set { highlighted = value; }
        }

        private bool inBetween = false;

        public bool InBetween
        {
            get { return inBetween; }
            set { inBetween = value; }
        }

        private bool mayContainChildren = true;

        public bool MayContainChildren
        {
            get { return mayContainChildren; }
            set { mayContainChildren = value; }
        }

        private bool mayHaveIcon = true;

        public bool MayHaveIcon
        {
            get { return mayHaveIcon; }
            set { mayHaveIcon = value; }
        }

        private bool selectable = true;

        public bool Selectable
        {
            get { return selectable; }
            set { selectable = value; }
        }

        protected Texture icon;

        public Texture Icon
        {
            get { return icon; }
        }

        protected Color background = new Color(1, 1, 1, 0);

        public Color Background
        {
            get { return background; }
            set { background = value; }
        }

        protected Color color = Color.white;

        public Color Color
        {
            get { return color; }
            set { color = value; }
        }

        protected List<TreeViewItem> items;

        public List<TreeViewItem> Items
        {
            get { return items; }
        }

        protected List<TreeViewItem> visibleItems;

        public List<TreeViewItem> VisibleItems
        {
            get { return visibleItems; }
        }

        public bool Selected
        {
            get
            {
                if (treeView == null)
                    return false;

                return treeView.Selection.Contains(tag);
            }
        }

        public int Count
        {
            get
            {
                if (!Open)
                    return 1;

                int i = 1;

                foreach (TreeViewItem item in items)
                    i += item.Count;

                return i;
            }
        }

        private bool alwaysVisible = false;

        public bool AlwaysVisible
        {
            get { return alwaysVisible; }
            set { alwaysVisible = value; }
        }

        private bool open = false;

        public bool Open
        {
            get { return open; }
            set
            {
                if (open != value)
                {
                    open = value;

                    treeView.BuildVisibility(items, visibleItems);

                    if (open && Opened != null)
                        Opened(this);
                    else if (!open && Closed != null)
                        Closed(this);
                }
            }
        }

        private bool editing = false;

        public bool Editing
        {
            get { return editing; }
            set { editing = value; }
        }

        private bool enabled = true;

        public bool Enabled
        {
            get { return enabled; }
            set { enabled = value; }
        }

        #region Textures
        private static Texture folderOpen;

        public static Texture FolderOpen
        {
            get
            {
                if (folderOpen == null)
                    folderOpen = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "FolderOpen.png");

                return folderOpen;
            }
        }

        private static Texture folderClose;

        public static Texture FolderClose
        {
            get
            {
                if (folderClose == null)
                    folderClose = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "FolderClosed.png");

                return folderClose;
            }
        }
        #endregion

        public event PropertyChangedEventHandler PropertyChanged;

        public event SenderEventHandler Opened;
        public event SenderEventHandler Closed;

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, null);
        }

        public TreeViewItem(TreeView treeView, TreeViewItem parent, object tag)
            : this(treeView, parent, tag, tag.ToString(), "", null)
        { }

        public TreeViewItem(TreeView treeView, TreeViewItem parent, object tag, Description description)
            : this(treeView, parent, tag, description.Name, description.Comment, description.Icon)
        { }

        public TreeViewItem(TreeView treeView, TreeViewItem parent, object tag, string name, string comment, Texture icon)
        {
            this.treeView = treeView;
            this.parent = parent;
            this.tag = tag;
            this.name = name;
            this.cachedName = name;
            this.comment = comment;
            this.icon = icon;

            INotifyPropertyChanged notify = tag as INotifyPropertyChanged;
            if (notify != null)
                notify.PropertyChanged += OnPropertyChanged;

            items = new List<TreeViewItem>();
            visibleItems = new List<TreeViewItem>();
        }

        public int CompareTo(TreeViewItem other)
        {
            return string.Compare(Name, other.Name);
        }

        public virtual void DrawBackground(Rect region, int depth, bool selected)
        {
            if (!Editing)
            {
                if (highlighted)
                    Helper.DrawColor(new Rect(region.x, region.y, region.width, HEIGHT), HIGHLIGHTED);
                else if (selected)
                    Helper.DrawColor(new Rect(region.x, region.y, region.width, HEIGHT), SELECTED);
                else
                    Helper.DrawColor(new Rect(region.x, region.y, region.width, HEIGHT), Background);
            }
        }

        public virtual void Draw(Rect region, int depth, bool selected)
        {
            bool current = GUI.enabled;
            GUI.enabled = enabled;

            if (Editing)
            {
                if (!selected)
                {
                    cachedName = name;
                    Editing = false;
                    treeView.OnNameChanged(name, tag);
                }

                if (Event.current.type == EventType.KeyDown)
                {
                    if (Event.current.keyCode == KeyCode.Escape)
                    {
                        name = cachedName;
                        Editing = false;
                    }
                    else if (Event.current.keyCode == KeyCode.Return)
                    {
                        cachedName = name;
                        Editing = false;
                        treeView.OnNameChanged(name, tag);
                    }
                }

                if (!Editing)
                {
                    GUIUtility.keyboardControl = 0;
                    treeView.Redraw();
                }
            }

            float offset = (depth * HEIGHT / 2);

            if (items.Count != 0)
            {
                if (Open)
                    GUI.DrawTexture(new Rect(region.x + offset, region.y, HEIGHT, HEIGHT), FolderOpen);
                else
                    GUI.DrawTexture(new Rect(region.x + offset, region.y, HEIGHT, HEIGHT), FolderClose);
            }

            float textOffset = BOARDER;
            float textWidth = 0;

            if (mayContainChildren)
                textOffset = HEIGHT;
            else
                textWidth = HEIGHT;

            if (icon != null)
                GUI.DrawTexture(new Rect(textOffset + region.x + offset, region.y, HEIGHT, HEIGHT), icon);

            GUI.skin.label.alignment = TextAnchor.MiddleLeft;

            if (selected && !Editing)
                GUI.color = Color.black;
            else
            {
                float average = ((Background.r * 2 + Background.g * 3 + Background.b) / 3) * Background.a;

                if (average > 0.5f)
                    GUI.color = Color.black;
                else
                    GUI.color = color;
            }

            if (mayHaveIcon)
                textOffset += HEIGHT;
            else
                textWidth += HEIGHT;

            if (!Editing)
                EditorGUI.LabelField(new Rect(textOffset + region.x + offset, region.y, region.width - HEIGHT * 2 - offset + textWidth, HEIGHT), name);
            else
                name = EditorGUI.TextField(new Rect(textOffset + region.x + offset, region.y, region.width - HEIGHT * 2 - offset + textWidth, HEIGHT), name);

            if (inBetween)
                Helper.DrawColor(new Rect(region.x, region.y - 1, region.width, 2), HIGHLIGHTED);

            GUI.color = Color.white;

            GUI.enabled = current;
        }

        public void Sort()
        {
            TreeViewItem[] arrayItems = items.ToArray();
            Array.Sort<TreeViewItem>(arrayItems);
            items = arrayItems.ToList();

            for (int i = 0; i < items.Count; i++)
                items[i].Sort();
        }

        public TreeViewItem Duplicate(TreeView newParent)
        {
            TreeViewItem duplicata = new TreeViewItem(newParent, null, tag, Name, Description, Icon);
            duplicata.Color = Color;
            duplicata.Background = Background;
            duplicata.MayHaveIcon = MayHaveIcon;
            duplicata.MayContainChildren = MayContainChildren;
            duplicata.Open = Open;

            foreach (TreeViewItem item in Items)
            {
                TreeViewItem sub = item.Duplicate(newParent);
                sub.Parent = duplicata;
                duplicata.Items.Add(sub);
            }

            return duplicata;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}