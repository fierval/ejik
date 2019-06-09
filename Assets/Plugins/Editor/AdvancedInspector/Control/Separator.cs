using UnityEngine;
using UnityEditor;
using System;

namespace AdvancedInspector
{
    /// <summary>
    /// A separator holds two control side by side and divide them with a draggable line.
    /// It can be horizontal or vertical.
    /// </summary>
    [Serializable]
    internal class Separator : IControl
    {
        #region Constant
        public const float MIN = 0.1f;
        public const float MAX = 0.9f;
        public const float WIDTH = 6;

        private static Color HOVERED = new Color(0.1f, 0.5f, 0.6f);
        #endregion

        #region Texture
        private static Texture separatorVerticalEmpty;
        private static Texture separatorVerticalGradient;
        private static Texture separatorVerticalFull;

        public static Texture SeparatorVertical
        {
            get
            {
                if (InspectorPreferences.Separator == SeparatorStyle.Empty)
                {
                    if (separatorVerticalEmpty == null)
                        separatorVerticalEmpty = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "SeparatorVertical_Empty.png");

                    return separatorVerticalEmpty;
                }
                else if (InspectorPreferences.Separator == SeparatorStyle.Gradient)
                {
                    if (separatorVerticalGradient == null)
                        separatorVerticalGradient = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "SeparatorVertical.png");

                    return separatorVerticalGradient;
                }
                else if (InspectorPreferences.Separator == SeparatorStyle.Full)
                {
                    if (separatorVerticalFull == null)
                        separatorVerticalFull = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "SeparatorVertical_Full.png");

                    return separatorVerticalFull;
                }

                return null;
            }
        }

        private static Texture separatorHorizontal;

        public static Texture SeparatorHorizontal
        {
            get
            {
                if (separatorHorizontal == null)
                    separatorHorizontal = AssetDatabase.LoadAssetAtPath<Texture>(AdvancedInspectorControl.DataPath + "SeparatorHorizontal.png");

                return separatorHorizontal;
            }
        }
        #endregion

        private IControl leftControl = null;

        public IControl LeftControl
        {
            get { return leftControl; }
            set { leftControl = value; }
        }

        private IControl rightControl = null;

        public IControl RightControl
        {
            get { return rightControl; }
            set { rightControl = value; }
        }

        [SerializeField]
        private bool perPixel = false;

        public bool PerPixel
        {
            get { return perPixel; }
            set { perPixel = value; }
        }

        [SerializeField]
        private float division = 0.5f;

        public float Division
        {
            get { return division; }
            set
            {
                if (perPixel)
                    division = Mathf.Clamp(value, 20, size - 20);
                else
                    division = Mathf.Clamp(value, MIN, MAX);
            }
        }

        private float size = 1000;

        public enum SeparatorOrientation
        {
            Vertical,
            Horizontal
        }

        [SerializeField]
        private SeparatorOrientation orientation = SeparatorOrientation.Vertical;

        public SeparatorOrientation Orientation
        {
            get { return orientation; }
            set { orientation = value; }
        }

        [SerializeField]
        private bool draggable = true;

        public bool Draggable
        {
            get { return draggable; }
            set { draggable = value; }
        }

        private bool drag = false;
        private Vector2 mousePosition;

        public event GenericEventHandler RequestRepaint;

        public Separator(IControl left, IControl right)
            : this(left, right, 0.5f, false, true)
        { }

        public Separator(IControl left, IControl right, float division)
            : this(left, right, division, false, true)
        { }

        public Separator(IControl left, IControl right, float division, bool perPixel)
            : this(left, right, division, perPixel, true)
        { }

        public Separator(IControl left, IControl right, float division, bool perPixel, bool draggable)
        {
            leftControl = left;
            rightControl = right;
            this.division = division;
            this.perPixel = perPixel;
            this.draggable = draggable;
        }

        /// <summary>
        /// Draw the whole region holding the two control and the separator.
        /// The separator pass down the proper region to Draw of the sub-controls.
        /// Returns true if the window needs to be repainted.
        /// </summary>
        public bool Draw(Rect region)
        {
            float half = (WIDTH * 0.5f);

            float position;
            Rect separator, left, right, draw;
            Texture texture;

            if (orientation == SeparatorOrientation.Vertical)
            {
                size = region.width;

                if (perPixel)
                    position = division + region.x;
                else
                    position = region.width * division + region.x;

                separator = new Rect(position - half - 2, region.y, WIDTH, region.height);

                if (draggable)
                    EditorGUIUtility.AddCursorRect(separator, MouseCursor.ResizeHorizontal);

                draw = new Rect(position - half, region.y, WIDTH - 4, region.height);
                if (PerPixel)
                {
                    left = new Rect(region.x, region.y, division - half, region.height);
                    right = new Rect(position + half, region.y, region.width - (division + half), region.height);
                }
                else
                {
                    left = new Rect(region.x, region.y, region.width * division - half, region.height);
                    right = new Rect(position + half, region.y, region.width - (region.width * division + half), region.height);
                }
                texture = SeparatorVertical;
            }
            else
            {
                size = region.height;

                if (perPixel)
                    position = division + region.y;
                else
                    position = region.height * division + region.x;

                separator = new Rect(region.x, position - half - 2, region.width, WIDTH);

                if (draggable)
                    EditorGUIUtility.AddCursorRect(separator, MouseCursor.ResizeVertical);

                draw = new Rect(region.x, position - half, region.width, WIDTH - 4);
                if (PerPixel)
                {
                    left = new Rect(region.x, region.y, region.width, division - half);
                    right = new Rect(region.x, position + half, region.width, region.height - (division + half));
                }
                else
                {
                    left = new Rect(region.x, region.y, region.width, region.height * division - half);
                    right = new Rect(region.x, position + half, region.width, region.height - (region.height * division + half));
                }
                texture = SeparatorHorizontal;
            }

            bool selected = false;

            if (draggable)
            {
                if (drag)
                    GUI.color = InspectorPreferences.SeparatorSelectedColor;
                else if (Event.current != null && separator.Contains(Event.current.mousePosition))
                {
                    GUI.color = HOVERED;
                    selected = true;
                }
                else
                    GUI.color = InspectorPreferences.SeparatorDefaultColor;
            }
            else
                GUI.color = InspectorPreferences.SeparatorDefaultColor;

            GUI.DrawTexture(draw, texture);

            GUI.color = Color.white;

            bool repaintLeft = false;
            bool repaintRight = false;

            if (leftControl != null)
                repaintLeft = leftControl.Draw(left);

            if (rightControl != null)
                repaintRight = rightControl.Draw(right);

            if (Event.current == null)
                return (repaintLeft | repaintRight | selected);

            if (draggable)
            {
                if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    if (separator.Contains(Event.current.mousePosition))
                    {
                        drag = true;
                        mousePosition = Event.current.mousePosition;
                        return true;
                    }
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    drag = false;
                    return true;
                }
                else if (Event.current.type == EventType.MouseDrag && drag)
                {
                    Vector2 delta = Event.current.mousePosition - mousePosition;

                    if (perPixel)
                    {
                        if (orientation == SeparatorOrientation.Vertical)
                            Division += delta.x;
                        else
                            Division += delta.y;
                    }
                    else
                    {
                        if (orientation == SeparatorOrientation.Vertical)
                            Division += (delta.x / region.width);
                        else
                            Division += (delta.y / region.height);
                    }

                    mousePosition = Event.current.mousePosition;

                    return true;
                }
            }

            return (repaintLeft | repaintRight | selected);
        }

        private void Repaint()
        {
            if (RequestRepaint != null)
                RequestRepaint();
        }
    }
}