using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;

namespace AdvancedInspector
{
    /// <summary>
    /// Define a popup window that return a result.
    /// Base class for IModal implementation.
    /// </summary>
    public abstract class ModalWindow : EditorWindow
    {
        /// <summary>
        /// Top title bar height
        /// </summary>
        public const float TITLEBAR = 18;

        private bool dragged = false;
        private Vector2 dragStart;

        private static Dictionary<Type, List<ModalWindow>> instances = new Dictionary<Type, List<ModalWindow>>();

        /// <summary>
        /// The object that invoked this modal.
        /// </summary>
        protected IModal owner;

        /// <summary>
        /// Internal modal result.
        /// </summary>
        protected WindowResult result = WindowResult.None;

        /// <summary>
        /// Result of this modal window.
        /// </summary>
        public WindowResult Result
        {
            get { return result; }
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ModalWindow()
        {
            Type type = GetType();

            List<ModalWindow> list = new List<ModalWindow>();
            if (instances.TryGetValue(type, out list))
                list.Add(this);
            else
                instances.Add(type, new List<ModalWindow>() { this });
        }

        /// <summary>
        /// Invoked by Unity when the user click outside the window.
        /// </summary>
        protected virtual void OnLostFocus()
        {
            result = WindowResult.LostFocus;

            if (owner != null)
                owner.ModalClosed(this);

            Remove();
        }

        /// <summary>
        /// Called when pressing Escape or Cancel.
        /// </summary>
        protected virtual void Cancel()
        {
            result = WindowResult.Cancel;
            Event.current.Use();

            if (owner != null)
                owner.ModalClosed(this);

            Close();
            Remove();
        }

        /// <summary>
        /// Called when pressing Enter or Ok.
        /// </summary>
        protected virtual void Ok()
        {
            result = WindowResult.Ok;
            Event.current.Use();

            if (owner != null)
                owner.ModalClosed(this);

            Close();
            Remove();
        }

        private void Remove()
        {
            Type type = GetType();

            List<ModalWindow> list = new List<ModalWindow>();
            if (instances.TryGetValue(type, out list))
                list.Remove(this);
        }

        /// <summary>
        /// Does this specific modal type exist?
        /// </summary>
        public static bool Exist<T>()
        {
            Type type = typeof(T);

            List<ModalWindow> list;
            if (instances.TryGetValue(type, out list))
                if (list.Count > 0)
                    return true;

            return false;
        }

        /// <summary>
        /// Usually you should not override this.
        /// </summary>
        protected virtual void OnGUI()
        {
            GUILayout.BeginArea(new Rect(0, 0, position.width, position.height));
            GUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label(titleContent.text);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();

            Rect titleBar = new Rect(0, 0, position.width, TITLEBAR);
            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 && titleBar.Contains(Event.current.mousePosition))
            {
                dragged = true;
                dragStart = new Vector2(position.x, position.y) - GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDrag && dragged)
            {
                Vector2 delta = GUIUtility.GUIToScreenPoint(Event.current.mousePosition) + dragStart;
                position = new Rect(delta.x, delta.y, position.width, position.height);
                Repaint();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                dragged = false;
            }

            Rect content = new Rect(0, TITLEBAR, position.width, position.height - TITLEBAR);
            Draw(content);
        }

        /// <summary>
        /// Implement your draw items here.
        /// </summary>
        protected abstract void Draw(Rect region);
    }
}