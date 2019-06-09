using UnityEngine;

namespace AdvancedInspector
{
    /// <summary>
    /// Define a control that can be drawn in an EditorWindow.
    /// </summary>
    public interface IControl
    {
        /// <summary>
        /// Draw the control. Return if the parent EditorWindow should repaint the window.
        /// </summary>
        bool Draw(Rect region);

        /// <summary>
        /// In the event an internal process of the Control want a repaint of the window outside the Drawing scope.
        /// </summary>
        event GenericEventHandler RequestRepaint;
    }
}